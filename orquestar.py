#!/usr/bin/env python3
"""
orquestar.py — Orquestador mínimo: Claude (arquitecto/revisor) + DeepSeek (implementador) + Antigravity (documentador)

Flujo:
  (planear)     -> Claude lee la nota del proyecto y agrega tareas pequeñas a tareas.md
  (implementar) -> DeepSeek escribe el código -> Claude revisa el git diff
                   -> si RECHAZA, DeepSeek corrige (máx. MAX_REINTENTOS)
                   -> si APRUEBA, commit + handoff para Antigravity
  (documentar)  -> genera un handoff .md en tu bóveda para que Antigravity actualice la nota

Formato de tareas.md:
  - [ ] (planear) Endpoint de tableros con JSON por dispositivo
  - [ ] (implementar) Crear modelo Tablero con device, number, green_led, red_led [archivos: Models/Tablero.cs]
  - [ ] (documentar) Describir el endpoint GET /api/tableros

Requisitos:
  pip install openai python-dotenv
  Claude Code instalado y con sesión iniciada (comando `claude`)
  El proyecto debe ser un repositorio git

Variables (.env junto a este script):
  DEEPSEEK_API_KEY=sk-...
  DEEPSEEK_MODEL=deepseek-flash   (deepseek-chat se retiró el 24/07/2026)
  PROYECTO_DIR=C:/dev/tableros-api
  VAULT_DIR=C:/Obsidian/MiBoveda
  NOTA_PROYECTO=C:/Obsidian/MiBoveda/Proyectos/Tableros API.md
  TAREAS=tareas.md
  MAX_REINTENTOS=2
  VERIFICAR_CMD=npm run lint     (opcional: se ejecuta antes de la revisión de Claude; si falla, DeepSeek corrige)
  AGY_CMD=                       (opcional: comando que se ejecuta con la ruta del handoff al final)
  DEEPSEEK_PRECIO_IN=            (opcional: USD por millón de tokens de entrada)
  DEEPSEEK_PRECIO_OUT=           (opcional: USD por millón de tokens de salida)

Uso:
  python orquestar.py            # procesa todas las tareas pendientes
  python orquestar.py --una      # procesa solo la siguiente tarea
  python orquestar.py --dry-run  # muestra qué haría sin llamar a ningún modelo
"""

import argparse
import csv
import json
import os
import re
import shlex
import shutil
import subprocess
import sys
from datetime import datetime
from pathlib import Path

# En Windows la consola/tubería puede ser cp1252 y los símbolos (→ ✔ ✘) romperían el log
for _flujo in (sys.stdout, sys.stderr):
    if hasattr(_flujo, "reconfigure"):
        _flujo.reconfigure(encoding="utf-8", errors="replace")

try:
    from dotenv import load_dotenv
    load_dotenv(Path(__file__).with_name(".env"))
except ImportError:
    pass

# ---------------------------------------------------------------- configuración
PROYECTO = Path(os.getenv("PROYECTO_DIR", ".")).resolve()
VAULT = Path(os.getenv("VAULT_DIR", ".")).resolve()
NOTA = Path(os.getenv("NOTA_PROYECTO", "")) if os.getenv("NOTA_PROYECTO") else None
TAREAS = Path(os.getenv("TAREAS", "tareas.md"))
MAX_REINTENTOS = int(os.getenv("MAX_REINTENTOS", "2"))
DEEPSEEK_MODEL = os.getenv("DEEPSEEK_MODEL", "deepseek-flash")
AGY_CMD = os.getenv("AGY_CMD", "").strip()
VERIFICAR_CMD = os.getenv("VERIFICAR_CMD", "").strip()
COSTOS = Path(__file__).with_name("costos.csv")
MAX_CHARS_ARCHIVO = 60_000      # para no mandar archivos enormes como contexto (App.tsx pesa ~30 KB:
                                # si se recorta, DeepSeek devolvería el archivo "completo" truncado)
MAX_CHARS_DIFF = 30_000

PATRON_TAREA = re.compile(r"^- \[ \] \((\w+)\)\s*(.+?)(?:\s*\[archivos:\s*(.+?)\])?\s*$")
# Un bloque termina en === FIN ===, en el siguiente === ARCHIVO: o al final: si DeepSeek omite un FIN,
# los archivos siguientes no deben quedar pegados dentro del anterior.
PATRON_ARCHIVO = re.compile(r"^=== ARCHIVO: (.+?) ===\n(.*?)(?=^=== FIN ===|^=== ARCHIVO: |\Z)", re.S | re.M)


# ---------------------------------------------------------------- utilidades
def log(msg):
    print(f"[{datetime.now():%H:%M:%S}] {msg}", flush=True)


def registrar_costo(modelo, tarea, tok_in, tok_out, usd=None):
    nuevo = not COSTOS.exists()
    with COSTOS.open("a", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        if nuevo:
            w.writerow(["fecha", "modelo", "tarea", "tokens_in", "tokens_out", "usd"])
        w.writerow([datetime.now().isoformat(timespec="seconds"), modelo, tarea[:80],
                    tok_in, tok_out, "" if usd is None else f"{usd:.5f}"])


def leer(path: Path, limite=MAX_CHARS_ARCHIVO):
    try:
        texto = path.read_text(encoding="utf-8")
    except (FileNotFoundError, UnicodeDecodeError):
        return ""
    return texto if len(texto) <= limite else texto[:limite] + "\n...[recortado]"


def git(*args, check=True):
    r = subprocess.run(["git", *args], cwd=PROYECTO, capture_output=True, text=True, encoding="utf-8")
    if check and r.returncode != 0:
        raise RuntimeError(f"git {' '.join(args)} falló: {r.stderr.strip()}")
    return r.stdout


def listar_archivos():
    """Lista de archivos versionados o nuevos (no ignorados): contexto barato del proyecto."""
    salida = git("ls-files", "--cached", "--others", "--exclude-standard", check=False)
    return "\n".join(salida.splitlines()[:300])


def diff_actual():
    git("add", "-N", ".", check=False)   # incluye archivos nuevos en el diff
    d = git("diff", check=False)
    return d if len(d) <= MAX_CHARS_DIFF else d[:MAX_CHARS_DIFF] + "\n...[diff recortado]"


def verificar():
    """Ejecuta VERIFICAR_CMD (p. ej. el type-check). Devuelve None si pasa o la salida si falla."""
    if not VERIFICAR_CMD:
        return None
    r = subprocess.run(VERIFICAR_CMD, shell=True, cwd=PROYECTO, capture_output=True, text=True,
                       encoding="utf-8", errors="replace")
    return None if r.returncode == 0 else (r.stdout + r.stderr).strip()[-4000:]


# ---------------------------------------------------------------- modelos
def llamar_deepseek(sistema, usuario, tarea):
    from openai import OpenAI
    clave = os.getenv("DEEPSEEK_API_KEY")
    if not clave:
        sys.exit("Falta DEEPSEEK_API_KEY en .env")
    cliente = OpenAI(api_key=clave, base_url="https://api.deepseek.com")
    r = cliente.chat.completions.create(
        model=DEEPSEEK_MODEL,
        messages=[{"role": "system", "content": sistema}, {"role": "user", "content": usuario}],
        temperature=0.2,
    )
    u = r.usage
    usd = None
    p_in, p_out = os.getenv("DEEPSEEK_PRECIO_IN"), os.getenv("DEEPSEEK_PRECIO_OUT")
    if p_in and p_out:
        usd = u.prompt_tokens / 1e6 * float(p_in) + u.completion_tokens / 1e6 * float(p_out)
    registrar_costo(f"deepseek:{DEEPSEEK_MODEL}", tarea, u.prompt_tokens, u.completion_tokens, usd)
    return r.choices[0].message.content


def llamar_claude(prompt, tarea):
    exe = shutil.which("claude")
    if not exe:
        sys.exit("No encuentro el comando `claude`. Instala Claude Code e inicia sesión.")
    # Usa tu suscripción de Claude Code (sesión iniciada), no la API de pago:
    # si existiera ANTHROPIC_API_KEY en el entorno, Claude Code la usaría y cobraría por API.
    entorno = {k: v for k, v in os.environ.items() if k != "ANTHROPIC_API_KEY"}
    r = subprocess.run([exe, "-p", "--output-format", "json"], input=prompt, cwd=PROYECTO,
                       capture_output=True, text=True, encoding="utf-8", env=entorno)
    if r.returncode != 0:
        raise RuntimeError(f"Claude Code falló: {r.stderr.strip()[:500]}")
    try:
        datos = json.loads(r.stdout)
    except json.JSONDecodeError:
        registrar_costo("claude", tarea, "", "")
        return r.stdout
    uso = datos.get("usage", {}) or {}
    # Con suscripción no se cobra por llamada: el costo que reporta Claude Code es solo un
    # equivalente estimado. Se guarda como referencia de cuánto "pesa" cada tarea en tu plan.
    registrar_costo("claude (suscripción, usd estimado)", tarea, uso.get("input_tokens", ""),
                    uso.get("output_tokens", ""), datos.get("total_cost_usd"))
    return datos.get("result", "")


# ---------------------------------------------------------------- tareas
def leer_tareas():
    if not TAREAS.exists():
        sys.exit(f"No existe {TAREAS}. Crea uno con líneas '- [ ] (tipo) descripción'.")
    pendientes = []
    for i, linea in enumerate(TAREAS.read_text(encoding="utf-8").splitlines()):
        m = PATRON_TAREA.match(linea.strip())
        if m:
            tipo, desc, archivos = m.groups()
            lista = [a.strip() for a in archivos.split(",")] if archivos else []
            pendientes.append({"linea": i, "tipo": tipo.lower(), "desc": desc, "archivos": lista})
    return pendientes


def marcar(tarea, estado="x", nota=""):
    lineas = TAREAS.read_text(encoding="utf-8").splitlines()
    original = lineas[tarea["linea"]]
    lineas[tarea["linea"]] = original.replace("- [ ]", f"- [{estado}]", 1) + (f"  <!-- {nota} -->" if nota else "")
    TAREAS.write_text("\n".join(lineas) + "\n", encoding="utf-8")


def agregar_tareas(nuevas):
    texto = TAREAS.read_text(encoding="utf-8").rstrip("\n")
    TAREAS.write_text(texto + "\n" + "\n".join(nuevas) + "\n", encoding="utf-8")


# ---------------------------------------------------------------- roles
def planear(t):
    nota = leer(NOTA) if NOTA else ""
    prompt = f"""Eres el arquitecto del proyecto. NO edites archivos; solo responde texto.
Nota del proyecto:
{nota or '(sin nota)'}

Archivos actuales:
{listar_archivos()}

Objetivo: {t['desc']}

Divide el objetivo en 2 a 6 tareas pequeñas y concretas. Responde SOLO con líneas en este formato exacto:
- [ ] (implementar) descripción breve [archivos: ruta1, ruta2]
- [ ] (documentar) descripción breve
Usa rutas relativas al proyecto."""
    respuesta = llamar_claude(prompt, t["desc"])
    nuevas = [l.strip() for l in respuesta.splitlines() if PATRON_TAREA.match(l.strip())]
    if not nuevas:
        log("Claude no devolvió tareas válidas:\n" + respuesta[:800])
        marcar(t, "!", "plan sin tareas válidas")
        return
    agregar_tareas(nuevas)
    marcar(t)
    log(f"Plan agregado: {len(nuevas)} tareas nuevas")


def escribir_archivos(respuesta):
    escritos = []
    for m in PATRON_ARCHIVO.finditer(respuesta):
        ruta, contenido = m.group(1), m.group(2)
        destino = (PROYECTO / ruta.strip()).resolve()
        if PROYECTO not in destino.parents:      # evita escribir fuera del proyecto
            log(f"  Ruta ignorada (fuera del proyecto): {ruta}")
            continue
        if not respuesta.startswith("=== FIN ===", m.end()):
            log(f"  Aviso: {ruta.strip()} no cerró con === FIN ===")
        destino.parent.mkdir(parents=True, exist_ok=True)
        destino.write_text(contenido.rstrip("\n") + "\n", encoding="utf-8")
        escritos.append(ruta.strip())
    return escritos


SISTEMA_DEEPSEEK = """Eres un desarrollador senior. Implementas exactamente la tarea pedida, sin extras.
Devuelve cada archivo COMPLETO (no fragmentos) en este formato, y nada fuera de él salvo una línea final de resumen:
=== ARCHIVO: ruta/relativa/archivo.ext ===
contenido completo
=== FIN ==="""


def implementar(t):
    nota = leer(NOTA) if NOTA else ""
    contexto = "\n\n".join(f"--- {a} ---\n{leer(PROYECTO / a)}" for a in t["archivos"] if (PROYECTO / a).exists())
    nuevos = [a for a in t["archivos"] if not (PROYECTO / a).exists()]
    if nuevos:
        log(f"  Aún no existen (DeepSeek los creará): {', '.join(nuevos)}")
    rutas = (f"\n\nUsa EXACTAMENTE estas rutas, sin renombrarlas ni moverlas: {', '.join(t['archivos'])}."
             if t["archivos"] else "")
    base = f"""Nota del proyecto:
{nota or '(sin nota)'}

Archivos del proyecto:
{listar_archivos()}

Contenido de archivos relevantes:
{contexto or '(ninguno todavía)'}

Tarea: {t['desc']}{rutas}"""

    # Las guías de docs/ también van al revisor: lo que piden cuenta como cambio solicitado
    spec = "\n\n".join(f"--- {a} ---\n{leer(PROYECTO / a)}" for a in t["archivos"]
                       if a.startswith("docs/") and (PROYECTO / a).exists())

    comentarios = ""
    for intento in range(MAX_REINTENTOS + 1):
        log(f"  DeepSeek implementando (intento {intento + 1})...")
        respuesta = llamar_deepseek(SISTEMA_DEEPSEEK, base + comentarios, t["desc"])
        escritos = escribir_archivos(respuesta)
        if not escritos:
            log("  DeepSeek no devolvió archivos en el formato esperado.")
            comentarios = "\n\nTu respuesta anterior no usó el formato === ARCHIVO: ... === / === FIN ===. Repite usando ese formato."
            continue
        log(f"  Archivos escritos: {', '.join(escritos)}")

        errores = verificar()
        if errores and any(s in errores for s in ("MSB3027", "MSB3021", "being used by another process")):
            # No es un error del código: la app sigue abierta y bloquea los binarios
            log("  ✘ El build no puede escribir los binarios: cierra `dotnet run` y repite la tarea.")
            break
        if errores:
            log(f"  ✘ {VERIFICAR_CMD} falló (no se gasta revisión de Claude):\n{errores[-800:]}")
            comentarios = f"\n\nTu versión anterior no pasa `{VERIFICAR_CMD}`. Corrige estos errores:\n{errores}"
            continue

        diff = diff_actual()
        log("  Claude revisando el diff...")
        veredicto = llamar_claude(f"""Eres el revisor de código. NO edites archivos; solo responde texto.
Tarea solicitada: {t['desc']}
Rutas pedidas: {', '.join(t['archivos']) or '(no especificadas)'}
Rechaza si el diff crea o modifica archivos con rutas distintas a las pedidas, o si reescribe código
ya existente que la tarea no pide cambiar. Los cambios que exige la especificación cuentan como pedidos.

Especificación de referencia (solo lectura):
{spec or '(ninguna)'}

Diff:
{diff}

La primera línea de tu respuesta debe ser exactamente APROBADO o RECHAZADO.
Si rechazas, lista en viñetas solo los problemas reales (errores, requisitos incumplidos, riesgos). Sé breve.""",
                                  t["desc"])
        primera = veredicto.strip().splitlines()[0].upper() if veredicto.strip() else ""
        if primera.startswith("APROBADO"):
            git("add", "-A")
            git("commit", "-m", f"orquestador: {t['desc'][:60]}", check=False)
            marcar(t)
            crear_handoff(t, diff, escritos)
            log("  ✔ Aprobado y commiteado")
            return
        log("  ✘ Rechazado:\n" + veredicto[:800])
        comentarios = f"\n\nEl revisor rechazó tu versión anterior. Corrige esto:\n{veredicto}"

    marcar(t, "!", "rechazado tras reintentos; revisar a mano")
    log("  Se agotaron los reintentos. Los cambios quedan sin commit para que los revises.")


# ---------------------------------------------------------------- Antigravity
def crear_handoff(t, diff="", archivos=None):
    carpeta = VAULT / "_pendiente-documentar"
    carpeta.mkdir(parents=True, exist_ok=True)
    ruta = carpeta / f"{datetime.now():%Y%m%d-%H%M%S}.md"
    # Ruta completa: en la bóveda hay un Indice.md por proyecto y el nombre solo sería ambiguo
    nota_rel = NOTA.as_posix() if NOTA else "la nota del proyecto"
    ruta.write_text(f"""---
estado: pendiente
fecha: {datetime.now().isoformat(timespec='seconds')}
nota_destino: "{nota_rel}"
---
# Documentar cambio

**Tarea:** {t['desc']}
**Archivos:** {', '.join(archivos or t.get('archivos') or []) or '—'}

## Instrucciones para Antigravity
1. Abre «{nota_rel}» y actualízala con lo que cambió (endpoints, modelos, decisiones).
2. No copies el código completo; describe qué hace y cómo se usa.
3. Cambia `estado: pendiente` a `estado: hecho` en este archivo.

## Diff
```diff
{diff[:MAX_CHARS_DIFF] if diff else '(sin diff: tarea solo de documentación)'}
```
""", encoding="utf-8")
    log(f"  Handoff para Antigravity: {ruta}")
    if AGY_CMD:
        try:
            subprocess.run([*shlex.split(AGY_CMD), str(ruta)], check=False)
        except FileNotFoundError:
            log(f"  No se pudo ejecutar AGY_CMD: {AGY_CMD}")
    return ruta


def documentar(t):
    crear_handoff(t)
    marcar(t)


# ---------------------------------------------------------------- main
ROLES = {"planear": planear, "implementar": implementar, "documentar": documentar}


def main():
    ap = argparse.ArgumentParser(description="Orquestador Claude + DeepSeek + Antigravity")
    ap.add_argument("--una", action="store_true", help="procesar solo la siguiente tarea")
    ap.add_argument("--dry-run", action="store_true", help="mostrar tareas sin ejecutar")
    args = ap.parse_args()

    if not (PROYECTO / ".git").exists():
        sys.exit(f"{PROYECTO} no es un repositorio git. Ejecuta `git init` ahí primero.")

    while True:
        pendientes = leer_tareas()
        if not pendientes:
            log("No hay tareas pendientes.")
            break
        t = pendientes[0]
        log(f"→ ({t['tipo']}) {t['desc']}")
        if not args.dry_run and git("status", "--porcelain", check=False).strip():
            # Cambios sin commit (p. ej. de una tarea [!]) entrarían al diff de la siguiente tarea
            log("  Hay cambios sin commit en el proyecto. Revísalos y haz commit o descártalos antes de seguir.")
            break
        if args.dry_run:
            for p in pendientes:
                print(f"   ({p['tipo']}) {p['desc']}  archivos={p['archivos']}")
            break
        rol = ROLES.get(t["tipo"])
        if not rol:
            log(f"  Tipo desconocido '{t['tipo']}'. Usa: {', '.join(ROLES)}")
            marcar(t, "!", "tipo desconocido")
        else:
            rol(t)
        if args.una:
            break


if __name__ == "__main__":
    main()
