📦 RestUtilities.Connections/     # 📌 Paquete de conexiones
 ├── 📂 Connections/              # 🔹 Módulo principal de conexiones
 │   ├── 📂 Interfaces/           # 📌 Interfaces para cada tipo de conexión
 │   │   ├── IDatabaseConnection.cs
 │   │   ├── IExternalServiceConnection.cs
 │   │   ├── IConnectionManager.cs
 │   │   ├── IWebSocketConnection.cs
 │   │   ├── IGrpcConnection.cs
 │   │   ├── IFtpConnection.cs
 │   │   └── IMessageQueueConnection.cs
 │   │
 │   ├── 📂 Providers/            # 📌 Implementaciones de conexión
 │   │   ├── Database/
 │   │   │   ├── AS400ConnectionProvider.cs
 │   │   │   ├── MSSQLConnectionProvider.cs
 │   │   │   ├── OracleConnectionProvider.cs
 │   │   │   ├── MySQLConnectionProvider.cs
 │   │   │   ├── PostgreSQLConnectionProvider.cs
 │   │   │   ├── MongoDBConnectionProvider.cs
 │   │   │   ├── RedisConnectionProvider.cs
 │   │   │   └── DatabaseConnectionFactory.cs
 │   │   │
 │   │   ├── Services/
 │   │   │   ├── RestServiceClient.cs
 │   │   │   ├── SoapServiceClient.cs
 │   │   │   ├── WebSocketConnectionProvider.cs
 │   │   │   ├── GrpcConnectionProvider.cs
 │   │   │   ├── RabbitMQConnectionProvider.cs
 │   │   │   ├── FtpConnectionProvider.cs
 │   │   │   └── ServiceConnectionFactory.cs
 │   │
 │   ├── 📂 Managers/             # 📌 Administradores de conexiones
 │   │   ├── ConnectionManager.cs
 │   │   ├── DatabaseManager.cs
 │   │   ├── ServiceManager.cs
 │   │   ├── WebSocketManager.cs
 │   │   └── GrpcManager.cs
 │   │
 │   ├── 📂 Models/               # 📌 Modelos de conexión
 │   │   ├── ConnectionInfo.cs
 │   │   ├── DatabaseSettings.cs
 │   │   ├── ServiceSettings.cs
 │   │   ├── WebSocketSettings.cs
 │   │   ├── GrpcSettings.cs
 │   │   └── RedisSettings.cs
 │   │
 │   ├── ConnectionSettings.cs    # 📌 Clase para manejar configuraciones de conexión
 │   ├── RestUtilities.Connections.csproj  # 📌 Archivo de configuración del paquete
 │   └── README.md                # 📌 Documentación específica del paquete
