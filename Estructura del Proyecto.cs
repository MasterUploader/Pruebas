RestUtilities.QueryBuilder/
│
├── Attributes/            # Atributos personalizados (como [SqlColumn], [SqlIgnore])
│
├── Builders/              # Construcción de queries SQL
│   ├── Select/
│   ├── Insert/
│   ├── Update/
│   ├── Delete/
│   └── Common/            # Componentes comunes reutilizados por todos los builders
│
├── Compatibility/         # Verifica soporte por motor SQL (ej. EXISTS no disponible en AS400)
│
├── DbContextSupport/      # Integración opcional con Entity Framework (FromSqlRaw, etc.)
│
├── Enums/                 # Enumeraciones como operadores, tipos de join, tipo de motor SQL
│
├── Extensions/            # Métodos de extensión para facilitar la fluidez del API
│
├── Interfaces/            # Contratos públicos para desacoplar lógica interna
│
├── Metadata/              # Extracción de metadatos desde clases y atributos (nombres, tipos, tamaños)
│
├── Models/                # Estructuras para filtros, condiciones, joins, subqueries, etc.
│
├── Translators/           # Traduce expresiones y queries según el motor (AS400, Oracle, etc.)
│   ├── AS400/
│   ├── SqlServer/
│   ├── Oracle/
│   ├── PostgreSql/
│   └── MySql/
│
├── Utilities/             # Clases auxiliares (ej: conversores, generadores de alias, formateadores)
│
├── Validators/            # Validaciones de tipos, longitudes, nulls, seguridad (inyección SQL)
│
├── RestUtilities.QueryBuilder.csproj
└── README.md              # Documentación del paquete
