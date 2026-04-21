using System.Security.Cryptography;

namespace ParallelStorageAnalyzer
{
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
}
