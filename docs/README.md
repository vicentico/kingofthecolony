# King of the Colony — GGPO Launcher

## ¿Qué es este proyecto?

**GGPO Launcher** es una aplicación de escritorio para Windows desarrollada en C# / WPF (.NET 10) que actúa como un **Orquestador de Conexión** entre dos jugadores remotos para jugar **KOF 2002** a través del emulador **FBNeo** con netcode **GGPO**.

La aplicación **no maneja el netcode del juego**. Su función es exclusivamente:

1. Conectar a dos jugadores mediante un socket TCP (puerto 6000).
2. Realizar un handshake (intercambio de parámetros).
3. Lanzar automáticamente el emulador FBNeo con los argumentos de red correctos.

---

## Documentación disponible

| Documento                                | Descripción                                                            |
| ---------------------------------------- | ---------------------------------------------------------------------- |
| [ARCHITECTURE.md](ARCHITECTURE.md)       | Arquitectura del proyecto, capas, clases y responsabilidades           |
| [SETUP-EMULATOR.md](SETUP-EMULATOR.md)   | Guía paso a paso para configurar FBNeo, ROMs y la carpeta del emulador |
| [NETWORK-FLOW.md](NETWORK-FLOW.md)       | Flujo completo de conexión, protocolo de handshake y lanzamiento       |
| [TROUBLESHOOTING.md](TROUBLESHOOTING.md) | Problemas comunes y soluciones paso a paso                             |

---

## Requisitos del sistema

- **Sistema Operativo:** Windows 10 / 11
- **Runtime:** .NET 10 (SDK para desarrollo, Runtime para ejecución)
- **Emulador:** FBNeo (`fbneo.exe`) con soporte GGPO
- **ROM:** `kof2002.zip` (romset compatible con FBNeo)
- **Red:** Puerto 6000 (TCP + UDP) abierto en el router del Host

---

## Inicio rápido

```bash
# Clonar el repositorio
git clone https://github.com/vicentico/kingofthecolony.git
cd kingofthecolony

# Compilar
dotnet build GGPOLauncher.sln

# Ejecutar
dotnet run --project GGPOLauncher
```

Antes de jugar, usa el botón **"Validar Requisitos del Sistema"** en la pantalla principal para verificar que todo esté correctamente configurado.

---

## Estructura del proyecto

```
/GGPOLauncher
├── App.xaml / App.xaml.cs           → Punto de entrada WPF
├── MainWindow.xaml / .xaml.cs       → Interfaz principal (3 paneles + diagnóstico)
├── /Core
│   ├── /Interfaces
│   │   ├── INetworkManager.cs       → Contrato de la capa de red
│   │   └── IEmulatorLauncher.cs     → Contrato del lanzador de emulador
│   ├── /Network
│   │   ├── TcpHost.cs               → Servidor TCP (Host)
│   │   └── TcpClientManager.cs      → Cliente TCP (Jugador 2)
│   ├── /Process
│   │   └── FbNeoLauncher.cs         → Lanza fbneo.exe con argumentos CLI
│   ├── /Diagnostics
│   │   └── EnvironmentValidator.cs  → Validador de requisitos del sistema
│   └── /Models
│       ├── HandshakePayload.cs      → Record del JSON de sincronización
│       └── DiagnosticResult.cs      → Record de resultado de validación
└── /Utils
    └── IpHelper.cs                  → Obtiene la IP pública del Host
```
