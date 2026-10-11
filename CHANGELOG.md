# Changelog

Todos los cambios notables de este proyecto se documentan en este archivo.

## [1.0.0] - 2026-10-11

### Añadido

- Emulador ZX Spectrum 48K funcional para escritorio (Windows / Linux / macOS) y Android.
- Núcleo Z80 emulado con timing a 3,5 MHz, contención de memoria ULA y raster exacto.
- ULA de vídeo: 256×192, atributos ink / paper / bright / flash, borde dinámico (barras de carga).
- Audio del beeper (escritorio) con modelo analógico Chris-Smith (mezcla EAR/MIC + filtro RC).
- Biblioteca de cintas unificada: TAP y TZX con Fast Load opcional (intercepción de LD-BYTES en `0x0556`).
- Teclado completo mapeado a la matriz de 8×5 del Spectrum.
- Controles táctiles en Android:
  - D-pad con mapeo QAOP.
  - Botones A (SPACE) y B (M).
  - Teclado QWERTY en pantalla con modificadores SHIFT y SYM.
  - Botón KBD para mostrar/ocultar el teclado.
  - Botón TAPE para play/pause de la cinta.
- Abstracciones `IAssetProvider` y `ILogger` que desacoplan la carga de ROM/cintas y la salida de logs del núcleo del emulador, permitiendo el mismo código en escritorio y Android.
- Logo del Spectrum (barras de colores) generado en `Resources/Drawable/Icon.png`.
- Workflows de GitHub Actions para construir el APK de Android y los binarios de escritorio automáticamente.
