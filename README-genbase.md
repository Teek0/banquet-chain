# La cadena del banquete

Prototipo 2D de mecanografía hecho con Unity 6 y URP 2D. El juego transcurre
en una cocina fija: el jugador completa palabras para ejecutar acciones,
avanzar recetas y mantener en marcha el banquete.

El diseño de producto y el plan de implementación viven en `documentacion/`.
Esa carpeta es memoria local de trabajo y no se versiona.

## Estado actual

Está terminada la primera sección jugable de las tareas 01 a 05 del backlog:

- `Playground` ya no contiene personaje controlable, suelo de plataformas ni
  cámara de seguimiento.
- La escena conserva cámara fija, `GameplayCanvas`, `PauseUI`, `EventSystem` y
  arranque directo mediante `_Bootstrap`.
- La pausa ya no depende de `PlayerInput` ni de la presencia de `Player2D`.
- Existe una normalización común para comparar palabras sin distinguir
  mayúsculas, tildes ni diéresis.
- `TypingInput` procesa una sola palabra activa, emite eventos de acierto,
  error, progreso y finalización, admite Backspace y puede bloquear la entrada.
- `WordBubbleUI` presenta la palabra activa, distingue visualmente el progreso,
  informa errores y confirma la finalización sin reconstruirse cada frame.
- Hay 15 casos Edit Mode para la normalización y el motor de escritura.

La burbuja ya está integrada en `Playground` con la palabra de demostración
`mantequilla`. El arte definitivo de la cocina, las recetas y los actores se
incorporará en las siguientes tareas.

## Flujo de escenas

La aplicación comienza en `Boot`, crea el `AppRoot` persistente y carga
`MainMenu`. Desde el menú se entra a `Playground` o `Credits`.

Orden del Build Profile:

1. `Boot`
2. `MainMenu`
3. `Playground`
4. `Credits`

`AppRootBootstrapper` permite iniciar Play directamente desde cualquier escena
navegable sin duplicar los servicios persistentes.

## Reglas de escritura implementadas

- Solo se consideran letras.
- Mayúsculas y minúsculas son equivalentes.
- Las vocales con o sin tilde son equivalentes.
- `ü` y `u` son equivalentes.
- Una letra incorrecta no avanza.
- Backspace retrocede sin producir índices negativos.
- No hace falta pulsar Enter.
- Cada palabra se completa una sola vez.
- La entrada puede deshabilitarse durante pausa, transiciones o animaciones.

El texto original se conserva para mostrar correctamente palabras como
`freír`, `limón` o `azúcar`.

## Controles actuales

| Acción | Control |
| --- | --- |
| Escribir | Teclado |
| Corregir progreso | Backspace |
| Pausa | Escape o Start en gamepad |
| Navegar menús | Mouse, teclado o gamepad |

## Estructura relevante

- `Assets/_Project/Scenes/Playground.unity`: escena jugable y cocina fija.
- `Assets/_Project/Scripts/Gameplay/Typing/`: normalizador y entrada de texto.
- `Assets/_Project/Scripts/UI/Gameplay/WordBubbleUI.cs`: presentación y feedback
  de la palabra activa.
- `Assets/_Project/Tests/EditMode/`: pruebas automáticas de mecanografía.
- `Assets/_Project/Prefabs/PauseUI.prefab`: overlay de pausa compartido.
- `Assets/_Project/Input/GameControls2D.inputactions`: acciones base y pausa.

Los scripts y prefabs heredados de plataformas se conservan como referencia,
pero `Player2D`, `PlayerMotor2D` y `CameraFollow2D` ya no forman parte de la
composición de `Playground`.

## Cómo probar

1. Abre el proyecto con Unity `6000.3.19f1`.
2. Inicia Play desde `Boot` y comprueba `MainMenu → Playground`.
3. Inicia Play directamente desde `Playground` y confirma que aparece un solo
   `DontDestroyOnLoad/AppRoot` y que la escena se revela tras el fundido.
4. Escribe `mantequilla`: una letra incorrecta no debe avanzar, Backspace debe
   retroceder y la palabra completa debe mostrar `OK · LISTO`.
5. En `Window → General → Test Runner`, abre `EditMode` y ejecuta todas las
   pruebas de `BanquetChain.Typing.Tests`.
6. Comprueba la pausa en `Playground` con Escape y, si está disponible, Start.

La compilación WebGL y la captura de texto con foco real en navegador se
abordarán en la tarea 06.

## Convenciones

- Assets propios dentro de `Assets/_Project`.
- No se agregan escenas, paquetes o singletons sin una necesidad del backlog.
- Los cambios de escena y prefab deben preservar referencias serializadas.
- `documentacion/` contiene el backlog, GDD copiado y memoria privada del
  proyecto; no reemplaza este README público.
