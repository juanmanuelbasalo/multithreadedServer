using System;
using System.Threading;
using GameServer.Business;

namespace GameServer
{
    class Program
    {
        private static ManualResetEvent manualReset = new ManualResetEvent(false);
        static void Main(string[] args)
        {
            Console.WriteLine("");
            WriteFullLine(" --------------------////////// Servidor del juego de Unity \\\\\\\\\\-------------------- ");
            Console.WriteLine("");
            Console.WriteLine(" Quiere iniciar el Servidor? ");
            Console.WriteLine(" Escriba una 'S' si asi lo desea ");
            string s = Console.ReadLine();
            if (s.Equals("S"))
            {
                try
                {
                    Server server = new Server();
                    server.Init();
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error al inicializar el servidor");
                    Console.WriteLine(e.Message);
                    Console.ResetColor();
                }
            }
            manualReset.WaitOne();
        }

        static void WriteFullLine(string value)
        {
            Console.BackgroundColor = ConsoleColor.Magenta;
            Console.ForegroundColor = ConsoleColor.White;
            Console.SetCursorPosition((Console.WindowWidth - value.Length) / 2, Console.CursorTop);
            Console.WriteLine(value);
            Console.ResetColor();
        }
    }
}
