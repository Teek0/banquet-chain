# La cadena del banquete

Prototipo 2D de mecanografía hecho con Unity 6 y URP 2D. El juego transcurre
en una cocina fija: el jugador completa palabras para ejecutar acciones,
avanzar recetas y mantener en marcha el banquete.

El GDD vive en la carpeta externa de diseño de Takernal Jam. El plan de
implementación y la memoria operativa viven en `documentacion/`; esa carpeta
es local y no se versiona.

## Estado actual

Están terminados los bloques 01 a 05 y 07 a 20 del backlog. Ya existe una
partida completa provisional dentro del Editor:

- `Playground` conserva cámara fija, HUD, pausa y arranque mediante `_Bootstrap`.
- `TypingInput` procesa una palabra activa, normaliza mayúsculas y tildes,
  admite Backspace y emite eventos de progreso, error y finalización.
- `WordBubbleUI` presenta la palabra activa y su feedback sin consultar el
  estado cada frame.
- Existen tres recetas editables: Pan caliente, Sopa de verduras y El Plato del
  Pueblo.
- `RecipeRunner` recorre una receta como máquina de estados, bloquea la entrada
  mientras se ejecuta cada acción y permite reiniciar sin recargar la escena.
- `GameFlow` recorre Pan caliente, Sopa de verduras y El Plato del Pueblo en
  ese orden, sin intervención desde la consola o el Inspector.
- El HUD superior muestra plato, pedido, estado, paso activo, pasos posteriores
  atenuados y la indicación `ESC · PAUSA` sin consultar datos cada frame.
- `Despensa`, `Horno` y `Servicio` se resaltan según el paso activo, anticipan
  con el progreso escrito, reaccionan al completar la palabra y celebran el
  plato terminado.
- El plato central agrega capas provisionales después de los pasos que lo
  transforman, distingue el estado listo y se desplaza al completar `servir`.
- El gato presenta cada pedido, reacciona al recibir un plato y aumenta su
  satisfacción hasta el ronroneo final.
- Tras el tercer plato aparece el cierre de partida y se continúa a `Credits`
  mediante el cargador de escenas existente.
- Las recetas largas usan explícitamente `Despensa`, `Horno` y `Servicio`; la
  palabra `compartir` hace reaccionar a toda la cocina antes de la entrega.
- Pan caliente incluye un tutorial contextual que explica la palabra activa,
  letras correctas, error, Backspace y la reacción de la cocina; se oculta al
  terminar y vuelve a aparecer al reiniciar.
- La pausa suspende la mecanografía conservando el progreso, la restaura al
  continuar y muestra una indicación para recuperar el foco en WebGL.
- `GameplayAudio` distingue acierto, error, palabra, acción, plato, reacción
  del gato y ronroneo; usa `Music`/`SFX` del mixer y fallbacks sustituibles.
- El menú explica la premisa y controles; el final comunica la contribución de
  todo el pueblo y `Credits` contiene campos editables de autoría y licencias.
- Las tres escenas usan referencia 960 × 600; la palabra activa se autoajusta,
  los estados no dependen solo del color y las reacciones evitan movimiento
  excesivo.
- La pasada integral cubre errores, Backspace, teclas ignoradas, pausa,
  reinicios intermedios, tres recetas, final y referencias críticas.
- Hay 47/47 pruebas Edit Mode y 24/24 pruebas Play Mode aprobadas.

No hay defectos bloqueantes confirmados dentro del Editor. El siguiente y
último bloque es T21, la build candidata; queda sin ejecutar hasta recibir una
solicitud explícita.

## Flujo de escenas

La aplicación comienza en `Boot`, crea el `AppRoot` persistente y carga
`MainMenu`. Desde el menú se entra a `Playground` o `Credits`.

Orden configurado en el Build Profile:

1. `Boot`
2. `MainMenu`
3. `Playground`
4. `Credits`

`AppRootBootstrapper` permite iniciar Play directamente desde cualquier escena
navegable sin duplicar los servicios persistentes.

## Flujo jugable disponible

```text
Presentar pedido
  → habilitar palabra
  → escribir correctamente
  → bloquear entrada durante la acción
  → avanzar al siguiente paso
  → servir
  → reacción del gato
  → siguiente receta (tres en total)
  → celebración final
  → Credits
```

Estados de `RecipeRunner`: `PresentingOrder`, `AwaitingInput`,
`ExecutingAction`, `ServingDish` y `RecipeCompleted`.

## Reglas de escritura

- Solo se consideran letras.
- Mayúsculas y minúsculas son equivalentes.
- Las vocales con o sin tilde son equivalentes.
- `ü` y `u` son equivalentes.
- Una letra incorrecta no avanza.
- Backspace retrocede sin producir índices negativos.
- No hace falta pulsar Enter.
- Cada palabra y cada paso se completan una sola vez.
- La entrada se deshabilita durante acciones, pausas y transiciones.

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
- `Assets/_Project/Scripts/Gameplay/Recipes/`: datos, pasos y `RecipeRunner`.
- `Assets/_Project/Scripts/Gameplay/Presentation/`: actores visuales y
  coordinación de actores, plato y gato por eventos.
- `Assets/_Project/Scripts/Gameplay/Flow/`: coordinación de las tres recetas,
  tutorial, audio, celebraciones y cierre de partida.
- `Assets/_Project/Data/Recipes/`: los tres platos editables desde el Inspector.
- `Assets/_Project/Scripts/UI/Gameplay/WordBubbleUI.cs`: palabra y feedback.
- `Assets/_Project/Scripts/UI/Gameplay/RecipeHUDUI.cs`: pedido y progreso por
  eventos del corredor.
- `Assets/_Project/Tests/EditMode/`: 47 pruebas de escritura, datos, HUD y
  escenas de presentación.
- `Assets/_Project/Tests/PlayMode/`: 24 pruebas del ciclo, actores, plato, gato
  y flujo completo.
- `Assets/_Project/Prefabs/PauseUI.prefab`: overlay de pausa compartido.

Los scripts y prefabs heredados de plataformas se conservan como referencia,
pero `Player2D`, `PlayerMotor2D` y `CameraFollow2D` no forman parte de la
composición actual de `Playground`.

## Cómo probar

1. Abre el proyecto con Unity `6000.3.19f1`.
2. Abre `Assets/_Project/Scenes/Playground.unity` y pulsa Play.
3. Completa las tres recetas en orden: Pan caliente (4 palabras), Sopa de
   verduras (7 palabras) y El Plato del Pueblo (13 palabras).
4. Comprueba que una letra incorrecta no avance y que Backspace retroceda.
5. Entre palabras, comprueba que la entrada quede bloqueada durante la espera
   configurada y luego aparezca el siguiente paso.
6. Comprueba que el HUD marque el paso activo, atenúe los posteriores y muestre
   los completados con una marca.
7. Comprueba que `pan` y `mantequilla` señalen `Despensa`, `tostar` señale
   `Horno` y `servir` señale `Servicio`; cada mesa debe anticipar al escribir y
   rebotar al completar su palabra.
8. Comprueba que el plato cambie con `pan`, `mantequilla` y `tostar`; al
   completar `servir` debe mostrar `ENTREGANDO...` y desplazarse a la derecha.
9. Tras cada `servir`, confirma la reacción del gato y el comienzo automático
   del pedido siguiente; su satisfacción debe avanzar de 0 a 3.
10. Tras el tercer plato, confirma el mensaje final, el ronroneo y la
    transición automática a `Credits`.
11. En `Window → General → Test Runner`, ejecuta las 47 pruebas de `EditMode` y
    las 24 pruebas de `PlayMode`.
12. Opcionalmente, inicia desde `Boot` y recorre
    `MainMenu → Playground → Credits → MainMenu`.

La compilación WebGL está pospuesta. El juego ya está completo y validado dentro
del Editor, pero T21 no se ejecutará sin una solicitud explícita de build.

## Convenciones

- Assets propios dentro de `Assets/_Project`.
- No se agregan escenas, paquetes o singletons sin una necesidad del backlog.
- Los cambios de escena y prefab deben preservar referencias serializadas.
- Los elementos visuales actuales son placeholders reemplazables por los assets
  definitivos sin cambiar la lógica de receta.
- `documentacion/` contiene el backlog, el prompt de trabajo y memoria privada
  del proyecto; el GDD permanece en la carpeta externa de diseño.
