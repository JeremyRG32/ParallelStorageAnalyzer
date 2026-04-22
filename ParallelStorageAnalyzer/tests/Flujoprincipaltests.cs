using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using ParallelStorageAnalyzer;

namespace ParallelStorageAnalyzer.Tests
{
    /// <summary>
    /// Test de flujo completo del programa:
    ///   1. Ruta      → C:\Users\Eidan\Desktop\TEST
    ///   2. Mínimo MB → 100
    ///   3. Modo      → 1 (Paralelo)
    ///   4. Menú      → 2 (Eliminar un archivo)
    ///   5. # archivo → 10
    ///   6. Confirmar → s
    /// 
    /// Estrategia: redirigir Console.In / Console.Out con StringReader / StringWriter.
    /// No se requiere Spectre.Console porque ConsoleUI usa Console estático de .NET.
    /// </summary>
    public class FlujoPrincipalTests : IDisposable
    {
        // ── Streams originales para restaurarlos en Dispose ──────────────────
        private readonly TextReader _originalIn = Console.In;
        private readonly TextWriter _originalOut = Console.Out;

        // ── Output capturado ──────────────────────────────────────────────────
        private readonly StringWriter _outputCapturado = new StringWriter();

        public FlujoPrincipalTests()
        {
            // Redirigir salida para que los Console.WriteLine del programa
            // no ensucien la consola de tests y podamos hacer asserts sobre ella.
            Console.SetOut(_outputCapturado);
        }

        // ── Helper: construye el StringReader con todas las respuestas ────────
        private static StringReader BuildInput(params string[] respuestas)
            => new StringReader(string.Join(Environment.NewLine, respuestas));

        // ── Helper: crea archivos temporales REALES en disco ─────────────────
        // ConsoleUI.EliminarArchivo llama archivo.Length antes de confirmar,
        // por lo que los archivos deben existir físicamente.
        private static List<FileInfo> CrearArchivosTemporales(int cantidad, string carpeta)
        {
            Directory.CreateDirectory(carpeta);
            var lista = new List<FileInfo>();
            for (int i = 1; i <= cantidad; i++)
            {
                string path = Path.Combine(carpeta, $"archivo_{i:D2}.tmp");
                File.WriteAllText(path, $"contenido de prueba {i}");
                lista.Add(new FileInfo(path));
            }
            return lista;
        }

        // ── Helper: limpia carpeta temporal ──────────────────────────────────
        private static void LimpiarCarpeta(string carpeta)
        {
            if (Directory.Exists(carpeta))
                Directory.Delete(carpeta, recursive: true);
        }

        // ─────────────────────────────────────────────────────────────────────
        // TEST 1 – PedirRuta devuelve la ruta correcta cuando existe
        // ─────────────────────────────────────────────────────────────────────
        [Fact]
        public void PedirRuta_RutaValida_DevuelveRuta()
        {
            // La ruta debe existir en el equipo donde corre el test.
            // Ajusta la ruta o crea el directorio si no existe.
            string rutaEsperada = @"C:\Users\Eidan\Desktop\TEST";

            // Precondición: si la carpeta no existe, créala temporalmente
            bool creada = false;
            if (!Directory.Exists(rutaEsperada))
            {
                Directory.CreateDirectory(rutaEsperada);
                creada = true;
            }

            try
            {
                Console.SetIn(BuildInput(rutaEsperada));

                string resultado = ConsoleUI.PedirRuta();

                Assert.Equal(rutaEsperada, resultado);
            }
            finally
            {
                if (creada) Directory.Delete(rutaEsperada);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // TEST 2 – PedirTamano devuelve 100 MB en bytes
        // ─────────────────────────────────────────────────────────────────────
        [Fact]
        public void PedirTamano_100MB_DevuelveBytesCorrectos()
        {
            long bytesEsperados = 100L * 1024 * 1024; // 104 857 600

            Console.SetIn(BuildInput("100"));

            long resultado = ConsoleUI.PedirTamano();

            Assert.Equal(bytesEsperados, resultado);
        }

        // ─────────────────────────────────────────────────────────────────────
        // TEST 3 – PedirModo devuelve 1 (Paralelo)
        // ─────────────────────────────────────────────────────────────────────
        [Fact]
        public void PedirModo_Opcion1_DevuelveParalelo()
        {
            Console.SetIn(BuildInput("1"));

            int resultado = ConsoleUI.PedirModo();

            Assert.Equal(1, resultado);
        }

        // ─────────────────────────────────────────────────────────────────────
        // TEST 4 – PedirOpcionMenu devuelve 2 (Eliminar un archivo)
        // ─────────────────────────────────────────────────────────────────────
        [Fact]
        public void PedirOpcionMenu_Opcion2_DevuelveEliminar()
        {
            Console.SetIn(BuildInput("2"));

            // hayDuplicados = false para simplificar el menú (3 opciones)
            int resultado = ConsoleUI.PedirOpcionMenu(hayDuplicados: false);

            Assert.Equal(2, resultado);
        }

        // ─────────────────────────────────────────────────────────────────────
        // TEST 5 – EliminarArchivo selecciona el archivo #10 y cancela (n)
        // ─────────────────────────────────────────────────────────────────────
        [Fact]
        public void EliminarArchivo_Numero10_YCancelacion_MuestraCancelado()
        {
            string carpetaTemp = Path.Combine(Path.GetTempPath(), "psa_test_5");
            var archivos = CrearArchivosTemporales(15, carpetaTemp);

            try
            {
                // Inputs: elige el #10, luego cancela con "n"
                Console.SetIn(BuildInput("10", "n"));

                ConsoleUI.EliminarArchivo(archivos);

                string salida = _outputCapturado.ToString();
                Assert.Contains("archivo_10.tmp", salida);
                Assert.Contains("Cancelado", salida);
            }
            finally
            {
                LimpiarCarpeta(carpetaTemp);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // TEST 6 – FLUJO COMPLETO integrado con confirmación "n"
        //   ruta → 100 MB → modo 1 → opción 2 → archivo #10 → cancelar
        // ─────────────────────────────────────────────────────────────────────
        [Fact]
        public void FlujoCompleto_Inputs_SecuenciaCorrecta()
        {
            string rutaTest = @"C:\Users\Eidan\Desktop\TEST";
            string carpetaTemp = Path.Combine(Path.GetTempPath(), "psa_test_6");

            bool rutaCreada = false;
            if (!Directory.Exists(rutaTest))
            {
                Directory.CreateDirectory(rutaTest);
                rutaCreada = true;
            }

            var archivos = CrearArchivosTemporales(15, carpetaTemp);

            try
            {
                var inputSecuencia = BuildInput(
                    rutaTest,  // 1 – PedirRuta
                    "100",     // 2 – PedirTamano
                    "1",       // 3 – PedirModo (Paralelo)
                    "2",       // 4 – PedirOpcionMenu (Eliminar archivo)
                    "10",      // 5 – número de archivo
                    "n"        // 6 – cancelar para no borrar nada
                );
                Console.SetIn(inputSecuencia);

                string ruta = ConsoleUI.PedirRuta();
                long minBytes = ConsoleUI.PedirTamano();
                int modo = ConsoleUI.PedirModo();
                int opcionMenu = ConsoleUI.PedirOpcionMenu(hayDuplicados: false);

                ConsoleUI.EliminarArchivo(archivos);

                // Assert
                Assert.Equal(rutaTest, ruta);
                Assert.Equal(100L * 1024 * 1024, minBytes);
                Assert.Equal(1, modo);
                Assert.Equal(2, opcionMenu);

                string salida = _outputCapturado.ToString();
                Assert.Contains("archivo_10.tmp", salida);
                Assert.Contains("Cancelado", salida);
            }
            finally
            {
                LimpiarCarpeta(carpetaTemp);
                if (rutaCreada) Directory.Delete(rutaTest);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // TEST 7 – FLUJO COMPLETO con confirmación "s"
        //   Igual al anterior pero confirma la eliminación.
        //   Usa archivos REALES temporales en disco para que Delete() no falle.
        // ─────────────────────────────────────────────────────────────────────
        [Fact]
        public void FlujoCompleto_ConfirmarEliminacion_ArchivoEliminadoDelista()
        {
            string rutaTest = @"C:\Users\Eidan\Desktop\TEST";

            bool creada = false;
            if (!Directory.Exists(rutaTest))
            {
                Directory.CreateDirectory(rutaTest);
                creada = true;
            }

            // Crear 15 archivos temporales reales en disco
            var archivosReales = new List<FileInfo>();
            for (int i = 1; i <= 15; i++)
            {
                string path = Path.Combine(rutaTest, $"archivo_{i:D2}.tmp");
                File.WriteAllText(path, $"contenido {i}");
                archivosReales.Add(new FileInfo(path));
            }

            var inputSecuencia = BuildInput(
                rutaTest,
                "100",
                "1",
                "2",
                "10",
                "s"   // ← confirma eliminación
            );

            Console.SetIn(inputSecuencia);

            try
            {
                // Act
                string ruta = ConsoleUI.PedirRuta();
                long minBytes = ConsoleUI.PedirTamano();
                int modo = ConsoleUI.PedirModo();
                int opcion = ConsoleUI.PedirOpcionMenu(hayDuplicados: false);

                int conteoAntes = archivosReales.Count; // 15
                ConsoleUI.EliminarArchivo(archivosReales);
                int conteoDespues = archivosReales.Count; // debe ser 14

                // Assert
                Assert.Equal(rutaTest, ruta);
                Assert.Equal(1, modo);
                Assert.Equal(2, opcion);
                Assert.Equal(conteoAntes - 1, conteoDespues); // se eliminó 1

                string salida = _outputCapturado.ToString();
                Assert.Contains("Eliminado exitosamente", salida);
            }
            finally
            {
                // Limpiar archivos temporales restantes
                foreach (var f in archivosReales)
                    if (f.Exists) f.Delete();

                // Limpiar archivos que pudieron quedar fuera de la lista
                foreach (var f in Directory.GetFiles(rutaTest, "*.tmp"))
                    File.Delete(f);

                if (creada) Directory.Delete(rutaTest);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        public void Dispose()
        {
            // Restaurar Console.In y Console.Out originales
            Console.SetIn(_originalIn);
            Console.SetOut(_originalOut);
            _outputCapturado.Dispose();
        }
    }
}