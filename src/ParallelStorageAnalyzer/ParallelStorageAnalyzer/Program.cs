using System.Collections.Concurrent;
using System.Security.Cryptography;

string rutaElegida = "";

bool continuarPrograma = true;
List<FileInfo> archivosOrdenados = new List<FileInfo>();
List<List<FileInfo>> duplicadosEncontrados = new List<List<FileInfo>>();

while (continuarPrograma)
{
    // Validacion de ruta
    string ruta = "";
    bool rutaValida = false;

    while (!rutaValida)
    {
        Console.Write(@"Ingrese la ruta a escanear (ej: C:\Windows): ");
        ruta = Console.ReadLine()?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(ruta))
            Console.WriteLine("Error: La ruta no puede estar vacía.\n");
        else if (!Directory.Exists(ruta))
            Console.WriteLine("Error: La ruta no existe o no es accesible.\n");
        else
        {
            rutaValida = true;
            rutaElegida = ruta;
        }
    }

    // Validacion de tamaño
    long minMB = 0;
    long minBytes = 0;
    bool tamanoValido = false;

    while (!tamanoValido)
    {
        Console.Write("Ingrese el tamaño mínimo de archivos a reportar en MB: ");
        if (long.TryParse(Console.ReadLine(), out minMB) && minMB >= 0)
        {
            minBytes = minMB * 1024 * 1024;
            tamanoValido = true;
        }
        else
            Console.WriteLine("Error: Ingrese un número entero positivo.\n");
    }

    // Seleccion de modo
    int modo = 0;
    while (modo != 1 && modo != 2)
    {
        Console.WriteLine("\nSeleccione el modo de busqueda \n 1. Paralelo \n 2. Secuencial\n");
        int.TryParse(Console.ReadLine(), out modo);
    }

    // Animacion
    BuscadorArchivo buscador = new BuscadorArchivo();
    bool buscando = true;

    var tareaAnimacion = Task.Run(() =>
    {
        string[] spinner = { "|", "/", "-", "\\" };
        int contadorAnimacion = 0;
        while (buscando)
        {
            Console.Write($"\r[{spinner[contadorAnimacion % 4]}] Cargando");
            contadorAnimacion++;
            Thread.Sleep(100);
        }
    });

    switch (modo)
    {
        case 1: buscador.Paralelo(ruta, minBytes); break;
        case 2: buscador.Secuencial(ruta, minBytes); break;
    }

    buscando = false;
    tareaAnimacion.Wait();
    Console.Write("\r" + new string(' ', 20) + "\r");

    // Dashboard
    archivosOrdenados = buscador.Archivos
        .OrderByDescending(f => f.Length)
        .ToList();

    if (archivosOrdenados.Count == 0)
    {
        Console.WriteLine("No se encontraron archivos con el tamaño especificado.");
        Console.Write("¿Deseas realizar otra búsqueda? (s/n): ");
        string respuesta = Console.ReadLine()?.Trim().ToLower() ?? "n";
        if (respuesta != "s") continuarPrograma = false;
        continue;
    }

    MostrarDashboard(archivosOrdenados);

    // Detectar duplicados
    var detector = new DetectorDuplicados();
    duplicadosEncontrados = detector.BuscarDuplicados(buscador.Archivos);

    // Menu despues de hacer una busqueda
    bool enMenu = true;
    while (enMenu)
    {
        Console.WriteLine("\n¿Qué deseas hacer?");
        Console.WriteLine("  1. Buscar archivos de nuevo");
        Console.WriteLine("  2. Eliminar un archivo");

        // Solo mostrar opcion 3 si hay duplicados
        if (duplicadosEncontrados.Count > 0)
            Console.WriteLine("  3. Eliminar archivos duplicados");

        Console.WriteLine(duplicadosEncontrados.Count > 0 ? "  4. Salir" : "  3. Salir");
        Console.Write("\nOpción: ");

        string opcion = Console.ReadLine()?.Trim() ?? "";

        bool esSalir = (duplicadosEncontrados.Count > 0 && opcion == "4") ||
                       (duplicadosEncontrados.Count == 0 && opcion == "3");

        bool esEliminarDuplicados = duplicadosEncontrados.Count > 0 && opcion == "3";

        if (opcion == "1")
        {
            enMenu = false; // Vuelve al inicio del while principal para pedir nueva ruta
        }
        else if (opcion == "2")
        {
            EliminarArchivo(archivosOrdenados);
            if (archivosOrdenados.Count > 0)
                MostrarDashboard(archivosOrdenados);
            else
            {
                Console.WriteLine("No quedan archivos en la lista.");
                enMenu = false;
                continuarPrograma = false;
            }
        }
        else if (esEliminarDuplicados)
        {
            EliminarDuplicados(duplicadosEncontrados, archivosOrdenados);
            if (archivosOrdenados.Count > 0)
                MostrarDashboard(archivosOrdenados);
            else
            {
                Console.WriteLine("No quedan archivos en la lista.");
                enMenu = false;
                continuarPrograma = false;
            }
        }
        else if (esSalir)
        {
            enMenu = false;
            continuarPrograma = false;
        }
        else
        {
            Console.WriteLine("Opción no válida.");
        }
    }
}

Console.WriteLine("\nPrograma finalizado. Presiona cualquier tecla para salir...");
Console.ReadKey();


// Eliminar archivo
static void EliminarArchivo(List<FileInfo> archivos)
{
    Console.WriteLine("\n╔══════════════════════════════════════════════╗");
    Console.WriteLine("║              ELIMINACIÓN DE ARCHIVO          ║");
    Console.WriteLine("╚══════════════════════════════════════════════╝");

    Console.Write($"\nIngresa el # del archivo a eliminar (1-{archivos.Count}): ");

    if (!int.TryParse(Console.ReadLine(), out int numero) || numero < 1 || numero > archivos.Count)
    {
        Console.WriteLine("Número inválido. No se eliminó ningún archivo.");
        return;
    }

    var archivo = archivos[numero - 1];

    Console.WriteLine($"\nArchivo seleccionado : {archivo.FullName}");
    Console.WriteLine($"Tamaño               : {FormatearTamano(archivo.Length)}");
    Console.Write("¿Confirmas la eliminación? Esta acción es irreversible. (s/n): ");

    if (Console.ReadLine()?.Trim().ToLower() == "s")
    {
        try
        {
            archivo.Delete();
            archivos.RemoveAt(numero - 1);
            Console.WriteLine("✓ Archivo eliminado exitosamente.");
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("✗ Error: No tienes permisos para eliminar este archivo.");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"✗ Error de E/S: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error inesperado: {ex.Message}");
        }
    }
    else
    {
        Console.WriteLine("Eliminación cancelada.");
    }
}

static void EliminarDuplicados(List<List<FileInfo>> duplicados, List<FileInfo> archivosOrdenados)
{
    Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║              ELIMINACIÓN DE ARCHIVOS DUPLICADOS             ║");
    Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");

    int grupoNum = 1;
    foreach (var grupo in duplicados)
    {
        Console.WriteLine($"\nGrupo {grupoNum++} ({grupo.Count} archivos idénticos):");
        Console.WriteLine($"  {"#",-4} {"Tamaño",-12} {"Ruta completa"}");
        Console.WriteLine("  " + new string('─', 80));

        for (int i = 0; i < grupo.Count; i++)
        {
            Console.WriteLine($"  {i + 1,-4} {FormatearTamano(grupo[i].Length),-12} {grupo[i].FullName}");
        }

        Console.WriteLine($"\n  Se conservará el archivo #1 y se eliminarán los demás ({grupo.Count - 1} archivo/s).");
        Console.Write("  ¿Eliminar duplicados de este grupo? (s/n): ");

        if (Console.ReadLine()?.Trim().ToLower() != "s")
        {
            Console.WriteLine("  Grupo omitido.");
            continue;
        }

        // Conservamos el primero, eliminamos el resto
        for (int i = 1; i < grupo.Count; i++)
        {
            try
            {
                var archivo = grupo[i];
                archivo.Delete();
                archivosOrdenados.RemoveAll(f => f.FullName == archivo.FullName);
                Console.WriteLine($"  ✓ Eliminado: {archivo.FullName}");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"  ✗ Sin permisos: {grupo[i].FullName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Error: {ex.Message}");
            }
        }

        // Limpiamos el grupo para que no vuelva a aparecer
        grupo.RemoveRange(1, grupo.Count - 1);
    }

    // Quitamos grupos que ya no tienen duplicados
    duplicados.RemoveAll(g => g.Count <= 1);

    Console.WriteLine("\nProceso de eliminación de duplicados completado.");
}

static void MostrarDashboard(List<FileInfo> archivos)
{
    Console.WriteLine($"\n{"#",-5} {"Tamaño",-12} {"Nombre",-40} {"Ruta"}");
    Console.WriteLine(new string('─', 110));

    for (int i = 0; i < archivos.Count; i++)
    {
        var f = archivos[i];
        string tamano = FormatearTamano(f.Length);
        string nombre = f.Name.Length > 38 ? f.Name[..35] + "..." : f.Name;
        string rutaCorta = f.DirectoryName?.Length > 50
            ? "..." + f.DirectoryName[^47..]
            : f.DirectoryName ?? "";

        Console.WriteLine($"{i + 1,-5} {tamano,-12} {nombre,-40} {rutaCorta}");
    }

    Console.WriteLine(new string('─', 110));
}

static string FormatearTamano(long bytes)
{
    return bytes switch
    {
        >= 1_073_741_824 => $"{(double)bytes / 1_073_741_824:F2} GB",
        >= 1_048_576 => $"{(double)bytes / 1_048_576:F2} MB",
        >= 1_024 => $"{(double)bytes / 1_024:F2} KB",
        _ => $"{bytes} B"
    };
}


public class BuscadorArchivo()
{
    public ConcurrentBag<FileInfo> Archivos { get; } = new ConcurrentBag<FileInfo>();

    public void Paralelo(string ruta, long minBytes) => ProcesarCarpeta(ruta, minBytes);

    public void Secuencial(string ruta, long minBytes)
    {
        Console.WriteLine("Esto no sera paralelo");
    }

    private void ProcesarCarpeta(string ruta, long minBytes)
    {
        try
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(ruta);
            foreach (var archivo in directoryInfo.GetFiles())
            {
                if (archivo.Length >= minBytes)
                    Archivos.Add(archivo);
            }

            var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            Parallel.ForEach(directoryInfo.GetDirectories(), options, subCarpeta =>
            {
                ProcesarCarpeta(subCarpeta.FullName, minBytes);
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"[Acceso denegado]: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error inesperado]: {ex.Message}");
        }
    }
}

public class DetectorDuplicados
{
    public List<List<FileInfo>> BuscarDuplicados(IEnumerable<FileInfo> Archivos)
    {
        Console.WriteLine("\nBuscando archivos duplicados...");

        var resultados = new List<List<FileInfo>>();

        var GruposPorTamano = Archivos
            .GroupBy(a => a.Length)
            .Where(g => g.Count() > 1);

        foreach (var grupo in GruposPorTamano)
        {
            var GruposPorHash = grupo
                .GroupBy(a => ObtenerHash(a))
                .Where(g => g.Count() > 1);

            foreach (var duplicados in GruposPorHash)
            {
                var lista = duplicados.ToList();
                resultados.Add(lista);

                Console.WriteLine($"  ↳ {lista.Count} copias de: {lista[0].Name}");
            }
        }

        if (resultados.Count == 0)
            Console.WriteLine("  No se encontraron duplicados.");
        else
            Console.WriteLine($"  Total: {resultados.Count} grupo(s) de duplicados encontrados.");

        return resultados;
    }

    static string ObtenerHash(FileInfo archivo)
    {
        using var sha256 = SHA256.Create();
        using var stream = archivo.OpenRead();
        byte[] hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash);
    }
}