# Datos semilla — tabla `Dinosaurios`

Contrato de datos para `Data/seed.sql`. Copiar los valores **exactamente** (incluidos acentos y guiones «–»).
Textos redactados a partir de los resúmenes de Wikipedia en español (consultados el 2026-10-26).
Las imágenes están en `wwwroot/images/dinosaurios/` y provienen de Wikimedia Commons; sus licencias
exigen atribución, por eso `CreditoImagen` es obligatorio y debe mostrarse en la vista de detalle.

## Esquema

| Columna | Tipo SQLite | Restricción | Propiedad C# |
|---|---|---|---|
| `Id` | `INTEGER` | `PRIMARY KEY AUTOINCREMENT` | `int Id` |
| `Nombre` | `TEXT` | `NOT NULL UNIQUE` | `string Nombre` |
| `Periodo` | `TEXT` | `NOT NULL` | `string Periodo` |
| `Antiguedad` | `TEXT` | `NOT NULL` | `string Antiguedad` |
| `Region` | `TEXT` | `NOT NULL` | `string Region` |
| `Dieta` | `TEXT` | `NOT NULL` | `string Dieta` |
| `Descripcion` | `TEXT` | `NOT NULL` | `string Descripcion` |
| `ImagenUrl` | `TEXT` | `NOT NULL` | `string ImagenUrl` |
| `CreditoImagen` | `TEXT` | `NOT NULL` | `string CreditoImagen` |
| `FuenteUrl` | `TEXT` | `NOT NULL` | `string FuenteUrl` |

## Valores de `Periodo` y filtrado

`Periodo` guarda la subdivisión completa, no solo el período. En los 8 registros solo hay dos valores:

- `Cretácico Superior` (6 registros: Tyrannosaurus, Triceratops, Velociraptor, Spinosaurus, Ankylosaurus, Parasaurolophus)
- `Jurásico Superior` (2 registros: Stegosaurus, Brachiosaurus)

`DinosaurioRepository.BuscarAsync` compara el periodo **por prefijo** mediante
`Periodo LIKE @Periodo || '%'`, de modo que valores como `Cretácico` o `Jurásico` devuelven
todos los registros de esa subdivisión y el valor completo sigue funcionando. El `%` se añade
en SQL, nunca se concatena texto del usuario: el valor viaja siempre como parámetro `@Periodo`
(recortado y con `null` cuando llega vacío, en cuyo caso no se filtra).

> [!note] Sensibilidad a mayúsculas y acentos
> `LIKE` en SQLite ignora mayúsculas y minúsculas solo en el rango ASCII; los acentos sí importan.
> El prefijo debe escribirse con la misma tilde que el dato (`Cretácico`, `Jurásico`).

## Registros

### 1. Tyrannosaurus rex
- **Periodo:** Cretácico Superior
- **Antiguedad:** 69–66 millones de años
- **Region:** América del Norte
- **Dieta:** Carnívoro
- **Descripcion:** Terópodo tiranosáurido y uno de los últimos dinosaurios no avianos antes de la extinción masiva del Cretácico-Paleógeno. Su distribución en el continente fue más amplia que la de otros tiranosáuridos y es una figura habitual de la cultura popular.
- **ImagenUrl:** /images/dinosaurios/tyrannosaurus.jpg
- **CreditoImagen:** ScottRobertAnselmo, CC BY-SA 3.0, vía Wikimedia Commons
- **FuenteUrl:** https://es.wikipedia.org/wiki/Tyrannosaurus_rex

### 2. Triceratops
- **Periodo:** Cretácico Superior
- **Antiguedad:** 68–66 millones de años
- **Region:** América del Norte
- **Dieta:** Herbívoro
- **Descripcion:** Ceratópsido con una gran gola ósea sobre el cuello y tres cuernos en la cara. Medía hasta 8–9 metros de largo y pesaba entre 5 y 9 toneladas; fue uno de los últimos géneros antes de la extinción masiva.
- **ImagenUrl:** /images/dinosaurios/triceratops.jpg
- **CreditoImagen:** Allie_Caulfield (derivada de MathKnight), CC BY-SA 3.0, vía Wikimedia Commons
- **FuenteUrl:** https://es.wikipedia.org/wiki/Triceratops

### 3. Velociraptor
- **Periodo:** Cretácico Superior
- **Antiguedad:** 75–71 millones de años
- **Region:** Asia (Mongolia y China)
- **Dieta:** Carnívoro
- **Descripcion:** Terópodo dromeosáurido del que se conocen dos especies: V. mongoliensis, hallada en Mongolia, y V. osmolskae, nombrada en 2008 con material de Mongolia Interior, en China.
- **ImagenUrl:** /images/dinosaurios/velociraptor.jpg
- **CreditoImagen:** Ben Townsend, CC BY 2.0, vía Wikimedia Commons
- **FuenteUrl:** https://es.wikipedia.org/wiki/Velociraptor

### 4. Stegosaurus
- **Periodo:** Jurásico Superior
- **Antiguedad:** 156–144 millones de años
- **Region:** América del Norte y posiblemente Europa
- **Dieta:** Herbívoro
- **Descripcion:** Tireóforo reconocible por las placas de su espalda y las espinas de la cola. Se han hallado restos de más de ochenta individuos en la Formación Morrison, y en 2006 apareció un espécimen en Portugal.
- **ImagenUrl:** /images/dinosaurios/stegosaurus.png
- **CreditoImagen:** Fred Wierum, CC BY-SA 4.0, vía Wikimedia Commons
- **FuenteUrl:** https://es.wikipedia.org/wiki/Stegosaurus

### 5. Brachiosaurus
- **Periodo:** Jurásico Superior
- **Antiguedad:** 154–153 millones de años
- **Region:** América del Norte
- **Dieta:** Herbívoro
- **Descripcion:** Saurópodo de cuello muy largo y cráneo pequeño, con patas delanteras más largas que las traseras, rasgo al que alude su nombre. Lo describió Elmer S. Riggs en 1903 con fósiles de la Formación Morrison.
- **ImagenUrl:** /images/dinosaurios/brachiosaurus.jpg
- **CreditoImagen:** James St. John, CC BY 2.0, vía Wikimedia Commons
- **FuenteUrl:** https://es.wikipedia.org/wiki/Brachiosaurus_altithorax

### 6. Spinosaurus
- **Periodo:** Cretácico Superior
- **Antiguedad:** 100–94 millones de años
- **Region:** Norte de África (Egipto, Marruecos y Níger)
- **Dieta:** Carnívoro
- **Descripcion:** Gran terópodo espinosáurido del Cenomaniano. Hoy se reconocen dos especies válidas: S. aegyptiacus y S. mirabilis.
- **ImagenUrl:** /images/dinosaurios/spinosaurus.jpg
- **CreditoImagen:** Mike Bowler, CC BY 2.0, vía Wikimedia Commons
- **FuenteUrl:** https://es.wikipedia.org/wiki/Spinosaurus

### 7. Ankylosaurus
- **Periodo:** Cretácico Superior
- **Antiguedad:** 68–66 millones de años
- **Region:** América del Norte
- **Dieta:** Herbívoro
- **Descripcion:** Anquilosáurido cubierto por una pesada armadura y con un gran mazo en la cola. Probablemente fue el mayor de su grupo y es el dinosaurio acorazado más representativo.
- **ImagenUrl:** /images/dinosaurios/ankylosaurus.png
- **CreditoImagen:** Emily Willoughby, CC BY-SA 3.0, vía Wikimedia Commons
- **FuenteUrl:** https://es.wikipedia.org/wiki/Ankylosaurus_magniventris

### 8. Parasaurolophus
- **Periodo:** Cretácico Superior
- **Antiguedad:** 76–73 millones de años
- **Region:** América del Norte
- **Dieta:** Herbívoro
- **Descripcion:** Hadrosáurido que caminaba tanto en dos como en cuatro patas. Lo describió William Parks en 1922 a partir de un cráneo hallado en Alberta, Canadá.
- **ImagenUrl:** /images/dinosaurios/parasaurolophus.jpg
- **CreditoImagen:** Zissoudisctrucker, CC BY-SA 4.0, vía Wikimedia Commons
- **FuenteUrl:** https://es.wikipedia.org/wiki/Parasaurolophus
