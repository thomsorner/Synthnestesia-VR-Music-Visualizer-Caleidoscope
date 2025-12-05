v0.1.0 — Descripción del Prototipo Actual

Este prototipo corresponde a la versión actualmente funcional del proyecto Synthnestesia, una aplicación VR en Unity que genera visuales inmersivos reactivos al sonido mediante un sistema de análisis espectral en tiempo real y un shader fractal adaptado para su uso en un entorno 360°.

La escena base del prototipo está compuesta por tres módulos principales: Player, AudioReactiveBus y SkyDome, cada uno cumpliendo una función específica dentro del flujo de captura, procesamiento y representación visual del audio.

1. Player

El Player se ubica en el centro de la escena y contiene los elementos necesarios para la visualización en Google Cardboard. Agrupa:

Main Camera (vista principal del usuario).

CardBoardReticle y su script, que funcionan como puntero dentro del visor VR.

Componentes requeridos por la integración con Google XR:

UniversalAdditionalCameraData

TracedPoseDriver (maneja la rotación de la cámara en VR).

AudioListener (captura el audio global para los cálculos del espectro).

Este módulo sirve como base para la experiencia inmersiva, asegurando un tracking estable y una visualización centralizada dentro del domo.

2. AudioReactiveBus

El AudioReactiveBus es el núcleo del sistema: el único punto de entrada, análisis y distribución de los datos de audio. Su diseño consolida en un solo componente la carga del archivo de sonido, el cálculo de la FFT, el suavizado temporal, la generación de texturas y la transmisión de datos al shader.

2.1 Carga y reproducción de audio

El módulo carga un archivo WAV desde StreamingAssets mediante una rutina asíncrona con UnityWebRequestMultimedia.

Una vez decodificado, asigna la pista a un AudioSource interno.

Desde el inspector es posible activar:

reproducción automática,

looping,

mute (para evitar salida física del audio sin afectar el análisis).

2.2 FFT y procesamiento espectral

En cada fotograma:

Se ejecuta una Transformada Rápida de Fourier (FFT) vía GetSpectrumData.

El tamaño de la FFT (32–1024 muestras) se ajusta automáticamente a potencias de dos.

El vector espectral es procesado mediante:

normalización y ganancia,

EMA (Exponential Moving Average) controlado por un parámetro smoothing.

El resultado es almacenado en un arreglo normalizado (0–1) que representa un espectro suave, apto para animación visual.

2.3 Textura 1D para el shader

El espectro procesado se empaqueta en una textura 1D, con un pixel de alto, actualizada cada frame.
Cada columna corresponde a un bin de frecuencia, permitiendo una lectura directa dentro del shader.

2.4 Bandas de frecuencia

Además del espectro completo, el módulo calcula tres promedios agregados:

Baja (20–250 Hz)

Media (250–2000 Hz)

Alta (2000–8000 Hz)

Estos valores son ajustados dinámicamente según el sample rate del dispositivo y permiten controlar efectos globales del shader (pulsos, destellos, variaciones de escala, etc.).

2.5 Evento de actualización

Al finalizar cada ciclo, el módulo envía la información espectral procesada al shader mediante un evento, asegurando sincronía entre el sonido y las animaciones.

3. SkyDome

El SkyDome es una esfera que encapsula al Player y sirve como superficie de proyección del shader audio reactivo.

Incluye:

Material con el shader fractal adaptado.

AudioVisualizerBinder, que recibe los datos provenientes del AudioReactiveBus.

Selección del estilo visual

Para definir la estética fractal del prototipo se elaboró un moodboard con shaders de Shadertoy, buscando específicamente:

patrones caleidoscópicos o fractales,

alta reactividad al sonido,

compatibilidad con visualización 360° (túneles, espacios envolventes).

Tras evaluar doce candidatos, se seleccionó “MandelKoch – Music Visualiser” por su capacidad de combinar fractales tipo Mandelbrot con modulaciones derivadas de múltiples bandas de frecuencia.

4. Shader

El shader elegido fue adaptado a un Unlit Shader URP con varias optimizaciones para VR móvil. Recibe parámetros configurables desde el inspector y desde la textura FFT enviadas por el AudioReactiveBus.

4.1 Parámetros principales

_TimeScale, _Intensity

_FFTtex (textura 1D con los datos del espectro)

_UseFFT

_UVScale / _UVRotate

_VignetteAmt

4.2 Etapa de vértices

Genera un lat-long mapping usando la normal del fragmento para proyectar correctamente el fractal en el domo.

4.3 Lectura del espectro

Lee cuatro puntos del espectro (f0–f3) correspondientes a frecuencias bajas, medias y altas, que modulan:

deformación del espacio fractal,

velocidad del tiempo interno,

escala y zoom,

color e intensidad.

4.4 Coordenadas fractales

Aplica rotaciones dependientes del audio, escalamiento dinámico y simetrías tipo Koch para generar un espacio UV recursivo apto para fractales.

4.5 Mandelbrot optimizado

Implementación móvil con:

64 iteraciones,

test de cardioide y bulb para descarte rápido,

smooth iteration count para transiciones suaves.

La imagen final mezcla:

fractal Koch,

Mandelbrot coloreado,

modulaciones por audio,

viñeteado y ajustes de intensidad global.

El resultado es una visualización psicodélica, envolvente y altamente reactiva al sonido.

APK: [https://drive.google.com/file/d/119-O8FNaVEEeyMC1ttv_PDeBWTQ2dtdo/view?usp=sharing](https://drive.google.com/file/d/11h3aXbX7pcz_NV4gSyEZKiGPhfl7enLZ/view?usp=sharing)

Bitácora de desarrollo

v0.0.1 — Experimento 1: Captura y análisis de sonido

Se realizó una primera prueba de exploración para la captura de sonido a través del micrófono en Unity. Los datos fueron representados en 7 canales de frecuencia, impresos directamente en la consola para análisis. Observaciones: Se evidenció una alta volatilidad en los valores de frecuencia, lo que plantea la necesidad de procesos de suavizado o normalización para futuras visualizaciones.

v0.0.2 — Experimento 2: Visualización en sliders

En esta segunda iteración, los datos de frecuencia fueron conectados a una interfaz visual mediante 7 sliders controlados por un manager. Se implementaron dos mejoras clave: un sistema de amplificación de la señal para mejorar la respuesta visual, y un suavizado para hacer más fluidos los movimientos de los sliders.

Conclusión: Esta metodología demostró ser viable para representar visualmente el sonido en tiempo real. Se identificó la importancia de normalizar los valores y aplicar suavizado para contrarrestar la inestabilidad inherente de los datos espectrales.

v0.0.3 — Experimento 3: Primer entorno en VR

Se evaluaron distintas formas de trasladar el experimento a un entorno de realidad virtual. Se consideró usar la plantilla oficial de Unity para VR, pero fue descartada por estar orientada a dispositivos de alta gama (como Oculus) y por su complejidad. La plantilla de Android también fue descartada al no facilitar un entorno VR desde cero. Finalmente, se optó por la plantilla oficial de Google Cardboard, por su enfoque en Android y su integración directa con visualización VR.

Se siguió la documentación y guía de instalación oficial de Google, aunque el proceso resultó complejo por la cantidad de parámetros requeridos para compilar correctamente.

La visualización evolucionó a 12 canales de frecuencia dispuestos en forma circular alrededor de la cámara, permitiendo una experiencia inmersiva en 360°, sin desplazamiento del usuario. Cada canal fue conectado a un spawner de partículas, similar a lo hecho con los sliders, pero en este caso con emisión visual dinámica. Además, se añadieron variables expuestas en el inspector: amplificación, cantidad de partículas, y una función para ignorar ruido ambiental.

Conclusión: La herramienta de Google Cardboard demostró ser óptima para los objetivos del proyecto, y será usada como base para el desarrollo de la aplicación por su compatibilidad con Android y VR básico. Se detectaron dificultades al exportar skyboxes para Android, posiblemente por el uso de materiales tipo Skybox/6 Sided no compatibles o requerimientos especiales de URP. Se requiere más investigación. Los resultados fueron prometedores: se logró una respuesta visual en tiempo real satisfactoria, aunque se deben seguir ajustando los efectos y trabajar en una identidad visual más definida.

v0.0.4 — Experimento 4: Conexión con shaders

En esta fase se exploró la integración de visuales generados mediante shaders dentro del entorno VR previamente construido. Con el objetivo de incrementar la complejidad visual y acercarse a una experiencia sinestésica más rica, se seleccionaron cuatro shaders provenientes del moodboard basado en Shadertoy y se inició su adaptación para Unity bajo el pipeline URP.

Para facilitar la comunicación entre los datos de audio y los shaders, el sistema evolucionó hacia el uso de un AudioReactiveBus optimizado. Con apoyo de ChatGPT, este script fue diseñado para centralizar el procesamiento del espectro de sonido y distribuir sus valores a cualquier shader conectado de forma consistente y escalable. Esto permitió unificar el flujo de datos y reducir la complejidad al momento de experimentar con múltiples materiales reactivos.

Se realizaron pruebas con distintos niveles de frecuencia, variaciones de intensidad y parámetros de UV, evaluando la respuesta visual en tiempo real dentro del domo VR. Aunque uno de los shaders seleccionados generó la estética más acorde al proyecto, todavía presentaba problemas de rendimiento debido al alto número de operaciones matemáticas involucradas en cada fotograma.

Conclusión: Este experimento confirmó la viabilidad de usar shaders complejos para generar visuales inmersivos reactivos al audio. Se alcanzó una comunicación eficiente entre el componente auditivo y los materiales, lo que permitió experimentar con múltiples estilos visuales sin reconfigurar el sistema base. Aunque el shader elegido aún no está completamente optimizado para dispositivos móviles, su implementación sobre una esfera tipo domo resultó ser una alternativa efectiva frente a las dificultades encontradas al exportar skyboxes personalizados en Android. Los resultados visuales fueron sólidos y abren paso a una fase de refinamiento enfocada en optimización y estilo artístico.
