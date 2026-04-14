
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
bool buscando = true;

//Tarea para crear una animacion mientras el programa analiza las carpetas
var tareaAnimacion = Task.Run(() =>
{
    string[] spinner = { "|", "/", "-", "\\" }; //Iconos para la animacion
    int contadorAnimacion = 0; //Contador para iterar sobre los iconos

    while (buscando)
    {
        Console.Write($"\r[{spinner[contadorAnimacion % 4]}] Cargando"); //Usamos \r para que sobrescriba el texto y simule una animacion
        contadorAnimacion++;
        Thread.Sleep(100); //Dormimos el hilo para la velocidad de la animacion
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

//Metodo para mostrar los resultados de la busqueda 
static void MostrarDashboard(List<FileInfo> archivos)
{
    //Formateamos lso datos en forma de tabla
    Console.WriteLine($"\n{"#",-5} {"Tamaño",-12} {"Nombre",-40} {"Ruta"}");
    Console.WriteLine(new string('─', 110));

    for (int i = 0; i < archivos.Count; i++)
    {
        var f = archivos[i];
        string tamano = FormatearTamano(f.Length);
        string nombre = f.Name.Length > 38 ? f.Name[..35] + "..." : f.Name; //Si el nombre es mayor a 38 caracteres solo tomamos hasta el 35
        string rutaCorta = f.DirectoryName?.Length > 50 //Si la ruta es mayor a 50 caracteres tomamos los ultimos 47 
            ? "..." + f.DirectoryName[^47..]
            : f.DirectoryName ?? "";

        Console.WriteLine($"{i + 1,-5} {tamano,-12} {nombre,-40} {rutaCorta}");
    }

    Console.WriteLine(new string('─', 110));
}

//Metodo para convertir Bytes en GB, MB o KB
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

