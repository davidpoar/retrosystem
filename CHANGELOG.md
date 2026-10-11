# Changelog

Todos los cambios notables de este proyecto se documentan en este archivo.

## [1.1.0] - 2026-10-11

### Añadido

- **ROM 48K incluida en el APK** (descargada automáticamente en build time desde rustzx, con permiso de Amstrad).
- **Selector de cintas integrado** (Storage Access Framework): botón `📂 LOAD` en la barra superior para abrir el file picker del sistema y elegir cualquier `.tap`/`.tzx` del dispositivo.
- **Menú en pantalla** (`MenuOverlayView`): opciones de Cargar cinta, Teclear LOAD, Fast Load toggle, Reset, Mostrar/ocultar teclado, Ayuda, Cerrar.
- **Barra de estado** en tiempo real mostrando el nombre de la cinta y el estado (▶ / ⏸ / ⏹) o un aviso si falta la ROM.
- **Auto-typing de LOAD""** desde el menú (escribe `LOAD "" + Enter` automáticamente para arrancar el primer programa de la cinta).
- **Toggle de Fast Load** dinámico (cambia entre carga rápida y carga clásica sin reiniciar).
- **Script `fetch_rom.ps1`** que descarga la ROM desde rustzx o fuse (16384 bytes exactos) en tiempo de build.
- **Adaptive icon** Android (`mipmap-anydpi-v26`) con versiones `mipmap-mdpi`/`hdpi`/`xhdpi`/`xxhdpi`/`xxxhdpi` generadas por `make_icon.ps1`.
- **Diálogo de ayuda** con instrucciones completas de uso.
- **Manejo del botón Back** del sistema: cierra menú → cierra teclado → sale.

### Cambiado

- Icono rediseñado (eliminado texto cortado, ahora son las clásicas barras de colores con marco verde).
- Layout reorganizado: barra de estado arriba con botones LOAD y menú, controles abajo.
- Botones LOAD y MENU accesibles directamente sin abrir el menú.

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
