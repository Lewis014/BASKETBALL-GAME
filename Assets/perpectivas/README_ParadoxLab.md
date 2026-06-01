# Paradox Lab

Carpeta creada para la escena 3 de perspectivas: `Assets/perpectivas`.

## Generar la escena

1. Abre el proyecto `BASKETBALL-GAME` en Unity 6.
2. Espera a que Unity compile los scripts.
3. Ejecuta el menu:

   `Tools > Perpectivas > Create Paradox Lab Scene`

4. Unity creara y guardara:

   `Assets/perpectivas/ParadoxLab.unity`

## Controles

- `WASD`: mover jugador.
- `Mouse`: mirar.
- `Click izquierdo`: tomar o soltar cubos escalables.
- `E`: interactuar con monitores e interruptores.
- `TAB`: cambiar entre camara FPS e isometrica.
- `ESC`: liberar o bloquear cursor.

## Mecanicas incluidas

- Puzzle 1: cubos que cambian de escala segun la distancia de proyeccion.
- Puzzle 2: mundo alternativo visto por monitor con `RenderTexture`.
- Puzzle 3: puente invisible que activa collider al alinear la perspectiva.
- Sala final: cubo escalable, interruptor de gravedad, superficies laterales y portal de salida.
- Post-procesado URP: Bloom, Chromatic Aberration, Lens Distortion y Color Adjustments.

Nota: el proyecto no tiene Cinemachine instalado. El cambio de camaras funciona con `ParadoxCameraSwitcher` sin dependencia externa para mantener la compilacion estable.
