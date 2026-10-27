# 🦖 Web_Dinosaurios

Catálogo web de dinosaurios del Mesozoico construido con **ASP.NET Core MVC (.NET 8)**, **Dapper** y **SQLite**.

El objetivo real del proyecto es **aprender a orquestar varias IAs** con un stack conocido. El código de funcionalidad lo escribió DeepSeek, Claude Code lo revisó y diseñó la interfaz, y Agy documentó cada cambio en el Second Brain.

![Catálogo agrupado por periodo](docs/capturas/catalogo.png)

## Funcionalidades

- **1617 dinosaurios** importados de la [Paleobiology Database](https://paleobiodb.org/) (PBDB) con `dotnet run -- importar-pbdb`: edad, dieta y países de los fósiles de la PBDB, descripción de Wikipedia en español y nombres de países de Unicode CLDR. Contrato: `docs/pbdb-importacion.md`.
- **Catálogo como columna estratigráfica:** ordenado del más reciente al más antiguo y agrupado por época con los colores oficiales de la Carta Cronoestratigráfica Internacional (ICS), del Cretácico Superior al Triásico Medio, con encabezados de estrato fijos (`sticky`) al desplazarse y una **escala proporcional del Mesozoico** (`nav.dino-escala`) para filtrar por periodo con un clic.
- **Búsqueda, filtros y paginación:** por nombre (`LIKE`), por periodo (Triásico, Jurásico o Cretácico, comparado por prefijo), por dieta (Herbívoro, Carnívoro, Omnívoro o Piscívoro) y casilla «Solo con ilustración», 24 por página con `LIMIT/OFFSET`, en consultas parametrizadas con Dapper.
- **Sello compacto para el icono de huella:** los géneros sin foto muestran una huella fósil de tres dedos (`wwwroot/images/dinosaurios/icono.svg`) en formato compacto (`64 px` en escritorio, `48 px` en móvil) para destacar las especies con fotografía propia (`220 px` `4:3`).
- **Ficha de cada especie:** periodo, antigüedad, región, dieta, descripción, imagen con su crédito y enlace a Wikipedia o a la PBDB.
- **Base local:** `DatabaseInitializer` crea `App_Data/dinosaurios.db` con `Data/schema.sql` y la llena con `Data/seed.sql` la primera vez.
- **Diseño responsivo y accesible:** foco visible, `prefers-reduced-motion` y textos en español.
- **Variante de interfaz con Google AI Studio:** existe una segunda versión de las vistas Razor con estética editorial de museo en [`Web_Dinosaurios_Razor_AIStudio`](https://github.com/DanielFalconGuerra/Web_Dinosaurios_Razor_AIStudio) (`D:/Daniel/Desarrollo/Web_Dinosaurios_Razor_AIStudio`).

| Ficha | Móvil (375 px) |
|---|---|
| ![Ficha de Tyrannosaurus rex](docs/capturas/ficha.png) | ![Catálogo y ficha en móvil](docs/capturas/movil.png) |

## Roles de la orquestación

| Rol | Agente | Qué hace |
|---|---|---|
| Orquestador y diseñador | Claude Code | Divide objetivos en tareas (`planear`), aprueba o rechaza cada `git diff` y hace las vistas y el CSS (`disenar`) según `docs/guia-ui.md` |
| Desarrollador de funcionalidad | DeepSeek | Escribe modelos, repositorios, controladores y SQL en las tareas `implementar` |
| Especialista en documentación | Agy (Antigravity) | Procesa los handoffs y actualiza las notas del proyecto en el Second Brain |

Las tareas de diseño (`disenar`) detienen el orquestador: necesitan ver la app a varios anchos, así que Claude Code las hace en una sesión interactiva con la skill `frontend-design` (`.claude/skills/`).

### Ciclo de una tarea `implementar`

1. DeepSeek recibe la tarea y los archivos de `[archivos: ...]` y devuelve los archivos completos.
2. `dotnet build` verifica el resultado; si falla, DeepSeek corrige sin gastar una revisión de Claude.
3. Claude revisa el `git diff` junto con las guías de `docs/` que pida la tarea y responde APROBADO o RECHAZADO.
4. Si aprueba: commit `orquestador: ...`, la tarea pasa a `[x]` y se crea un handoff para Agy. Tras `MAX_REINTENTOS` rechazos queda en `[!]`.

El orquestador se detiene si hay cambios sin commit (para que una tarea fallida no contamine la siguiente) y si `dotnet run` bloquea los binarios.

## Estructura

```
Web_Dinosaurios/
├── orquestar.py                 # ciclo planear → implementar → revisar → documentar
├── tareas.md                    # cola de tareas (no se versiona)
├── .env.example                 # configuración del orquestador (copiar a .env)
├── .claude/skills/frontend-design/  # skill de diseño de Anthropic (Apache 2.0)
├── Controllers/DinosauriosController.cs  # Index (búsqueda) y Detalle
├── Models/Dinosaurio.cs
├── Repositories/                # IDinosaurioRepository y DinosaurioRepository (Dapper)
├── Data/                        # fábrica de conexiones, inicializador, schema.sql, seed.sql y paises-es.json
├── Services/                    # PbdbCliente, WikipediaCliente, MapeadorPbdb e ImportadorPbdb
├── Helpers/Presentacion.cs      # clases de periodo y época, icono y textos compartidos por las vistas
├── Views/Dinosaurios/           # Index.cshtml y Detalle.cshtml
├── wwwroot/css/site.css         # tokens y estilos dino-* de la guía de UI
├── wwwroot/images/dinosaurios/  # imágenes de Wikimedia Commons e icono.svg
└── docs/
    ├── dinosaurios-semilla.md   # contrato de datos: esquema y 8 registros semilla
    ├── guia-ui.md               # contrato visual: tokens, tipografía y layout
    ├── pbdb-importacion.md      # contrato de la importación desde la PBDB
    └── capturas/                # capturas del README
```

## Uso

Requisitos: SDK de .NET 8 o superior (el proyecto apunta a `net8.0`), Python 3, Claude Code con sesión iniciada y una clave de DeepSeek.

```powershell
dotnet run                      # http://localhost:5288
dotnet run -- importar-pbdb     # llena la base desde la PBDB y Wikipedia (~1 min; repetible)

# Orquestador
pip install openai python-dotenv
copy .env.example .env          # y completar DEEPSEEK_API_KEY
python orquestar.py --dry-run   # ver las tareas pendientes sin llamar a ningún modelo
python orquestar.py --una       # procesar solo la siguiente tarea
```

Formato de `tareas.md`:

```
- [ ] (planear) Objetivo que Claude divide en tareas pequeñas
- [ ] (implementar) Tarea de funcionalidad para DeepSeek [archivos: ruta1, ruta2]
- [ ] (disenar) Cambio de vistas o CSS para Claude Code
- [ ] (documentar) Instrucción para Agy
```

> Cierra `dotnet run` antes de ejecutar el orquestador: en Windows la app abierta bloquea los binarios y el build falla.

Documentación del proyecto: `D:/Daniel/Obsidian/Data/00_Human/02_Second_Brain/Software/05_Projects/18_Web_Dinosaurios/Indice.md`

## Créditos

- **Datos:** [Paleobiology Database](https://paleobiodb.org/), licencia CC BY 4.0. Nombres de países: Unicode CLDR 46.
- **Imágenes:** Wikimedia Commons bajo licencias CC BY y CC BY-SA. El autor y la licencia de cada una están en `docs/dinosaurios-semilla.md` (campo `CreditoImagen`) y se muestran en la ficha.
- **Textos:** extractos de Wikipedia en español (CC BY-SA 4.0), consultados el 2026-10-27.
- **Skill `frontend-design`:** [anthropics/skills](https://github.com/anthropics/skills), licencia Apache 2.0.
