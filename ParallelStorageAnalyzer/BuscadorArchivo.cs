using System.Collections.Concurrent;
using System.Diagnostics;

namespace ParallelStorageAnalyzer
{
    public class BuscadorArchivo
    {
        private ConcurrentBag<FileInfo> _archivos { get; } = new ConcurrentBag<FileInfo>();

        public ResultadoBusqueda Buscar(string ruta, long minBytes, int modo)
        {
            _archivos.Clear(); // limpiar antes de cada búsqueda

            var sw = Stopwatch.StartNew();

            if (modo == 1)
            {
                ProcesarParalelo(ruta, minBytes);
            }
            else
            {
                ProcesarSecuencial(ruta, minBytes);
            }

            sw.Stop();

            return new ResultadoBusqueda
            {
                Archivos = _archivos.OrderByDescending(f => f.Length).ToList(),
                TiempoMs = sw.ElapsedMilliseconds,
                Modo = modo
            };
        }

        public void ProcesarSecuencial(string ruta, long minBytes)
        {
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(ruta);

                // Archivos de la carpeta actual
                foreach (var archivo in directoryInfo.GetFiles())
                {
                    if (archivo.Length >= minBytes)
                    {
                        _archivos.Add(archivo);
                    }
                }

                // Subcarpetas (recursivo)
                foreach (var subCarpeta in directoryInfo.GetDirectories())
                {
                    ProcesarSecuencial(subCarpeta.FullName, minBytes);
                }
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

        private void ProcesarParalelo(string ruta, long minBytes)
        {
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(ruta);

                //  Archivos de la carpeta actual
                foreach (var archivo in directoryInfo.GetFiles())
                {
                    if (archivo.Length >= minBytes)
                        _archivos.Add(archivo);
                }

                //  Subcarpetas en paralelo
                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                };

                Parallel.ForEach(directoryInfo.GetDirectories(), options, subCarpeta =>
                {
                    ProcesarParalelo(subCarpeta.FullName, minBytes);
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
}