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
using sacta_proxy.Helpers;
using sacta_proxy.WebServer;
using sacta_proxy.model;

namespace sacta_proxy.Managers
{

    class ScvManager : BaseManager, IDisposable
    {
        enum SactaState { WaitingSactaActivity, WaitingSectorization, SendingPresences, Stopped }
        
        public EventHandler<ScvManagerActivityArgs> EventActivity;
        public EventHandler<ScvManagerSectorizationArgs> EventSectorization;

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

                Listener = new UdpSocket(Cfg.Comm.Listen.Ip1, Cfg.Comm.Listen.Port);
                /** Para seleccionar correctamente la Interfaz de salida de las tramas MCAST */
                Listener.Base.MulticastLoopback = false;
                Listener.Base.JoinMulticastGroup(IPAddress.Parse(Cfg.Comm.SendTo.Ip1),
                    IPAddress.Parse(Cfg.Comm.SendTo.NetworkIf));
                Listener.Base.JoinMulticastGroup(IPAddress.Parse(Cfg.Comm.SendTo.Ip2),
                    IPAddress.Parse(Cfg.Comm.SendTo.NetworkIf));
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

                Logger.Info<ScvManager>($"ScvManager for {Id}. Waiting for Sacta Activity...");
            }
            catch (Exception x)
            {
                Logger.Exception<ScvManager>(x, $"On {Cfg.Id}");
                Dispose();
            }
        }
        public override void Stop()
        {
            Logger.Info<ScvManager>($"Stopping ScvManager for {Id}...");
            Dispose();
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

        }
        public override object Status
        {
            get
            {
                return new { Id, res = "En implementacion" };
            }
        }
        public override bool EnableTx { get; set; }

        #endregion Public

        #region Protected
        protected void OnDataReceived(object sender, DataGram dg)
        {
            lock (Locker)
            {
                SactaMsg.Deserialize(dg.Data, (msg) =>
                {
                    ManageOnLan(dg.Client.Address, (lan, LastActivityFlag) => 
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
                                        LastActivityFlag = DateTime.Now;
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
                                // Evento de Conexion con SACTA.
                                SafeLaunchEvent<ScvManagerActivityArgs>(EventActivity, new ScvManagerActivityArgs()
                                {
                                     ScvId=Cfg.Id,
                                     Activity = true
                                });
                            }
                            break;
                        case SactaState.SendingPresences:
                            if (!IsThereLanActivity)
                            {
                                GlobalState = SactaState.WaitingSactaActivity;
                                SactaSPSIUsers.Values.ToList().ForEach(u => u.LastSectMsgId = -1);
                                // Evento de Desconexion con SACTA.
                                SafeLaunchEvent<ScvManagerActivityArgs>(EventActivity, new ScvManagerActivityArgs()
                                {
                                    ScvId = Cfg.Id,
                                    Activity = false
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
                                // Evento de Desconexion con SACTA.
                                SafeLaunchEvent<ScvManagerActivityArgs>(EventActivity, new ScvManagerActivityArgs()
                                {
                                    ScvId = Cfg.Id,
                                    Activity = false
                                });
                            }
                            else
                            {
                                if (DateTime.Now - WhenSectorAsked > TimeSpan.FromSeconds(Cfg.SactaProtocol.SectorizationTimeout))
                                {
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
        protected void SendInit()
        {
            if (EnableTx)
            {
                if ((DateTime.Now - LastActivityOnLan1) < TimeSpan.FromMilliseconds(Cfg.SactaProtocol.TimeoutAlive))
                    Listener.Send(new IPEndPoint(IPAddress.Parse(Cfg.Comm.SendTo.Ip1), Cfg.Comm.SendTo.Port),
                        SactaMsg.MsgToSacta(Cfg, SactaMsg.MsgType.Init, SactaMsg.InitId, 0).Serialize());

                if ((DateTime.Now - LastActivityOnLan2) < TimeSpan.FromMilliseconds(Cfg.SactaProtocol.TimeoutAlive))
                    Listener.Send(new IPEndPoint(IPAddress.Parse(Cfg.Comm.SendTo.Ip2), Cfg.Comm.SendTo.Port),
                        SactaMsg.MsgToSacta(Cfg, SactaMsg.MsgType.Init, SactaMsg.InitId, 0).Serialize());

                Sequence = 0;
                Logger.Info<ScvManager>($"On {Cfg.Id} Init Msg sended.");
            }
        }
        protected void SendPresence()
        {
            if (EnableTx)
            {
                if ((DateTime.Now - LastActivityOnLan1) < TimeSpan.FromMilliseconds(Cfg.SactaProtocol.TimeoutAlive))
                    Listener.Send(new IPEndPoint(IPAddress.Parse(Cfg.Comm.SendTo.Ip1), Cfg.Comm.SendTo.Port),
                        SactaMsg.MsgToSacta(Cfg, SactaMsg.MsgType.Presence, 0, Sequence).Serialize());

                if ((DateTime.Now - LastActivityOnLan2) < TimeSpan.FromMilliseconds(Cfg.SactaProtocol.TimeoutAlive))
                    Listener.Send(new IPEndPoint(IPAddress.Parse(Cfg.Comm.SendTo.Ip2), Cfg.Comm.SendTo.Port),
                        SactaMsg.MsgToSacta(Cfg, SactaMsg.MsgType.Presence, 0, Sequence).Serialize());
                Sequence = Sequence >= 287 ? 0 : Sequence + 1;
                LastPresenceSended = DateTime.Now;
                Logger.Info<ScvManager>($"On {Cfg.Id} Presence Msg sended.");
            }
        }
        protected void SendSectAsk()
        {
            if (EnableTx)
            {
                if ((DateTime.Now - LastActivityOnLan1) < TimeSpan.FromMilliseconds(Cfg.SactaProtocol.TimeoutAlive))
                    Listener.Send(new IPEndPoint(IPAddress.Parse(Cfg.Comm.SendTo.Ip1), Cfg.Comm.SendTo.Port),
                        SactaMsg.MsgToSacta(Cfg, SactaMsg.MsgType.SectAsk, 0, Sequence).Serialize());

                if ((DateTime.Now - LastActivityOnLan2) < TimeSpan.FromMilliseconds(Cfg.SactaProtocol.TimeoutAlive))
                    Listener.Send(new IPEndPoint(IPAddress.Parse(Cfg.Comm.SendTo.Ip2), Cfg.Comm.SendTo.Port),
                        SactaMsg.MsgToSacta(Cfg, SactaMsg.MsgType.SectAsk, 0, Sequence).Serialize());
                Sequence = Sequence >= 287 ? 0 : Sequence + 1;
                WhenSectorAsked = DateTime.Now;
                Logger.Info<ScvManager>($"On {Cfg.Id} SectAsk Msg sended.");
            }
        }
        protected void SendSectAnsw(int version, int result)
        {
            if (EnableTx)
            {
                if ((DateTime.Now - LastActivityOnLan1) < TimeSpan.FromMilliseconds(Cfg.SactaProtocol.TimeoutAlive))
                    Listener.Send(new IPEndPoint(IPAddress.Parse(Cfg.Comm.SendTo.Ip1), Cfg.Comm.SendTo.Port),
                        SactaMsg.MsgToSacta(Cfg, SactaMsg.MsgType.SectAnswer, 0, Sequence, version, result).Serialize());

                if ((DateTime.Now - LastActivityOnLan2) < TimeSpan.FromMilliseconds(Cfg.SactaProtocol.TimeoutAlive))
                    Listener.Send(new IPEndPoint(IPAddress.Parse(Cfg.Comm.SendTo.Ip2), Cfg.Comm.SendTo.Port),
                        SactaMsg.MsgToSacta(Cfg, SactaMsg.MsgType.SectAnswer, 0, Sequence, version, result).Serialize());
                Sequence = Sequence >= 287 ? 0 : Sequence + 1;
                Logger.Info<ScvManager>($"On {Cfg.Id} SectAnswer Msg sended.");
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
        protected void ManageOnLan(IPAddress from, Action<int, DateTime> deliver)
        {
            try
            {
                if (IpHelper.IsInSubnet(Cfg.Comm.Listen.FromMask1, from))
                    deliver(0,LastActivityOnLan1);
                else if (IpHelper.IsInSubnet(Cfg.Comm.Listen.FromMask2, from))
                    deliver(1,LastActivityOnLan2);
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
                if (UnknowUcs.Count() > 0)
                    deliver(false, $"Unknow Ucs: {UnknowUcs.Aggregate((i, j) => i + ", " + j)}");
                if (UnknowSectors.Count() > 0)
                    deliver(false, $"Unknow Sectors: {UnknowSectors.Aggregate((i, j) => i + ", " + j)}");
                if (duplicatedSect.Count() > 0)
                    deliver(false, $"Duplicated Sectors: {duplicatedSect.Aggregate((i, j) => i + ", " + j)}");
            }
            else
            {
                // Actulizar con los datos recibidos la sectorizacion global...
                SafeLaunchEvent<ScvManagerSectorizationArgs>(EventSectorization, new ScvManagerSectorizationArgs()
                {
                    ScvId = Cfg.Id,
                    SectorMap = sectorsToProcess.ToDictionary(s => s.SectorCode, s => (int)s.Ucs)
                });
                deliver(true, "");
            }
        }
        protected bool IsThereLanActivity
        {
            get
            {
                return DateTime.Now - LastActivityOnLan1 < TimeSpan.FromSeconds(Cfg.SactaProtocol.TimeoutAlive) ||
                    DateTime.Now - LastActivityOnLan2 < TimeSpan.FromSeconds(Cfg.SactaProtocol.TimeoutAlive);
            }
        }
        
        #endregion Protected

        #region Datos
        string Id { get; set; }
        Configuration.DependecyConfig Cfg { get; set; }
        Dictionary<ushort, PSIInfo> SactaSPSIUsers { get; set; }
        Dictionary<ushort, PSIInfo> SactaSPVUsers { get; set; }
        UdpSocket Listener { get; set; }
        System.Timers.Timer TickTimer { get; set; }
        SactaState GlobalState { get; set; }
        DateTime LastActivityOnLan1 { get; set; }
        DateTime LastActivityOnLan2 { get; set; }
        DateTime LastPresenceSended { get; set; }
        DateTime WhenSectorAsked { get; set; }
        object Locker { get; set; }
        int Sequence { get; set; }
        #endregion Datos

#if DEBUG
        public void SendFromSacta(byte [] data)
        {
        }
#endif
    }
 
}
