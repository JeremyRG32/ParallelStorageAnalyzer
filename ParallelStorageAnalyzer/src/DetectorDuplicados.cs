using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;

namespace ParallelStorageAnalyzer
{
    public class DetectorDuplicados
    {
        public void BuscarDuplicados(ResultadoBusqueda resultado)
        {

            var sw = Stopwatch.StartNew();

            if (resultado.Modo == 2)
            {
                resultado.Duplicados = BuscarSecuencial(resultado.Archivos);
            }
            else
            {
                resultado.Duplicados = BuscarParalelo(resultado.Archivos);
            }

            sw.Stop();
            resultado.TiempoMs += sw.ElapsedMilliseconds;
        }

        private List<List<FileInfo>> BuscarSecuencial(IEnumerable<FileInfo> archivos)
        {
            var resultados = new List<List<FileInfo>>();

            var gruposPorTamano = archivos
                .GroupBy(a => a.Length)
                .Where(g => g.Count() > 1);

            foreach (var grupo in gruposPorTamano)
            {
                var gruposPorHash = grupo
                    .GroupBy(a => ObtenerHash(a))
                    .Where(g => !string.IsNullOrEmpty(g.Key) && g.Count() > 1);

                foreach (var duplicados in gruposPorHash)
                {
                    var lista = duplicados.ToList();
                    resultados.Add(lista);
                }
            }

            return resultados;
        }

        private List<List<FileInfo>> BuscarParalelo(IEnumerable<FileInfo> archivos)
        {
            var resultados = new List<List<FileInfo>>();

            var gruposPorTamano = archivos
                .GroupBy(a => a.Length)
                .Where(g => g.Count() > 1);

            foreach (var grupo in gruposPorTamano)
            {
                var diccionario = new ConcurrentDictionary<string, List<FileInfo>>();

                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                };

                Parallel.ForEach(grupo, options, archivo =>
                {
                    string hash = ObtenerHash(archivo);

                    if (!string.IsNullOrEmpty(hash))
                    {
                        diccionario.AddOrUpdate(
                        hash,
                        new List<FileInfo> { archivo },
                        (key, listaExistente) =>
                        {
                            lock (listaExistente)
                            {
                                listaExistente.Add(archivo);
                            }
                            return listaExistente;
                        });
                    }
                });

                foreach (var kvp in diccionario.Where(x => x.Value.Count > 1))
                {
                    resultados.Add(kvp.Value);
                }
            }

            return resultados;
        }

        private static string ObtenerHash(FileInfo archivo)
        {
            try
            {
                using var sha256 = SHA256.Create();
                using var stream = archivo.OpenRead();
                byte[] hash = sha256.ComputeHash(stream);
                return BitConverter.ToString(hash);
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"\n[Acceso denegado]: {archivo.FullName}\n");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Error inesperado]: {ex.Message}\n");
                return string.Empty;
            }

        }
    }
}