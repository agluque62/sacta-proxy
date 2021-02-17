using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Timers;

using sacta_proxy.Helpers;
using sacta_proxy.WebServer;
using sacta_proxy.model;

namespace sacta_proxy.Managers
{
    class PsiManager : BaseManager, IDisposable
    {
        #region Events
        public event EventHandler<ActivityOnLanArgs> EventActivity;
        public event EventHandler<SectorizationRequestArgs> EventSectRequest;
        #endregion Events

        #region Publics
        public override void Start(Configuration.DependecyConfig cfg)
        {
            Logger.Info<PsiManager>($"Starting PsiManager...");
            try
            {
                Locker = new object();
                Cfg = cfg;
                EnableTx = false;
                ScvActivity = false;
                Sequence = 0;
                SectorizationVersion = 0;
                LastActivityOnLan1 = DateTime.MinValue;
                LastActivityOnLan2 = DateTime.MinValue;
                LastPresenceSended = DateTime.MinValue;

                Listener1 = new UdpSocket(Cfg.Comm.Listen.Lan1.McastIf, Cfg.Comm.Listen.Port);
                /** Para seleccionar correctamente la Interfaz de salida de las tramas MCAST */
                Listener1.Base.MulticastLoopback = false;
                Listener1.Base.JoinMulticastGroup(IPAddress.Parse(Cfg.Comm.Listen.Lan1.McastGroup),
                    IPAddress.Parse(Cfg.Comm.Listen.Lan1.McastIf));
                Listener1.Base.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 16);
                Listener1.NewDataEvent += OnDataReceived;
                Listener1.BeginReceive();

                Listener2 = new UdpSocket(Cfg.Comm.Listen.Lan2.McastIf, Cfg.Comm.Listen.Port);
                /** Para seleccionar correctamente la Interfaz de salida de las tramas MCAST */
                Listener2.Base.MulticastLoopback = false;
                Listener2.Base.JoinMulticastGroup(IPAddress.Parse(Cfg.Comm.Listen.Lan2.McastGroup),
                    IPAddress.Parse(Cfg.Comm.Listen.Lan2.McastIf));
                Listener2.Base.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 16);
                Listener2.NewDataEvent += OnDataReceived;
                Listener2.BeginReceive();

                TickTimer = new Timer(TimeSpan.FromSeconds(1).TotalMilliseconds);
                TickTimer.AutoReset = false;
                TickTimer.Elapsed += OnTick;
                TickTimer.Enabled = true;

                SendInitMsg();
                Logger.Info<PsiManager>($"PsiManager. Waiting for SCV Activity on {Cfg.Comm.Listen.Lan1.McastIf}:{Cfg.Comm.Listen.Port} / {Cfg.Comm.Listen.Lan2.McastIf}:{Cfg.Comm.Listen.Port}");
                PS.Set(ProcessStates.Running);
            }
            catch (Exception x)
            {
                Logger.Exception<PsiManager>(x, $"On PSI");
                Dispose();
                PS.SignalFatal<PsiManager>($"Exception on Starting => {x}");
            }
        }
        public override void Stop()
        {
            Logger.Info<PsiManager>($"Ending PsiManager...");
            Dispose();
            Logger.Info<PsiManager>($"PsiManager Ended...");
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
            Listener1 = Listener2 = null;
            TickTimer = null;
        }
        public override object Status 
        { 
            get
            {
                return new
                {
                    global_state = PS.Status,
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
                        ver = SectorizationVersion,
                        LastPresenceSended,
                    },
                };
            }
        }
        public override bool EnableTx { get; set; }
        public void SendSectorization(Dictionary<string, int> sectorMap)
        {
            lock (Locker)
            {
                SendSectorizationMsg(sectorMap);
            }
        }
        #endregion Publics

        #region Protected Methods
        protected void OnDataReceived(object sender, DataGram dg)
        {
            lock (Locker)
            {
                SactaMsg.Deserialize(dg.Data, (msg) =>
                {
                    ManageOnLan(sender as UdpSocket, (lan, LastActivityFlag) =>
                    {
                        try
                        {
                            if (IsValid(msg))
                            {
                                switch (msg.Type)
                                {
                                    case SactaMsg.MsgType.Init:
                                    case SactaMsg.MsgType.Presence:
                                    case SactaMsg.MsgType.SectAsk:
                                    case SactaMsg.MsgType.SectAnswer:
                                        Logger.Debug<PsiManager>($"On PSI from Scv Lan {lan} Valid message {msg.Type} received");
                                        if (msg.Type == SactaMsg.MsgType.Init)
                                        {
                                        }
                                        else if (msg.Type == SactaMsg.MsgType.Presence)
                                        { 
                                        }
                                        else if (msg.Type == SactaMsg.MsgType.SectAsk)
                                        {
                                            Logger.Info<PsiManager>($"On PSI from Scv Lan {lan} Sectorization Sectoriztion Request Received");
                                            SafeLaunchEvent<SectorizationRequestArgs>(EventSectRequest, new SectorizationRequestArgs());
                                        }
                                        else if (msg.Type == SactaMsg.MsgType.SectAnswer)
                                        {
                                            SactaMsg.SectAnswerInfo info = (SactaMsg.SectAnswerInfo)(msg.Info);
                                            Logger.Info<PsiManager>($"On PSI from Scv Lan {lan} Sectorization {info.Version} {(info.Result == 1 ? "Accepted" : "Rejected")}.");
                                        }
                                        LastActivityFlag = DateTime.Now;
                                        break;
                                    default:
                                        Logger.Warn<PsiManager>($"On PSI from SCV Lan {lan} Invalid message {msg.Type} received");
                                        Logger.Trace<PsiManager>($"On PSI from SCV Lan {lan} Invalid message received: {msg.ToString()}");
                                        break;
                                }
                            }
                            else
                            {
                                Logger.Warn<PsiManager>($"On PSI from SCV Lan {lan} Invalid message {msg.Type} received");
                                Logger.Trace<PsiManager>($"On PSI from SCV Lan {lan} Invalid message received: {msg.ToString()}");
                            }
                        }
                        catch (Exception x)
                        {
                            Logger.Exception<PsiManager>(x, $"On PSI");
                        }
                    });
                },
                (error) => // Error en el Deserialize.
                {
                    Logger.Warn<PsiManager>($"On PSI Deserialize Error: {error}");
                });
            }
        }
        protected void OnTick(object sender, ElapsedEventArgs e)
        {
            lock (Locker)
            {
                try
                {
                    if (!ScvActivity && IsThereLanActivity)
                    {
                        ScvActivity = true;
                        Logger.Info<PsiManager>($"On Psi Activity on LAN ON ...");
                        // Evento de Conexion con SCV.
                        SafeLaunchEvent<ActivityOnLanArgs>(EventActivity, new ActivityOnLanArgs()
                        {
                            ActivityOnLan = true
                        });
                    }
                    else if (ScvActivity && !IsThereLanActivity)
                    {
                        ScvActivity = false;
                        Logger.Info<PsiManager>($"On Psi Activity on LAN OFF ...");
                        // Evento de Desconexion con SCV.
                        SafeLaunchEvent<ActivityOnLanArgs>(EventActivity, new ActivityOnLanArgs()
                        {
                            ActivityOnLan = false
                        });
                    }
                    if (DateTime.Now - LastPresenceSended > TimeSpan.FromSeconds(Cfg.SactaProtocol.TickAlive))
                    {
                        SendPresenceMsg();
                        LastPresenceSended = DateTime.Now;
                    }
                }
                catch (Exception x)
                {
                    Logger.Exception<PsiManager>(x, $"On PSI");
                }
                finally
                {
                    TickTimer.Enabled = true;
                }
            }
        }
        protected void ManageOnLan(UdpSocket ListenerFrom, Action<int, DateTime> deliver)
        {
            if (ListenerFrom == Listener1)
                deliver(0, LastActivityOnLan1);
            else if (ListenerFrom == Listener2)
                deliver(1, LastActivityOnLan2);
            else
            {
                Logger.Warn<PsiManager>($"On PSI Recibida Trama no identificada...");
            }
        }
        protected bool IsValid(SactaMsg msg)
        {
            return ((msg.DomainOrg == Cfg.SactaProtocol.Scv.Domain) &&
                    (msg.CenterOrg == Cfg.SactaProtocol.Scv.Center) &&
                    (msg.UserOrg == Cfg.SactaProtocol.Scv.Scv) &&
                    (msg.DomainDst == Cfg.SactaProtocol.Sacta.Domain) &&
                    (msg.CenterDst == Cfg.SactaProtocol.Sacta.Center) &&
                    (msg.UserDst == Cfg.SactaProtocol.Sacta.PsiGroup));
        }
        protected bool BroadMessage(byte[] message)
        {
            if (EnableTx)
            {
                Logger.Trace<PsiManager>($"On PSI Sending Data on LAN1 ...");
                Listener1.Send(new IPEndPoint(IPAddress.Parse(Cfg.Comm.SendTo.Lan1.Ip), Cfg.Comm.SendTo.Port), message);
                Logger.Trace<PsiManager>($"On PSI Sending Data on LAN2 ...");
                Listener2.Send(new IPEndPoint(IPAddress.Parse(Cfg.Comm.SendTo.Lan2.Ip), Cfg.Comm.SendTo.Port), message);
                return true;
            }
            Logger.Trace<PsiManager>($"On PSI Discarding data on LAN1/LAN2 (TxDisabled) ...");
            return false;
        }
        protected void SendInitMsg()
        {
            Logger.Trace<PsiManager>($"On PSI Sending Init Msg ...");
            var msg = SactaMsg.MsgToScv(Cfg, SactaMsg.MsgType.Init, SactaMsg.InitId, 0).Serialize();
            if (BroadMessage(msg))
            {
                Sequence = 0;
                Logger.Info<PsiManager>($"On PSI Init Msg sended.");
            }
        }
        protected void SendPresenceMsg()
        {
            Logger.Trace<PsiManager>($"On PSI Sending Presence Msg (Sequence {Sequence}...");
            var msg = SactaMsg.MsgToScv(Cfg, SactaMsg.MsgType.Presence, 0, Sequence).Serialize();
            if (BroadMessage(msg))
            {
                Sequence = Sequence >= 287 ? 0 : Sequence + 1;
                LastPresenceSended = DateTime.Now;
                Logger.Info<PsiManager>($"On PSI Presence Msg sended. (New Sequence {Sequence})");
            }
        }
        protected void SendSectorizationMsg(Dictionary<string,int> sectorUcs)
        {
            Logger.Trace<PsiManager>($"On PSI Sending Sectorization Msg (Sequence {Sequence}...");
            var msg = SactaMsg.MsgToScv(Cfg, SactaMsg.MsgType.Sectorization, 0, Sequence, SectorizationVersion, sectorUcs).Serialize();
            if (BroadMessage(msg))
            {
                Sequence = Sequence >= 287 ? 0 : Sequence + 1;
                SectorizationVersion++;
                Logger.Info<PsiManager>($"On PSI Sectorization Msg sended. (New Sequence {Sequence}, New Version {SectorizationVersion})");
            }
        }
        #endregion

        #region Private Data
        bool ScvActivity { get; set; }
        int SectorizationVersion { get; set; }
        UdpSocket Listener1, Listener2;
        #endregion
    }
}
