# 🦖 Web_Dinosaurios

Catálogo web de dinosaurios (lista con descripción e imagen) construido con **ASP.NET Core MVC (.NET 8)**, **Dapper** y **SQLite**.

El objetivo real del proyecto es **aprender a orquestar varias IAs** con un stack conocido:

| Rol | Agente | Qué hace |
|---|---|---|
| Orquestador y diseñador | Claude Code | Divide objetivos en tareas (`planear`), aprueba o rechaza cada `git diff` y hace las vistas y el CSS (`disenar`) según `docs/guia-ui.md` |
| Desarrollador de funcionalidad | DeepSeek | Escribe modelos, repositorios, controladores y SQL en las tareas `implementar` |
| Especialista en documentación | Agy (Antigravity) | Procesa los handoffs y actualiza las notas del proyecto en el Second Brain |

Las tareas de diseño (`disenar`) detienen el orquestador: necesitan ver la app a varios anchos, así que Claude Code las hace en una sesión interactiva con la skill `frontend-design` (`.claude/skills/`).

## Estructura inicial

```
Web_Dinosaurios/
├── orquestar.py               # ciclo planear → implementar → revisar → documentar
├── tareas.md                  # cola de tareas (no se versiona)
├── .env.example               # configuración del orquestador (copiar a .env)
├── docs/dinosaurios-semilla.md  # contrato de datos: esquema y 8 registros semilla
├── docs/guia-ui.md            # contrato visual: tokens, tipografía y layout
├── wwwroot/images/dinosaurios/  # imágenes de Wikimedia Commons (ver créditos en docs/)
└── (plantilla dotnet new mvc)
```

## Uso

```powershell
pip install openai python-dotenv
copy .env.example .env          # y completar DEEPSEEK_API_KEY
python orquestar.py --dry-run   # ver qué haría
python orquestar.py --una       # procesar una sola tarea
dotnet run                      # http://localhost:5288
```

Documentación del proyecto: `D:/Daniel/Obsidian/Data/00_Human/02_Second_Brain/Software/05_Projects/18_Web_Dinosaurios/Indice.md`

## Créditos de las imágenes

Las imágenes provienen de Wikimedia Commons bajo licencias CC BY y CC BY-SA. El autor y la licencia de cada una están en `docs/dinosaurios-semilla.md` (campo `CreditoImagen`).
