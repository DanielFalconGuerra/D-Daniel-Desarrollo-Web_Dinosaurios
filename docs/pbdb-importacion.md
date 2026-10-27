# Importación desde la Paleobiology Database (PBDB)

Contrato para ampliar el catálogo de 8 a unos 1 600 géneros. Lo escribió Claude Code (orquestador) tras probar las APIs
el 2026-10-27. **Documento de solo lectura:** se envía como referencia; no lo modifiques ni lo devuelvas en tu respuesta.

## 1. Fuentes

| Dato | Fuente | Licencia |
|---|---|---|
| Géneros, edad, dieta, familia | PBDB `taxa/list` | CC BY 4.0 |
| Países de los fósiles | PBDB `occs/list` | CC BY 4.0 |
| Descripción (2 frases) | API de Wikipedia en español (`prop=extracts`) | CC BY-SA 4.0 |
| Nombres de países en español | `Data/paises-es.json` (generado de Unicode CLDR 46) | Unicode License |

Ninguna API pide clave. Las tres se llaman **solo** al ejecutar `dotnet run -- importar-pbdb`, nunca al arrancar la web.

## 2. Llamadas

**Géneros** (1 llamada, ~1 775 registros, sin aves ni huevos fósiles):

```
GET https://paleobiodb.org/data1.2/taxa/list.json?base_name=Dinosauria%5EAves&rank=genus&taxon_status=valid&pres=regular&show=app,class,ecospace
```

Campos usados de cada elemento de `records` (los nombres son abreviados y pueden faltar):

| Campo | Ejemplo | Uso |
|---|---|---|
| `oid` | `"txn:38613"` | `PbdbId` = número tras `txn:` |
| `nam` | `"Tyrannosaurus"` | `Nombre` |
| `fea` | `72.1` | Edad máxima en millones de años → `EdadMaxMa` y `Periodo` |
| `lla` | `66` | Edad mínima → `Antiguedad` |
| `fml` | `"Tyrannosauridae"` | Familia (ignorar `"NO_FAMILY_SPECIFIED"`) |
| `jdt` | `"carnivore"` | `Dieta` |

**Descartar** los registros sin `fea` o con `fea <= 66` (fuera del Mesozoico): quedan ~1 617 géneros.

**Países** (1 llamada, ~7 500 ocurrencias):

```
GET https://paleobiodb.org/data1.2/occs/list.json?base_name=Dinosauria%5EAves&pres=regular&idreso=genus&show=class,loc
```

Agrupar por `gnl` (género) el conjunto de `cc2` (código de país). El 90 % de los géneros tiene al menos un país.

**Descripciones** (lotes de 20 títulos, ~81 llamadas):

```
GET https://es.wikipedia.org/w/api.php?action=query&format=json&prop=extracts&exintro=1&explaintext=1&exsentences=2&exlimit=20&redirects=1&titles=Género1|Género2|…
```

- Cabecera obligatoria `User-Agent: Web_Dinosaurios/1.0 (proyecto de aprendizaje)`; sin ella Wikipedia rechaza la llamada.
- Esperar 250 ms entre lotes.
- Las redirecciones llegan en `query.redirects` (`from` → `to`) y las normalizaciones en `query.normalized`;
  usarlas para volver del título final al género pedido.
- Página sin `extract` o con `missing` = sin artículo (en una muestra de 60 géneros, 55 tenían artículo).
- Quitar del texto los caracteres invisibles `​` y espacios repetidos.

## 3. Mapeo a la tabla `Dinosaurios`

Columnas nuevas (`ALTER TABLE` en bases existentes y en `schema.sql` para bases nuevas):

| Columna | Tipo | Uso |
|---|---|---|
| `PbdbId` | `INTEGER NULL` + `CREATE UNIQUE INDEX IF NOT EXISTS IX_Dinosaurios_PbdbId ON Dinosaurios(PbdbId)` | Evita importar dos veces; `NULL` en los 8 registros semilla. SQLite no admite `ADD COLUMN ... UNIQUE`: la unicidad va en el índice, que permite varios `NULL` |
| `EdadMaxMa` | `REAL NULL` | Orden del catálogo; en los 8 semilla = primer número de `Antiguedad` (69, 68, 75, 156, 154, 100, 68, 76) |

| Columna | Regla |
|---|---|
| `Nombre` | `nam` |
| `Periodo` | Por `fea` con los límites de la ICS (abajo) |
| `Antiguedad` | `"{fea}–{lla} millones de años"` con un decimal como máximo, coma decimal (`"72,1–66 millones de años"`) y raya «–»; si `fea == lla`, solo `"{fea} millones de años"` |
| `Region` | Nombres de `Data/paises-es.json` de sus `cc2`, ordenados por número de ocurrencias, máximo 3, unidos con `", "` y `" y "` antes del último (`"China, Mongolia y Kazajistán"`); sin países: `"Sin datos"` |
| `Dieta` | Primer término de `jdt`: `herbivore` → `Herbívoro`, `carnivore` → `Carnívoro`, `omnivore` → `Omnívoro`, `granivore` → `Granívoro`, `insectivore` → `Insectívoro`, `piscivore` → `Piscívoro`; otro o vacío → `"Sin datos"` |
| `Descripcion` | Extracto de Wikipedia; si no hay: `"Género de dinosaurio de la familia {fml} registrado en la Paleobiology Database."` (sin familia: `"Género de dinosaurio registrado en la Paleobiology Database."`) |
| `ImagenUrl` | `/images/dinosaurios/icono.svg` (lo dibuja el diseñador) |
| `CreditoImagen` | `"Icono de Web_Dinosaurios"` |
| `FuenteUrl` | Con extracto: `https://es.wikipedia.org/wiki/{título final con espacios como _}`; sin extracto: `https://paleobiodb.org/classic/basicTaxonInfo?taxon_no={PbdbId}` |
| `PbdbId`, `EdadMaxMa` | `oid` sin `txn:` y `fea` |

**Periodo según `fea`** (millones de años, Carta Cronoestratigráfica Internacional):

| Condición | `Periodo` |
|---|---|
| `fea > 247.2` | `Triásico Inferior` |
| `237 < fea <= 247.2` | `Triásico Medio` |
| `201.4 < fea <= 237` | `Triásico Superior` |
| `174.7 < fea <= 201.4` | `Jurásico Inferior` |
| `161.5 < fea <= 174.7` | `Jurásico Medio` |
| `145 < fea <= 161.5` | `Jurásico Superior` |
| `100.5 < fea <= 145` | `Cretácico Inferior` |
| `66 < fea <= 100.5` | `Cretácico Superior` |

## 4. Reglas del importador

- **No tocar los 8 registros semilla:** omitir el género si ya existe una fila con `Nombre = género` o
  `Nombre LIKE género || ' %'` (así `Tyrannosaurus` no duplica a `Tyrannosaurus rex`).
- Insertar con `INSERT OR IGNORE` (por `PbdbId` y `Nombre` únicos), todo en **una transacción** y con parámetros de Dapper.
- Se puede ejecutar varias veces: la segunda vez no inserta nada.
- Si una API falla, se aborta sin insertar nada y se muestra el error; la web sigue funcionando con los datos que había.
- Al terminar, mostrar en consola: géneros recibidos, descartados, omitidos por existir, insertados y cuántos tienen descripción de Wikipedia.

## 5. Catálogo con 1 600 registros

- `BuscarAsync` pagina en SQL (`LIMIT @Tamano OFFSET @Desplazamiento`) y devuelve también el total de coincidencias.
- Orden: `EdadMaxMa` ascendente (el más reciente primero, como los estratos), luego `Nombre`; los `NULL` al final.
- 24 especies por página; el número de página llega por query string (`?pagina=2`) y se conserva junto con los filtros.
- `Triásico` se agrega a los periodos válidos del filtro (que compara por prefijo).
