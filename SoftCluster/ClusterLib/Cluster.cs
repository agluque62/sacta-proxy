using System;
using System.IO;
using System.Net;
using System.Timers;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

using ClusterLib.Properties;
using Utilities;
using helpers;

namespace ClusterLib
{
    public class Cluster : IDisposable
    {
        enum ClusterIpState { Unknown, Finding, Found, NotFound }
        public ClusterState State
        {
            get { return _State; }
        }
#if DEBUG
        // Simular la desconexion de las redes principales.
        public bool WorkingNetworkSimulOff { get; set; }
#endif
        public int SimulatedAdapters { get; set; }
        public Cluster(ClusterSettings settings)
        {
            _Settings = settings;
            _State = new ClusterState(_Settings);

            _EndPoint = new IPEndPoint(IPAddress.Parse(_Settings.EpIp), _Settings.EpPort);

            _PeriodicTasks = new Timer(_Settings.Tick);
            _PeriodicTasks.AutoReset = false;
            _PeriodicTasks.Elapsed += PeriodicTasks;

            _ToStart = new Timer(_Settings.TimeToStart);
            _ToStart.AutoReset = false;
            _ToStart.Elapsed += ToStart;

            SimulatedAdapters = 0;
            WorkingThread = new EventQueue((sender, ex) =>
            {
                Logger.Exception<EventQueue>(ex);
            });
            WorkingThread.Start();

            Logger.Info<Cluster>($"Starting CLUSTER.\nSettings {_Settings}");
        }
        ~Cluster()
        {
            Dispose(false);
            GC.SuppressFinalize(this);
        }
        public void Run()
        {
            WorkingThread.Enqueue("Cluster Run", () =>
            {
                _ToStart.Enabled = true;
            });
        }
        public void Activate()
        {
            WorkingThread.Enqueue("Cluster Activate", () =>
            {
                Logger.Trace<Cluster>("Activate");
                InternalActivate();
            });
            // Todo Sincronizar con Activacion Efectiva
        }
        public void Deactivate()
        {
            WorkingThread.Enqueue("Cluster Deactivate", () =>
            {
                Logger.Trace<Cluster>("Deactivate");
                if (_State.LocalNode.State == NodeState.NoActive)
                {
                    Logger.Info<Cluster>(String.Format(Resources.AlreadyDeactivate, _State.LocalNode.StateBegin));
                    return;
                }

                if (_State.RemoteNode.State == NodeState.NoValid)
                {
                    throw new InvalidOperationException(Resources.DeactivateValidRemoteNodeError);
                }

                if ((NumValidAdapters(_State.RemoteNode.ValidAdaptersMask) < NumValidAdapters(_State.LocalNode.ValidAdaptersMask)))
                {
                    throw new InvalidOperationException(Resources.DeactivateNumAdaptersError);
                }

                if (_State.LocalNode.State == NodeState.Active)
                {
                    DeleteVirtualAddresses();
                }

                _State.LocalNode.SetState(NodeState.NoActive, Resources.LocalDeactivateAsk);
            });
        }

        #region IDisposable Members

        public void Dispose()
        {
            WorkingThread.Enqueue("Cluster Dispose", () =>
            {
                Logger.Trace<Cluster>("Public Dispose");

                _State.LocalNode.SetState(NodeState.NoActive, Resources.LocalDeactivateAsk);
                _State.LocalNode.SetState(NodeState.NoValid, Resources.ExitApplication);

                Dispose(true);
            });
            WorkingThread.ControlledStop();
        }

        #endregion

        #region Private Members

        ClusterSettings _Settings;
        ClusterState _State;

        UdpSocket _Comm = null;
        IPEndPoint _EndPoint = null;
        Timer _PeriodicTasks = null;
        Timer _ToStart = null;
        DateTime _RemoteStateTime;
        bool _Disposed = false;
        EventQueue WorkingThread { get; set; }

        void DeleteArpTable()
        {
            // 20171020. Esta accion que dura tiempo y puede ser ejecutada de forma asincrona la saco el thread del automata..
            Task.Factory.StartNew(() =>
            {
                try
                {
                    Logger.Info<Cluster>("Deleting ARP Table...");
                    if (SimulatedAdapters == 0)
                    {
                        System.Diagnostics.Process process = new System.Diagnostics.Process();
                        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                        startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                        startInfo.WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                        startInfo.FileName = "delarp.bat";
                        process.StartInfo = startInfo;
                        process.Start();
                        process.WaitForExit();
                    }
                    System.Threading.Thread.Sleep(1000);
                    Logger.Info<Cluster>("ARP Table Deleted.");
                }
                catch (Exception ex)
                {
                    Logger.Exception<Cluster>(ex);
                }
            });
        }
        void ResetInternalLan()
        {
            try
            {
                Logger.Trace<Cluster>("ResetInternalLan");
                if (_Comm != null)
                {
                    _Comm.Dispose();
                    _Comm = null;
                    _ToStart.Enabled = true;
                }
            }
            catch (Exception x)
            {
                Logger.Exception<Cluster>(x);
            }
        }
        void Dispose(bool bDispose)
        {
            Logger.Trace<Cluster>("Private Dispose");
            if (!_Disposed)
            {
                Logger.Trace<Cluster>("Private Disposing...");
                _Disposed = true;

                if (bDispose)
                {
                    _PeriodicTasks.Enabled = false;
                    _PeriodicTasks.Close();
                    _PeriodicTasks = null;

                    if (_Comm != null)
                    {
                        _Comm.Dispose();
                        _Comm = null;
                    }
                    DeleteVirtualAddresses();
                    _Settings = null;
                    _State = null;
                    _EndPoint = null;
                }
            }
        }
        int NumValidAdapters(int globalAdaptersMask)
        {
            if (SimulatedAdapters > 0)
            {
                return SimulatedAdapters;
            }
            int nAdapters = 0;
            int mask = 0x01;
            for (int i = 0; i < 8; i++)
            {
                var bit = globalAdaptersMask & mask;
                nAdapters = bit != 0 ? nAdapters + 1 : nAdapters;
                mask <<= 1;
            }
            return nAdapters;
        }
        //bool IsValidAdapter(ClusterIpSetting ips/*int adapterIndex*/)
        //{
        //    // Comprobar ValidAdapters,
        //    return ((_State.LocalNode.ValidAdaptersMask & ips.AdapterMask/* adapterIndex*/) != 0);
        //}
        int GetAdaptersState()
        {
#if DEBUG
            if (WorkingNetworkSimulOff)
                return 0;
#endif
            if (SimulatedAdapters > 0)
            {
                return SimulatedAdapters == 1 ? 1 :
                    SimulatedAdapters == 2 ? 3 : 7;
            }

            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            int GlobalAdaptersMaks = 0;

            foreach (NetworkInterface adapter in nics)
            {
                if ((adapter.OperationalStatus == OperationalStatus.Up) && (adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback))
                {
                    UnicastIPAddressInformationCollection ips = adapter.GetIPProperties().UnicastAddresses;

                    foreach (UnicastIPAddressInformation ip in ips)
                    {
                        string strIp = ip.Address.ToString();
                        Logger.Debug<Cluster>($"IPV4 {strIp} Checking...");

                        var cfgItemIp = _Settings.VirtualIps.Where(i => i.AdapterIp == strIp).FirstOrDefault();
                        if (cfgItemIp != null)
                        {
                            GlobalAdaptersMaks |= cfgItemIp.AdapterMask;
                            cfgItemIp.AdapterIndex = adapter.GetIPProperties().GetIPv4Properties().Index;
                        }
                    }
                }
                else
                {
                    Logger.Trace<Cluster>($"Adapter {adapter.Name} Ignored ST: {adapter.OperationalStatus}, FT: {adapter.NetworkInterfaceType}");
                }
            }
            return GlobalAdaptersMaks;
        }
        bool ExistClusterAddresses()
        {
            bool retorno = false;
            // AGL. 3ª IP
            _Settings.VirtualIps.ForEach(ipc =>
            {
                try
                {
                    var reply = (new Ping()).Send(ipc.ClusterIp, 500);
                    Logger.Debug<Cluster>(String.Format("PingReply {1,8} From {0,15}, {2,6} ms",
                        reply.Address != null ? reply.Address.ToString() : ipc.ClusterIp, reply.Status.ToString(), reply.RoundtripTime));

                    if (reply != null && reply.Status == IPStatus.Success)
                        retorno = true;
                }
                catch (Exception ex)
                {
                    Logger.Exception<Cluster>(ex, $" PING to {ipc.ClusterIp} Exception. ");
                }
            });

            return retorno;
        }
        void CreateVirtualAddresses(Action<bool> error)
        {
            if (ExistClusterAddresses())
            {
                Logger.Error<Cluster>("ERROR: Virtuals addresses exist when creating them...");
                error(true);
            }
            else if (SimulatedAdapters > 0)
            {
                Logger.Info<Cluster>($"Virtuals IPs on SimulatedAdapters ({SimulatedAdapters}) Created...");
                error(false);
            }
            else
            {
                bool GlobalError = false;
                _Settings.VirtualIps.ForEach(ips =>
                {
                    Task<bool> t = Task.Run(() =>
                    {
                        try
                        {
                            ips.VirtualIpContext = Native.IpHlpApi.AddIPAddress(ips.ClusterIp, ips.ClusterMsk, ips.AdapterIndex);
                            Logger.Info<Cluster>(String.Format("Virtual IP {0} Created...", ips.ClusterIp));
                            return false;
                        }
                        catch (Exception ex)
                        {
                            Logger.Exception<Cluster>(ex, ips.ClusterIp);
                            return true;
                        }
                    });
                    if (t.Result == true) GlobalError = true;
                });
                error(GlobalError);
            }
        }
        void DeleteVirtualAddresses()
        {
            Logger.Trace<Cluster>("DeleteVirtualAdd");
            if (SimulatedAdapters > 0)
            {
                Logger.Info<Cluster>($"Virtuals IPs on SimulatedAdapters ({SimulatedAdapters}) Deleted...");
            }
            else
            {
                _Settings.VirtualIps.ForEach(ips =>
                {
                    //Task.Factory.StartNew(() => ForceDeleteVirtualAddressTask(ips)).Wait();
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            if (ips.VirtualIpContext >= 0)
                            {
                                Native.IpHlpApi.DeleteIPAddressOnContext(ips.VirtualIpContext);
                                ips.VirtualIpContext = -1;
                            }
                            else
                            {
                                Native.IpHlpApi.DeleteIPAddress(ips.ClusterIp);
                            }
                            Logger.Info<Cluster>(String.Format("Virtual IP {0} Deleted.", ips.ClusterIp));
                        }
                        catch (Exception ex)
                        {
                            Logger.Exception<Cluster>(ex, $"Exception deleting the Virtual IP {ips.ClusterIp}. ");
                        }
                    }).Wait();
                });
            }
        }
        void SendState()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, _State.LocalNode);

                try
                {
                    if (_Comm != null)
                        _Comm.Send(_EndPoint, ms.ToArray());
                }
                catch (Exception ex)
                {
                    Logger.Exception<Cluster>(ex);
                    /** 20171019. Si hay una excepcion aqui entiendo que se ha perdido la LAN interna... */
                    ResetInternalLan();
                }
            }
        }
        void InternalActivate()
        {
            Logger.Trace<Cluster>("Activate");

            if (_State.LocalNode.State == NodeState.Active)
            {
                Logger.Info<Cluster>(String.Format(Resources.AlreadyActivate, _State.LocalNode.StateBegin));
                return;
            }
            if (_State.LocalNode.State == NodeState.NoActive)
            {
                if ((_State.RemoteNode.State != NodeState.NoValid) &&
                   (NumValidAdapters(_State.LocalNode.ValidAdaptersMask) < NumValidAdapters(_State.RemoteNode.ValidAdaptersMask)))
                {
                    throw new InvalidOperationException(Resources.ActivateNumAdaptersError);
                }

                _State.LocalNode.SetState(NodeState.Activating, Resources.LocalActivateAsk);
            }
        }
        void ToStart(object sender, ElapsedEventArgs e)
        {
            WorkingThread.Enqueue("Cluster Starting Timer", () =>
            {
                // 20170925. No arranca hasta que no consigue que se active la LAN Interna.
                // Se considera que hay red si se completa favorablemente toda la inicializacion ...
                // 20171019. Este parte del timer se ejecutará periodicamente hasta que se inicie la red interna...
                Logger.Info<Cluster>("Starting... (ToStart TICK)");
                try
                {
                    _Comm = new UdpSocket(_Settings.Ip, _Settings.Port);
                    _Comm.NewDataEvent += OnNewData;
                    _Comm.BeginReceive();
                    Logger.Info<Cluster>("Internal Network Available.");
                }
                catch (Exception x)
                {
                    Logger.Exception<Cluster>(x);
                    // Rearranco este timer...
                    _ToStart.Enabled = true;
                }
                try
                {
                    // 20171019. Esta parte del timer solo se ejecuta la primera vez...Si no hay errores internos...
                    if (_State.LocalNode.State == NodeState.NoValid)
                    {
                        // 20171019. Fuerzo el borrado de la IP virtual...
                        DeleteVirtualAddresses();
                        /////////////////////////////////////////////////
                        _State.LocalNode.ValidAdaptersMask = GetAdaptersState();
                        if (_State.LocalNode.ValidAdaptersMask > 0)
                        {
                            if (!ExistClusterAddresses())
                            {
                                //_State.LocalNode.SetState(NodeState.Activating, Resources.LocalActivateAsk);
                                _State.LocalNode.SetState(NodeState.NoActive, "Initial Status"/*Resources.LocalActivateAsk*/);
                            }
                            else
                            {
                                _State.LocalNode.SetState(NodeState.NoActive, Resources.FoundClusterIps);
                            }
                        }
                        else
                        {
                            _State.LocalNode.SetState(NodeState.NoActive, Resources.DeactivateByNotAdapters);
                        }
                        _PeriodicTasks.Enabled = true;
                    }
                }
                catch (Exception x)
                {
                    Logger.Exception<Cluster>(x);
                    // Rearranco este timer...
                    _ToStart.Enabled = true;
                }
            });
        }
        void OnNewData(object sender, DataGram dg)
        {
            WorkingThread.Enqueue("Cluster OnNewData", () =>
            {
                try
                {
                    using (MemoryStream ms = new MemoryStream(dg.Data))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        object msg = bf.Deserialize(ms);
                        if (msg is MsgType)
                        {
                            switch ((MsgType)msg)
                            {
                                case MsgType.Activate:
                                    Logger.Info<Cluster>(Resources.RemoteActivateAsk);
                                    InternalActivate();
                                    break;
                                case MsgType.Deactivate:
                                    Logger.Info<Cluster>(Resources.RemoteDeactivateAsk);
                                    Deactivate();
                                    break;
                                case MsgType.GetState:
                                    MemoryStream info = new MemoryStream();
                                    bf.Serialize(info, _State);
                                    if (_Comm != null)
                                        _Comm.Send(dg.Client, info.ToArray());
                                    break;
                                default:
                                    throw new Exception(Resources.UnknownMsgType);
                            }
                        }
                        else if (msg is NodeInfo)
                        {
                            if (_State.RemoteNode.State == NodeState.NoValid)
                                Logger.Info<Cluster>(String.Format(Resources.ReceivedRemoteNodeState.Replace("\\n", Environment.NewLine), msg));
                            else
                                Logger.Trace<Cluster>(String.Format(Resources.ReceivedRemoteNodeState.Replace("\\n", Environment.NewLine), msg));
                            _State.RemoteNode.UpdateInfo((NodeInfo)msg);
                            _RemoteStateTime = DateTime.Now;
                        }
                        else
                        {
                            throw new Exception(Resources.UnknownMsgType);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception<Cluster>(ex);
                }
            });
        }
        void PeriodicTasks(object sender, ElapsedEventArgs e)
        {
            WorkingThread.Enqueue("Cluster Tick", () =>
            {
                bool FromLocalInvalid = false;
                try
                {
                    Logger.Trace<Cluster>($"Periodic Task. ClusterState => {_State}");

                    // Si el nodo remoto ha estado hablando y ha dejado de hablar se pone a INVALIDO
                    if ((_State.RemoteNode.State != NodeState.NoValid) &&
                       ((DateTime.Now - _RemoteStateTime).TotalMilliseconds > _Settings.RemoteTimeout))
                    {
                        Logger.Warn<Cluster>(Resources.RemoteNodeNotOperational);
                        _State.RemoteNode.SetState(NodeState.NoValid, null);
                    }

                    // Chequea la disponibilidad de los Adaptadores tanto en Local como en Remoto
                    _State.LocalNode.ValidAdaptersMask = GetAdaptersState();
                    int numLocalValidAdapters = NumValidAdapters(_State.LocalNode.ValidAdaptersMask);
                    int numRemoteValidAdapters = (_State.RemoteNode.State != NodeState.NoValid ? NumValidAdapters(_State.RemoteNode.ValidAdaptersMask) : 0);

                    if (_State.LocalNode.ValidAdaptersMask > 0)                 // Si hay adaptadores diponibles.
                    {
                        if (_State.LocalNode.State == NodeState.NoValid)        // Hay adaptadores disponibles y el estado anterior decia que no. 
                        {
                            Logger.Warn<Cluster>($"On {_State.LocalNode.State} there are valid adapters. Changing to NoActive...");
                            DeleteVirtualAddresses();
                            _State.LocalNode.SetState(NodeState.NoActive,/*Resources.AdapterDetected*/"");
                            FromLocalInvalid = true;
                        }
                        else if (_State.LocalNode.State == NodeState.NoActive)
                        {
                            if (_State.RemoteNode.State == NodeState.NoValid)
                            {
                                // 20171020. Si estoy NoActivo y Remoto en NoValido puede ser por desconexion de LAN INTERNA...
                                if (!ExistClusterAddresses())
                                {
                                    Logger.Warn<Cluster>($"On NoActive State and NoValid Remote State, Virtual Ips do not exist. Changing to Activating.");
                                    _State.LocalNode.SetState(NodeState.Activating, Resources.LocalActivateAsk);
                                }
                                else
                                {
                                    Logger.Warn<Cluster>($"On NoActive State and NoValid Remote State, Virtual Ips exist!!!. Ignore.");
                                }
                            }
                            else if (_State.RemoteNode.State == NodeState.NoActive)
                            {
                                if ((numLocalValidAdapters > numRemoteValidAdapters) ||
                                   ((numLocalValidAdapters == numRemoteValidAdapters) &&
                                   (_State.LocalNode.StateBegin.Ticks < _State.RemoteNode.StateBegin.Ticks)))
                                {
                                    _State.LocalNode.SetState(NodeState.Activating, Resources.LocalActivateAsk);
                                }
                                else
                                {
                                    Logger.Warn<Cluster>($"On NoActive State and NoActive Remote State. The remote has better note for Activating.!!!");
                                }
                            }
                            else if ((_State.RemoteNode.State == NodeState.Activating) || (_State.RemoteNode.State == NodeState.Active))
                            {
                                if (numLocalValidAdapters > numRemoteValidAdapters)
                                {
                                    _State.LocalNode.SetState(NodeState.Activating, Resources.LocalActivateAsk);
                                    Logger.Warn<Cluster>($"On NoActive State and Active or Activate Remote State. The remote has worst note for Activating.!!!");
                                }
                            }
                        }
                        else if (_State.LocalNode.State == NodeState.Activating)
                        {
                            if ((DateTime.Now - _State.LocalNode.StateBegin).TotalMilliseconds > (2 * _Settings.RemoteTimeout))
                            {
                                _State.LocalNode.SetState(NodeState.NoActive, Resources.ActivateTimeout);
                                Logger.Warn<Cluster>($"On Activating State. Timeout for activate!!!");
                            }
                            else if (_State.RemoteNode.State == NodeState.NoValid)
                            {
                                Logger.Warn<Cluster>($"On Activating State and NoValid RemoteState. Activating the node.");
                                CreateVirtualAddresses((error) =>
                                {
                                    if (!error)
                                    {
                                        Logger.Info<Cluster>("IP Virtual asignada al nodo");
                                        _State.LocalNode.SetState(NodeState.Active, Resources.NodeActive);
                                        DeleteArpTable();
                                    }
                                    else
                                    {
                                        DeleteVirtualAddresses();
                                        _State.LocalNode.SetState(NodeState.NoActive, Resources.LocalDeactivateAsk);
                                    }
                                });
                            }
                            else if (_State.RemoteNode.State == NodeState.NoActive)
                            {
                                if (numLocalValidAdapters >= numRemoteValidAdapters)
                                {
                                    Logger.Warn<Cluster>($"On Activating State and NoActive RemoteState. The remote has worst note. Activating the node...");
                                    CreateVirtualAddresses((error) =>
                                    {
                                        if (!error)
                                        {
                                            Logger.Info<Cluster>("IP Virtual asignada al nodo");
                                            _State.LocalNode.SetState(NodeState.Active, Resources.NodeActive);
                                            DeleteArpTable();
                                        }
                                        else
                                        {
                                            DeleteVirtualAddresses();
                                            _State.LocalNode.SetState(NodeState.NoActive, Resources.LocalDeactivateAsk);
                                        }
                                    });
                                }
                                else
                                {
                                    Logger.Warn<Cluster>($"On Activating State and NoActive RemoteState. The remote has better note. Waiting...");
                                }
                            }
                            else if (_State.RemoteNode.State == NodeState.Activating)
                            {
                                if ((numRemoteValidAdapters > numLocalValidAdapters) ||
                                   ((numRemoteValidAdapters == numLocalValidAdapters) &&
                                   (_State.RemoteNode.StateBegin.Ticks < _State.LocalNode.StateBegin.Ticks)))
                                {
                                    Logger.Warn<Cluster>($"On Activating State and Activating RemoteState. The remote has better note. Changing to NoActive...");
                                    _State.LocalNode.SetState(NodeState.NoActive, Resources.RemoteNodeActivating);
                                }
                                else
                                {
                                    Logger.Warn<Cluster>($"On Activating State and Activating RemoteState. The remote has worst note. Waiting...");
                                }
                            }
                            else if (_State.RemoteNode.State == NodeState.Active)
                            {
                                if (numRemoteValidAdapters > numLocalValidAdapters)
                                {
                                    Logger.Warn<Cluster>($"On Activating State and Active RemoteState. The remote has better note. Changing to NoActive...");
                                    _State.LocalNode.SetState(NodeState.NoActive, Resources.ActivateNumAdaptersError);
                                }
                                else
                                {
                                    Logger.Warn<Cluster>($"On Activating State and Active RemoteState. The remote has worst note. Waiting...");
                                }
                            }
                        }
                        else if (_State.LocalNode.State == NodeState.Active)
                        {
                            if (_State.RemoteNode.State == NodeState.Activating)
                            {
                                if (numRemoteValidAdapters >= numLocalValidAdapters)
                                {
                                    Logger.Warn<Cluster>($"On Active State and Activating RemoteState. The remote wants to activate. Changing to NoActive...");
                                    DeleteVirtualAddresses();
                                    _State.LocalNode.SetState(NodeState.NoActive, Resources.RemoteDeactivateAsk);
                                }
                                else
                                {
                                    Logger.Warn<Cluster>($"On Active State and Activating RemoteState. The remote wants to activate. The remote has worst note. Ignoring...");
                                }
                            }
                            else if (_State.RemoteNode.State == NodeState.Active)
                            {
                                Logger.Warn<Cluster>($"On Active State and Active RemoteState. Error. Changing to NoActive...");
                                //if ((numRemoteValidAdapters > numLocalValidAdapters) ||
                                //   ((numRemoteValidAdapters == numLocalValidAdapters) &&
                                //   (_State.RemoteNode.StateBegin.Ticks < _State.LocalNode.StateBegin.Ticks)))
                                {
                                    Logger.Warn<Cluster>(Resources.DetectedTwoActiveNodesError);
                                    DeleteVirtualAddresses();
                                    _State.LocalNode.SetState(NodeState.NoActive, Resources.LocalDeactivateAsk);
                                }
                            }
                        }
                        SendState();
                    }
                    else
                    {
                        Logger.Warn<Cluster>($"On {_State.LocalNode.State} there are no valid adapters. Changing to NoValid...");
                        if (_State.LocalNode.State != NodeState.NoValid)
                        {
                            /** 20170803. AGL, si no hay adaptadores hay que forzar el borrado de las IP virtuales. */
                            DeleteVirtualAddresses();
                        }
                        _State.LocalNode.SetState(NodeState.NoValid, Resources.DeactivateByNotAdapters);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception<Cluster>(ex);
                }
                finally
                {
                    if (!_Disposed)
                    {
                        // 20171020. Para intentar sincronizar las recuperaciones de red....
                        _PeriodicTasks.Interval = FromLocalInvalid ? _Settings.TimeToStart : _Settings.Tick;
                        _PeriodicTasks.Enabled = true;
                    }
                }
            });
        }

        #endregion
    }
}
