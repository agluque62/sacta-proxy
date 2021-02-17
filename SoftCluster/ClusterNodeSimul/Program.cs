using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterNodeSimul
{
    class Program
    {
        static ClusterNode Node = new ClusterNode();
        static void Main(string[] args)
        {
            Node.Go();
            ConsoleKeyInfo result;
            do
            {
                PrintTitulo();
                PrintMenu();
                result = Console.ReadKey(true);
                switch (result.Key)
                {
                    case ConsoleKey.D0:
                        Node.SendRemoteStateAsk();
                        break;
                    case ConsoleKey.D1:
                        Node.ClusterStateSwitch();
                        break;
                    case ConsoleKey.D2:
                        break;
                }

            } while (result.Key != ConsoleKey.Q);
            Node.Stop();
        }

        static void PrintTitulo()
        {
            Console.Clear();
            ConsoleColor last = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Simulador de Nodo CLUSTER. Nucleo 2021");
            Console.WriteLine($"Escuchando en {Node.ListenEndp}, Nodo Remoto Esperado en {Node.RemoteEndp}");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Nodo Local");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"{Node.LocalStatus}");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Nodo Remoto");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"{Node.RemoteStatus}");
            Console.WriteLine();
            Console.ForegroundColor = last;
        }

        static void PrintMenu()
        {
            ConsoleColor last = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Opciones de WEB:\n");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("      [0] Pide Estado");
            Console.WriteLine("      [1] Conmuta Nodo");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            //Console.WriteLine("      [3] Equipo => SNMP-Local  ");
            //Console.WriteLine("      [4] Todos a SNMP ...  ");
            //Console.WriteLine();
            //Console.WriteLine("      [5] Equipo => SIP-OK  ");
            //Console.WriteLine("      [6] Equipo => SIP-Error  ");
            //Console.WriteLine("      [7] Todos a SIP ... ");
            //Console.WriteLine();
            //Console.WriteLine("      [S] Secuencias Programadas...  ");

            Console.WriteLine();
            Console.WriteLine("[Q] Salir");
            Console.WriteLine();

            Console.ForegroundColor = last;
        }


    }
}
