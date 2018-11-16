using System;
using System.IO;
using ADL.Cli.Shell;

namespace ADL.Cli
{
    public class Program
    {                

        /// <summary>
        /// Main cli loop
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            const int bufferSize = 1024 * 67 + 128;
            var inputStream = Console.OpenStandardInput(bufferSize);
            Console.SetIn(new StreamReader(inputStream, Console.InputEncoding, false, bufferSize));
            IShellBase shell = new Koopa();
            shell.Run(args);
        }

        /// <summary>
        /// Prints application errors
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="ex"></param>
        private static void PrintErrorLogs(TextWriter writer, Exception ex)
        {
            while (true)
            {
                writer.WriteLine(ex.GetType());
                writer.WriteLine(ex.Message);
                writer.WriteLine(ex.StackTrace);

                if (ex is AggregateException ex2)
                {
                    foreach (var inner in ex2.InnerExceptions)
                    {
                        writer.WriteLine();
                        PrintErrorLogs(writer, inner);
                    }
                }
                else if (ex.InnerException != null)
                {
                    writer.WriteLine();
                    ex = ex.InnerException;
                    continue;
                }
                break;
            }
        }

        /// <summary>
        /// Catches unhandled exceptions and writes them to an error file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            using (var fs = new FileStream("error.log", FileMode.Create, FileAccess.Write, FileShare.None))

            using (var writer = new StreamWriter(fs))
                if (e.ExceptionObject is Exception ex)
                {
                    PrintErrorLogs(writer, ex);
                }
                else
                {
                    writer.WriteLine(e.ExceptionObject.GetType());
                    writer.WriteLine(e.ExceptionObject);
                }
        }
    }
}