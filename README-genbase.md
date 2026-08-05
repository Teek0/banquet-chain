# Base genérica de plataformas 2D

Proyecto base en Unity 6 con URP 2D, Input System, navegación de escenas,
menú de pausa, ajustes de audio y un controlador de plataformas configurable.

## Flujo de escenas

La compilación comienza en `Boot`, que crea el `AppRoot` persistente y carga
`MainMenu`. Desde allí se puede entrar a `Playground` o `Credits`.

Orden actual en Build Profiles:

1. `Boot`
2. `MainMenu`
3. `Playground`
4. `Credits`

## Estado actual

La configuración base está completa:

- `Boot` utiliza una instancia vinculada de `AppRoot.prefab`.
- `MainMenu`, `Playground` y `Credits` contienen un `_Bootstrap` configurado.
- Es posible iniciar Play desde cualquiera de esas escenas.
- El perfil de compilación comienza en `Boot` y no contiene escenas obsoletas.
- URP 2D, Physics2D, Input System y el mezclador de audio están configurados.

## Arquitectura de arranque

`AppRoot` contiene los servicios persistentes de navegación, audio y fundido
de pantalla. Al comenzar desde `Boot`, se conserva mediante
`DontDestroyOnLoad` durante el resto de la sesión.

`AppRootBootstrapper` permite trabajar directamente en escenas individuales.
Si todavía no existe un `AppRoot`, instancia `AppRoot.prefab`; durante el flujo
normal desde `Boot` no crea duplicados.

Al crear una nueva escena navegable:

1. Añade un GameObject raíz llamado `_Bootstrap`.
2. Añade el componente `AppRootBootstrapper`.
3. Asigna `Assets/_Project/Prefabs/AppRoot` en `App Root Prefab`.
4. Agrega la escena al Build Profile después de `Boot`.

## Controles

| Acción | Teclado | Gamepad |
| --- | --- | --- |
| Mover | A/D o flechas | Stick izquierdo |
| Saltar | Espacio | Botón sur |
| Pausa | Escape | Start |

Las acciones están en
`Assets/_Project/Input/GameControls2D.inputactions`. `PauseMenu` obtiene la
acción `Gameplay/Pause` desde el `PlayerInput` de la escena.

## Ajuste del jugador

Los valores principales viven en `Player2D.prefab`, componente
`PlayerMotor2D`:

- `Movement Speed`: velocidad horizontal máxima.
- `Ground Acceleration` y `Ground Deceleration`: respuesta sobre el suelo.
- `Air Acceleration` y `Air Deceleration`: control aéreo.
- `Jump Speed`: impulso vertical.
- `Coyote Time`: tolerancia para saltar después de abandonar una plataforma.
- `Jump Buffer Time`: tolerancia para pulsar salto antes de tocar el suelo.
- `Jump Cut Multiplier`: altura reducida al soltar el botón.
- `Max Fall Speed`: límite de caída.

El suelo debe usar la capa `Ground`. El `Ground Check` del prefab debe quedar
ligeramente bajo el `CapsuleCollider2D`.

## Validación manual recomendada

1. Inicia Play desde `Boot` y confirma la transición a `MainMenu`.
2. Abre ajustes y comprueba los tres buses del mezclador.
3. Entra a `Playground` con teclado y gamepad.
4. Prueba salto corto, salto largo, coyote time y jump buffer.
5. Pausa con Escape y Start; prueba continuar, reiniciar y volver al menú.
6. Inicia Play directamente desde `Playground` y confirma que `_Bootstrap`
   crea los servicios sin errores.
7. Repite la entrada directa desde `MainMenu` y `Credits`.

## Convenciones

- Assets propios dentro de `Assets/_Project`.
- Escenas de juego dentro de `Assets/_Project/Scenes`.
- Prefabs compartidos dentro de `Assets/_Project/Prefabs`.
- No versionar `Library`, `Temp`, `Logs`, `UserSettings` ni archivos de
  solución generados por Unity.
