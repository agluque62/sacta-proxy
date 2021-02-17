using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;
using System.Configuration.Install;
using NLog;

using ClusterSrv.Properties;
using Utilities;

namespace ClusterSrv
{
   static class Program
   {
      /// <summary>
      /// The main entry point for the application.
      /// </summary>
      static void Main(string[] args)
      {
         if (args.Length >= 1)
         {
            if (args[0] == "-cfg")
            {
               Application.EnableVisualStyles();
               Application.SetCompatibleTextRenderingDefault(false);
               Application.Run(new ClusterConfig());
            }
            else if (args[0] == "-install")
            {
               Native.Kernel32.AttachConsole(Native.Ntdll.GetParentProcessID(Process.GetCurrentProcess().Id));

               ProjectInstaller installer = new ProjectInstaller();

               installer.Context = new InstallContext();
               installer.Context.Parameters["assemblypath"] = Application.ExecutablePath;
               Hashtable state = new Hashtable();

               try
               {
                  installer.Install(state);
               }
               catch (Exception ex)
               {
                  installer.Rollback(state);
                  LogManager.GetCurrentClassLogger().Fatal(ex, Resources.ServerRunningError, ex);
               }
            }
            else if (args[0] == "-uninstall")
            {
               Native.Kernel32.AttachConsole(Native.Ntdll.GetParentProcessID(Process.GetCurrentProcess().Id));

               ProjectInstaller installer = new ProjectInstaller();

               try
               {
                  installer.Context = new InstallContext();
                  installer.Uninstall(null);
               }
               catch (Exception ex)
               {
                  LogManager.GetCurrentClassLogger().Fatal(ex, Resources.ServerRunningError, ex);
                  Console.WriteLine(Resources.ServerRunningError + ": " + ex.Message);
               }
            }
            else if (args[0] == "-console")
            {
               Native.Kernel32.AttachConsole(Native.Ntdll.GetParentProcessID(Process.GetCurrentProcess().Id));

               ClusterSrv.RunAsProcess(args);
            }
         }
         else
         {
            System.IO.Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            ClusterSrv.RunAsServer(args);
         }
      }
   }
}