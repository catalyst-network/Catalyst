using System;
using System.IO;
using Catalyst.Helpers.Logger;

namespace Catalyst.Helpers.Exceptions
{
    public static class Unhandled
    {
        /// <summary>
        ///     Catches unhandled exceptions and writes them to an error file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            using (var fs = new FileStream("error.log", FileMode.Create, FileAccess.Write, FileShare.None))

            using (var writer = new StreamWriter(fs))
            {
                if (e.ExceptionObject is Exception ex)
                {
                    ErrorLog.Print(writer, ex);
                }
                else
                {
                    writer.WriteLine(e.ExceptionObject.GetType());
                    writer.WriteLine(e.ExceptionObject);
                }
            }
        }
    }
}