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
        public override void Start(Configuration.DependecyConfig cfg)
        {
            Id = cfg.Id;
            Cfg = cfg;
            Logger.Info<ScvManager>($"Starting ScvManager for {Id}...");
            try
            {
                Locker = new object();
                SactaSPSIUsers = Cfg.SactaProtocol.Sacta.Psis.Select(i=>(ushort)i).ToDictionary(i => i, i => new PSIInfo());
                SactaSPVUsers = Cfg.SactaProtocol.Sacta.Spvs.Select(i=>(ushort)i).ToDictionary(i => i, i => new PSIInfo());
                GlobalState = SactaState.Stopped;
                EnableTx = false;

                Listener = new UdpSocket(Cfg.Comm.Listen.Lan1.Ip, Cfg.Comm.Listen.Port);
                /** Para seleccionar correctamente la Interfaz de salida de las tramas MCAST */
                Listener.Base.MulticastLoopback = false;
                Listener.Base.JoinMulticastGroup(IPAddress.Parse(Cfg.Comm.SendTo.Lan1.McastGroup),
                    IPAddress.Parse(Cfg.Comm.SendTo.Lan1.McastIf));
                Listener.Base.JoinMulticastGroup(IPAddress.Parse(Cfg.Comm.SendTo.Lan2.McastGroup),
                    IPAddress.Parse(Cfg.Comm.SendTo.Lan2.McastIf));
                /** 20180731. Para poder pasar por una red de ROUTERS */
                Listener.Base.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 16);
                Listener.NewDataEvent += OnDataReceived;
                Listener.BeginReceive();

                TickTimer = new Timer(TimeSpan.FromSeconds(1).TotalMilliseconds);
                TickTimer.AutoReset = false;
                TickTimer.Elapsed += OnTick;
                TickTimer.Enabled = true;

                GlobalState = SactaState.WaitingSactaActivity;
                LastActivityOnLan1 = DateTime.MinValue;
                LastActivityOnLan2 = DateTime.MinValue;
                LastPresenceSended = DateTime.MinValue;
                WhenSectorAsked = DateTime.MinValue;

                Logger.Info<ScvManager>($"ScvManager for {Id}. Waiting for Sacta Activity on {Cfg.Comm.Listen.Lan1.Ip}:{Cfg.Comm.Listen.Port} ...");
                PS.Set(ProcessStates.Running);
            }
            catch (Exception x)
            {
                Logger.Exception<ScvManager>(x, $"On {Cfg.Id}");
                Dispose();
                PS.SignalFatal<ScvManager>($"Exception on Starting {x}");
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
            if (Listener != null)
            {
                Listener.Dispose();
                Listener.NewDataEvent -= OnDataReceived;
            }
            if (TickTimer != null)
            {
                TickTimer.Enabled = false;
                TickTimer.Dispose();
                TickTimer.Elapsed -= OnTick;
            }
            Listener = null;
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
                            LastActivityOnLan1
                        },
                        lan2 = new
                        {
                            ActivityOnLan2,
                            LastActivityOnLan2,
                        },
                    },
                    tx = EnableTx,
                    sacta_protocol = new
                    {
                        seq = Sequence,
                        LastPresenceSended,
                        LastSectorAscked = WhenSectorAsked
                    },
                };
            }
        }
        public override bool EnableTx { get; set; }

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
                                                        SendSectAnsw((int)((SactaMsg.SectInfo)(msg.Info)).Version, 0);
                                                    }
                                                    else
                                                    {
                                                        Logger.Warn<ScvManager>($"On {Cfg.Id}. Sectorization {msg.Id} Rejected => {error}");
                                                        SendSectAnsw((int)((SactaMsg.SectInfo)(msg.Info)).Version, 1);
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
                                        LastActivityOnLan1 = lan==0 ? DateTime.Now : LastActivityOnLan1;
                                        LastActivityOnLan2 = lan==1 ? DateTime.Now : LastActivityOnLan2;
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
            if (EnableTx)
            {
                if ((DateTime.Now - LastActivityOnLan1) < TimeSpan.FromMilliseconds(Cfg.SactaProtocol.TimeoutAlive))
                {
                    Logger.Trace<ScvManager>($"On {Id} Sending Data on LAN1 ...");
                    Listener.Send(new IPEndPoint(IPAddress.Parse(Cfg.Comm.SendTo.Lan1.McastGroup), Cfg.Comm.SendTo.Port), message);
                }
                else
                {
                    Logger.Trace<ScvManager>($"On {Id} Discarding data on LAN1 ...");
                }
                if ((DateTime.Now - LastActivityOnLan2) < TimeSpan.FromMilliseconds(Cfg.SactaProtocol.TimeoutAlive))
                {
                    Logger.Trace<ScvManager>($"On {Id} Sending Data on LAN2 ...");
                    Listener.Send(new IPEndPoint(IPAddress.Parse(Cfg.Comm.SendTo.Lan2.McastGroup), Cfg.Comm.SendTo.Port), message);
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
            Logger.Trace<ScvManager>($"On {Id} Sending Init Msg ...");
            var msg = SactaMsg.MsgToSacta(Cfg, SactaMsg.MsgType.Init, SactaMsg.InitId, 0).Serialize();
            if (BroadMessage(msg))
            {
                Sequence = 0;
                Logger.Info<ScvManager>($"On {Cfg.Id} Init Msg sended.");
            }
        }
        protected void SendPresence()
        {
            Logger.Trace<ScvManager>($"On {Id} Sending Presence Msg (Sequence {Sequence}) ...");
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
            Logger.Trace<ScvManager>($"On {Id} Sending SectAsk Msg (Sequence {Sequence}) ...");
            var msg = SactaMsg.MsgToSacta(Cfg, SactaMsg.MsgType.SectAsk, 0, Sequence).Serialize();
            if (BroadMessage(msg))
            {
                Sequence = Sequence >= 287 ? 0 : Sequence + 1;
                WhenSectorAsked = DateTime.Now;
                Logger.Info<ScvManager>($"On {Cfg.Id} SectAsk Msg sended. (New Sequence {Sequence})");
            }
        }
        protected void SendSectAnsw(int version, int result)
        {
            Logger.Trace<ScvManager>($"On {Id} Sending SectAnsw Msg (Sequence {Sequence}, Version {version}, result {result}) ...");
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
            PSIInfo psi = SactaSPSIUsers.ContainsKey(msg.UserOrg) ? SactaSPSIUsers[msg.UserOrg]: null;
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
                .Where(s => Cfg.Sectorization.Virtuals.Contains(int.Parse(s.SectorCode)) == false)
                .ToList();
            var UnknowUcs = sectorsToProcess
                .Where(s => Cfg.Sectorization.Positions.Contains((int)s.Ucs) == false)
                .Select(s => s.Ucs.ToString())
                .ToList();
            var UnknowSectors= sectorsToProcess
                .Where(s => Cfg.Sectorization.Sectors.Contains(int.Parse(s.SectorCode)) == false)
                .Select(s => s.SectorCode)
                .ToList();
            var duplicatedSect = sectorsToProcess
                .GroupBy(s => s.SectorCode)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            bool err = UnknowUcs.Count() > 0 || UnknowSectors.Count() > 0 || duplicatedSect.Count() > 0;
            if (err)
            {
                var message = UnknowUcs.Count() > 0 ? $"Unknow Ucs: {UnknowUcs.Aggregate((i, j) => i + ", " + j)}. " : "";
                message += UnknowSectors.Count() > 0 ? $"Unknow Sectors: {UnknowSectors.Aggregate((i, j) => i + ", " + j)}. " : "";
                message += duplicatedSect.Count() > 0 ? $"Duplicated Sectors: {duplicatedSect.Aggregate((i, j) => i + ", " + j)}. " : "";

                // Evento para el Historico.
                SafeLaunchEvent<SectorizationReceivedArgs>(EventSectorization, new SectorizationReceivedArgs()
                {
                    Accepted = false,
                    ScvId = Cfg.Id,
                    SectorMap = sectorsToProcess.ToDictionary(s => s.SectorCode, s => (int)s.Ucs),
                    RejectCause = message
                });
                deliver(false, message);
                //if (UnknowUcs.Count() > 0)
                //    deliver(false, $"Unknow Ucs: {UnknowUcs.Aggregate((i, j) => i + ", " + j)}");
                //if (UnknowSectors.Count() > 0)
                //    deliver(false, $"Unknow Sectors: {UnknowSectors.Aggregate((i, j) => i + ", " + j)}");
                //if (duplicatedSect.Count() > 0)
                //    deliver(false, $"Duplicated Sectors: {duplicatedSect.Aggregate((i, j) => i + ", " + j)}");
            }
            else
            {
                // Actulizar con los datos recibidos la sectorizacion global...
                SafeLaunchEvent<SectorizationReceivedArgs>(EventSectorization, new SectorizationReceivedArgs()
                {
                    Accepted = true,
                    ScvId = Cfg.Id,
                    SectorMap = sectorsToProcess.ToDictionary(s => s.SectorCode, s => (int)s.Ucs)
                }); 
                deliver(true, "");
            }
        }
        protected void ManageOnLan(IPAddress from, Action<int> deliver)
        {
            try
            {
                if (IpHelper.IsInSubnet(Cfg.Comm.Listen.Lan1.FromMask, from))
                {
                    deliver(0);
                }
                else if (IpHelper.IsInSubnet(Cfg.Comm.Listen.Lan2.FromMask, from))
                {
                    deliver(1);
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
        protected UdpSocket Listener { get; set; }
        Dictionary<ushort, PSIInfo> SactaSPSIUsers { get; set; }
        Dictionary<ushort, PSIInfo> SactaSPVUsers { get; set; }
        SactaState GlobalState { get; set; }
        DateTime WhenSectorAsked { get; set; }
        #endregion Datos

#if DEBUG
        public void SendFromSacta(byte [] data)
        {
        }
#endif
    }
 
}
