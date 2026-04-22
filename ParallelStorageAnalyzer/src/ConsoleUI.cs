namespace ParallelStorageAnalyzer
{
    public static class ConsoleUI
    {
        #region Inputs requeridos
        // Inputs
        public static string PedirRuta()
        {
            while (true)
            {
                Console.Write(@"Ingrese la ruta a escanear (ej: C:\Windows): ");
                string ruta = Console.ReadLine()?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(ruta))
                    Console.WriteLine("Error: La ruta no puede estar vacía.\n");
                else if (!Directory.Exists(ruta))
                    Console.WriteLine("Error: La ruta no existe o no es accesible.\n");
                else
                    return ruta;
            }
        }

        public static long PedirTamano()
        {
            while (true)
            {
                Console.Write("Ingrese el tamaño mínimo de archivos a reportar en MB: ");
                if (long.TryParse(Console.ReadLine(), out long mb) && mb >= 0)
                {
                    return mb * 1024 * 1024;
                }
                else
                    Console.WriteLine("Error: Ingrese un número entero positivo.\n");
            }
        }
        public static int PedirModo()
        {
            int modo = 0;
            while (modo != 1 && modo != 2)
            {
                Console.WriteLine("\nSeleccione el modo de busqueda \n 1. Paralelo \n 2. Secuencial\n");
                int.TryParse(Console.ReadLine(), out modo);
            }
            return modo;
        }
        #endregion
        #region Dashboards y Menu de Eliminación
        // Dashboard busqueda de archivos
        public static void MostrarDashboard(ResultadoBusqueda resultado)
        {
            Console.WriteLine($"\n{"#",-5} {"Tamaño",-12} {"Nombre",-40} {"Ruta"}");
            Console.WriteLine(new string('─', 110));

            for (int i = 0; i < resultado.Archivos.Count; i++)
            {
                var f = resultado.Archivos[i];
                string tamano = FormatearTamano(f.Length);
                string nombre = f.Name.Length > 38 ? f.Name[..35] + "..." : f.Name;
                string rutaCorta = f.DirectoryName?.Length > 50
                    ? "..." + f.DirectoryName[^47..]
                    : f.DirectoryName ?? "";

                Console.WriteLine($"{i + 1,-5} {tamano,-12} {nombre,-40} {rutaCorta}");
            }

            Console.WriteLine(new string('─', 110));
            Console.WriteLine($"Tiempo de ejecucion: {resultado.TiempoMs / 1000}s");
            Console.WriteLine($"Modo de ejecución: {resultado.ModoNombre}");
        }

        // Dashboard archivos duplicados
        public static void MostrarDashboardDuplicados(List<List<FileInfo>> grupos, long tiempoMs, ModoEjecucion modo)
        {
            Console.WriteLine($"\nBúsqueda de duplicados ({modo}): {tiempoMs} ms");

            if (grupos.Count == 0)
            {
                Console.WriteLine("No se encontraron duplicados.");
                return;
            }

            Console.WriteLine($"Total: {grupos.Count} grupo(s) de duplicados encontrados.");

            foreach (var grupo in grupos)
                Console.WriteLine($"  ↳ {grupo.Count} copias de: {grupo[0].Name}");
        }


        // Menu

        public static int PedirOpcionMenu(bool hayDuplicados)
        {
            Console.WriteLine("\n¿Qué deseas hacer?");
            Console.WriteLine("  1. Buscar archivos de nuevo");
            Console.WriteLine("  2. Eliminar un archivo");

            // Solo mostrar opcion 3 si hay duplicados
            if (hayDuplicados)
                Console.WriteLine("  3. Eliminar archivos duplicados");

            Console.WriteLine(hayDuplicados ? "  4. Salir" : "  3. Salir");
            Console.Write("\nOpción: ");

            int.TryParse(Console.ReadLine(), out int opcion);
            return opcion;
        }

        // Menu de Eliminacion (Archivo Seleccionado)
        public static void EliminarArchivo(List<FileInfo> archivos)
        {
            Console.WriteLine("\n╔══════════════════════════════════════════════╗");
            Console.WriteLine("║           ELIMINACIÓN DE ARCHIVO             ║");
            Console.WriteLine("╚══════════════════════════════════════════════╝");

            Console.Write($"\nIngresa el # del archivo (1-{archivos.Count}): ");

            if (!int.TryParse(Console.ReadLine(), out int num) || num < 1 || num > archivos.Count)
            {
                Console.WriteLine("Número inválido.");
                return;
            }

            var archivo = archivos[num - 1];
            Console.WriteLine($"\nArchivo seleccionado : {archivo.FullName}");
            Console.WriteLine($"Tamaño  : {FormatearTamano(archivo.Length)}");
            Console.Write("¿Confirmas? Esta acción es irreversible. (s/n): ");

            if (Console.ReadLine()?.Trim().ToLower() != "s")
            {
                Console.WriteLine("Cancelado.");
                return;
            }

            try
            {
                archivo.Delete();
                archivos.RemoveAt(num - 1);
                Console.WriteLine("✓ Eliminado exitosamente.");
            }
            catch (UnauthorizedAccessException) { Console.WriteLine("✗ Sin permisos."); }
            catch (IOException ex) { Console.WriteLine($"✗ Error de E/S: {ex.Message}"); }
            catch (Exception ex) { Console.WriteLine($"✗ Error: {ex.Message}"); }
        }

        // Menu de Eliminacion de duplicados
        public static void EliminarDuplicados(List<List<FileInfo>> duplicados, List<FileInfo> archivosOrdenados)
        {
            Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║              ELIMINACIÓN DE ARCHIVOS DUPLICADOS             ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");

            int grupoNum = 1;
            foreach (var grupo in duplicados)
            {
                Console.WriteLine($"\nGrupo {grupoNum++} ({grupo.Count} archivos idénticos):");
                Console.WriteLine($"  {"#",-4} {"Tamaño",-12} {"Ruta completa"}");
                Console.WriteLine("  " + new string('─', 80));

                for (int i = 0; i < grupo.Count; i++)
                {
                    Console.WriteLine($"  {i + 1,-4} {ConsoleUI.FormatearTamano(grupo[i].Length),-12} {grupo[i].FullName}");
                }

                Console.WriteLine($"\n  Se conservará el archivo #1 y se eliminarán los demás ({grupo.Count - 1} archivo/s).");
                Console.Write("  ¿Eliminar duplicados de este grupo? (s/n): ");

                if (Console.ReadLine()?.Trim().ToLower() != "s")
                {
                    Console.WriteLine("  Grupo omitido.");
                    continue;
                }

                // Conservamos el primero, eliminamos el resto
                for (int i = 1; i < grupo.Count; i++)
                {
                    try
                    {
                        var archivo = grupo[i];
                        archivo.Delete();
                        archivosOrdenados.RemoveAll(f => f.FullName == archivo.FullName);
                        Console.WriteLine($"  ✓ Eliminado: {archivo.FullName}");
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.WriteLine($"  ✗ Sin permisos: {grupo[i].FullName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  ✗ Error: {ex.Message}");
                    }
                }

                // Limpiamos el grupo para que no vuelva a aparecer
                grupo.RemoveRange(1, grupo.Count - 1);
            }

            // Quitamos grupos que ya no tienen duplicados
            duplicados.RemoveAll(g => g.Count <= 1);

            Console.WriteLine("\nProceso de eliminación de duplicados completado.");
        }
        #endregion
        #region Metodos Helper y Animación
        static string FormatearTamano(long bytes)
        {
            return bytes switch
            {
                >= 1_073_741_824 => $"{(double)bytes / 1_073_741_824:F2} GB",
                >= 1_048_576 => $"{(double)bytes / 1_048_576:F2} MB",
                >= 1_024 => $"{(double)bytes / 1_024:F2} KB",
                _ => $"{bytes} B"
            };
        }
        public static async Task MostrarSpinner(Func<Task> operacion, string busqueda)
        {
            bool buscando = true;
            var animacion = Task.Run(() =>
            {
                string[] spinner = { "|", "/", "-", "\\" };
                int i = 0;
                while (buscando)
                {
                    Console.Write($"\r[{spinner[i++ % 4]}] {busqueda}");
                    Thread.Sleep(100);
                }
            });
            await operacion();
            buscando = false;
            await animacion;
            Console.Write("\r" + new string(' ', 20) + "\r");
        }
        #endregion
    }
}
