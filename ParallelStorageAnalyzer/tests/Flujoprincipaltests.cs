using System;
using System.Collections.Generic;
using System.IO;
using ParallelStorageAnalyzer;

namespace ParallelStorageAnalyzer.Tests
{
    public class FlujoPrincipalTests : IDisposable
    {
        private const string RutaTest = @"C:\Users\Eidan\Desktop\TEST"; // Modificable

        private readonly TextReader _originalIn = Console.In;
        private readonly TextWriter _originalOut = Console.Out;
        private readonly StringWriter _outputCapturado = new StringWriter();

        public FlujoPrincipalTests()
        {
            Console.SetOut(_outputCapturado);
        }

        private static StringReader BuildInput(params string[] respuestas)
            => new StringReader(string.Join(Environment.NewLine, respuestas));

        private static ResultadoBusqueda BuscarArchivosReales()
            => new BuscadorArchivo().Buscar(RutaTest, minBytes: 0, modo: 1);

        private static List<List<FileInfo>> DetectarDuplicadosReales(ResultadoBusqueda resultado)
            => new DetectorDuplicados().BuscarDuplicados(resultado.Archivos, ModoEjecucion.Paralelo);

        // Test 1 - Verifica que PedirRuta retorna la ruta ingresada
        [Fact]
        public void PedirRuta_RutaValida_DevuelveRuta()
        {
            Assert.True(Directory.Exists(RutaTest), $"La carpeta '{RutaTest}' no existe.");
            Console.SetIn(BuildInput(RutaTest));

            string resultado = ConsoleUI.PedirRuta();

            Assert.Equal(RutaTest, resultado);
        }

        // Test 2 - Verifica que 100 MB se convierte correctamente a bytes
        [Fact]
        public void PedirTamano_100MB_DevuelveBytesCorrectos()
        {
            Console.SetIn(BuildInput("100")); // Modificable

            long resultado = ConsoleUI.PedirTamano();

            Assert.Equal(100L * 1024 * 1024, resultado);
        }

        // Test 3 - Verifica que el modo 1 (Paralelo) es retornado correctamente
        [Fact]
        public void PedirModo_Opcion1_DevuelveParalelo()
        {
            Console.SetIn(BuildInput("1")); // Modificable

            int resultado = ConsoleUI.PedirModo();

            Assert.Equal(1, resultado);
        }

        // Test 4 - Verifica que la opcion 2 del menu (Eliminar archivo) es retornada correctamente
        [Fact]
        public void PedirOpcionMenu_Opcion2_DevuelveEliminar()
        {
            Console.SetIn(BuildInput("2")); // Modificable

            int resultado = ConsoleUI.PedirOpcionMenu(hayDuplicados: true);

            Assert.Equal(2, resultado);
        }

        // Test 5 - Selecciona el archivo #1 y cancela la eliminacion con "n"
        [Fact]
        public void EliminarArchivo_Numero1_YCancelacion_MuestraCancelado()
        {
            var resultado = BuscarArchivosReales();
            Assert.True(resultado.Archivos.Count >= 1, "Se necesita al menos 1 archivo en TEST.");

            Console.SetIn(BuildInput("1", "n"));
            ConsoleUI.EliminarArchivo(resultado.Archivos);

            Assert.Contains("Cancelado", _outputCapturado.ToString());
        }

        // Test 6 - Flujo completo hasta eliminar archivo cancelando con "n"
        [Fact]
        public void FlujoCompleto_HastaEliminarArchivo_Cancelado()
        {
            Assert.True(Directory.Exists(RutaTest), $"La carpeta '{RutaTest}' no existe.");
            var resultado = BuscarArchivosReales();
            Assert.True(resultado.Archivos.Count >= 1, "Se necesita al menos 1 archivo en TEST.");

            Console.SetIn(BuildInput(RutaTest, "0", "1", "2", "1", "n"));

            string ruta = ConsoleUI.PedirRuta();
            long minBytes = ConsoleUI.PedirTamano();
            int modo = ConsoleUI.PedirModo();
            int opcionMenu = ConsoleUI.PedirOpcionMenu(hayDuplicados: true);
            ConsoleUI.EliminarArchivo(resultado.Archivos);

            Assert.Equal(RutaTest, ruta);
            Assert.Equal(0L, minBytes);
            Assert.Equal(1, modo);
            Assert.Equal(2, opcionMenu);
            Assert.Contains("Cancelado", _outputCapturado.ToString());
        }

        // Test 7 - Flujo completo confirmando eliminacion de archivo #1 con "s"
        [Fact]
        public void FlujoCompleto_ConfirmarEliminacion_ArchivoEliminadoDeLista()
        {
            Assert.True(Directory.Exists(RutaTest), $"La carpeta '{RutaTest}' no existe.");
            var resultado = BuscarArchivosReales();
            Assert.True(resultado.Archivos.Count >= 1, "Se necesita al menos 1 archivo en TEST.");

            Console.SetIn(BuildInput(RutaTest, "0", "1", "2", "1", "s"));

            string ruta = ConsoleUI.PedirRuta();
            long minBytes = ConsoleUI.PedirTamano();
            int modo = ConsoleUI.PedirModo();
            int opcion = ConsoleUI.PedirOpcionMenu(hayDuplicados: true);

            int conteoAntes = resultado.Archivos.Count;
            ConsoleUI.EliminarArchivo(resultado.Archivos);

            Assert.Equal(RutaTest, ruta);
            Assert.Equal(1, modo);
            Assert.Equal(2, opcion);
            Assert.Equal(conteoAntes - 1, resultado.Archivos.Count);
            Assert.Contains("Eliminado exitosamente", _outputCapturado.ToString());
        }

        // Test 8 - Flujo completo: eliminar archivo #1 y luego eliminar duplicados
        [Fact]
        public void FlujoCompleto_EliminarArchivo_LuegoEliminarDuplicados()
        {
            Assert.True(Directory.Exists(RutaTest), $"La carpeta '{RutaTest}' no existe.");
            var resultado = BuscarArchivosReales();
            var duplicados = DetectarDuplicadosReales(resultado);

            Assert.True(resultado.Archivos.Count >= 1, "Se necesita al menos 1 archivo en TEST.");
            Assert.True(duplicados.Count > 0,
                $"No hay duplicados en '{RutaTest}'. Copia cualquier archivo dentro de TEST.");

            var inputs = new List<string> { "2", "1", "s", "3" };
            foreach (var _ in duplicados)
                inputs.Add("s");

            Console.SetIn(BuildInput(inputs.ToArray()));

            int opcion1 = ConsoleUI.PedirOpcionMenu(hayDuplicados: true);
            Assert.Equal(2, opcion1);

            int conteoAntes = resultado.Archivos.Count;
            ConsoleUI.EliminarArchivo(resultado.Archivos);
            Assert.Equal(conteoAntes - 1, resultado.Archivos.Count);

            int opcion2 = ConsoleUI.PedirOpcionMenu(hayDuplicados: true);
            Assert.Equal(3, opcion2);

            ConsoleUI.EliminarDuplicados(duplicados, resultado.Archivos);

            string salida = _outputCapturado.ToString();
            Assert.Contains("Eliminado exitosamente", salida);
            Assert.Contains("Proceso de eliminación de duplicados completado", salida);
            Assert.Empty(duplicados);
        }

        public void Dispose()
        {
            Console.SetIn(_originalIn);
            Console.SetOut(_originalOut);
            _outputCapturado.Dispose();
        }
    }
}