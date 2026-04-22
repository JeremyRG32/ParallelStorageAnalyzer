## 1. Comparativa de Métricas 

A continuación, se detallan los resultados obtenidos al procesar un conjunto de archivos variando la cantidad de núcleos utilizados en la ejecución paralela frente a la secuencial.

### Resumen de Ejecuciones

| Núcleos | Tiempo Secuencial (ms) | Tiempo Paralelo (ms) | Speedup | Eficiencia |
|:---:|:---:|:---:|:---:|:---:|
| **2** | 23,076 | 10,989 | 2.10x | 105.0% |
| **4** | 23,564 | 8,484 | 2.78x | 69.4% |
| **8** | 23,267 | 7,275 | 3.20x | 40.0% |
| **16** | 32,065 | 7,684 | 4.17x | 26.1% |

---

## 2. Análisis Detallado por Configuración

###  Configuración: 16 Núcleos
- **Speedup Logrado:** 4.17x
- **Eficiencia Real:** 26.1%
- **Observación:** Se logra el mayor Speedup absoluto, pero con la eficiencia más baja debido al overhead de gestión de hilos.

### Configuración: 8 Núcleos
- **Speedup Logrado:** 3.20x
- **Eficiencia Real:** 40.0%
- **Observación:** Es el punto donde el tiempo paralelo es más bajo (7,275 ms), demostrando que para esta carga de trabajo, 8 núcleos son más efectivos que 16.

### Configuración: 4 Núcleos
- **Speedup Logrado:** 2.78x
- **Eficiencia Real:** 69.4%
- **Observación:** Representa el equilibrio ideal entre ganancia de velocidad y uso responsable de recursos de hardware.

### Configuración: 2 Núcleos
- **Speedup Logrado:** 2.10x
- **Eficiencia Real:** 105.0%
- **Observación:** El rendimiento excede el 100%, probablemente debido a una mejor gestión de la memoria caché.

---
