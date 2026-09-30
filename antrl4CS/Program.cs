using Antlr4.Runtime;
using antrl4CS.Node;
using antrl4CS;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics; // <--- NECESARIO PARA EJECUTAR PROCESOS

namespace antrl4CS
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string projectDir;
            string[] sourceFiles;

            // =============================================================
            // 1. FASE DE DESCUBRIMIENTO (Scanning)
            // =============================================================
            if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            {
                if (Directory.Exists(args[0]))
                {
                    projectDir = Path.GetFullPath(args[0]);
                }
                else if (File.Exists(args[0]))
                {
                    projectDir = Path.GetDirectoryName(Path.GetFullPath(args[0]))!;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: La ruta especificada no existe.");
                    Console.ResetColor();
                    return;
                }
            }
            else
            {
                projectDir = Directory.GetCurrentDirectory();
            }

            bool singleFileMode = false;
            string? singleFilePath = null;

            if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            {
                if (File.Exists(args[0]))
                {
                    singleFileMode = true;
                    singleFilePath = Path.GetFullPath(args[0]);
                    projectDir = Path.GetDirectoryName(singleFilePath)!;
                }
                else if (Directory.Exists(args[0]))
                {
                    projectDir = Path.GetFullPath(args[0]);
                }
            }

            if (singleFileMode)
            {
                sourceFiles = new[] { singleFilePath! };
            }
            else
            {
                sourceFiles = ScanProjectDirectory(projectDir, "*.red");
                if (sourceFiles.Length == 0)
                    sourceFiles = ScanProjectDirectory(projectDir, "*.cds");
            }

            if (sourceFiles.Length == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Warning: No se encontraron archivos de código (.red o .cds) en: {projectDir}");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"Project Root: {projectDir}");
            Console.WriteLine($"Found {sourceFiles.Length} source files.");

            // =============================================================
            // 2. FASE DE PARSEO INDIVIDUAL
            // =============================================================
            var parsedNodes = new List<ProgramNode>();
            bool hasParseErrors = false;

            foreach (var file in sourceFiles)
            {
                Console.Write($"Parsing: {Path.GetFileName(file)}... ");
                var node = ParseFile(file);

                if (node != null)
                {
                    parsedNodes.Add(node);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("OK");
                }
                else
                {
                    hasParseErrors = true;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("FAILED");
                }
                Console.ResetColor();
            }

            if (hasParseErrors)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("La compilación se detuvo por errores de sintaxis.");
                Console.ResetColor();
                return;
            }

            // =============================================================
            // 3. FASE DE FUSIÓN (Merging - El Master Node)
            // =============================================================
            var masterNode = new ProgramNode();

            foreach (var node in parsedNodes)
            {
                masterNode.UseNodes.AddRange(node.UseNodes);
                masterNode.ClassNodes.AddRange(node.ClassNodes);
            }

            Console.WriteLine("\nAST fusionado correctamente (Todo el proyecto).");

            // =============================================================
            // 4. IMPRESIÓN DEL AST (Depuración)
            // =============================================================
            try
            {
                // AstPrinter.DumpAst(masterNode); // Descomenta si quieres ver el árbol
                // PrintSummary(masterNode);
            }
            catch (Exception) { }

            // =============================================================
            // 5. ANÁLISIS SEMÁNTICO & GENERACIÓN DE CÓDIGO
            // =============================================================
            try
            {
                // A. SEMÁNTICA
                Console.WriteLine("\n=== SEMANTIC ANALYSIS ===");
                var analyzer = new SemanticAnalyzer();
                analyzer.AnalyzeProject(parsedNodes);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n[OK] Análisis semántico completado sin errores.");
                Console.ResetColor();

                // B. GENERACIÓN DE CÓDIGO (LLVM)
                Console.WriteLine("\n=== CODE GENERATION (LLVM) ===");

                using (var generator = new CodeGenerator("RedLangModule"))
                {
                    // 1. Generar IR
                    string irCode = generator.Generate(masterNode);

                    // 2. Guardar en disco
                    string outputPath = Path.Combine(projectDir, "output.ll");
                    generator.WriteToFile(outputPath);

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"\n[EXITO] Código generado en: {outputPath}");
                    Console.ResetColor();

                    // =========================================================
                    // 6. AUTOMATIZACIÓN: CLANG + EJECUCIÓN
                    // =========================================================
                    CompileAndRun(projectDir, "output.ll");
                }
            }
            catch (CompilerException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nSEMANTIC ERROR: {ex.Message}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"\nINTERNAL ERROR: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }

            Console.WriteLine("\n------------------------------------------------");
            Console.WriteLine("Proceso finalizado.");
        }

        // --- Helper Methods ---

        /// <summary>
        /// Compila el .ll usando Clang y ejecuta el .exe resultante
        /// </summary>
        private static void CompileAndRun(string workingDir, string llFileName)
        {
            Console.WriteLine("\nCompilando ejecutable con Clang...");

            string exeName = "programa.exe";

            // 1. Configurar proceso de Clang
            var clangProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "clang",
                    // Incluimos la librería legacy para evitar errores de Linker
                    Arguments = $"{llFileName} -o {exeName} -llegacy_stdio_definitions",
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            // 2. Ejecutar Clang
            try
            {
                clangProcess.Start();
                string output = clangProcess.StandardOutput.ReadToEnd();
                string errors = clangProcess.StandardError.ReadToEnd();
                clangProcess.WaitForExit();

                if (clangProcess.ExitCode != 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ Error de compilación en Clang:");
                    Console.WriteLine(errors);
                    Console.ResetColor();
                    return;
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ No se pudo ejecutar 'clang'. Asegúrate de estar en la terminal x64 Native Tools.\nError: {e.Message}");
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ Compilación exitosa. Ejecutando programa...\n");
            Console.ResetColor();
            Console.WriteLine("=============================================");

            // 3. Ejecutar el programa resultante
            var programProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(workingDir, exeName),
                    WorkingDirectory = workingDir,
                    UseShellExecute = false // False para que salga en la misma consola
                }
            };

            try
            {
                programProcess.Start();
                programProcess.WaitForExit();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error al ejecutar el programa: {e.Message}");
            }

            Console.WriteLine("\n=============================================");
        }

        private static string[] ScanProjectDirectory(string rootDir, string extension)
        {
            return Directory.GetFiles(rootDir, extension, SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) &&
                            !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) &&
                            !f.Contains(Path.DirectorySeparatorChar + "Legacy" + Path.DirectorySeparatorChar))
                .ToArray();
        }

        private static ProgramNode? ParseFile(string path)
        {
            try
            {
                string input = File.ReadAllText(path);
                var inputStream = new AntlrInputStream(input);

                var lexer = new construccion_semana2Lexer(inputStream);
                var tokens = new CommonTokenStream(lexer);
                var parser = new construccion_semana2Parser(tokens);

                var tree = parser.program();

                if (parser.NumberOfSyntaxErrors > 0) return null;

                var visitor = new AstBuilderVisitor();
                return visitor.VisitProgram(tree) as ProgramNode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing '{path}': {ex.Message}");
                return null;
            }
        }

        private static void PrintSummary(ProgramNode ast)
        {
            Console.WriteLine("\n=== AST SUMMARY ===");
            Console.WriteLine($"Classes: {ast.ClassNodes.Count}");
            foreach (var c in ast.ClassNodes)
            {
                Console.WriteLine($" - class {c.Name}");
            }
        }
    }
}