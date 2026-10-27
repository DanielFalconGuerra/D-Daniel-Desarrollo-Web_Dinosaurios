# Guía de UI — Web_Dinosaurios

Contrato visual para las vistas Razor. Creada con la skill `frontend-design` (`.claude/skills/frontend-design/`).
Toda tarea de vistas o CSS debe seguirla; si algo no está aquí, usa Bootstrap 5 sin adornos.
**Responsable:** Claude Code (diseñador). DeepSeek no edita vistas ni CSS; si una tarea de funcionalidad
necesita mostrar algo nuevo, reutiliza las clases `dino-*` existentes de `wwwroot/css/site.css`.
**Documento de solo lectura:** se envía como referencia; no lo modifiques ni lo devuelvas en tu respuesta.

## 1. Brief

- **Tema:** unos 1 600 dinosaurios del Mesozoico (8 con foto y el resto, importados de la PBDB, con el icono de huella),
  con su periodo, antigüedad, región, dieta, descripción e imagen.
- **Público:** una persona curiosa que lee en español y quiere comparar especies.
- **Trabajo principal:** recorrer el catálogo, filtrar por nombre o periodo y abrir la ficha de una especie.

## 2. Idea central: la columna estratigráfica

La lista se lee como un corte de roca: **cada periodo es un estrato** con el color oficial de la
[Carta Cronoestratigráfica Internacional (ICS)](https://stratigraphy.org/chart), y los estratos más recientes van arriba
(Cretácico sobre Jurásico y Triásico). El periodo da el color de la banda y la época el del encabezado. El color no decora: dice a qué periodo pertenece cada especie.
Es el único elemento llamativo de la página; todo lo demás es sobrio.

## 3. Tokens

Declararlos en `:root` de `wwwroot/css/site.css` y usarlos siempre por nombre:

| Token | Valor | Uso |
|---|---|---|
| `--dino-fondo` | `#F2F3EF` | Fondo de la página (caliza, gris frío) |
| `--dino-superficie` | `#FFFFFF` | Fondo de filas, formulario y ficha |
| `--dino-texto` | `#1E2B2F` | Texto principal (pizarra) |
| `--dino-texto-suave` | `#55625F` | Texto secundario y créditos |
| `--dino-linea` | `#C9CFCB` | Bordes y separadores de 1 px |
| `--dino-triasico` | `#812B92` | Banda del Triásico (color ICS) |
| `--dino-triasico-inferior` / `-medio` / `-superior` | `#983999` / `#B168B1` / `#BD8CC3` | Encabezado de cada época (ICS) |
| `--dino-jurasico` | `#34B2C9` | Banda del Jurásico (color ICS) |
| `--dino-jurasico-inferior` / `-medio` / `-superior` | `#42AED0` / `#80CFD8` / `#B3E3EE` | Encabezado de cada época (ICS) |
| `--dino-cretacico` | `#7FC64E` | Banda del Cretácico (color ICS) |
| `--dino-cretacico-inferior` / `-superior` | `#8CCD57` / `#A6D84A` | Encabezado de cada época (ICS) |

Colores verificados con la plantilla `Template:Period_color` de Wikipedia en inglés, que reproduce la carta de la ICS.

- Sobre los colores de periodo solo va texto `--dino-texto` (contraste ≥ 4.5:1). Nunca texto blanco ni texto de color.
- El Triásico Inferior (2.3:1) y el Medio (3.8:1) no llegan a 4.5:1: su encabezado usa `color-mix()` con blanco
  (45 % y 55 % del color) para conservar el tono. La banda de 8 px sí usa el color puro, porque no lleva texto.
- Las clases salen de `Helpers/Presentacion.cs` (`ClasesPeriodo`): periodo (`dino-cretacico`) más época (`dino-cretacico-superior`).
- Periodo desconocido: usar `--dino-linea` como banda.
- Botón principal: fondo `--dino-texto`, texto blanco. No usar `btn-primary` azul ni `badge bg-*`.

## 4. Tipografía

Cargar en `_Layout.cshtml`, antes de `site.css`:

```html
<link rel="preconnect" href="https://fonts.googleapis.com" />
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Archivo:ital,wdth,wght@0,62..125,100..900;1,62..125,100..900&family=Source+Serif+4:opsz,wght@8..60,200..900&display=swap" />
```

| Rol | Familia | Ajustes |
|---|---|---|
| Títulos, nombres, nav, botones, etiquetas | `"Archivo", system-ui, sans-serif` | Títulos: `font-weight: 750; font-stretch: 115%; letter-spacing: -0.01em` |
| Descripciones y texto largo | `"Source Serif 4", Georgia, serif` | `font-size: 1.0625rem; line-height: 1.65` |

- Escala (razón 1.25): `0.8rem`, `1rem`, `1.25rem`, `1.563rem`, `1.953rem`, `2.441rem`, `3.052rem`.
- `h1` de página `2.441rem` (`1.953rem` en móvil); nombre en la ficha `3.052rem`; nombre en la fila `1.563rem`.
- Texto largo con `max-width: 68ch`.
- La antigüedad (`69–66 millones de años`) se escribe abreviada en la lista como `69–66 Ma`, con `font-variant-numeric: tabular-nums`.
- Los nombres científicos (`Tyrannosaurus rex`) van en cursiva con la clase `dino-nombre-cientifico`: es la convención
  biológica, no un adorno.
- Prohibido: mayúsculas sostenidas en etiquetas, resaltar una sola palabra del título, fuentes monoespaciadas.

## 5. Layout

### Catálogo (`Index`)

```
Dinosaurios del Mesozoico
1617 dinosaurios, del más reciente al más antiguo. […]   (con filtros: «23 dinosaurios coinciden con la búsqueda…»)
┌───────────────────────────────────────────────────────────┐
│ [Nombre_____________]  [Periodo ▾]  [Buscar] Quitar filtros │  formulario en una fila
└───────────────────────────────────────────────────────────┘
▌ Cretácico Superior                           100,5–66 Ma   encabezado: época y sus límites ICS
▌┌────────────┐ Tyrannosaurus rex                  69–66 Ma    fila: banda de 8 px a la izquierda
▌│  imagen    │ Carnívoro, América del Norte
▌│  4:3       │ Descripción en serif, máx. 3 líneas…
▌└────────────┘                                   Ver ficha
▌ Cretácico Inferior                          145–100,5 Ma
▌ …
Del 1 al 24 de 1617            Anterior 1 … 4 [5] 6 … 68 Siguiente   paginación
```

- **Filas, no tarjetas:** cada especie es una fila horizontal (`display: grid; grid-template-columns: 220px 1fr`),
  separada por una línea de 1 px, sin sombra y con `border-radius: 0`.
- La banda izquierda (`border-left: 8px solid`) usa el color del periodo; el encabezado del estrato usa el color claro.
- El SQL ya ordena por `EdadMaxMa` (más reciente primero): la vista agrupa con `Model.GroupBy(d => d.Periodo)` sin
  reordenar, porque `GroupBy` conserva el orden de aparición. Un estrato puede continuar en la página siguiente.
- El encabezado del estrato muestra los límites de la época (`Presentacion.LimitesEpoca`), no un conteo: con 24 filas
  por página un conteo solo diría cuántas hay en esa página.
- La línea de datos omite los «Sin datos» de la importación (`Presentacion.DatosBreves`): «Carnívoro, Argentina»,
  «Herbívoro» o «Dieta y región sin datos».
- Imagen `aspect-ratio: 4 / 3; object-fit: cover`, `alt="Ilustración de {Nombre}"`, `loading="lazy"`.
- **Icono** (`/images/dinosaurios/icono.svg`): huella fósil de tres dedos en `#55625F` para los géneros sin foto.
  Se muestra con `img.dino-imagen--icono` (fondo `--dino-fondo`, borde de 1 px, `object-fit: contain`) y `alt=""`,
  porque no ilustra a ese dinosaurio. En móvil baja a `aspect-ratio: 3 / 1` para no repetir 24 cuadros enormes.
  En el SVG no puede haber dos guiones seguidos dentro de un comentario XML: invalidan el archivo.
- **Paginación** (`nav.dino-paginacion`, `aria-label="Páginas del catálogo"`): a la izquierda «Del 25 al 48 de 1617»;
  a la derecha «Anterior», la primera, la última y dos páginas a cada lado de la actual, con «…» en los huecos.
  La actual es un `span` con `aria-current="page"` en `--dino-texto`; los enlaces conservan nombre y periodo, y la
  página 1 no lleva `?pagina`. Una página fuera de rango muestra el estado vacío con «Ir a la primera página».
- Descripción recortada a 3 líneas con `-webkit-line-clamp: 3`.
- Móvil (< 576 px): la imagen pasa arriba a todo el ancho y el texto debajo; la banda se mantiene.
- Texto alineado a la izquierda en toda la página; nada centrado salvo el estado vacío.

### Ficha (`Detalle`)

```
← Volver al catálogo
▌ Cretácico Superior   69–66 millones de años        franja del color del periodo
Tyrannosaurus rex                                    nombre a 3.052rem
┌──────────────────────────┐  Descripción en serif (68ch)
│ imagen grande, 4:3       │  ─────────────────────────
│                          │  Región   América del Norte
└──────────────────────────┘  Dieta    Carnívoro
Imagen: crédito (obligatorio) Leer en Wikipedia
```

- Dos columnas desde `lg` (imagen 7/12, texto 5/12); una columna en móvil.
- Datos en `<dl>` con dos columnas; etiquetas en Archivo `--dino-texto-suave`, valores en `--dino-texto`.
- `CreditoImagen` siempre visible bajo la imagen (`0.8rem`, `--dino-texto-suave`): la licencia lo exige.
  Con el icono, el pie de figura dice «Este dinosaurio todavía no tiene ilustración: se muestra el icono de huella del catálogo.»
- Enlace a la fuente: «Leer en Wikipedia» o «Ver en la Paleobiology Database» (`Presentacion.TextoFuente`).

## 6. Textos

- Español neutro con **tuteo** («prueba», «elige»), nunca voseo («probá», «usá»). Mayúscula solo al inicio.
- Botones con el verbo de la acción: «Buscar», «Quitar filtros», «Ver ficha», «Leer en Wikipedia», «Volver al catálogo»,
  «Anterior», «Siguiente».
- Sin flechas `→` al final de botones ni enlaces; la flecha `←` solo en «Volver al catálogo».
- Sin cadenas con puntos medios (`A · B · C`): usar comas.
- Estado vacío: «Ninguna especie coincide con la búsqueda.» y debajo el enlace «Quitar filtros». Sin disculpas.
- Menú en español: «Catálogo» (Dinosaurios/Index) y «Privacidad»; la marca «Dinosaurios del Mesozoico» enlaza al catálogo.
- `<html lang="es">`. Pie: «Datos de la Paleobiology Database (CC BY 4.0), textos de Wikipedia en español (CC BY-SA 4.0)
  e imágenes de Wikimedia Commons.», con enlace a paleobiodb.org.
- Los números de 4 cifras van sin separador («1617»), como recomienda la RAE; así no chocan con la coma decimal de «72,1 Ma».

## 7. Piso de calidad

- Foco visible: `outline: 3px solid var(--dino-texto); outline-offset: 2px` en `:focus-visible`.
  **Sustituye** a la regla de la plantilla `.btn:focus, … { box-shadow: … #258cfb; }`: se elimina esa regla
  y también se neutralizan los anillos azules propios de Bootstrap en `.btn`, `.form-control` y `.form-select`,
  para que solo haya un indicador de foco.
- Enlaces: color `--dino-texto` y **subrayados siempre** en el texto (no se quita el subrayado de Bootstrap);
  el menú y los botones siguen sin subrayado, como en Bootstrap.
- Sin animaciones de entrada ni efectos hover en cada fila.
- Respetar `@media (prefers-reduced-motion: reduce)`.
- Estilos en `site.css` con clases `dino-*`. Nada de `style="..."` en línea en las vistas.
- Probar a 375 px y a 1280 px de ancho.

## 8. Revisión del plan contra los defaults

| Default genérico | Decisión para este brief |
|---|---|
| Rejilla de tarjetas iguales con la misma sombra | Filas agrupadas por estrato, sin sombra |
| Fondo crema con acento terracota | Fondo caliza gris frío; el color sale de la carta ICS |
| Etiquetas en mayúsculas sobre cada título | Encabezados de estrato en caja normal |
| `badge` de Bootstrap de colores sin significado | La banda de color ya indica el periodo; dieta y región como texto |
