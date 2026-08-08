# Fuente de papel — tipografía de referencia

Alfabeto español inspirado directamente en las proporciones de la palabra `TOMATE` de la referencia suministrada.

## Variantes

- `red`: papel rojo coral.
- `gray`: papel gris neutro.

Ambas variantes contienen 27 letras PNG RGBA individuales, con textura fibrosa y fondo transparente.

## Orden de la hoja

```text
A B C D E F G H I
J K L M N Ñ O P Q
R S T U V W X Y Z
```

## Unity

- Hoja: `2016 × 960 px`.
- Cuadrícula: 9 columnas × 3 filas.
- Celda: `224 × 320 px`.
- Importación: `Sprite Mode: Multiple` y `Slice > Grid by Cell Size`.
- Pivote recomendado: `Center`.

La Ñ individual utiliza el nombre `letter_ENYE.png`.

## Integración jugable

- `PaperAlphabetGlyphSet.asset` reúne las 27 letras de `gray` y `red`.
- `PaperWordRenderer` compone la palabra activa con imágenes, conservando la
  proporción de cada glifo y reduciendo el tamaño cuando una palabra es larga.
- Las letras pendientes se muestran en gris y el prefijo correcto en rojo.
- `Ñ` tiene glifo propio. Las vocales con tilde y `Ü` usan el glifo de su vocal
  base porque este alfabeto no contiene diacríticos.
- `WordLabel` conserva TextMesh Pro como respaldo si faltara algún sprite.
