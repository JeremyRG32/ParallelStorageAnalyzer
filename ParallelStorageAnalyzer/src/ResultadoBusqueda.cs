namespace ParallelStorageAnalyzer
{
    public class ResultadoBusqueda
    {
        public List<FileInfo> Archivos { get; set; } = new();
        public List<List<FileInfo>> Duplicados { get; set; } = new();
        public long TiempoMs { get; set; }
        public int Nucleos { get; set; }
        public int Modo { get; set; } // 1 = Paralelo, 2 = Secuencial
        public string ModoNombre => Modo == 1 ? "Paralelo" : "Secuencial";
    }


}
