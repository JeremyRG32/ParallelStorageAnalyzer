using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;

namespace ParallelStorageAnalyzer
{
    public enum ModoEjecucion
    {
        Secuencial,
        Paralelo
    }

    public class DetectorDuplicados
    {
        public (List<List<FileInfo>> Grupos, long TiempoMs) BuscarDuplicados(IEnumerable<FileInfo> archivos, ModoEjecucion modo)
        {

            var sw = Stopwatch.StartNew();

            List<List<FileInfo>> resultados;

            if (modo == ModoEjecucion.Secuencial)
            {
                resultados = BuscarSecuencial(archivos);
            }
            else
            {
                resultados = BuscarParalelo(archivos);
            }

            sw.Stop();

            return (resultados, sw.ElapsedMilliseconds);
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
                    .Where(g => g.Count() > 1);

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
            using var sha256 = SHA256.Create();
            using var stream = archivo.OpenRead();
            byte[] hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash);
        }
    }
}