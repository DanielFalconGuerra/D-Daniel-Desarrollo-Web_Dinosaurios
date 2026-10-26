-- Datos semilla — tabla `Dinosaurios`
-- Contrato: docs/dinosaurios-semilla.md
-- Los valores se copian exactamente del contrato (acentos y guiones «–» incluidos).
-- `Id` es INTEGER PRIMARY KEY AUTOINCREMENT, por eso no se incluye en el INSERT.

INSERT INTO Dinosaurios (Nombre, Periodo, Antiguedad, Region, Dieta, Descripcion, ImagenUrl, CreditoImagen, FuenteUrl)
VALUES (
    'Tyrannosaurus rex',
    'Cretácico Superior',
    '69–66 millones de años',
    'América del Norte',
    'Carnívoro',
    'Terópodo tiranosáurido y uno de los últimos dinosaurios no avianos antes de la extinción masiva del Cretácico-Paleógeno. Su distribución en el continente fue más amplia que la de otros tiranosáuridos y es una figura habitual de la cultura popular.',
    '/images/dinosaurios/tyrannosaurus.jpg',
    'ScottRobertAnselmo, CC BY-SA 3.0, vía Wikimedia Commons',
    'https://es.wikipedia.org/wiki/Tyrannosaurus_rex'
);

INSERT INTO Dinosaurios (Nombre, Periodo, Antiguedad, Region, Dieta, Descripcion, ImagenUrl, CreditoImagen, FuenteUrl)
VALUES (
    'Triceratops',
    'Cretácico Superior',
    '68–66 millones de años',
    'América del Norte',
    'Herbívoro',
    'Ceratópsido con una gran gola ósea sobre el cuello y tres cuernos en la cara. Medía hasta 8–9 metros de largo y pesaba entre 5 y 9 toneladas; fue uno de los últimos géneros antes de la extinción masiva.',
    '/images/dinosaurios/triceratops.jpg',
    'Allie_Caulfield (derivada de MathKnight), CC BY-SA 3.0, vía Wikimedia Commons',
    'https://es.wikipedia.org/wiki/Triceratops'
);

INSERT INTO Dinosaurios (Nombre, Periodo, Antiguedad, Region, Dieta, Descripcion, ImagenUrl, CreditoImagen, FuenteUrl)
VALUES (
    'Velociraptor',
    'Cretácico Superior',
    '75–71 millones de años',
    'Asia (Mongolia y China)',
    'Carnívoro',
    'Terópodo dromeosáurido del que se conocen dos especies: V. mongoliensis, hallada en Mongolia, y V. osmolskae, nombrada en 2008 con material de Mongolia Interior, en China.',
    '/images/dinosaurios/velociraptor.jpg',
    'Ben Townsend, CC BY 2.0, vía Wikimedia Commons',
    'https://es.wikipedia.org/wiki/Velociraptor'
);

INSERT INTO Dinosaurios (Nombre, Periodo, Antiguedad, Region, Dieta, Descripcion, ImagenUrl, CreditoImagen, FuenteUrl)
VALUES (
    'Stegosaurus',
    'Jurásico Superior',
    '156–144 millones de años',
    'América del Norte y posiblemente Europa',
    'Herbívoro',
    'Tireóforo reconocible por las placas de su espalda y las espinas de la cola. Se han hallado restos de más de ochenta individuos en la Formación Morrison, y en 2006 apareció un espécimen en Portugal.',
    '/images/dinosaurios/stegosaurus.png',
    'Fred Wierum, CC BY-SA 4.0, vía Wikimedia Commons',
    'https://es.wikipedia.org/wiki/Stegosaurus'
);

INSERT INTO Dinosaurios (Nombre, Periodo, Antiguedad, Region, Dieta, Descripcion, ImagenUrl, CreditoImagen, FuenteUrl)
VALUES (
    'Brachiosaurus',
    'Jurásico Superior',
    '154–153 millones de años',
    'América del Norte',
    'Herbívoro',
    'Saurópodo de cuello muy largo y cráneo pequeño, con patas delanteras más largas que las traseras, rasgo al que alude su nombre. Lo describió Elmer S. Riggs en 1903 con fósiles de la Formación Morrison.',
    '/images/dinosaurios/brachiosaurus.jpg',
    'James St. John, CC BY 2.0, vía Wikimedia Commons',
    'https://es.wikipedia.org/wiki/Brachiosaurus_altithorax'
);

INSERT INTO Dinosaurios (Nombre, Periodo, Antiguedad, Region, Dieta, Descripcion, ImagenUrl, CreditoImagen, FuenteUrl)
VALUES (
    'Spinosaurus',
    'Cretácico Superior',
    '100–94 millones de años',
    'Norte de África (Egipto, Marruecos y Níger)',
    'Carnívoro',
    'Gran terópodo espinosáurido del Cenomaniano. Hoy se reconocen dos especies válidas: S. aegyptiacus y S. mirabilis.',
    '/images/dinosaurios/spinosaurus.jpg',
    'Mike Bowler, CC BY 2.0, vía Wikimedia Commons',
    'https://es.wikipedia.org/wiki/Spinosaurus'
);

INSERT INTO Dinosaurios (Nombre, Periodo, Antiguedad, Region, Dieta, Descripcion, ImagenUrl, CreditoImagen, FuenteUrl)
VALUES (
    'Ankylosaurus',
    'Cretácico Superior',
    '68–66 millones de años',
    'América del Norte',
    'Herbívoro',
    'Anquilosáurido cubierto por una pesada armadura y con un gran mazo en la cola. Probablemente fue el mayor de su grupo y es el dinosaurio acorazado más representativo.',
    '/images/dinosaurios/ankylosaurus.png',
    'Emily Willoughby, CC BY-SA 3.0, vía Wikimedia Commons',
    'https://es.wikipedia.org/wiki/Ankylosaurus_magniventris'
);

INSERT INTO Dinosaurios (Nombre, Periodo, Antiguedad, Region, Dieta, Descripcion, ImagenUrl, CreditoImagen, FuenteUrl)
VALUES (
    'Parasaurolophus',
    'Cretácico Superior',
    '76–73 millones de años',
    'América del Norte',
    'Herbívoro',
    'Hadrosáurido que caminaba tanto en dos como en cuatro patas. Lo describió William Parks en 1922 a partir de un cráneo hallado en Alberta, Canadá.',
    '/images/dinosaurios/parasaurolophus.jpg',
    'Zissoudisctrucker, CC BY-SA 4.0, vía Wikimedia Commons',
    'https://es.wikipedia.org/wiki/Parasaurolophus'
);
