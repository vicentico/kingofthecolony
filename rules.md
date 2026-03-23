🎯 1. Objetivo del Proyecto
Construir una aplicación de escritorio en C# (.NET 10, preferentemente WPF) que actúe como un "Orquestador de Conexión". Su única función es conectar a dos jugadores remotamente mediante un socket TCP simple, acordar los parámetros de red y lanzar automáticamente el emulador FBNeo (con GGPO integrado) para jugar KOF 2002. La aplicación NO maneja el netcode del juego, solo el handshake inicial.

👤 2. Experiencia de Usuario (UX) y Mensajes para Principiantes
La aplicación será utilizada por jugadores sin conocimientos técnicos. Copilot debe implementar los siguientes mensajes exactos en la UI para guiar a los usuarios:

Pantalla Principal (Selección de Rol)
Botón Host: "Crear Partida (Host)"

Botón Cliente: "Unirse a Partida"

Flujo del Host (El que crea la sala)
Mensaje de instrucción: "Para crear una partida, asegúrate de haber abierto el puerto 6000 (UDP y TCP) en la configuración de tu router (Port Forwarding). Tu IP pública actual es: [Mostrar IP Pública]. Compártela con tu amigo."

Estado de espera: "Esperando a que el jugador 2 se conecte..."

Éxito: "¡Jugador conectado! Lanzando KOF 2002 en 3 segundos..."

Flujo del Cliente (El que se une)
Mensaje de instrucción: "Ingresa la dirección IP que te dio tu amigo para conectarte a su partida."

Input: [Caja de texto para IP] - [Botón: Conectar]

Estado: "Conectando con el Host..."

Error común: "No se pudo conectar. Verifica que la IP sea correcta y que tu amigo haya abierto el puerto 6000 en su router."

⚙️ 3. Arquitectura y Componentes Clave
La aplicación debe seguir los principios SOLID, separando la UI de la lógica de negocio.

UI Layer: Maneja los eventos de los botones y actualiza los mensajes en pantalla de forma asíncrona (Dispatcher).

Network Layer (INetworkManager): Maneja el TcpListener (Host) y el TcpClient (Cliente). Todo debe ser async/await para no bloquear la UI.

Launcher Layer (IEmulatorLauncher): Se encarga de construir los argumentos de línea de comandos (CLI) y ejecutar Process.Start().

🔄 4. Flujo de Ejecución y Protocolo (El Handshake)
El Host y el Cliente se comunican por TCP en el puerto 6000. Una vez conectados, intercambian un JSON simple para asegurar que ambos están listos antes de lanzar el ejecutable local.

Payload JSON de Sincronización:

JSON
{
"Type": "StartGame",
"HostIp": "190.x.x.x",
"UdpPort": 6000,
"GameRom": "kof2002",
"DelayFrames": 2
}
Secuencia estricta:

Host inicia TcpListener(6000).

Cliente hace TcpClient.ConnectAsync(HostIp, 6000).

Host detecta la conexión y envía el payload JSON al Cliente.

Cliente recibe el JSON, responde con un {"Type": "Ack"} (Acknowledge).

Ambos sistemas invocan simultáneamente la capa IEmulatorLauncher.

La conexión TCP se cierra (ya no es necesaria, el emulador usará UDP).

🚀 5. Especificación del Lanzador (Process Launcher)
Copilot debe generar una clase que lance el ejecutable asumiendo que fbneo.exe y la ROM kof2002.zip están en la misma carpeta o en un subdirectorio /emulator/.

Ejemplo de argumentos esperados para FBNeo con GGPO:

Para el Host: fbneo.exe kof2002 -net 6000 (Asumiendo que el comando local abre el puerto de escucha).

Para el Cliente: fbneo.exe kof2002 -net [IP_DEL_HOST]:6000
(Nota para Copilot: Implementar esto usando ProcessStartInfo con UseShellExecute = false).

📁 6. Estructura de Archivos Solicitada a Copilot
Generar la solución con la siguiente estructura de carpetas:

Plaintext
/GGPOLauncher
├── App.xaml (o Program.cs si es Forms/Console)
├── MainWindow.xaml (Contiene la UI simple dividida en dos paneles: Host / Join)
├── /Core
│ ├── /Interfaces
│ │ ├── INetworkManager.cs
│ │ └── IEmulatorLauncher.cs
│ ├── /Network
│ │ ├── TcpHost.cs
│ │ └── TcpClientManager.cs
│ ├── /Process
│ │ └── FbNeoLauncher.cs
│ └── /Models
│ └── HandshakePayload.cs
└── /Utils
└── IpHelper.cs (Para obtener la IP pública del Host)
🧠 7. Restricciones y Reglas de Código
C# 12 / .NET 10: Usar características modernas (Primary constructors, records para el JSON).

Manejo de Excepciones: Proveer bloques try/catch amigables en la capa de red. Si el Process.Start falla (porque no encuentra fbneo.exe), mostrar un MessageBox o alerta en la UI diciendo: "No se encontró el emulador. Asegúrate de que fbneo.exe esté en la misma carpeta."

No sobreingeniería: No usar Entity Framework ni bases de datos. Mantenerlo estrictamente funcional y en memoria.
