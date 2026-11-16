# Proyecto Final de Simuladores: Simulador Balístico 3D

Este proyecto es un simulador físico interactivo desarrollado en **Unity 6** aplicando estrictamente la **Arquitectura MVC (Modelo-Vista-Controlador)**. El sistema modela el disparo de un proyectil parabólico contra una estructura destructible, calculando impactos y telemetría en tiempo real.

## ⚙️ Descripción del Sistema Modelado
El simulador representa un cañón articulado que dispara esferas (proyectiles) contra una torre formada por bloques unidos mediante *FixedJoints*. Toda la interacción está gobernada por el motor de físicas de Unity.
La condición de destrucción es estricta: un bloque solo se considera "derribado" si cae físicamente fuera de la plataforma base (superando el límite del eje Y), ignorando desplazamientos menores.

## 🎛️ Parámetros e Interacción en Tiempo Real
El usuario controla la simulación a través de una interfaz de usuario avanzada (UI) que modifica el estado físico en tiempo real:
*   **Ángulo de Inclinación (5° a 85°):** Rota físicamente el cañón. Al moverlo, se aplica un efecto matemático de "tambaleo" (wobble) nativo y se actualiza la trayectoria predictiva.
*   **Masa del Proyectil (1 kg a 20 kg):** Altera el peso del Rigidbody del proyectil, modificando su inercia e impacto.
*   **Fuerza / Impulso (20 N·s a 400 N·s):** Define el impulso de salida (E = ½·m·v₀²).

## 📊 Telemetría y Registro de Cambios
El sistema cuenta con persistencia de datos y registro histórico:
1.  **Telemetría de Impacto:** Al chocar, el sistema calcula el tiempo de vuelo, punto exacto de colisión, velocidad relativa y cantidad de piezas derribadas válidas.
2.  **Historial JSON:** Cada disparo genera un objeto `SimulationRecord` con un ID único y un Timestamp. Este registro empaqueta las condiciones iniciales (masa, fuerza, ángulo) y el resultado del impacto, serializándolo en formato JSON para guardar un historial detallado de la sesión experimental.

## 🛠️ Tecnologías Utilizadas
*   Unity 6000.4.1f1
*   C# (Arquitectura MVC limpia)
*   Sistema de Físicas 3D (Rigidbodies, HingeJoints, FixedJoints)