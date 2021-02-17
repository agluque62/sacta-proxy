using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using NLog;

using Utilities;
using ClusterLib;
using ClusterSrv.Properties;

namespace ClusterSrv
{
   public partial class ClusterSrv : ServiceBase
   {
      public ClusterSrv()
      {
         InitializeComponent();
      }

      public static void RunAsServer(string[] args)
      {
         ServiceBase.Run(new ClusterSrv());
      }

      public static void RunAsProcess(string[] args)
      {
         try
         {
            Native.Kernel32.SetConsoleTitle(" CLUSTER SRV (" + Settings.Default.NodeId + ") ");

            Console.WriteLine(Resources.ConsoleMenu.Replace("\\n", Environment.NewLine), Settings.Default.NodeId);

            _Cluster = new Cluster(GetClusterSettings());
            _Cluster.Run();

            // Asegurar que libera la IP virtual antes de entrar.
             // Puede ser que ambos servidores, hayan cogido la IP virtual porque estén incomunicados
            //_Cluster.Deactivate();

            ConsoleKeyInfo cki = new ConsoleKeyInfo();

            do
            {
               while (Console.KeyAvailable == false)
               {
                  System.Threading.Thread.Sleep(500);
               }

               cki = Console.ReadKey(true);

               try
               {
                  switch (cki.Key)
                  {
                      case ConsoleKey.M:
                        Console.WriteLine(Resources.ConsoleMenu.Replace("\\n", Environment.NewLine), Settings.Default.NodeId);
                        break;
                     case ConsoleKey.A:
                        _Cluster.Activate();
                        break;
                     case ConsoleKey.C:
                        if (_Cluster.State.LocalNode.State == NodeState.NoActive)
                        {
                           _Cluster.Activate();
                        }
                        else
                        {
                           _Cluster.Deactivate();
                        }
                        break;
                     case ConsoleKey.I:
                        Console.WriteLine(_Cluster.State);
                        break;
                     case ConsoleKey.Q:
                        break;
                     case ConsoleKey.H:
                        Console.WriteLine(Resources.ConsoleMenu.Replace("\n", Environment.NewLine), Settings.Default.NodeId);
                        break;
#if DEBUG
                      case ConsoleKey.N:
                        _Cluster.WorkingNetworkSimulOff = !_Cluster.WorkingNetworkSimulOff;
                        break;
#endif
                  }
               }
               catch (Exception ex)
               {
                  _Logger.Error(ex, Resources.ConsoleCommandError);
               }

            } while (cki.Key != ConsoleKey.Q);

            _Cluster.Dispose();
         }
         catch (Exception ex)
         {
            _Logger.Fatal(ex, Resources.ServerRunningError);
         }
      }

      protected override void OnStart(string[] args)
      {
         try
         {
            _Cluster = new Cluster(GetClusterSettings());
            _Cluster.Run();
         }
         catch (Exception ex)
         {
            _Logger.Fatal(ex, Resources.ServerRunningError, ex);
            throw;
         }
      }

      protected override void OnStop()
      {
         _Cluster.Dispose();
      }

      private static ClusterSettings GetClusterSettings()
      {
         Settings st = Settings.Default;
         ClusterSettings cst = new ClusterSettings();
   
         cst.NodeId = st.NodeId;
         cst.Ip = st.Ip;
         cst.Port = st.Port;
         cst.EpIp = st.EpIp;
         cst.EpPort = st.EpPort;
         cst.AdapterIp1 = st.AdapterIp1;
         cst.AdapterIp2 = st.AdapterIp2;
         cst.ClusterIp1 = st.ClusterIp1;
         cst.ClusterIp2 = st.ClusterIp2;
         cst.ClusterMask1 = st.ClusterMask1;
         cst.ClusterMask2 = st.ClusterMask2;
         cst.TimeToStart = st.TimeToStart;
         cst.MaintenanceServerForTraps = st.MaintenanceServerForTraps;

         return cst;
      }

      static Logger _Logger = LogManager.GetCurrentClassLogger();
      static Cluster _Cluster;
   }
}
