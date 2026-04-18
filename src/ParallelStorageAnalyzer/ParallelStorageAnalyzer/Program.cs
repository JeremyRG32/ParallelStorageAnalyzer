using System.Collections.Concurrent;

bool continuar = true;
string? rutaGuardada = null;

while (continuar)
{
    string ruta = "";
    bool rutaValida = false;

    // Si ya hay una ruta guardada, preguntar si quiere usar la misma
    if (rutaGuardada != null)
    {
        Console.Write($"\n¿Desea escanear la misma ruta anterior ({rutaGuardada})? (s/n): ");
        string respuesta = Console.ReadLine()?.Trim().ToLower() ?? "";
        if (respuesta == "s" || respuesta == "si" || respuesta == "sí")
        {
            ruta = rutaGuardada;
            rutaValida = true;
        }
    }

    // Validacion de Ruta
    while (!rutaValida)
    {
        Console.Write(@"Ingrese la ruta a escanear (ej: C:\Windows): ");
        ruta = Console.ReadLine()?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(ruta))
        {
            Console.WriteLine("Error: La ruta no puede estar vacía.\n");
        }
        else if (!Directory.Exists(ruta))
        {
            Console.WriteLine("Error: La ruta no existe o no es accesible.\n");
        }
        else
        {
            rutaValida = true;
        }
    }

    rutaGuardada = ruta;

    // Validacion de Tamaño
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
        {
            Console.WriteLine("Error: Ingrese un número entero positivo.\n");
        }
    }

    // Seleccion de modo de busqueda
    int modo = 0;
    while (modo != 1 && modo != 2)
    {
        Console.WriteLine("\nSeleccione el modo de busqueda \n 1. Paralelo \n 2. Secuencial\n");
        int.TryParse(Console.ReadLine(), out modo);
    }

    BuscadorArchivo buscador = new BuscadorArchivo();
    bool buscando = true;

    // Tarea para crear una animacion mientras el programa analiza las carpetas
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
        case 1:
            buscando = true;
            buscador.Paralelo(ruta, minBytes);
            break;
        case 2:
            buscando = true;
            buscador.Secuencial(ruta, minBytes);
            break;
    }

    buscando = false;
    tareaAnimacion.Wait();
    Console.Write("\r                    \r"); // Limpiar la línea del spinner

    // Ordenamos los archivos de mayor a menor
    var archivosOrdenados = buscador.Archivos
        .OrderByDescending(f => f.Length)
        .ToList();

    if (archivosOrdenados.Count == 0)
    {
        Console.WriteLine("No se encontraron archivos con el tamaño especificado.");
    }
    else
    {
        MostrarDashboard(archivosOrdenados);

        // Menu post-busqueda
        bool enMenu = true;
        while (enMenu)
        {
            Console.WriteLine("\n¿Qué desea hacer?");
            Console.WriteLine(" 1. Eliminar un archivo");
            Console.WriteLine(" 2. Nueva búsqueda");
            Console.WriteLine(" 3. Salir");
            Console.Write("\nOpción: ");

            int opcion = 0;
            int.TryParse(Console.ReadLine(), out opcion);

            switch (opcion)
            {
                case 1:
                    EliminarArchivo(archivosOrdenados);
                    // Refrescar el dashboard después de eliminar
                    var restantes = archivosOrdenados.Where(f => f.Exists).ToList();
                    if (restantes.Count > 0)
                        MostrarDashboard(restantes);
                    else
                        Console.WriteLine("\nNo quedan archivos en la lista.");
                    break;

                case 2:
                    enMenu = false;     // Sale del menu
                    break;

                case 3:
                    enMenu = false;
                    continuar = false;  // Sale del bucle principal
                    break;

                default:
                    Console.WriteLine("Opción no válida. Intente de nuevo.");
                    break;
            }
        }
    }

    // Si no había archivos, preguntar si quiere salir o buscar de nuevo
    if (archivosOrdenados.Count == 0)
    {
        Console.Write("\n¿Desea realizar una nueva búsqueda? (s/n): ");
        string respuesta = Console.ReadLine()?.Trim().ToLower() ?? "";
        continuar = respuesta == "s" || respuesta == "si" || respuesta == "sí";
    }
}

Console.WriteLine("\nHasta luego.");


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
}

static void EliminarArchivo(List<FileInfo> archivos)
{
    // Filtramos solo los que aun existen en disco
    var disponibles = archivos.Where(f => f.Exists).ToList();

    if (disponibles.Count == 0)
    {
        Console.WriteLine("No hay archivos disponibles para eliminar.");
        return;
    }

    Console.Write($"\nIngrese el # del archivo a eliminar (1-{disponibles.Count}): ");

    if (!int.TryParse(Console.ReadLine(), out int numero) || numero < 1 || numero > disponibles.Count)
    {
        Console.WriteLine("Número inválido. Operación cancelada.");
        return;
    }

    // El # en pantalla corresponde al índice original en archivos, no en disponibles,
    // así que buscamos por posición visual 
    FileInfo archivo = disponibles[numero - 1];

    Console.WriteLine($"\nArchivo seleccionado: {archivo.FullName}");
    Console.WriteLine($"Tamaño: {FormatearTamano(archivo.Length)}");
    Console.Write("¿Está seguro que desea eliminar este archivo? (s/n): ");

    string confirmacion = Console.ReadLine()?.Trim().ToLower() ?? "";

    if (confirmacion == "s" || confirmacion == "si" || confirmacion == "sí")
    {
        try
        {
            archivo.Delete();
            Console.WriteLine("✓ Archivo eliminado correctamente.");
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("Error: No tiene permisos para eliminar este archivo.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al eliminar: {ex.Message}");
        }
    }
    else
    {
        Console.WriteLine("Operación cancelada.");
    }
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
        catch (UnauthorizedAccessException) { }
        catch (Exception) { }
    }
}