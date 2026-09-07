# La cadena del banquete

Prototipo 2D de mecanografía hecho con Unity 6 y URP 2D. El juego transcurre
en una cocina fija: el jugador completa palabras para ejecutar acciones,
avanzar recetas y mantener en marcha el banquete.

## Flujo de escenas

La aplicación comienza en `Boot`, crea el `AppRoot` persistente y carga
`MainMenu`. Desde el menú se entra a `Prelude`, `Playground` o `Credits`.

Orden configurado en el Build Profile:

1. `Boot`
2. `MainMenu`
3. `Prelude`
4. `Playground`
5. `Credits`

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
- Cada letra alfabética queda escrita, incluso si es incorrecta.
- Se puede seguir escribiendo después de un error, pero la palabra solo se
  completa cuando todo el texto coincide exactamente.
- Backspace elimina letras del texto y es necesario para corregir errores.
- No hace falta pulsar Enter.
- Cada palabra y cada paso se completan una sola vez.
- La entrada se deshabilita durante acciones, pausas y transiciones.

El texto original se conserva en los datos y en el respaldo TMP. El alfabeto
gráfico incluye `Ñ`; las vocales acentuadas y `Ü` usan el mismo glifo de papel
que su vocal base porque el recurso no contiene variantes con diacríticos.

## Controles actuales

| Acción | Control |
| --- | --- |
| Escribir | Teclado |
| Corregir progreso | Backspace |
| Pausa | Escape o Start en gamepad |
| Navegar menús | Mouse, teclado o gamepad |

## Presentación de escenario

Los chefs visibles son objetos del escenario con `SpriteRenderer`: `Chef1`
atiende `despensa`, `Chef2` atiende `horno` y `Chef3` atiende `servicio`. Las
burbujas viven serializadas en el Canvas y solo siguen la posición de esos
objetos; no se crean elementos visuales durante Play Mode.

Después de incorporar estos scripts, abre `Playground` y ejecuta una vez
`Banquet Chain > Configurar gameplay en escenario`. El comando guarda en la
escena las tres burbujas de chef, `SunRequestBubble`, `CatFlowActor` y la
variante inactiva `BigCatSleeping`. También conecta el HUD: la primera receta
muestra la ayuda completa y las posteriores ocultan la lista y la palabra
pendiente, conservando en pantalla únicamente lo que el jugador escribe.

Los Animator son opcionales hasta disponer de los clips. Cuando se asignen,
los scripts publican los enteros `VisualState` y `ReactionType`; la secuencia
final ya recorre comer, felicidad y sueño antes de abrir `Credits`.

## Cómo probar

1. Abre el proyecto con Unity `6000.3.19f1`.
2. Abre `Assets/_Project/Scenes/Playground.unity` y pulsa Play.
3. Completa las tres recetas en orden: Pan caliente (4 palabras), Sopa de
   verduras (7 palabras) y El Plato del Pueblo (13 palabras).
4. Comprueba que una letra incorrecta quede escrita, que permita seguir
   tecleando y que Backspace sea necesario para corregirla.
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
11. En `Window → General → Test Runner`, ejecuta las 50 pruebas de `EditMode` y
    las 24 pruebas de `PlayMode`.
12. Opcionalmente, inicia desde `Boot` y recorre
    `MainMenu → Playground → Credits → MainMenu`.
