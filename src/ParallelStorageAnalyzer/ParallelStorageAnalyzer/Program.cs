
using System.Collections.Concurrent;

//Validacion de Ruta

string ruta = "";
bool rutaValida = false;

while (!rutaValida)
{
    Console.Write(@"Ingrese la ruta a escanear (ej: C:\Windows): ");
    ruta = Console.ReadLine()?.Trim();

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
switch (modo)
{
    case 1:
        buscador.Paralelo(ruta, minBytes);
        break;
    case 2:
        buscador.Secuencial(ruta, minBytes);
        break;
    default:

        break;
}

//Ordenamos los archivos de mayor a menor
var archivosOrdenados = buscador.Archivos
    .OrderByDescending(f => f.Length)
    .ToList();


if (archivosOrdenados.Count == 0)
{
    Console.WriteLine("No se encontraron archivos con el tamaño especificado");
}
else
{
    MostrarDashboard(archivosOrdenados);
}

//Mostramos resultados
static void MostrarDashboard(List<FileInfo> archivos)
{

    foreach (var x in archivos)
    {
        Console.WriteLine($"Nombre: {x.Name}, Ruta: {x.FullName}, Tamaño: {FormatearTamano(x.Length)}\n");
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
    //Lista para guardar los archivos con su ruta
    public ConcurrentBag<FileInfo> Archivos { get; } = new ConcurrentBag<FileInfo>();

    public void Paralelo(string ruta, long minBytes)
    {
        ProcesarCarpeta(ruta, minBytes);
    }

    public void Secuencial(string ruta, long minBytes)
    {
        Console.WriteLine("Esto no sera paralelo");
    }

    //Metodo para procesar carpetas de manera recursiva
    private void ProcesarCarpeta(string ruta, long minBytes)
    {
        try
        {
            //Añadimos cada archivo de la ruta seleccionada por el usuario a una lista 
            DirectoryInfo directoryInfo = new DirectoryInfo(ruta);
            foreach (var archivo in directoryInfo.GetFiles())
            {
                if (archivo.Length >= minBytes)
                {
                    Archivos.Add(archivo);
                }
            }

            //Procesamos las subcarpetas y limitamos las tareas simultaneas segun la capacidad del procesador 
            var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            Parallel.ForEach(directoryInfo.GetDirectories(), options, subCarpeta =>
            {
                ProcesarCarpeta(subCarpeta.FullName, minBytes);
            });
        }
        catch (UnauthorizedAccessException)
        {

        }
        catch (Exception)
        {

        }
    }
}

