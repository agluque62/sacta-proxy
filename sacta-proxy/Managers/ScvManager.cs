using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Timers;

using sacta_proxy;
using sacta_proxy.helpers;
using sacta_proxy.WebServer;
using sacta_proxy.model;

namespace sacta_proxy.Managers
{
    class ScvManager : BaseManager, IDisposable
    {
        enum SactaState { WaitingSactaActivity, WaitingSectorization, SendingPresences, Stopped }
        #region Events
        public event EventHandler<ActivityOnLanArgs> EventActivity;
        public event EventHandler<SectorizationReceivedArgs> EventSectorization;
        #endregion Events

        #region Public
        public ScvManager(int Protocolversion, Configuration.DependecyConfig cfg, Func<History> hist)
        {
            Locker = new object();
            Id = cfg.Id;
            Cfg = cfg;
            Version = Protocolversion;
            History = hist;
            SactaSPSIUsers = Cfg.SactaProtocol.Sacta.PsisList().Select(i => (ushort)i).ToDictionary(i => i, i => new PsiOrScvInfo());
            SactaSPVUsers = Cfg.SactaProtocol.Sacta.SpvsList().Select(i => (ushort)i).ToDictionary(i => i, i => new PsiOrScvInfo());
            GlobalState = SactaState.Stopped;
            TxEnabled = false;

            LastActivityOnLan1 = DateTime.MinValue;
            LastActivityOnLan2 = DateTime.MinValue;
            LastPresenceSended = DateTime.MinValue;
            WhenSectorAsked = DateTime.MinValue;

            Lan1Listen = new IPEndPoint(IPAddress.Parse(Cfg.Comm.If1.Ip), Cfg.Comm.ListenPort);
            Lan1Sendto = new IPEndPoint(IPAddress.Parse(Cfg.Comm.If1.McastGroup), Cfg.Comm.SendingPort);

            Lan2Listen = new IPEndPoint(IPAddress.Parse(Cfg.Comm.If2.Ip), Cfg.Comm.ListenPort);
            Lan2Sendto = new IPEndPoint(IPAddress.Parse(Cfg.Comm.If2.McastGroup), Cfg.Comm.SendingPort);
        }
        public override void Start()
        {
            Logger.Info<ScvManager>($"Starting ScvManager for {Id}...");
            try
            {
                Listener1 = new UdpSocket(Lan1Listen);
                /** Para seleccionar correctamente la Interfaz de salida de las tramas MCAST */
                Listener1.Base.MulticastLoopback = false;
                Listener1.Base.JoinMulticastGroup(IPAddress.Parse(Cfg.Comm.If1.McastGroup),
                    IPAddress.Parse(Cfg.Comm.If1.Ip));
                /** 20180731. Para poder pasar por una red de ROUTERS */
                Listener1.Base.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 16);
                Listener1.NewDataEvent += OnDataReceived;
                if (Version == 0)
                {
                    Listener2 = new UdpSocket(Lan2Listen);
                    /** Para seleccionar correctamente la Interfaz de salida de las tramas MCAST */
                    Listener2.Base.MulticastLoopback = false;
                    Listener2.Base.JoinMulticastGroup(IPAddress.Parse(Cfg.Comm.If2.McastGroup),
                        IPAddress.Parse(Cfg.Comm.If2.Ip));
                    /** 20180731. Para poder pasar por una red de ROUTERS */
                    Listener2.Base.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 16);
                    Listener2.NewDataEvent += OnDataReceived;
                }
                else
                {
                    Listener2 = null;
                    Listener1.Base.JoinMulticastGroup(IPAddress.Parse(Cfg.Comm.If2.McastGroup),
                        IPAddress.Parse(Cfg.Comm.If1.Ip));
                }

                Listener1.BeginReceive();
                if (Version == 0)
                {
                    Listener2.BeginReceive();
                }

                TickTimer = new Timer(TimeSpan.FromSeconds(1).TotalMilliseconds);
                TickTimer.AutoReset = false;
                TickTimer.Elapsed += OnTick;
                TickTimer.Enabled = true;

                GlobalState = SactaState.WaitingSactaActivity;

                if (Version == 0)
                {
                    Logger.Info<ScvManager>($"ScvManager for {Id}. Waiting for Sacta Activity on {Cfg.Comm.If1.Ip}:{Cfg.Comm.ListenPort}, {Cfg.Comm.If2.Ip}:{Cfg.Comm.ListenPort} ...");
                }
                else
                {
                    Logger.Info<ScvManager>($"ScvManager for {Id}. Waiting for Sacta Activity on {Cfg.Comm.If1.Ip}:{Cfg.Comm.ListenPort} ...");
                }
                Logger.Info<PsiManager>($"ScvManager for {Id}. Sectores {Cfg.Sectorization.Sectors} Posiciones {Cfg.Sectorization.Positions}");
                PS.Set(ProcessStates.Running);
            }
            catch (Exception x)
            {
                Logger.Exception<ScvManager>(x, $"On {Cfg.Id}");
                Dispose();
                PS.SignalFatal<ScvManager>($"Exception on Starting {x}", History());
            }
        }
        public override void Stop()
        {
            Logger.Info<ScvManager>($"Ending ScvManager for {Id}...");
            Dispose();
            Logger.Info<ScvManager>($"ScvManager for {Id} Ended...");
            PS.Set(ProcessStates.Stopped);
        }
        public void Dispose()
        {
            if (Listener1 != null)
            {
                Listener1.Dispose();
                Listener1.NewDataEvent -= OnDataReceived;
            }
            if (Listener2 != null)
            {
                Listener2.Dispose();
                Listener2.NewDataEvent -= OnDataReceived;
            }
            if (TickTimer != null)
            {
                TickTimer.Enabled = false;
                TickTimer.Dispose();
                TickTimer.Elapsed -= OnTick;
            }
            Listener1 = null;
            TickTimer = null;
        }
        public override object Status
        {
            get
            {
                return new
                {
                    global_state = PS.Status,
                    aut_state = GlobalState,
                    act = new
                    {
                        global = IsThereLanActivity,
                        lan1 = new
                        {
                            ActivityOnLan1,
                            LastActivityOnLan1,
                            listen = Lan1Listen.ToString(),
                            sendto = Lan1Sendto.ToString()
                        },
                        lan2 = new
                        {
                            ActivityOnLan2,
                            LastActivityOnLan2,
                            listen = Version==0 ? Lan2Listen.ToString() : Lan1Listen.ToString(),
                            sendto = Lan2Sendto.ToString()
                        },
                    },
                    tx = TxEnabled,
                    sacta_protocol = new
                    {
                        seq = Sequence,
                        LastPresenceSended,
                        LastSectorAscked = WhenSectorAsked
                    },
                };
            }
        }

        #endregion Public

        #region Protected Methods
        protected void OnDataReceived(object sender, DataGram dg)
        {
            lock (Locker)
            {
                SactaMsg.Deserialize(dg.Data, (msg) =>
                {
                    ManageOnLan(dg.Client.Address, (lan) => 
                    {
                        try
                        {
                            if (IsValid(msg))
                            {
                                switch (msg.Type)
                                {
                                    case SactaMsg.MsgType.Init:
                                    case SactaMsg.MsgType.Presence:
                                    case SactaMsg.MsgType.Sectorization:
                                        Logger.Debug<ScvManager>($"On {Cfg.Id} from Sacta Lan {lan} Valid message {msg.Type} received");
                                        if (msg.Type == SactaMsg.MsgType.Init)
                                        {
                                            // todo
                                        }
                                        else if (msg.Type == SactaMsg.MsgType.Sectorization)
                                        {
                                            if (IsSecondSectMsg(msg) == false)
                                            {
                                                ProccessSectorization(msg, (ok, error) =>
                                                {
                                                    if (ok)
                                                    {
                                                        Logger.Info<ScvManager>($"On {Cfg.Id}. Sectorization {msg.Id} Processed.");
                                                        SendSectAnsw((int)((SactaMsg.SectInfo)(msg.Info)).Version, 1);
                                                    }
                                                    else
                                                    {
                                                        Logger.Warn<ScvManager>($"On {Cfg.Id}. Sectorization {msg.Id} Rejected => {error}");
                                                        SendSectAnsw((int)((SactaMsg.SectInfo)(msg.Info)).Version, 0);
                                                    }
                                                    GlobalState = SactaState.SendingPresences;
                                                });
                                            }
                                            else
                                            {
                                                Logger.Info<ScvManager>($"Sectorization Request (Red = {lan}, Versión = {((SactaMsg.SectInfo)(msg.Info)).Version}, IGNORED. Already in Progress...");
                                            }
                                        }
                                        else
                                        {
                                            // todo
                                        }
                                        break;
                                    default:
                                        Logger.Warn<ScvManager>($"On {Cfg.Id} from Sacta Lan {lan} Invalid message {msg.Type} received");                                
                                        Logger.Trace<ScvManager>($"On {Cfg.Id} from Sacta Lan {lan} Invalid message received: {msg.ToString()}");
                                        break;
                                }
                            }
                            else
                            {
                                Logger.Warn<ScvManager>($"On {Cfg.Id} from Sacta Lan {lan} Invalid message {msg.Type} received");
                                Logger.Trace<ScvManager>($"On {Cfg.Id} from Sacta Lan {lan} Invalid message received: {msg.ToString()}");
                            }
                        }
                        catch (Exception x)
                        {
                            Logger.Exception<ScvManager>(x, $"On {Cfg.Id}");
                        }
                    });
                }, 
                (error) => // Error en el Deserialize.
                {
                    Logger.Warn<ScvManager>($"On {Cfg.Id} Deserialize Error: {error}");
                });
            }
        }
        protected void OnTick(object sender, ElapsedEventArgs e)
        {
            lock (Locker)
            {
                try
                {
                    switch (GlobalState)
                    {
                        case SactaState.WaitingSactaActivity:
                            if (IsThereLanActivity)
                            {
                                SendInit();
                                SendPresence();
                                SendSectAsk();
                                GlobalState = SactaState.WaitingSectorization;
                                Logger.Info<ScvManager>($"On {Id} while WaitingSactaActivity Activity on LAN ON ...");

                                // Evento de Conexion con SACTA.
                                SafeLaunchEvent<ActivityOnLanArgs>(EventActivity, new ActivityOnLanArgs()
                                {
                                     ScvId=Cfg.Id,
                                     ActivityOnLan = true
                                });
                            }
                            break;
                        case SactaState.SendingPresences:
                            if (!IsThereLanActivity)
                            {
                                GlobalState = SactaState.WaitingSactaActivity;
                                SactaSPSIUsers.Values.ToList().ForEach(u => u.LastSectMsgId = -1);
                                Logger.Info<ScvManager>($"On {Id} while SendingPresences Activity on LAN OFF ...");

                                // Evento de Desconexion con SACTA.
                                SafeLaunchEvent<ActivityOnLanArgs>(EventActivity, new ActivityOnLanArgs()
                                {
                                    ScvId = Cfg.Id,
                                    ActivityOnLan = false
                                });
                            }
                            else
                            {
                                if (DateTime.Now - LastPresenceSended > TimeSpan.FromSeconds(Cfg.SactaProtocol.TickAlive))
                                {
                                    SendPresence();
                                    LastPresenceSended = DateTime.Now;
                                }
                            }
                            break;
                        case SactaState.WaitingSectorization:
                            if (!IsThereLanActivity)
                            {
                                GlobalState = SactaState.WaitingSactaActivity;
                                SactaSPSIUsers.Values.ToList().ForEach(u => u.LastSectMsgId = -1);
                                Logger.Info<ScvManager>($"On {Id} while WaitingSectorization Activity on LAN OFF ...");

                                // Evento de Desconexion con SACTA.
                                SafeLaunchEvent<ActivityOnLanArgs>(EventActivity, new ActivityOnLanArgs()
                                {
                                    ScvId = Cfg.Id,
                                    ActivityOnLan = false
                                });
                            }
                            else
                            {
                                if (DateTime.Now - WhenSectorAsked > TimeSpan.FromSeconds(Cfg.SactaProtocol.SectorizationTimeout))
                                {
                                    Logger.Info<ScvManager>($"On {Id} while WaitingSectorization Request Sectorization ...");
                                    SendInit();
                                    SendPresence();
                                    SendSectAsk();
                                    LastPresenceSended = DateTime.Now;
                                }
                            }
                            break;
                    }
                }
                catch (Exception x)
                {
                    Logger.Exception<ScvManager>(x, $"On {Cfg.Id}");
                }
                finally
                {
                    TickTimer.Enabled = true;
                }
            }
        }
        protected bool BroadMessage(byte [] message)
        {
            if (TxEnabled)
            {
                if ((DateTime.Now - LastActivityOnLan1) < TimeSpan.FromSeconds(Cfg.SactaProtocol.TimeoutAlive))
                {
                    Logger.Trace<ScvManager>($"On {Id} Sending Data on LAN1 ...");
                    Listener1.Send(Lan1Sendto, message);
                }
                else
                {
                    Logger.Trace<ScvManager>($"On {Id} Discarding data on LAN1 ...");
                }
                if ((DateTime.Now - LastActivityOnLan2) < TimeSpan.FromSeconds(Cfg.SactaProtocol.TimeoutAlive))
                {
                    Logger.Trace<ScvManager>($"On {Id} Sending Data on LAN2 ...");
                    if (Version == 0)
                    {
                        Listener2.Send(Lan2Sendto, message);
                    }
                    else
                    {
                        Listener1.Send(Lan2Sendto, message);
                    }
                }
                else
                {
                    Logger.Trace<ScvManager>($"On {Id} Discarding data on LAN2 ...");
                }
                return true;
            }
            Logger.Trace<ScvManager>($"On {Id} Discarding data on LAN1/LAN2 (TxDisabled) ...");
            return false;
        }
        protected void SendInit()
        {
            Logger.Info<ScvManager>($"On {Id} (TXE {TxEnabled}) Sending Init Msg ...");
            var msg = SactaMsg.MsgToSacta(Cfg, SactaMsg.MsgType.Init, SactaMsg.InitId, 0).Serialize();
            if (BroadMessage(msg))
            {
                Sequence = 0;
                Logger.Info<ScvManager>($"On {Cfg.Id} Init Msg sended.");
            }
        }
        protected void SendPresence()
        {
            Logger.Info<ScvManager>($"On {Id} (TXE {TxEnabled}) Sending Presence Msg (Sequence {Sequence}) ...");
            var msg = SactaMsg.MsgToSacta(Cfg, SactaMsg.MsgType.Presence, 0, Sequence).Serialize();
            if (BroadMessage(msg))
            {
                Sequence = Sequence >= 287 ? 0 : Sequence + 1;
                LastPresenceSended = DateTime.Now;
                Logger.Info<ScvManager>($"On {Cfg.Id} Presence Msg sended. (New Sequence {Sequence}) ");
            }
        }
        protected void SendSectAsk()
        {
            Logger.Info<ScvManager>($"On {Id} (TXE {TxEnabled}) Sending SectAsk Msg (Sequence {Sequence}) ...");
            var msg = SactaMsg.MsgToSacta(Cfg, SactaMsg.MsgType.SectAsk, 0, Sequence).Serialize();
            if (BroadMessage(msg))
            {
                Sequence = Sequence >= 287 ? 0 : Sequence + 1;
                Logger.Info<ScvManager>($"On {Cfg.Id} SectAsk Msg sended. (New Sequence {Sequence})");
            }
            WhenSectorAsked = DateTime.Now;
        }
        protected void SendSectAnsw(int version, int result)
        {
            Logger.Info<ScvManager>($"On {Id} (TXE {TxEnabled}) Sending SectAnsw Msg (Sequence {Sequence}, Version {version}, result {result}) ...");
            var msg = SactaMsg.MsgToSacta(Cfg, SactaMsg.MsgType.SectAnswer, 0, Sequence, version, result).Serialize();
            if (BroadMessage(msg))
            {
                Sequence = Sequence >= 287 ? 0 : Sequence + 1;
                Logger.Info<ScvManager>($"On {Cfg.Id} SectAnswer Msg sended. (New Sequence {Sequence}, Version {version}, result {result})");
            }
        }
        protected bool IsValid(SactaMsg msg)
        {
            var validUsers = msg.Type == SactaMsg.MsgType.Sectorization ? SactaSPSIUsers : SactaSPVUsers;

            return ((msg.DomainOrg == Cfg.SactaProtocol.Sacta.Domain) && 
                    (msg.CenterOrg == Cfg.SactaProtocol.Sacta.Center) &&
                    validUsers.ContainsKey(msg.UserOrg) &&
                    (msg.DomainDst == Cfg.SactaProtocol.Scv.Domain) &&
                    (msg.CenterDst == Cfg.SactaProtocol.Scv.Center) &&
                    (msg.UserDst == Cfg.SactaProtocol.Scv.Scv));
        }
        protected bool IsSecondSectMsg(SactaMsg msg)
        {
            PsiOrScvInfo psi = SactaSPSIUsers.ContainsKey(msg.UserOrg) ? SactaSPSIUsers[msg.UserOrg]: null;
            if (psi == null)
            {
                Logger.Warn<ScvManager>($"On {Cfg.Id} => Mensaje de Usuario Desconocido: {msg.UserOrg}");
                return true;
            }
            else if ((psi.LastSectMsgId == msg.Id) && (psi.LastSectVersion == ((SactaMsg.SectInfo)(msg.Info)).Version))
            {
                Logger.Info<ScvManager>($"On {Cfg.Id} => Segundo MSG Sectorizacion UserOrg={msg.UserOrg}, IDs [Last:Current] {psi.LastSectMsgId}:{msg.Id}, Versions [Last:Current]{psi.LastSectVersion}:{((SactaMsg.SectInfo)(msg.Info)).Version}");
                return true;
            }
            else
            {
                Logger.Info<ScvManager>($"On {Cfg.Id} => Primer MSG Sectorizacion UserOrg={msg.UserOrg}, IDs [Last:Current] {psi.LastSectMsgId}:{msg.Id}, Versions [Last:Current]{psi.LastSectVersion}:{((SactaMsg.SectInfo)(msg.Info)).Version}");
            }

            psi.LastSectMsgId = msg.Id;
            psi.LastSectVersion = ((SactaMsg.SectInfo)(msg.Info)).Version;
            return false;
        }
        protected void ProccessSectorization(SactaMsg msg, Action<bool,string> deliver)
        {
            var sectorsReceived = ((SactaMsg.SectInfo)(msg.Info)).Sectors.ToList();
            var sectorsToProcess = sectorsReceived
                .Where(s => Cfg.Sectorization.VirtualsList().Contains(int.Parse(s.SectorCode)) == false)
                .ToList();
            var idSectorsToProcess = sectorsToProcess
                .Select(s => int.Parse(s.SectorCode)).ToList();
            var SectorsNotFound = Cfg.Sectorization.SectorsList()
                .Where(s => idSectorsToProcess.Contains(s) == false)
                .Select(s => s.ToString())
                .ToList();
            var UnknowUcs = sectorsToProcess
                .Where(s => Cfg.Sectorization.PositionsList().Contains((int)s.Ucs) == false)
                .Select(s => s.Ucs.ToString())
                .ToList();
            var UnknowSectors= sectorsToProcess
                .Where(s => Cfg.Sectorization.SectorsList().Contains(int.Parse(s.SectorCode)) == false)
                .Select(s => s.SectorCode)
                .ToList();
            var duplicatedSect = sectorsToProcess
                .GroupBy(s => s.SectorCode)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            bool err = SectorsNotFound.Count > 0 || UnknowUcs.Count() > 0 || UnknowSectors.Count() > 0 || duplicatedSect.Count() > 0;
            if (err)
            {
                var message = "";
                message += SectorsNotFound.Count() > 0 ? $"Sectors not Found: {SectorsNotFound.Aggregate((i, j) => i + ", " + j)}. " : "";
                message += UnknowUcs.Count() > 0 ? $"Unknow Ucs: {UnknowUcs.Aggregate((i, j) => i + ", " + j)}. " : "";
                message += UnknowSectors.Count() > 0 ? $"Unknow Sectors: {UnknowSectors.Aggregate((i, j) => i + ", " + j)}. " : "";
                message += duplicatedSect.Count() > 0 ? $"Duplicated Sectors: {duplicatedSect.Aggregate((i, j) => i + ", " + j)}. " : "";

                deliver(false, message);
                // Evento para el Historico.
                SafeLaunchEvent<SectorizationReceivedArgs>(EventSectorization, new SectorizationReceivedArgs()
                {
                    Accepted = false,
                    ScvId = Cfg.Id,
                    //SectorMap = sectorsToProcess.ToDictionary(s => s.SectorCode, s => (int)s.Ucs),
                    ReceivedMap = sectorsReceived
                        .Select(s=>s.ToString())
                        .Aggregate((a, i) => a + "," + i),
                    RejectCause = message
                }) ;
            }
            else
            {
                deliver(true, "");
                // Actulizar con los datos recibidos la sectorizacion global...
                SafeLaunchEvent<SectorizationReceivedArgs>(EventSectorization, new SectorizationReceivedArgs()
                {
                    Accepted = true,
                    ScvId = Cfg.Id,
                    SectorMap = sectorsToProcess.ToDictionary(s => s.SectorCode, s => (int)s.Ucs)
                }); 
            }
        }
        protected void ManageOnLan(IPAddress from, Action<int> deliver)
        {
            try
            {
                if (IpHelper.IsInSubnet(Cfg.Comm.If1.FromMask, from))
                {
                    deliver(0);
                    LastActivityOnLan1 = DateTime.Now;
                }
                else if (IpHelper.IsInSubnet(Cfg.Comm.If2.FromMask, from))
                {
                    deliver(1);
                    LastActivityOnLan2 = DateTime.Now;
                }
                else
                {
                    Logger.Error<ScvManager>($"On {Cfg.Id} Recibida Trama no identificada de {from}");
                }
            }
            catch (Exception x)
            {
                Logger.Exception<ScvManager>(x, $"On {Cfg.Id}");
            }
        }

        #endregion Protected

        #region Datos
        protected UdpSocket Listener1 { get; set; }
        protected UdpSocket Listener2 { get; set; }
        Dictionary<ushort, PsiOrScvInfo> SactaSPSIUsers { get; set; }
        Dictionary<ushort, PsiOrScvInfo> SactaSPVUsers { get; set; }
        SactaState GlobalState { get; set; }
        DateTime WhenSectorAsked { get; set; }
        Func<History> History { get; set; }

        #endregion Datos

#if DEBUG
        public void SendFromSacta(byte [] data)
        {
        }
#endif
    }
 
}
