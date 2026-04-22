using ParallelStorageAnalyzer;


bool continuarPrograma = true;

var buscador = new BuscadorArchivo();
var detector = new DetectorDuplicados();

while (continuarPrograma)
{
    // Inputs
    string ruta = ConsoleUI.PedirRuta();
    long minBytes = ConsoleUI.PedirTamano();
    int nucleos = ConsoleUI.PedirNucleos();
    int modo = ConsoleUI.PedirModo();


    ResultadoBusqueda resultado = null!;

    // Busqueda con Animacion 
    await ConsoleUI.MostrarSpinner(async () =>
    {
        await Task.Run(() => resultado = buscador.Buscar(ruta, minBytes, modo, nucleos));
    }, "Buscando Archivos...");


    if (resultado.Archivos.Count == 0)
    {
        Console.WriteLine("\nNo se encontraron archivos con el tamaño especificado.");
        Console.Write("¿Deseas realizar otra búsqueda? (s/n): ");
        string respuesta = Console.ReadLine()?.Trim().ToLower() ?? "n";
        if (respuesta != "s") continuarPrograma = false;
        continue;
    }

    // Dashboard
    ConsoleUI.MostrarDashboard(resultado);


    // Detectar duplicados con Animacion de busqueda
    await ConsoleUI.MostrarSpinner(async () =>
    {
        await Task.Run(() =>
        {
            detector.BuscarDuplicados(resultado, nucleos);
        });
    }, "Buscando Duplicados...");

    ConsoleUI.MostrarDashboardDuplicados(resultado);

    // Menu despues de hacer una busqueda
    bool enMenu = true;
    while (enMenu)
    {
        var opcion = ConsoleUI.PedirOpcionMenu(resultado.Duplicados.Count > 0);

        bool esSalir = (resultado.Duplicados.Count > 0 && opcion == 4) ||
                       (resultado.Duplicados.Count == 0 && opcion == 3);

        bool esEliminarDuplicados = resultado.Duplicados.Count > 0 && opcion == 3;

        if (opcion == 1)
        {
            enMenu = false; // Vuelve al inicio del while principal para pedir nueva ruta
        }
        else if (opcion == 2)
        {
            ConsoleUI.EliminarArchivo(resultado.Archivos);
            if (resultado.Archivos.Count > 0)
                ConsoleUI.MostrarDashboard(resultado);
            else
            {
                Console.WriteLine("No quedan archivos en la lista.");
                enMenu = false;
                continuarPrograma = false;
            }
        }
        else if (esEliminarDuplicados)
        {
            ConsoleUI.EliminarDuplicados(resultado.Duplicados, resultado.Archivos);
            if (resultado.Archivos.Count > 0)
                ConsoleUI.MostrarDashboard(resultado);
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

