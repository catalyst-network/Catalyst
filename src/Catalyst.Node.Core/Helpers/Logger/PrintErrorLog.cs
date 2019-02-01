using System;
using System.IO;

namespace Catalyst.Node.Core.Helpers.Logger
{
    public static class ErrorLog
    {
        /// <summary>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="ex"></param>
        public static void Print(TextWriter writer, Exception ex)
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
                        Print(writer, inner);
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
    }
}