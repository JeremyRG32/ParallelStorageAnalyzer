# ParallelStorageAnalyzer
Analizador de almacenamiento de alto rendimiento desarrollado en C#. Utiliza programación paralela y descomposición recursiva para identificar archivos de gran tamaño y duplicados mediante hashing.

---

## Requisitos previos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) o superior
- Visual Studio 2022 (v17.x) o VS Code con extensión de C#
- Windows, Linux o macOS

---

## Instalación

```bash
git clone https://github.com/JeremyRG32/ParallelStorageAnalyzer.git
cd ParallelStorageAnalyzer
```

---

## Compilar

**Visual Studio 2022**
1. Abra `ParallelStorageAnalyzer.sln`
2. Seleccione **Build → Build Solution** (`Ctrl+Shift+B`)
3. Verifique que no haya errores en la ventana de salida

**Terminal**
```bash
cd src/ParallelStorageAnalyzer/ParallelStorageAnalyzer
dotnet build
```

---

## Ejecutar

```bash
dotnet run --project src/ParallelStorageAnalyzer/ParallelStorageAnalyzer
```

O desde Visual Studio con `F5`

---

## Uso

La aplicación guía al usuario mediante tres pasos:

**Paso 1 — Ruta a escanear**
```
Ingrese la ruta a escanear (ej: C:\Windows): C:\Users\Usuario\Documents
```
El sistema valida que la ruta exista y no esté vacía. Si es inválida, lo notifica y vuelve a solicitarla.

**Paso 2 — Tamaño mínimo**
```
Ingrese el tamaño mínimo de archivos a reportar en MB: 50
```
Solo se reportarán archivos que superen ese umbral. Debe ser un número entero ≥ 0.

**Paso 3 — Modo de búsqueda**
```
Seleccione el modo de busqueda
 1. Paralelo
 2. Secuencial
 3. Comparativa de Ambos modos
```

| Modo | Descripción |
|------|-------------|
| `1. Paralelo` | Usa varios núcleos del procesador (`Parallel.ForEach`) |
| `2. Secuencial` | Recorre carpeta por carpeta de forma tradicional |
| `3. Comparativa` | Ejecuta ambos modos y muestra métricas de rendimiento (speedup y eficiencia) |

---

## Resultados

Los archivos encontrados se muestran ordenados de mayor a menor tamaño:

> Si no se encuentran archivos con el tamaño indicado, el sistema muestra:
> `No se encontraron archivos con el tamaño especificado`

---

## Pruebas

```bash
dotnet test
```

Los resultados indicarán cuántas pruebas pasaron, fallaron o fueron omitidas.

---

## Tecnologías

- C# / .NET 8.0
- Task Parallel Library (TPL)
- `ConcurrentBag<T>` y `ConcurrentDictionary<K,V>`
- SHA-256 para detección de duplicados

---

## Integrantes



| Jeremy Reyes Gonzalez
| Rubby Keyther Martinez Zorrilla
| Eidan Yamil Then Elizo
