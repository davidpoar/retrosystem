# Changelog

Todos los cambios notables de este proyecto se documentan en este archivo.

## [1.3.1] - 2026-10-11

### Cambiado

- **🔄 Disposición de los controles táctiles rehecha completamente.** Problema reportado: la cruceta y los botones de acción estaban mal distribuidos.

#### Antes (problema)
- D-pad como 4 botones sueltos en grid 3×3 (no parecía una cruceta real)
- Botones A y B pequeños y apilados diagonalmente con KBD y TAPE mezclados
- Botones de sistema (LIBRERÍA, ONLINE, MENU) en la barra de estado, robando espacio

#### Ahora (v1.3.1)
- **D-pad rediseñado como cruceta real**: View personalizada con `Canvas` que dibuja una cruz con 4 brazos redondeados, hub central, y labels de la tecla Spectrum (Q, A, O, P) en cada brazo. Mayor tamaño (28% del ancho) y respuesta táctil mejorada.
- **Botones A y B rediseñados**: círculos grandes estilo Nintendo, con la letra (A/B) y la tecla Spectrum que mapean (SPACE/M) claramente visibles. Sombras, rim oscuro, highlight interior. Tamaño 28% del ancho.
- **Panel de sistema flotante** en la esquina superior derecha: KBD · TAPE · MENU como iconos en un panel oscuro translúcido. No roba espacio a la pantalla del juego.
- **Carga de cintas desde el menú** (no desde la barra superior): opciones "Cargar local" y "Buscar online" en el menú principal. La barra superior queda slim (36dp, solo el estado).
- **Distribución de pantalla**: 28% D-pad | 44% juego | 28% A/B. Más espacio para el juego que antes.
- **Tape toggle visual**: el botón TAPE muestra ▶/❚❚ según el estado de reproducción.
- **Sliding entre direcciones**: el D-pad detecta gestos de arrastre (ACTION_MOVE) y cambia la dirección pulsada sin necesidad de levantar el dedo.

## [1.3.0] - 2026-10-11

### Añadido

- **🌐 Búsqueda online de juegos** en la base de datos ZXInfo (powered by [ZXInfo.dk](https://zxinfo.dk/), API abierta basada en ZXDB).
- **OnlineSearchActivity** con barra de búsqueda Material, lista de resultados con miniaturas, badges de año/genre/publisher, y botón de descarga.
- **Descarga automática de .tap/.tzx** desde ZXInfo. Soporta archivos .zip (los descomprime automáticamente).
- **Nueva `ZxInfoClient` (C#)** que consume el API REST de ZXInfo y deserializa el JSON con `System.Text.Json`.
- **Indicador de progreso de descarga** en cada fila durante la descarga.
- **Auto-añadir a recientes** después de una descarga exitosa.
- **Permisos `INTERNET` y `ACCESS_NETWORK_STATE`** en AndroidManifest.
- **Botón "🌐 Online"** en la barra superior de la activity principal.
- **Opción "🌐 Buscar y descargar online"** en el menú principal.
- **Vector drawables nuevos**: `ic_globe`, `ic_download`, `ic_clear`, `ic_check_circle`, `ic_error`, `illu_search_empty`, `illu_cassette`.
- **Fondos nuevos**: `bg_search_bar`, `bg_button_accent`.
- **Layouts nuevos**: `activity_online_search.xml`, `item_search_result.xml`.

### Inspiración de UI

- [RetroArch/libretro](https://github.com/libretro/RetroArch) (GPLv3) — Patrón de menú OSD con búsqueda.
- [Lemuroid](https://github.com/Swordfish90/Lemuroid) (GPL-3.0) — Patrón de cards y barra de búsqueda superior.
- [ZXInfo / ZXDB](https://github.com/zxdb/ZXDB) — Base de datos y API abierta.

## [1.2.1] - 2026-10-11

### Corregido

- **Crash al pulsar "Cargar cinta"**: `TapeLibraryActivity` se cerraba al abrir por dos bugs:
  1. `RequestWindowFeature(NoTitle)` se llamaba después de `SetContentView` → `AndroidRuntimeException`.
  2. `Window.SetBackgroundDrawableResource(Resource.Color.background)` recibía un color int en vez de un drawable resource → `Resources$NotFoundException`.
  Solución: mover `RequestWindowFeature` antes de `SetContentView` y eliminar la llamada errónea (el tema `AppTheme` ya define `windowBackground`).
- **Robustez**: `OnCreate`, `LoadTapes` y `TapeScanner.ScanAll` ahora capturan excepciones y registran el error con `Dbg.E`, mostrando un `Toast` en lugar de crashear.
- **Permisos**: `TapeScanner` ahora añade `try/catch` alrededor de cada `Directory.EnumerateFiles` para ignorar directorios sin permiso de lectura en lugar de fallar todo el escaneo.

## [1.2.0] - 2026-10-11

### Añadido

- **Pantalla de librería** (`TapeLibraryActivity`) con patrón inspirado en [Lemuroid](https://github.com/Swordfish90/Lemuroid): lista de cintas con miniaturas, badges TAP/TZX, formato y fecha.
- **Sistema de recientes** (`RecentsManager`): las últimas 8 cintas cargadas se guardan en SharedPreferences y se muestran en una sección "Recientes" arriba.
- **Escaneo automático de cintas** en `Download`, `Spectrum`, `Tapes`, `ZXSpectrum`, almacenamiento externo y caché.
- **Botón "Buscar..."** en la librería para invocar el file picker del sistema (Storage Access Framework) si el escaneo automático no encuentra la cinta.
- **Paleta de colores oscuros** (`Colors.xml`) inspirada en Lemuroid.
- **Iconos vectoriales** para cassette, play, pause, reset, help, search, star, back, etc.
- **Cards con bordes redondeados** y efecto pressed para las cintas (`bg_card_selectable.xml`).
- **Layouts XML reutilizables** (`activity_tape_library.xml`, `item_tape.xml`, `item_section_header.xml`).
- **Tema `AppTheme`** con colores oscuros consistentes.

### Cambiado

- El botón `📂 LOAD` ahora abre la **librería** (no directamente el file picker).
- Texto de ayuda actualizado para reflejar el nuevo flujo "Librería → Seleccionar → Cargar".

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
