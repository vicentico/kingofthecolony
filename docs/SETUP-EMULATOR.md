# Configuración del Emulador FBNeo

## ¿Qué es FBNeo?

**FinalBurn Neo (FBNeo)** es un emulador multi-arcade que soporta miles de ROMs, incluyendo las de Neo-Geo como KOF 2002. La versión estándar de FBNeo incluye soporte nativo para **GGPO** — un sistema de netcode basado en rollback que proporciona una experiencia online fluida.

---

## Requisitos previos

| Requisito  | Detalle                                         |
| ---------- | ----------------------------------------------- |
| Ejecutable | `fbneo.exe` (versión estándar con soporte GGPO) |
| ROM        | `kof2002.zip` (romset compatible con FBNeo)     |
| Sistema    | Windows 10/11 (64-bit recomendado)              |

---

## Paso 1: Obtener fbneo.exe

### Opción A: Desde GitHub Releases

1. Descarga FBNeo desde: https://github.com/finalburnneo/FBNeo/releases
2. Extrae el archivo descargado.
3. Copia `fbneo.exe` (o `fbneo64.exe` en sistemas de 64 bits) a la carpeta del proyecto.

### Opción B: Compilación desde fuente

1. Clona el repositorio de FBNeo: `https://github.com/finalburnneo/FBNeo`
2. Compila siguiendo las instrucciones del repositorio.
3. El ejecutable resultante debe llamarse `fbneo.exe`.

---

## Paso 2: Colocar el ejecutable

El Launcher busca `fbneo.exe` en dos ubicaciones (en este orden):

```
📂 Carpeta de la aplicación (donde está GGPOLauncher.exe)
├── fbneo.exe          ← Opción 1: junto al launcher
└── 📂 emulator/
    └── fbneo.exe      ← Opción 2: subcarpeta emulator
```

**Estructura recomendada:**

```
📂 KingOfTheColony/
├── GGPOLauncher.exe
├── 📂 emulator/
│   ├── fbneo.exe
│   ├── 📂 roms/
│   │   └── kof2002.zip
│   └── (otros archivos de FBNeo: config, savestates, etc.)
```

---

## Paso 3: Colocar la ROM

La ROM `kof2002.zip` se busca en estas ubicaciones:

| Prioridad | Ruta                                       |
| --------- | ------------------------------------------ |
| 1         | `{AppDirectory}/kof2002.zip`               |
| 2         | `{AppDirectory}/emulator/kof2002.zip`      |
| 3         | `{AppDirectory}/roms/kof2002.zip`          |
| 4         | `{AppDirectory}/emulator/roms/kof2002.zip` |

> **Importante:** El archivo debe llamarse exactamente `kof2002.zip`. No lo renombres ni lo descomprimas.

---

## Paso 4: Verificar compatibilidad de la ROM

No todas las ROMs de KOF 2002 son iguales. FBNeo requiere un romset específico.

### ¿Cómo saber si la ROM es correcta?

1. Abre una terminal en la carpeta donde está `fbneo.exe`.
2. Ejecuta:
   ```
   fbneo.exe -listinfo kof2002
   ```
3. Si muestra información del juego, la ROM es compatible.
4. Si da error, necesitas buscar la versión correcta del romset.

### Romset esperado

- **Nombre:** `kof2002`
- **Sistema:** Neo-Geo (MVS/AES)
- **Formato:** ZIP sin comprimir internamente (los archivos dentro del ZIP deben estar sin comprimir o con compresión estándar)

---

## Argumentos CLI de FBNeo para GGPO

El Launcher construye y ejecuta automáticamente los comandos. Para referencia:

### Host (Jugador 1 — el que crea la partida)

```bash
fbneo.exe kof2002 -net 6000
```

| Argumento   | Significado                                             |
| ----------- | ------------------------------------------------------- |
| `kof2002`   | Nombre de la ROM a cargar                               |
| `-net 6000` | Abre el puerto 6000 para escuchar conexiones GGPO (UDP) |

### Cliente (Jugador 2 — el que se une)

```bash
fbneo.exe kof2002 -net 190.x.x.x:6000
```

| Argumento             | Significado                                    |
| --------------------- | ---------------------------------------------- |
| `kof2002`             | Nombre de la ROM a cargar                      |
| `-net 190.x.x.x:6000` | Se conecta al Host en la IP y puerto indicados |

---

## Paso 5: Configuración de GGPO dentro de FBNeo

Una vez que el emulador se lanza desde el Launcher, GGPO se activa automáticamente. Sin embargo, puedes ajustar algunos parámetros desde el menú del emulador:

| Parámetro         | Valor por defecto | Descripción                                                                                                                                                                   |
| ----------------- | ----------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Delay Frames**  | 2                 | Frames de delay para la sincronización. Valores más altos = más estable pero con más input lag. Para conexiones locales/cercanas, 1-2 es ideal. Para conexiones lejanas, 3-4. |
| **Input Display** | Off               | Muestra los inputs en pantalla (útil para debug)                                                                                                                              |

### Delay Frames recomendados

| Distancia entre jugadores | Delay recomendado |
| ------------------------- | ----------------- |
| Misma ciudad / país       | 1-2               |
| Mismo continente          | 2-3               |
| Inter-continental         | 3-4               |

---

## Paso 6: Configuración del router (Port Forwarding)

> **Solo necesario para el Host (Jugador 1).**

El Host debe abrir el **puerto 6000** (TCP y UDP) en su router para que el Cliente pueda conectarse.

### Instrucciones generales

1. Averigua la **IP local** de tu PC:

   ```powershell
   ipconfig
   ```

   Busca la línea **"Dirección IPv4"** (ej: `192.168.1.100`).

2. Accede a la configuración de tu router:
   - Abre un navegador y ve a `http://192.168.1.1` (o la puerta de enlace que indique `ipconfig`).
   - Inicia sesión con las credenciales de tu router.

3. Busca la sección de **Port Forwarding** (puede estar en "Avanzado", "NAT", "Virtual Server" o "Reenvío de puertos").

4. Crea **dos reglas**:

   | Nombre      | Protocolo | Puerto externo | Puerto interno | IP interna    |
   | ----------- | --------- | -------------- | -------------- | ------------- |
   | KOF2002-TCP | TCP       | 6000           | 6000           | 192.168.1.100 |
   | KOF2002-UDP | UDP       | 6000           | 6000           | 192.168.1.100 |

5. Guarda los cambios y reinicia el router si es necesario.

### Marcas comunes de router

| Marca                   | Ruta típica al Port Forwarding                      |
| ----------------------- | --------------------------------------------------- |
| TP-Link                 | Avanzado → NAT Forwarding → Virtual Servers         |
| Netgear                 | Avanzado → Configuración avanzada → Port Forwarding |
| Linksys                 | Aplicaciones y juegos → Port Range Forwarding       |
| Huawei (Movistar/Claro) | Avanzado → NAT → Port Mapping                       |
| ZTE (Compañías ISP)     | Aplicación → Port Forwarding                        |

---

## Verificación rápida

Usa el botón **"Validar Requisitos del Sistema"** en la pantalla principal del Launcher. Este ejecuta automáticamente las siguientes comprobaciones:

- ✅ `fbneo.exe` encontrado
- ✅ `kof2002.zip` encontrada
- ✅ Puerto 6000 disponible
- ✅ Reglas de firewall configuradas
- ✅ Conexión a Internet activa

Si alguna falla, la aplicación muestra las instrucciones exactas para solucionarlo.
