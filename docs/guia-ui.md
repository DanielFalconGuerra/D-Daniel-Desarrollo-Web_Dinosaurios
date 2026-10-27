# Guía de UI — Web_Dinosaurios

Contrato visual para las vistas Razor. Creada con la skill `frontend-design` (`.claude/skills/frontend-design/`).
Toda tarea de vistas o CSS debe seguirla; si algo no está aquí, usa Bootstrap 5 sin adornos.
**Documento de solo lectura:** se envía como referencia; no lo modifiques ni lo devuelvas en tu respuesta.

## 1. Brief

- **Tema:** ocho dinosaurios del Mesozoico, con su periodo, antigüedad, región, dieta, descripción e imagen.
- **Público:** una persona curiosa que lee en español y quiere comparar especies.
- **Trabajo principal:** recorrer el catálogo, filtrar por nombre o periodo y abrir la ficha de una especie.

## 2. Idea central: la columna estratigráfica

La lista se lee como un corte de roca: **cada periodo es un estrato** con el color oficial de la
[Carta Cronoestratigráfica Internacional (ICS)](https://stratigraphy.org/chart), y los estratos más recientes van arriba
(Cretácico sobre Jurásico). El color no decora: dice a qué periodo pertenece cada especie.
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
| `--dino-jurasico` | `#34B2C9` | Banda del Jurásico (color ICS) |
| `--dino-jurasico-claro` | `#B3E3EE` | Fondo del encabezado del Jurásico Superior (ICS) |
| `--dino-cretacico` | `#7FC64E` | Banda del Cretácico (color ICS) |
| `--dino-cretacico-claro` | `#A6D84A` | Fondo del encabezado del Cretácico Superior (ICS) |

- Sobre los colores de periodo solo va texto `--dino-texto` (contraste ≥ 4.5:1). Nunca texto blanco ni texto de color.
- Periodo desconocido: usar `--dino-linea` como banda.
- Botón principal: fondo `--dino-texto`, texto blanco. No usar `btn-primary` azul ni `badge bg-*`.

## 4. Tipografía

Cargar en `_Layout.cshtml`, antes de `site.css`:

```html
<link rel="preconnect" href="https://fonts.googleapis.com" />
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Archivo:wdth,wght@62..125,100..900&family=Source+Serif+4:opsz,wght@8..60,200..900&display=swap" />
```

| Rol | Familia | Ajustes |
|---|---|---|
| Títulos, nombres, nav, botones, etiquetas | `"Archivo", system-ui, sans-serif` | Títulos: `font-weight: 750; font-stretch: 115%; letter-spacing: -0.01em` |
| Descripciones y texto largo | `"Source Serif 4", Georgia, serif` | `font-size: 1.0625rem; line-height: 1.65` |

- Escala (razón 1.25): `0.8rem`, `1rem`, `1.25rem`, `1.563rem`, `1.953rem`, `2.441rem`, `3.052rem`.
- `h1` de página `2.441rem` (`1.953rem` en móvil); nombre en la ficha `3.052rem`; nombre en la fila `1.563rem`.
- Texto largo con `max-width: 68ch`.
- La antigüedad (`69–66 millones de años`) se escribe abreviada en la lista como `69–66 Ma`, con `font-variant-numeric: tabular-nums`.
- Prohibido: mayúsculas sostenidas en etiquetas, resaltar una sola palabra del título, fuentes monoespaciadas.

## 5. Layout

### Catálogo (`Index`)

```
Dinosaurios del Mesozoico
8 especies, de la más reciente a la más antigua por periodo.
┌───────────────────────────────────────────────────────────┐
│ [Nombre_____________]  [Periodo ▾]  [Buscar] Quitar filtros │  formulario en una fila
└───────────────────────────────────────────────────────────┘
▌ Cretácico Superior                              6 especies   encabezado del estrato
▌┌────────────┐ Tyrannosaurus rex                  69–66 Ma    fila: banda de 8 px a la izquierda
▌│  imagen    │ Carnívoro, América del Norte
▌│  4:3       │ Descripción en serif, máx. 3 líneas…
▌└────────────┘                                   Ver ficha
▌ Jurásico Superior                               2 especies
▌ …
```

- **Filas, no tarjetas:** cada especie es una fila horizontal (`display: grid; grid-template-columns: 220px 1fr`),
  separada por una línea de 1 px, sin sombra y con `border-radius: 0`.
- La banda izquierda (`border-left: 8px solid`) usa el color del periodo; el encabezado del estrato usa el color claro.
- Agrupar en la vista con `Model.GroupBy(d => d.Periodo)`, ordenando los grupos con el Cretácico primero.
  Dentro de cada grupo se respeta el orden por nombre del SQL.
- Imagen `aspect-ratio: 4 / 3; object-fit: cover`, `alt="Ilustración de {Nombre}"`, `loading="lazy"`.
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

## 6. Textos

- Español neutro con **tuteo** («prueba», «elige»), nunca voseo («probá», «usá»). Mayúscula solo al inicio.
- Botones con el verbo de la acción: «Buscar», «Quitar filtros», «Ver ficha», «Leer en Wikipedia», «Volver al catálogo».
- Sin flechas `→` al final de botones ni enlaces; la flecha `←` solo en «Volver al catálogo».
- Sin cadenas con puntos medios (`A · B · C`): usar comas.
- Estado vacío: «Ninguna especie coincide con la búsqueda.» y debajo el enlace «Quitar filtros». Sin disculpas.
- Menú en español: «Catálogo» (Dinosaurios/Index) y «Privacidad». Marca: «Dinosaurios del Mesozoico».
- `<html lang="es">`. Pie: «Imágenes de Wikimedia Commons y textos basados en Wikipedia en español.»

## 7. Piso de calidad

- Foco visible: `outline: 3px solid var(--dino-texto); outline-offset: 2px` en `:focus-visible`.
- Sin animaciones de entrada ni efectos hover en cada fila; solo subrayado en enlaces al pasar el cursor.
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
