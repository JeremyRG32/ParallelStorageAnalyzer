using ParallelStorageAnalyzer;


bool continuarPrograma = true;

var buscador = new BuscadorArchivo();
var detector = new DetectorDuplicados();

List<FileInfo> archivosOrdenados = new List<FileInfo>();
List<List<FileInfo>> duplicadosEncontrados = new List<List<FileInfo>>();

while (continuarPrograma)
{
    // Inputs
    string ruta = ConsoleUI.PedirRuta();
    long minBytes = ConsoleUI.PedirTamano();
    int modo = ConsoleUI.PedirModo();
    ModoEjecucion modoSeleccionado = (modo == 1)
     ? ModoEjecucion.Paralelo
     : ModoEjecucion.Secuencial;

    // Busqueda con Animación
    ResultadoBusqueda resultado = null!;
    await ConsoleUI.MostrarSpinner(async () =>
    {
        await Task.Run(() => resultado = buscador.Buscar(ruta, minBytes, modo));
    });


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


    // Detectar duplicados
    duplicadosEncontrados = detector.BuscarDuplicados(resultado.Archivos, modoSeleccionado);

    // Menu despues de hacer una busqueda
    bool enMenu = true;
    while (enMenu)
    {
        var opcion = ConsoleUI.PedirOpcionMenu(duplicadosEncontrados.Count > 0);

        bool esSalir = (duplicadosEncontrados.Count > 0 && opcion == 4) ||
                       (duplicadosEncontrados.Count == 0 && opcion == 3);

        bool esEliminarDuplicados = duplicadosEncontrados.Count > 0 && opcion == 3;

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
            ConsoleUI.EliminarDuplicados(duplicadosEncontrados, archivosOrdenados);
            if (archivosOrdenados.Count > 0)
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

