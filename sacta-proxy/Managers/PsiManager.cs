using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Timers;

using sacta_proxy.helpers;
using sacta_proxy.WebServer;
using sacta_proxy.model;

namespace sacta_proxy.Managers
{
    class PsiManager : BaseManager, IDisposable
    {
        #region Events
        public event EventHandler<SectorizationRequestArgs> EventSectRequest;
        public event EventHandler<ScvActivityEventArgs> EventScvActivity;
        #endregion Events

        #region Publics
        public PsiManager(int ProtocolVersion, Configuration.DependecyConfig cfg, Func<History> hist)
        {
            Cfg = cfg;
            Version = ProtocolVersion;
            History = hist;
            Locker = new object();
            TxEnabled = false;
            ScvActivity = false;
            Sequence = 0;
            SectorizationVersion = 0;
            LastActivityOfLan1 = DateTime.MinValue;
            LastActivityOfLan2 = DateTime.MinValue;
            LastPresenceSended = DateTime.MinValue;

            Lan1Listen = new IPEndPoint(IPAddress.Parse(Cfg.Comm.If1.Ip), Cfg.Comm.ListenPort);
            Lan1Sendto = new IPEndPoint(IPAddress.Parse(Cfg.Comm.If1.IpTo), Cfg.Comm.SendingPort);
            Lan2Listen = new IPEndPoint(IPAddress.Parse(Cfg.Comm.If2.Ip), Cfg.Comm.ListenPort);
            Lan2Sendto = new IPEndPoint(IPAddress.Parse(Version == 0 ? Cfg.Comm.If2.IpTo : Cfg.Comm.If1.IpTo), Cfg.Comm.SendingPort);

        }
        public override void Start()
        {
            Logger.Info<PsiManager>($"Starting PsiManager...");
            try
            {

                Listener1 = new UdpSocket(Lan1Listen);
                /** Para seleccionar correctamente la Interfaz de salida de las tramas MCAST */
                //Listener1.Base.MulticastLoopback = false;
                Listener1.Base.JoinMulticastGroup(IPAddress.Parse(Cfg.Comm.If1.McastGroup),
                    IPAddress.Parse(Cfg.Comm.If1.Ip));
                Listener1.Base.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 16);
                Listener1.NewDataEvent += OnDataReceived;
                Listener1.BeginReceive();

                Listener2 = new UdpSocket(Lan2Listen);
                /** Para seleccionar correctamente la Interfaz de salida de las tramas MCAST */
                //Listener2.Base.MulticastLoopback = false;
                Listener2.Base.JoinMulticastGroup(IPAddress.Parse(Cfg.Comm.If2.McastGroup),
                    IPAddress.Parse(Cfg.Comm.If2.Ip));
                Listener2.Base.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 16);
                Listener2.NewDataEvent += OnDataReceived;
                Listener2.BeginReceive();

                TickTimer = new Timer(TimeSpan.FromSeconds(1).TotalMilliseconds);
                TickTimer.AutoReset = false;
                TickTimer.Elapsed += OnTick;
                TickTimer.Enabled = true;

                SendInitMsg();
                Logger.Info<PsiManager>($"PsiManager. Waiting for SCV Activity on {Cfg.Comm.If1.Ip}:{Cfg.Comm.ListenPort} / {Cfg.Comm.If2.Ip}:{Cfg.Comm.ListenPort}");
                Logger.Info<PsiManager>($"PsiManager. Sectores {Cfg.Sectorization.Sectors} Posiciones {Cfg.Sectorization.Positions}");
                PS.Set(ProcessStates.Running);
            }
            catch (Exception x)
            {
                Logger.Exception<PsiManager>(x, $"On PSI");
                Dispose();
                PS.SignalFatal<PsiManager>($"Excepcion en el Arranque => {x}", History());
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
                    aut_state = SactaState.SendingPresences,
                    act = new 
                    { 
                        global = IsThereLanActivity, 
                        lan1 = new
                        {
                            ActivityOnLan1,
                            LastActivityOfLan1,
                            listen = Lan1Listen.ToString(),
                            sendto = Lan1Sendto.ToString()
                        },
                        lan2 = new
                        {
                            ActivityOnLan2,
                            LastActivityOfLan2,
                            listen = Lan2Listen.ToString(),
                            sendto = Lan2Sendto.ToString()
                        },
                    },
                    tx = TxEnabled,
                    sacta_protocol = new 
                    { 
                        seq = Sequence, 
                        ver = SectorizationVersion,
                        LastPresenceSended,
                    },
                };
            }
        }
        public void SendSectorization(Dictionary<string, int> sectorMap, Action<bool> response)
        {
            lock (Locker)
            {
                SendSectorizationMsg(sectorMap);
                SectResponse = response;
            }
        }
        public bool PreprocessSectorizationToSend(Dictionary<string, int> sectorMap, Action<string> OnError)
        {
            var idSectorsToProcess = sectorMap.Keys.Select(k => k)
                .Where(s => Cfg.Sectorization.VirtualsList().Contains(int.Parse(s)) == false)
                .Select(s => int.Parse(s));
            var SectorsNotFound = Cfg.Sectorization.SectorsList()
                .Where(s => idSectorsToProcess.Contains(s) == false)
                .Select(s => s.ToString())
                .ToList();
            if (SectorsNotFound.Count > 0)
            {
                OnError($"Sectores no Encontrados: {String.Join(", ", SectorsNotFound)}. ");
                return false;
            }
            return true;
        }
        #endregion Publics

        #region Protected Methods
        protected void OnDataReceived(object sender, DataGram dg)
        {
            lock (Locker)
            {
                SactaMsg.Deserialize(dg.Data, (msg) =>
                {
                    ManageOnLan(sender as UdpSocket, (lan) =>
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
                                        Logger.Debug<PsiManager>($"On PSI from Scv Lan {lan} Valid message {msg.Type} received.  Id = {msg.Id}");
                                        if (msg.Type == SactaMsg.MsgType.Init)
                                        {
                                            // Si el mensaje llega por las dos redes solo respondo a uno
                                            if (InitMsgPending == false)
                                            {
                                                InitMsgPending = true;
                                                // Una vez tratado el primento, abro una ventana de no atencion de 500 msg.
                                                Task.Run(() => { Task.Delay(500).Wait(); InitMsgPending = false; });

                                                //if (ScvActivity == true)
                                                //{
                                                //    // Hay un reset de SCV que no se ha detectado por TIMEOUT...
                                                //    Logger.Warn<PsiManager>($"On Psi Activity on LAN OFF. Cause: Init Received ...");
                                                //    // Evento de Desconexion con SCV.
                                                //    LaunchEventActivity(WhatLanItems.Global, false);
                                                //}

                                                SafeLaunchEvent<ScvActivityEventArgs>(EventScvActivity, new ScvActivityEventArgs()
                                                {
                                                    ScvId = "PSI",
                                                    OnOff = true
                                                });
                                                ScvActivity = true;
                                            }
                                        }
                                        else if (msg.Type == SactaMsg.MsgType.Presence)
                                        { 
                                        }
                                        else if (msg.Type == SactaMsg.MsgType.SectAsk)
                                        {
                                            Logger.Info<PsiManager>($"On PSI from Scv Lan {lan} Sectorization Sectorization Request Received");
                                            // Si el mensaje llega por las dos redes solo respondo una vez...
                                            if (SectAskpending == false)
                                            {
                                                SectAskpending = true;
                                                // Una vez respondido a la primera, abro una ventana de no atencion de 500 msg.
                                                Task.Run(() => { Task.Delay(500).Wait(); SectAskpending = false; });
                                                SafeLaunchEvent<SectorizationRequestArgs>(EventSectRequest, new SectorizationRequestArgs());
                                            }
                                            else
                                            {
                                                Logger.Warn<PsiManager>($"On PSI from Scv lan {lan}. Sectorization Request Message Ignored...");
                                            }
                                        }
                                        else if (msg.Type == SactaMsg.MsgType.SectAnswer)
                                        {
                                            SactaMsg.SectAnswerInfo info = (SactaMsg.SectAnswerInfo)(msg.Info);
                                            Logger.Info<PsiManager>($"On PSI from Scv Lan {lan} Sectorization {info.Version} {(info.Result == 1 ? "Accepted" : "Rejected")}.");
                                            if (SectResponse!=null)
                                                SectResponse(info.Result == 1);
                                            SectResponse = null;
                                        }
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
                    //if (!ScvActivity && IsThereLanActivity)
                    //{
                    //    ScvActivity = true;
                    //    Logger.Info<PsiManager>($"On Psi Activity on LAN ON ...");
                    //    // Evento de Conexion con SCV.
                    //    LaunchEventActivity(WhatLanItems.Global, true);
                    //}
                    //else if (ScvActivity && !IsThereLanActivity)
                    //{
                    //    ScvActivity = false;
                    //    Logger.Info<PsiManager>($"On Psi Activity SCV OFF ...");
                    //    // Evento de Desconexion con SCV.
                    //    LaunchEventActivity(WhatLanItems.Global, false);
                    //}
                    //if (!ScvActivity && IsThereLanActivity)
                    //{
                    //    Logger.Info<PsiManager>($"On Psi Activity on LAN ON ...");
                    //    // Evento de Desconexion con SCV.
                    //    SafeLaunchEvent<ScvActivityEventArgs>(EventScvActivity, new ScvActivityEventArgs()
                    //    {
                    //        ScvId = "PSI",
                    //        OnOff = true
                    //    });
                    //    ScvActivity = true;
                    //}
                    //else
                    if (ScvActivity && !IsThereLanActivity)
                    {
                        Logger.Info<PsiManager>($"On Psi Activity SCV OFF ...");
                        // Evento de Desconexion con SCV.
                        SafeLaunchEvent<ScvActivityEventArgs>(EventScvActivity, new ScvActivityEventArgs()
                        {
                            ScvId = "PSI",
                            OnOff = false
                        });
                        ScvActivity = false;
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
        protected void ManageOnLan(UdpSocket ListenerFrom, Action<int> deliver)
        {
            if (ListenerFrom == Listener1)
            {
                deliver(0);
                LastActivityOfLan1 = DateTime.Now;
            }
            else if (ListenerFrom == Listener2)
            {
                deliver(1);
                LastActivityOfLan2 = DateTime.Now;
            }
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
            if (TxEnabled)
            {
                Logger.Trace<PsiManager>("On PSI Sending Data on LAN1 ...");
                Listener1.Send(Lan1Sendto, message);
                Logger.Trace<PsiManager>($"On PSI Sending Data on LAN2 ...");
                Listener2.Send(Lan2Sendto, message);
                return true;
            }
            Logger.Trace<PsiManager>($"On PSI Discarding data on LAN1/LAN2 (TxDisabled) ...");
            return false;
        }
        protected void SendInitMsg()
        {
            Logger.Info<PsiManager>($"On PSI (TXE {TxEnabled}) Sending Init Msg ...");
            var msg = SactaMsg.MsgToScv(Cfg, SactaMsg.MsgType.Init, SactaMsg.InitId, 0).Serialize();
            if (BroadMessage(msg))
            {
                Sequence = 0;
                Logger.Info<PsiManager>($"On PSI Init Msg sended.");
            }
        }
        protected void SendPresenceMsg()
        {
            Logger.Debug<PsiManager>($"On PSI (TXE {TxEnabled}) Sending Presence Msg (Sequence {Sequence}...");
            var msg = SactaMsg.MsgToScv(Cfg, SactaMsg.MsgType.Presence, 0, Sequence).Serialize();
            if (BroadMessage(msg))
            {
                Sequence = Sequence >= 287 ? 0 : Sequence + 1;
                LastPresenceSended = DateTime.Now;
                Logger.Debug<PsiManager>($"On PSI Presence Msg sended. (New Sequence {Sequence})");
            }
        }
        protected void SendSectorizationMsg(Dictionary<string,int> sectorUcs)
        {
            Logger.Info<PsiManager>($"On PSI (TXE {TxEnabled}) Sending Sectorization Msg (Sequence {Sequence}...");
            var msg = SactaMsg.MsgToScv(Cfg, SactaMsg.MsgType.Sectorization, 0, Sequence, SectorizationVersion, sectorUcs).Serialize();
            if (BroadMessage(msg))
            {
                Sequence = Sequence >= 287 ? 0 : Sequence + 1;
                SectorizationVersion++;
                Logger.Info<PsiManager>($"On PSI Sectorization Msg sended. (New Sequence {Sequence}, New Version {SectorizationVersion})");
            }
        }

        public override bool TxEnabled 
        { 
            get => base.TxEnabled;
            set
            {
                if (value == true)
                {
                    if (IsThereLanActivity == true)
                    {
                        SafeLaunchEvent<ScvActivityEventArgs>(EventScvActivity, new ScvActivityEventArgs()
                        {
                            ScvId = "PSI",
                            OnOff = true
                        });
                        ScvActivity = true;
                    }
                }
                base.TxEnabled = value;
            }
        }

        #endregion

        #region Private Data
        public bool ScvActivity { get; set; }
        int SectorizationVersion { get; set; }
        UdpSocket Listener1, Listener2;

        bool SectAskpending { get; set; } = false;
        bool InitMsgPending { get; set; } = false;

        Func<History> History { get; set; }

        Action<bool> SectResponse = null;
        #endregion
    }
}
