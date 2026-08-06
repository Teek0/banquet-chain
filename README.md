# La cadena del banquete

Prototipo 2D de mecanografía hecho con Unity 6 y URP 2D. El juego transcurre
en una cocina fija: el jugador completa palabras para ejecutar acciones,
avanzar recetas y mantener en marcha el banquete.

El diseño de producto y el plan de implementación viven en `documentacion/`.
Esa carpeta es memoria local de trabajo y no se versiona.

## Estado actual

Están terminados los bloques 01 a 05 y 07 a 10 del backlog. Ya existe el primer
bucle jugable provisional dentro del Editor:

- `Playground` conserva cámara fija, HUD, pausa y arranque mediante `_Bootstrap`.
- `TypingInput` procesa una palabra activa, normaliza mayúsculas y tildes,
  admite Backspace y emite eventos de progreso, error y finalización.
- `WordBubbleUI` presenta la palabra activa y su feedback sin consultar el
  estado cada frame.
- Existen tres recetas editables: Pan caliente, Sopa de verduras y El Plato del
  Pueblo.
- `RecipeRunner` recorre una receta como máquina de estados, bloquea la entrada
  mientras se ejecuta cada acción y permite reiniciar sin recargar la escena.
- `Playground` ejecuta provisionalmente Pan caliente al entrar en Play:
  `pan → mantequilla → tostar → servir`.
- El HUD superior muestra plato, pedido, estado, paso activo, pasos posteriores
  atenuados y la indicación `ESC · PAUSA` sin consultar datos cada frame.
- `Despensa`, `Horno` y `Servicio` se resaltan según el paso activo, anticipan
  con el progreso escrito, reaccionan al completar la palabra y celebran el
  plato terminado.
- Hay 23/23 pruebas Edit Mode y 9/9 pruebas Play Mode aprobadas.

Todavía no hay transformación visual del plato, reacción del gato, progresión
entre las tres recetas ni arte final. El siguiente bloque es la tarea 11:
mostrar la transformación progresiva del plato.

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
  → receta completada
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
  coordinación de reacciones por eventos.
- `Assets/_Project/Data/Recipes/`: los tres platos editables desde el Inspector.
- `Assets/_Project/Scripts/UI/Gameplay/WordBubbleUI.cs`: palabra y feedback.
- `Assets/_Project/Scripts/UI/Gameplay/RecipeHUDUI.cs`: pedido y progreso por
  eventos del corredor.
- `Assets/_Project/Tests/EditMode/`: 23 pruebas de escritura, datos y HUD.
- `Assets/_Project/Tests/PlayMode/`: 9 pruebas del ciclo real y los actores.
- `Assets/_Project/Prefabs/PauseUI.prefab`: overlay de pausa compartido.

Los scripts y prefabs heredados de plataformas se conservan como referencia,
pero `Player2D`, `PlayerMotor2D` y `CameraFollow2D` no forman parte de la
composición actual de `Playground`.

## Cómo probar

1. Abre el proyecto con Unity `6000.3.19f1`.
2. Abre `Assets/_Project/Scenes/Playground.unity` y pulsa Play.
3. Escribe, en orden: `pan`, `mantequilla`, `tostar` y `servir`.
4. Comprueba que una letra incorrecta no avance y que Backspace retroceda.
5. Entre palabras, comprueba que la entrada quede bloqueada durante la espera
   configurada y luego aparezca el siguiente paso.
6. Comprueba que el HUD marque el paso activo, atenúe los posteriores y muestre
   los completados con una marca.
7. Comprueba que `pan` y `mantequilla` señalen `Despensa`, `tostar` señale
   `Horno` y `servir` señale `Servicio`; cada mesa debe anticipar al escribir y
   rebotar al completar su palabra.
8. Al terminar `servir`, confirma que las tres mesas celebren, que el HUD
   muestre `RECETA COMPLETADA` y que no se acepte más texto.
9. En `Window → General → Test Runner`, ejecuta las 23 pruebas de `EditMode` y
   las 9 pruebas de `PlayMode`.
10. Opcionalmente, inicia desde `Boot` y comprueba `MainMenu → Playground`.

La compilación WebGL está pospuesta. No se generará un build hasta que exista
un juego completo y representativo dentro del Editor.

## Convenciones

- Assets propios dentro de `Assets/_Project`.
- No se agregan escenas, paquetes o singletons sin una necesidad del backlog.
- Los cambios de escena y prefab deben preservar referencias serializadas.
- `documentacion/` contiene el backlog, GDD copiado y memoria privada del
  proyecto; no reemplaza este README público.
