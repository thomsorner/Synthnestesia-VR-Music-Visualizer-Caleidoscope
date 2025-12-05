Synthnestesia - v0.1.0

Bitácora abierta de una exploración sinestésica en VR

Este repositorio contiene la versión 0.0.3, una prueba de concepto funcional desarrollada en Unity 2022.3.57f1 para dispositivos Android con integración del SDK de Google Cardboard. Esta versión está en una etapa temprana de desarrollo y no constituye un producto final, sino una serie de experimentos que buscan validar hipótesis técnicas y sensoriales en torno a la sinestesia simulada a través de realidad virtual.

Este espacio no solo recopila el código fuente y documentación técnica, sino que funciona como bitácora de desarrollo y canal de interacción con la comunidad. Tu retroalimentación es valiosa: el proyecto está concebido como un espacio abierto a la colaboración, el aprendizaje colectivo y la construcción compartida.

Este proyecto nace del deseo de expandir la percepción humana mediante tecnología inmersiva. Utilizando realidad virtual como medio, busca simular la sinestesia como fenómeno estético, mezclando estímulos auditivos y visuales en un entorno envolvente.

APK: https://drive.google.com/file/d/119-O8FNaVEEeyMC1ttv_PDeBWTQ2dtdo/view?usp=sharing

Guía de instalación del proyecto en Unity pendiente

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
