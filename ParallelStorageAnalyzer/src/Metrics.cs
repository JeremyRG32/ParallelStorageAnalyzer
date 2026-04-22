namespace ParallelStorageAnalyzer.src
{
    public class Metrics
    {
        private readonly BuscadorArchivo _buscador = new();
        private readonly DetectorDuplicados _detector = new();

        public async Task<(ResultadoBusqueda Secuencial, ResultadoBusqueda Paralelo)> EjecutarComparacion(
        string ruta, long minBytes, int nucleos)
        {
            ResultadoBusqueda secuencial = null!;
            ResultadoBusqueda paralelo = null!;

            await ConsoleUI.MostrarSpinner(async () =>
            {
                await Task.Run(() =>
                {
                    secuencial = _buscador.Buscar(ruta, minBytes, modo: 2, nucleos);
                    _detector.BuscarDuplicados(secuencial, nucleos);
                });
            }, "Ejecutando Busqueda Secuencial");

            await ConsoleUI.MostrarSpinner(async () =>
            {
                await Task.Run(() =>
                {
                    paralelo = _buscador.Buscar(ruta, minBytes, modo: 1, nucleos);
                    _detector.BuscarDuplicados(paralelo, nucleos);
                });
            }, "Ejecutando Busqueda Paralela");

            return (secuencial, paralelo);
        }
    }
}
