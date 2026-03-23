# Solución de Problemas (Troubleshooting)

## Índice rápido

| Problema                            | Ir a                                                                             |
| ----------------------------------- | -------------------------------------------------------------------------------- |
| No se encontró el emulador          | [#1 Emulador no encontrado](#1-emulador-no-encontrado)                           |
| No se encontró la ROM               | [#2 ROM no encontrada](#2-rom-no-encontrada)                                     |
| Puerto 6000 en uso                  | [#3 Puerto en uso](#3-puerto-6000-en-uso)                                        |
| No se pudo conectar al Host         | [#4 Conexión rechazada](#4-no-se-puede-conectar-al-host)                         |
| Firewall bloqueando                 | [#5 Firewall](#5-firewall-de-windows-bloquea-la-conexión)                        |
| Sin Internet / IP no detectada      | [#6 Sin Internet](#6-sin-conexión-a-internet)                                    |
| El emulador se abre pero no conecta | [#7 Emulador no conecta](#7-el-emulador-se-abre-pero-no-conecta-al-otro-jugador) |
| Error al compilar el proyecto       | [#8 Errores de compilación](#8-errores-de-compilación)                           |

---

## 1. Emulador no encontrado

**Mensaje:** _"No se encontró el emulador. Asegúrate de que fbneo.exe esté en la misma carpeta."_

### Causa

La aplicación no encontró `fbneo.exe` en ninguna de las rutas esperadas.

### Solución

1. Localiza o descarga `fbneo.exe`:
   - Descarga FBNeo desde: https://github.com/finalburnneo/FBNeo/releases
   - Extrae el archivo descargado.

2. Copia `fbneo.exe` en **una** de estas ubicaciones:

   ```
   {CarpetaDeLaApp}\fbneo.exe
   {CarpetaDeLaApp}\emulator\fbneo.exe
   ```

3. Ejecuta **"Validar Requisitos del Sistema"** para confirmar.

---

## 2. ROM no encontrada

**Mensaje:** _"No se encontró kof2002.zip."_

### Causa

El archivo `kof2002.zip` no está en ninguna ruta esperada.

### Solución

1. Consigue `kof2002.zip` (romset compatible con FBNeo, no MAME).

2. Coloca el archivo en **una** de estas ubicaciones:

   ```
   {CarpetaDeLaApp}\kof2002.zip
   {CarpetaDeLaApp}\emulator\kof2002.zip
   {CarpetaDeLaApp}\roms\kof2002.zip
   {CarpetaDeLaApp}\emulator\roms\kof2002.zip
   ```

3. **No descomprimas** el archivo. Debe ser `kof2002.zip` tal cual.

4. **No lo renombres.** El nombre debe ser exactamente `kof2002.zip`.

---

## 3. Puerto 6000 en uso

**Mensaje:** _"El puerto 6000 ya está en uso por otro programa."_

### Causa

Otro programa (o una instancia previa del Launcher) ya está usando el puerto 6000.

### Solución

1. Abre **PowerShell como Administrador**.

2. Identifica qué programa usa el puerto:

   ```powershell
   netstat -aon | findstr :6000
   ```

   Resultado ejemplo:

   ```
   TCP  0.0.0.0:6000  0.0.0.0:0  LISTENING  12345
   ```

   El número `12345` es el PID del proceso.

3. Identifica el proceso:

   ```powershell
   Get-Process -Id 12345
   ```

4. Ciérralo:

   ```powershell
   Stop-Process -Id 12345 -Force
   ```

5. Si no puedes cerrarlo, **reinicia el equipo**.

6. Vuelve a ejecutar la validación.

---

## 4. No se puede conectar al Host

**Mensaje:** _"No se pudo conectar. Verifica que la IP sea correcta y que tu amigo haya abierto el puerto 6000 en su router."_

### Causas posibles

| Causa                       | Solución                                                                                                                              |
| --------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| IP incorrecta               | Pide de nuevo la IP al Host. Debe ser la IP **pública**, no la local (192.168.x.x).                                                   |
| Host no inició el servidor  | El Host debe hacer clic en "Iniciar Host" **antes** de que el Cliente intente conectar.                                               |
| Puerto no abierto en router | El Host debe configurar Port Forwarding (ver [SETUP-EMULATOR.md](SETUP-EMULATOR.md#paso-6-configuración-del-router-port-forwarding)). |
| Firewall bloqueando         | Ver [sección #5](#5-firewall-de-windows-bloquea-la-conexión).                                                                         |
| ISP bloqueando puertos      | Algunos ISP bloquean puertos. Contacta a tu proveedor o usa un puerto alternativo.                                                    |

### Verificación rápida para el Host

El Host puede verificar si su puerto está abierto visitando:

- https://www.canyouseeme.org/ (ingresa el puerto `6000`)
- Esto debe hacerse **después** de hacer clic en "Iniciar Host".

---

## 5. Firewall de Windows bloquea la conexión

**Mensaje:** _"No se encontró una regla de firewall para el puerto 6000."_

### Solución rápida (PowerShell como Administrador)

```powershell
# Regla TCP
netsh advfirewall firewall add rule name="KOF2002 GGPO TCP" dir=in action=allow protocol=TCP localport=6000

# Regla UDP
netsh advfirewall firewall add rule name="KOF2002 GGPO UDP" dir=in action=allow protocol=UDP localport=6000
```

### Solución manual (Panel de Control)

1. Abre **Panel de Control** → **Sistema y seguridad** → **Firewall de Windows Defender**.
2. Clic en **"Configuración avanzada"** (panel izquierdo).
3. Clic en **"Reglas de entrada"** → **"Nueva regla..."**.
4. Selecciona **"Puerto"** → Siguiente.
5. Selecciona **"TCP"**, escribe **6000** → Siguiente.
6. Selecciona **"Permitir la conexión"** → Siguiente → Siguiente.
7. Nombre: **"KOF2002 GGPO TCP"** → Finalizar.
8. **Repite** los pasos 3-7 pero selecciona **"UDP"** en el paso 5.

### Verificar que las reglas existen

```powershell
netsh advfirewall firewall show rule name="KOF2002 GGPO TCP"
netsh advfirewall firewall show rule name="KOF2002 GGPO UDP"
```

### Eliminar las reglas (si ya no las necesitas)

```powershell
netsh advfirewall firewall delete rule name="KOF2002 GGPO TCP"
netsh advfirewall firewall delete rule name="KOF2002 GGPO UDP"
```

---

## 6. Sin conexión a Internet

**Mensaje:** _"No se pudo obtener la IP pública."_

### Solución

1. Verifica que tu equipo tenga conexión a Internet (abre un navegador).
2. Visita https://api.ipify.org — debería mostrar tu IP.
3. Si usas **VPN**, desconéctala temporalmente.
4. Revisa la configuración de tu adaptador de red:
   ```powershell
   ipconfig /all
   ```
5. Si nada funciona, **reinicia tu router** y espera 30 segundos.

> **Nota:** La IP pública solo es necesaria para el **Host**. El Cliente no necesita conocer su propia IP pública.

---

## 7. El emulador se abre pero no conecta al otro jugador

### Causas posibles

| Causa                      | Solución                                                                               |
| -------------------------- | -------------------------------------------------------------------------------------- |
| Puerto UDP bloqueado       | Asegúrate de haber abierto el puerto 6000 **UDP** (no solo TCP).                       |
| ROM incompatible           | Ambos jugadores deben tener **exactamente la misma ROM** (`kof2002.zip`).              |
| Versión de FBNeo diferente | Ambos jugadores deben usar la **misma versión** de `fbneo.exe`.                        |
| NAT estricto               | Algunos routers tienen NAT tipo "estricto". Intenta habilitar DMZ o UPnP en tu router. |

### Verificar que el UDP está pasando

Desde el equipo del **Cliente**, ejecuta:

```powershell
Test-NetConnection -ComputerName {IP_DEL_HOST} -Port 6000
```

Si `TcpTestSucceeded` es `True`, el TCP funciona. Para UDP no hay un test directo en PowerShell, pero si el TCP funciona y el firewall tiene ambas reglas, el UDP debería funcionar.

---

## 8. Errores de compilación

### "El tipo 'FileNotFoundException' no se encontró"

Falta el `using`:

```csharp
using System.IO;
```

### "No se pudo copiar apphost.exe" (archivo bloqueado)

El ejecutable anterior está corriendo. Ciérralo:

```powershell
Stop-Process -Name GGPOLauncher -Force
```

Luego recompila:

```powershell
dotnet build GGPOLauncher.sln
```

### No se encuentra el SDK de .NET 10

Descarga el SDK de .NET 10 desde: https://dotnet.microsoft.com/download/dotnet/10.0

Verifica la instalación:

```powershell
dotnet --version
```

---

## Checklist pre-partida

Usa esta lista para verificar que todo está listo antes de jugar:

- [ ] `fbneo.exe` está en la carpeta correcta
- [ ] `kof2002.zip` está en la carpeta correcta
- [ ] Ambos jugadores tienen la **misma versión** de `fbneo.exe`
- [ ] Ambos jugadores tienen la **misma ROM** `kof2002.zip`
- [ ] El **Host** abrió el puerto 6000 (TCP + UDP) en su router
- [ ] El **Host** agregó reglas de firewall para el puerto 6000
- [ ] El **Host** compartió su IP pública con el Cliente
- [ ] El botón **"Validar Requisitos del Sistema"** muestra todo en verde ✅
