using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SactaSectionHandler;
using Sacta;

namespace SimulScv
{
    class Program
    {
        static SactaModule SactaMod = null;
        static bool Lan1 { get; set; }
        static bool Lan2 { get; set; }
        static void Main(string[] args)
        {
            var cfg = Properties.Settings.Default;
            // Carga la configuracion...
            CfgSacta.CfgSactaUdp.PuertoOrigen = int.Parse(cfg.Listen.Split(':')[1]);         // Listen
            CfgSacta.CfgSactaUdp.PuertoDestino = int.Parse(cfg.SendingLan1.Split(':')[1]);          // Send
            CfgSacta.CfgMulticast.Interfaz = cfg.Listen.Split(':')[0];
            CfgSacta.CfgMulticast.RedA = cfg.SendingLan1.Split(':')[0];
            CfgSacta.CfgMulticast.RedB = cfg.SendingLan2.Split(':')[0];
            CfgSacta.CfgIpAddress.IpRedA = cfg.FromLan1;         // From LAN1 
            CfgSacta.CfgIpAddress.IpRedB = cfg.FromLan2;         // From LAN2
            CfgSacta.CfgSactaUsuarioSectores.IdSectores = cfg.Sectores;
            CfgSacta.CfgSactaUsuarioSectores.IdUcs = cfg.Posiciones;

            Lan1 = Lan2 = true;

            ConsoleKeyInfo result;
            do
            {
                PrintMenu();
                result = Console.ReadKey(true);
                switch (result.Key)
                {
                    case ConsoleKey.A:
                        if (SactaMod == null)
                        {
                            SactaMod = new SactaModule("sim");
                            SactaMod.Lan1 = Lan1;
                            SactaMod.Lan2 = Lan2;
                            SactaMod.Start();
                        }
                        break;

                    case ConsoleKey.P:
                        if (SactaMod != null)
                        {
                            SactaMod.Stop();
                            SactaMod = null;
                        }
                        break;

                    case ConsoleKey.D1:
                        Lan1 = !Lan1;
                        if (SactaMod != null) SactaMod.Lan1 = Lan1;
                        break;

                    case ConsoleKey.D2:
                        Lan2 = !Lan2;
                        if (SactaMod != null) SactaMod.Lan2 = Lan2;
                        PrintMenu();
                        break;

                    case ConsoleKey.M:
                        break;
                }
            } while (result.Key != ConsoleKey.Escape);

            if (SactaMod != null)
            {
                SactaMod.Stop();
                SactaMod = null;
            }

        }
        static void PrintMenu()
        {
            Console.Clear();
            Console.WriteLine("SimulSactaOnScv. Nucleo 2020...2021.");
            Console.WriteLine();
            Console.WriteLine($"Lan1: {Lan1}, Lan2:{Lan2}");
            Console.WriteLine();
            Console.WriteLine("\t A => Arrancar, P => Parar.");
            Console.WriteLine("\t 1 => LAN1 on/off. 2 => LAN2 on/off.");
            Console.WriteLine();
            Console.WriteLine("\tESC. Exit");
        }
    }
}
