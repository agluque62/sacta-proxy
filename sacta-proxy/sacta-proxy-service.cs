using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.IO;

using sacta_proxy.helpers;
using sacta_proxy.WebServer;
using sacta_proxy.model;
using sacta_proxy.Managers;

namespace sacta_proxy
{
    using SectMap = Dictionary<string, int>;

    public class DependencyControl
    {
        public string Id { get; set; }
        public bool IsMain { get; set; }
        public Configuration.DependecyConfig Cfg { get; set; }
        public BaseManager Manager { get; set; }
        public bool Activity { get; set; }
        public DateTime LastChange { get; set; }
        public SectMap MapOfSectors 
        {
            get
            {
                return sectorization;
            }
            set
            {
                sectorization = value;
                SectorizationPersistence.Set(Cfg.Id, sectorization);
                LastChange = DateTime.Now;
            }
        }
        public Dictionary<int,int> SectorsMap { get; set; }
        public Dictionary<int,int> PositionsMap { get; set; }
        public DependencyControl(string id)
        {
            Cfg = null;
            Manager = null;
            Activity = false;
            Id = id;
            SectorizationPersistence.Get(id, (date, data) =>
            {
                LastChange = date;
                if (data != null)
                    sectorization = data;
            });
        }
        public List<SectorizationItem> Sectorization
        {
            get
            {
                var sect = MapOfSectors.Select(m => new SectorizationItem() { Sector = m.Key, Position = m.Value }).ToList();
                return sect;
            }
        }
        public void ResetSectorization()
        {
            sectorization.Clear();
        }
        public void MergeSectorization(SectMap map)
        {
            foreach(var item in map)
            {
                sectorization[item.Key] = item.Value;
            }
            SectorizationPersistence.Set(Cfg.Id, sectorization);
            LastChange = DateTime.Now;
        }
        public SectMap Map(SectMap map)
        {
            SectMap mapped = new SectMap();
            foreach(var item in map)
            {
                var newSect = Int32.Parse(item.Key);
                var newPosi = item.Value;
                mapped.Add(SectorsMap.ContainsKey(newSect) ? SectorsMap[newSect].ToString() : newSect.ToString(),
                    PositionsMap.ContainsKey(newPosi) ? PositionsMap[newPosi] : newPosi);
            }
            return mapped;
        }

        private SectMap sectorization = new SectMap();
    }
    public partial class SactaProxy : ServiceBase
    {
        public static SactaProxy This { get; set; }
        public SactaProxy(bool WebEnabled=true)
        {
            InitializeComponent();
            if (WebEnabled)
            {
                SactaProxyWebApp = new SactaProxyWebApp(() => History);
                SactaProxyWebApp.UserActivityEvent += (sender, args) =>
                {
                    EventThread?.Enqueue("OnUserActivityEvent", () =>
                    {
                        var idh = args.InOut ? HistoryItems.UserLogin : args.Cause == "" ? HistoryItems.UserLogout : HistoryItems.UserErrorAccess;
                        History?.Add(idh, args.User, "", "", "", args.Cause);
                    });
                };
            }
            else
            {
                SactaProxyWebApp = null;
            }
            This = this;
        }
        public void StartOnConsole(string[] args)
        {
            OnStart(args);
        }
        public void StopOnConsole()
        {
            OnStop();
            Task.Delay(TimeSpan.FromSeconds(2)).Wait();
        }
        protected override void OnStart(string[] args)
        {
            // Se ejecuta al arrancar el programa.
            EventThread = new EventQueue((sender, x) => Logger.Exception<SactaProxy>(x));
            History = new History();
            MainTask = Task.Run(MainProcessing);
        }
        protected override void OnStop()
        {
            MainTaskSync.Set();
            MainTask.Wait(TimeSpan.FromSeconds(5));
            History.Dispose();
        }
        protected void MainProcessing()
        {
            Logger.Info<SactaProxy>("Arrancando Servicio.");
            
            MainTaskSync = new System.Threading.ManualResetEvent(false);
            EventThread.Start();
            MainTaskConfigured = ConfigureService();
            StartWebServer();
            History.Add(HistoryItems.ServiceStarted);
            do
            {
                EventThread.Enqueue("MainProcessing", () =>
                {
                    try
                    {
                        if (MainTaskConfigured == false)
                        {
                            StopManagers(true);
                            StopWebServer();
                            MainTaskConfigured = ConfigureService();
                            StartWebServer();
                        }
                        GlobalStateManager.MainStandbyCheck((isDual, isMain) =>
                        {
                            if (isDual)
                            {
                                if (isMain && !PS.IsStarted)
                                {
                                    History.Add(HistoryItems.ServiceInMode, "", "", "Master");
                                    StartManagers();
                                }
                                else if (!isMain && PS.IsStarted)
                                {
                                    History.Add(HistoryItems.ServiceInMode, "", "", "Standby");
                                    StopManagers();
                                }
                            }
                            else
                            {
                                if (!PS.IsStarted)
                                {
                                    History.Add(HistoryItems.ServiceInMode, "", "", "Simple");
                                    StartManagers();
                                }
                            }
                        });
                    }
                    catch
                    {
                    }
                });
            }
            while (MainTaskSync.WaitOne(TimeSpan.FromSeconds(2)) == false);

            StopManagers(true);
            StopWebServer();
            History.Add(HistoryItems.ServiceEnded);
            EventThread.ControlledStop();
            Logger.Info<SactaProxy>("Servicio Detenido.");
        }
        protected bool ConfigureService()
        {
            Logger.Info<SactaProxy>("Configurando Servicio");
            cfgManager.Get((cfg =>
            {
                // Se utiliza 'siempre' version 0 para CD30 y version 1 para ULISES.
                cfg.ProtocolVersion = Properties.Settings.Default.ScvType;
                History.Configure(cfg.General.HistoryMaxDays, cfg.General.HistoryMaxItems);
                Managers.Clear();
                cfg.Psi.Sectorization.Positions = "";
                cfg.Psi.Sectorization.Sectors = "";
                cfg.Psi.Sectorization.Virtuals = "";

                var manager = new PsiManager(cfg.ProtocolVersion, cfg.Psi, () => History);
                manager.EventActivity += OnPsiEventActivity;
                manager.EventSectRequest += OnPsiEventSectorizationAsk;
                manager.EventScvActivity += OnPsiEventScvActivity;
                Managers.Add(new DependencyControl(cfg.Psi.Id)
                {
                    IsMain = true,
                    Cfg = cfg.Psi,
                    Manager = manager
                });
                cfg.Dependencies.ForEach(dep =>
                {
                    var dependency = new ScvManager(cfg.ProtocolVersion, dep, () => History);
                    dependency.EventActivity += OnScvEventActivity;
                    dependency.EventSectorization += OnScvEventSectorization;

                    /** Construyendo la configuracion de Sectorizacion general */
                    var sectorsMap = dep.Sectorization.SectorsMap.Split(',')
                        .Where(i => Configuration.MapOfSectorsEntryValid(i))
                        .ToDictionary(k => int.Parse(k.Split(':')[0]), v => int.Parse(v.Split(':')[1]));
                    var positionsMap = dep.Sectorization.PositionsMap.Split(',')
                        .Where(i => Configuration.MapOfSectorsEntryValid(i))
                        .ToDictionary(k => int.Parse(k.Split(':')[0]), v => int.Parse(v.Split(':')[1]));
                    var virtuals = Configuration.ListString2String(
                            dep.Sectorization.VirtualsList()
                                .Select(v => sectorsMap.Keys.Contains(v) ? sectorsMap[v].ToString() : v.ToString())
                                .ToList());
                    var reals = String.Join(",", dep.Sectorization.SectorsList()
                        .Select(r => sectorsMap.Keys.Contains(r) ? sectorsMap[r].ToString() : r.ToString())
                        .ToList());
                    //.Aggregate((i, j) => i + "," + j.ToString());
                    var positions = String.Join(",", dep.Sectorization.PositionsList()
                        .Select(p => positionsMap.Keys.Contains(p) ? positionsMap[p].ToString() : p.ToString())
                        .ToList());
                        //.Aggregate((i, j) => i + "," + j.ToString());

                    cfg.Psi.Sectorization.Positions = Configuration.AgreggateString(cfg.Psi.Sectorization.Positions, positions);
                    cfg.Psi.Sectorization.Virtuals = Configuration.AgreggateString(cfg.Psi.Sectorization.Virtuals, virtuals);
                    cfg.Psi.Sectorization.Sectors = Configuration.AgreggateString(cfg.Psi.Sectorization.Sectors, reals);

                    Managers.Add(new DependencyControl(dep.Id)
                    {
                        IsMain = false,
                        Cfg = dep,
                        Manager = dependency,
                        SectorsMap = sectorsMap,
                        PositionsMap = positionsMap
                    });
                });
                /** Test de la configuracion que maneja la PSI, que debe coincidir con la configurada en BD */
                SectorizationHelper.CompareWithDb(cfg.Psi.Sectorization.Positions,
                    cfg.Psi.Sectorization.Sectors, cfg.Psi.Sectorization.Virtuals, (error) =>
                    {
                        // Marcar el Warning...
                        PS.SignalWarning<SactaProxy>($"Incoherencia de Configuración con Base de Datos: {error}", History);
                    });
                /** */
                var ids = cfg.Dependencies.Select(d => d.Id).ToList();
                ids.Add(cfg.Psi.Id);
                SectorizationPersistence.Sanitize(ids);
                Cfg = cfg;
                cfgManager.Write(Cfg);
                Logger.Info<SactaProxy>("Servicio Configurado...");
            }));
            return true;
        }
        protected void StartWebServer()
        {
            webCallbacks.Clear();
            webCallbacks.Add("/config", OnWebRequestConfig);
            webCallbacks.Add("/status", OnWebRequestState);
            webCallbacks.Add("/version", OnWebRequestVersion);
            webCallbacks.Add("/history", OnWebRequestHistory);
#if DEBUG
            webCallbacks.Add("/testing/*", OnWebRequestTesting);
#endif
            SactaProxyWebApp?.Start(Cfg.General.WebPort, Cfg.General.WebActivityMinTimeout, webCallbacks);
            Logger.Info<SactaProxy>("Servidor WEB Arrancado.");
        }
        protected void StopWebServer()
        {
            SactaProxyWebApp?.Stop();
            webCallbacks.Clear();
            Logger.Info<SactaProxy>("Servidor Web Detenido.");
        }
        protected void StartManagers()
        {
            /** Chequear que no haya sectores o posiciones repetidas */
            var duplicatedSec = Cfg.Psi.Sectorization.SectorsList().GroupBy(s => s)
                .Where(g => g.Count() > 1).Select(g => g.Key.ToString()).ToList();
            var duplicatedPos = Cfg.Psi.Sectorization.PositionsList().GroupBy(s => s)
                .Where(g => g.Count() > 1).Select(g => g.Key.ToString()).ToList();
            var duplicatedVir = Cfg.Psi.Sectorization.VirtualsList().GroupBy(s => s)
                .Where(g => g.Count() > 1).Select(g => g.Key.ToString()).ToList();
            TestDuplicated(duplicatedPos, duplicatedSec, duplicatedVir, () =>
            {
                Logger.Info<SactaProxy>($"Arrancando Gestores. ProtocolVersion => {Cfg.ProtocolVersion}, InCluster => {Cfg.InCluster}");
                // Solo arrancan los gestores cuando no hay duplicados.
                Managers.ForEach(dependency =>
                {
                    dependency.Manager.Start();
                    if (dependency.IsMain)
                        Task.Delay(TimeSpan.FromSeconds(dependency.Cfg.SactaProtocol.TickAlive*2)).Wait();
                });
                PS.Set(ProcessStates.Running);
                Logger.Info<SactaProxy>("Gestores Arrancados.");
            });
        }
        protected void StopManagers(bool forced=false)
        {
            Logger.Info<SactaProxy>("Deteniendo Gestores.");

            Managers.ForEach(depEntry =>
            {
                if (forced)
                {
                    if (depEntry.IsMain)
                    {
                        (depEntry.Manager as PsiManager).EventActivity -= OnPsiEventActivity;
                        (depEntry.Manager as PsiManager).EventSectRequest -= OnPsiEventSectorizationAsk;
                        (depEntry.Manager as PsiManager).EventScvActivity -= OnPsiEventScvActivity;
                    }
                    else
                    {
                        (depEntry.Manager as ScvManager).EventActivity -= OnScvEventActivity;
                        (depEntry.Manager as ScvManager).EventSectorization -= OnScvEventSectorization;
                    }
                }
                depEntry.Manager.Stop();
            });
            if (forced)
            {
                Managers.Clear();
            }

            PS.Set(ProcessStates.Stopped);
            Logger.Info<SactaProxy>("Gestores Detenidos.");
        }

#region Callbacks Web
        protected void OnWebRequestState(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
#if DEBUG1
                ThrowExcepcionEveryMinute();
#endif
                context.Response.StatusCode = 200;
                sb.Append(JsonHelper.ToString(new
                {
                    res = "ok",
                    user = SystemUsers.CurrentUserIdAndProfile,
                    version = GenericHelper.VersionManagement.AssemblyVersion,
                    logic = Cfg.General.ActivateSactaLogic,
                    global = GlobalStateManager.Info,
                    Status
                }, false));
            }
            else
            {
                context.Response.StatusCode = 404;
                sb.Append(JsonHelper.ToString(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }, false));
            }
        }
        protected void OnWebRequestConfig(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                string data = JsonHelper.ToString(new { res = "Error al obtener configuracion" }, false);
                context.Response.StatusCode = 500;
                cfgManager.Get((cfg) =>
                {
                    // Se utiliza 'siempre' version 0 para CD30 y version 1 para ULISES.
                    cfg.ProtocolVersion = Properties.Settings.Default.ScvType;
                    data = JsonHelper.ToString(new { res = "ok", cfg}, false);
                    context.Response.StatusCode = 200;
                });
                sb.Append(data);
            }
            else if (context.Request.HttpMethod == "POST")
            {
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    string strData = reader.ReadToEnd();
                    cfgManager.Set(strData, (error, errorMsg) =>
                    {
                        if (!error)
                        {
                            /** Reiniciar el Servicio */
                            Reset();
                            /** Historico */
                            History.Add(HistoryItems.UserConfigChange, SystemUsers.CurrentUserId, "", "OK");
                            context.Response.StatusCode = 200;
                            sb.Append(JsonHelper.ToString(new { res = "ok" }, false));
                        }
                        else
                        {
                            History.Add(HistoryItems.UserConfigChange, SystemUsers.CurrentUserId, "", "ERROR", "", errorMsg);
                            context.Response.StatusCode = 500;
                            sb.Append(JsonHelper.ToString(new { res = $"Error actualizando la configuracion => {errorMsg}" }, false));
                        }
                    });
                }
            }
            else
            {
                context.Response.StatusCode = 404;
                sb.Append(JsonHelper.ToString(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }, false));
            }
        }
        protected void OnWebRequestVersion(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                context.Response.StatusCode = 200;
                sb.Append(JsonHelper.ToString((new GenericHelper.VersionManagement("versiones.json")).Version, false));
            }
            else
            {
                context.Response.StatusCode = 404;
                sb.Append(JsonHelper.ToString(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }, false));
            }
        }
        protected void OnWebRequestHistory(HttpListenerContext context, StringBuilder sb)
        {
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                context.Response.StatusCode = 200;
                sb.Append(JsonHelper.ToString(new { data = History.Get }));
            }
            else
            {
                context.Response.StatusCode = 404;
                sb.Append(JsonHelper.ToString(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }, false));
            }
        }
        protected void OnWebRequestTesting(HttpListenerContext context, StringBuilder sb)
        {
            string cmd = context.Request.Url.LocalPath.Split('/')[2];
            context.Response.ContentType = "application/json";
            if (context.Request.HttpMethod == "GET")
            {
                if (cmd == "reset")
                {
                    Reset();
                }
                context.Response.StatusCode = 200;
                sb.Append(JsonHelper.ToString(new { res = "ok"}, false));
            }
            else
            {
                context.Response.StatusCode = 404;
                sb.Append(JsonHelper.ToString(new { res = context.Request.HttpMethod + ": Metodo No Permitido" }, false));
            }
        }
#endregion Callbacks Web

#region EventManagers
        private EventQueue EventThread { get; set; }
        protected void OnScvEventActivity(Object sender, ActivityOnLanArgs data)
        {
            EventThread.Enqueue("OnScvEventActivity", () =>
            {
                //var controlDep = DepManager.Where(d => d.Key == data.ScvId).Select(d => d.Value).ToList();
                Logger.Info<SactaProxy>($"OnScvEventActivity => {data.ToString()}");
                var ctrldep = DepManagers.Where(d => d.Cfg.Id == data.ScvId).FirstOrDefault();
                if (ctrldep != null)
                {
                    switch (data.What)
                    {
                        case BaseManager.WhatLanItems.Lan1:
                        case BaseManager.WhatLanItems.Lan2:
                            var strLan = data.What == BaseManager.WhatLanItems.Lan1 ? "LAN1" : "LAN2";
                            var strStd = data.ActivityOnLan ? "ON" : "OFF";
                            History.Add(HistoryItems.DepActivityEvent, "", ctrldep.Cfg.Id, $"{strLan} {strStd}");
                            return;
                    }
                    if (ctrldep.Activity != data.ActivityOnLan)
                    {
                        /** Actualiza el estado de la dependencia y Genera el Historico */
                        ctrldep.Activity = data.ActivityOnLan;
                        History.Add(HistoryItems.DepActivityEvent, "", ctrldep.Cfg.Id, data.ActivityOnLan ? "ON" : "OFF");

                        /** Actualiza el Tx del SCV */
                        var oldEnableTx = MainManager.Manager.TxEnabled;
                        var actives = DepManagers.Where(d => d.Activity == true).ToList();
                        var newEnableTx = Cfg.General.ActivateSactaLogic == "OR" ? actives.Count() > 0 : actives.Count() == DepManagers.Count();
                        MainManager.Manager.TxEnabled = newEnableTx;
                        /** Se genera el historico si corresponde */
                        if (oldEnableTx != MainManager.Manager.TxEnabled)
                        {
                            History.Add(HistoryItems.DepTxstateChange, "", MainManager.Cfg.Id, MainManager.Manager.TxEnabled ? "ON" : "OFF");
                        }

                        /** Para agilizar el modo AND Actualizo los Tx de las otras dependencias. */
                        if (Cfg.General.ActivateSactaLogic=="AND" && newEnableTx == false)
                        {
                            DepManagers/*.Where(d => d.Cfg.Id != data.ScvId).ToList()*/.ForEach(d =>
                            {
                                d.Manager.TxEnabled = false;
                            });
                        }
                    }
                }
                else
                {
                    // Algo no va bien.
                    Logger.Fatal<SactaProxy>($"OnScvEventActivity. {data.ScvId} Dependency is missing or duplicated");
                }
            });
        }
        protected void OnScvEventSectorization(Object sender, SectorizationReceivedArgs data)
        {
            /** Se ha recibido una sectorizacion correcta de una dependencia */ 
            EventThread.Enqueue("OnScvEventSectorization", () =>
            {
                Logger.Info<SactaProxy>($"OnScvEventSectorization => {data.ToString()}");
                var ctrldep = DepManagers.Where(d => d.Cfg.Id == data.ScvId).FirstOrDefault();
                string cause = "";
                if (ctrldep != null)
                {
                    if (data.Accepted)
                    {
                        // RM4907. Actualizo la Sectorizacion en la Dependencia solo si ha sido aceptada.
                        ctrldep.MapOfSectors = data.SectorMap;

                        // Genero la Sectorizacion del SCV...
                        MainManager.ResetSectorization();
                        DepManagers.ForEach(dep =>
                        {
                            MainManager.MergeSectorization(dep.Map(dep.MapOfSectors));
                        });

                        // Historico.
                        History.Add(HistoryItems.DepSectorizationReceivedEvent, "", ctrldep.Cfg.Id, "", SectorizationHelper.MapToString(data.SectorMap));

                        // Miro si la Sectorizacion Global cumple Parametros.
                        if ((MainManager.Manager as PsiManager).PreprocessSectorizationToSend(MainManager.MapOfSectors, (error)=>cause=error)==true)
                        {
                            // Propagar la Sectorizacion al SCV real si todas las dependencias han recibido sectorizacion.
                            var actives = DepManagers.Where(d => d.Activity == true).ToList();
                            var whithsect = DepManagers.Where(d => d.MapOfSectors.Count > 0).ToList().Count == DepManagers.Count;
                            var sectenable = Cfg.General.ActivateSactaLogic == "OR" ? actives.Count() > 0 : actives.Count() == DepManagers.Count();

                            //var DepWithSectInfo = DepManagers.Where(d => d.MapOfSectors.Count > 0).ToList();
                            //if (DepWithSectInfo.Count == DepManagers.Count)
                            if (sectenable && whithsect)
                            {
                                if (ScvSectorizationAskPending == false)
                                {
                                    (MainManager.Manager as PsiManager).SendSectorization(MainManager.MapOfSectors, (accepted) =>
                                    {
                                        if (accepted)
                                        {
                                            // Historico
                                            History.Add(HistoryItems.ScvSectorizationSendedEvent, "", MainManager.Cfg.Id, "",
                                                SectorizationHelper.MapToString(MainManager.MapOfSectors), $"Recibida de SACTA ({data.ScvId})");
                                            data.Acknowledge(true);
                                        }
                                        else
                                        {
                                            History.Add(HistoryItems.DepSectorizationRejectedEvent, "", MainManager.Cfg.Id,
                                                "", SectorizationHelper.MapToString(MainManager.MapOfSectors), $"Rechazada por SCV");
                                            data.Acknowledge(false);
                                        }
                                    });
                                }
                                else
                                {
                                    Logger.Warn<SactaProxy>($"OnScvEventSectorization from {data.ScvId}. ScvSectAskSync ({ScvSectAskSync!=null}), Blocked Sectorization. Cause: Scv ASK Pending.");
                                    ScvSectAskSync?.Signal(data.ScvId);
                                    data.Acknowledge(true);
                                }
                            }
                            else
                            {
                                cause = !sectenable ? "No se cumple la condicion AND/OR para sectorizar" :
                                    "No todas las dependencias tienen sectorizaciones válidas.";
                                History.Add(HistoryItems.DepSectorizationRejectedEvent, "", ctrldep.Cfg.Id,
                                    "", SectorizationHelper.MapToString(data.SectorMap), cause);
                                data.Acknowledge(false);
                            }
                        }
                        else
                        {
                            History.Add(HistoryItems.DepSectorizationRejectedEvent, "", MainManager.Cfg.Id,
                                "", SectorizationHelper.MapToString(MainManager.MapOfSectors), cause);
                            data.Acknowledge(false);
                        }
                    }
                    else
                    {
                        // Evento de Sectorizacion Rechazada. Historico
                        History.Add(HistoryItems.DepSectorizationRejectedEvent, "", ctrldep.Cfg.Id,
                            "", data.ReceivedMap, data.RejectCause);
                        data.Acknowledge(false);
                    }
                }
                else
                {
                    Logger.Fatal<SactaProxy>($"OnScvEventSectorization. {data.ScvId} Dependency is missing or duplicated");
                }
            });
        }
        protected void OnPsiEventActivity(Object sender, ActivityOnLanArgs data)
        {
            EventThread.Enqueue("OnPsiEventActivity", () =>
            {
                Logger.Info<SactaProxy>($"OnPsiEventActivity => {data.ToString()}");
                switch (data.What)
                {
                    case BaseManager.WhatLanItems.Lan1:
                    case BaseManager.WhatLanItems.Lan2:
                        var strLan = data.What == BaseManager.WhatLanItems.Lan1 ? "LAN1" : "LAN2";
                        var strStd = data.ActivityOnLan ? "ON" : "OFF";
                        History.Add(HistoryItems.DepActivityEvent, "", MainManager.Cfg.Id, $"{strLan} {strStd}");
                        return;
                }
                //if (data.ActivityOnLan != MainManager.Activity)
                //{
                //    MainManager.Activity = data.ActivityOnLan;
                //    /** Historico del Cambio */
                //    History.Add(HistoryItems.DepActivityEvent, "", MainManager.Cfg.Id, MainManager.Activity ? "ON" : "OFF");
                //    // Si se pierde la conectividad con el SCV real, se simula 'inactividad' en la interfaz sacta.
                //    DepManagers.ForEach(dependency =>
                //    {
                //        dependency.Manager.TxEnabled = MainManager.Activity;
                //        /** Historico del Cambio */
                //        History.Add(HistoryItems.DepTxstateChange, "", dependency.Cfg.Id, MainManager.Activity ? "ON" : "OFF");
                //    });
                //}
            });
        }
        protected void OnPsiEventSectorizationAsk(Object sender, SectorizationRequestArgs data)
        {
            EventThread.Enqueue("OnPsiEventActivity", () =>
            {
                Logger.Info<SactaProxy>($"OnPsiEventSectorizationAsk => {data.ToString()}");
                var DepWithSectInfo = DepManagers.Where(d => d.MapOfSectors.Count > 0).Select(d => d.Id).ToList();
                if (DepWithSectInfo.Count == DepManagers.Count)
                {
                    ScvSectorizationAskPending = true;
                    ScvSectAskSync = new NamedEventsSync(DepWithSectInfo.ToArray());
                    Task.Run(() =>
                    {
                        using (ScvSectAskSync)
                        {
                            ScvSectAskSync.Wait(TimeSpan.FromSeconds(Properties.Settings.Default.ScvSectInitTimeout), (timeout) =>
                            {
                                (MainManager.Manager as PsiManager).SendSectorization(MainManager.MapOfSectors, (accepted)=>
                                {
                                    /** Historico */
                                    if (accepted)
                                        History.Add(HistoryItems.ScvSectorizationSendedEvent, "", MainManager.Cfg.Id, "",
                                            SectorizationHelper.MapToString(MainManager.MapOfSectors), $"Peticion SCV");
                                    else
                                    {
                                        // Reject
                                        History.Add(HistoryItems.DepSectorizationRejectedEvent, "", MainManager.Cfg.Id,
                                            "", SectorizationHelper.MapToString(MainManager.MapOfSectors), $"Rechazada por SCV");
                                    }
                                });
                            });
                        }
                        ScvSectAskSync = null;
                        ScvSectorizationAskPending = false;
                    });
                    History.Add(HistoryItems.ScvSectorizationAskEvent, "", MainManager.Id);
                }
                else
                {
                    Logger.Warn<SactaProxy>($"OnPsiEventSectorizationAsk. IGNORED. No all Sectorization Info Present.");
                }
            });
        }
        protected void OnPsiEventScvActivity(object sender, ScvActivityEventArgs data)
        {
            EventThread.Enqueue("OnPsiEventScvActivity", () =>
            {
                Logger.Info<SactaProxy>($"OnPsiEventScvActivity => {data.ToString()}");
                MainManager.Activity = data.OnOff;
                /** Historico del Cambio */
                History.Add(HistoryItems.DepActivityEvent, "", MainManager.Cfg.Id, MainManager.Activity ? "ON" : "OFF");
                // Si se pierde la conectividad con el SCV real, se simula 'inactividad' en la interfaz sacta.
                DepManagers.ForEach(dependency =>
                {
                    dependency.Manager.TxEnabled = MainManager.Activity;
                        /** Historico del Cambio */
                    History.Add(HistoryItems.DepTxstateChange, "", dependency.Cfg.Id, MainManager.Activity ? "ON" : "OFF");
                });
            });
        }
#endregion EventManagers
        void TestDuplicated(List<string> pos, List<string> sec, List<string> vir, Action continues)
        {
            if (pos.Count() > 0)
                PS.SignalFatal<SactaProxy>($"Existen Id de Posiciones duplicadas => {String.Join(",", pos)}", History);
            if (sec.Count() > 0)
                PS.SignalFatal<SactaProxy>($"Existen Id de Sectores Reales duplicados => {String.Join(",", sec)}", History);
            if (vir.Count() > 0)
                PS.SignalFatal<SactaProxy>($"Existen Id de Sectores Virtuales duplicados => {String.Join(",", vir)}", History);
            if (pos.Count() <= 0 && sec.Count() <= 0 && vir.Count() <= 0)
                continues();
        }
        Object Status 
        { 
            get
            {
                return new
                {
                    service = PS.Status,
                    web = SactaProxyWebApp?.Status,
                    ems = Managers.Select(d => new { id = d.Id, status = d.Manager.Status, sect=new { d.LastChange, d.Sectorization } }).ToList()
                };
            }
        }
        public void Reset()
        {
            EventThread.Enqueue("Reset", () =>
            {
#if VERSION0
                Task.Delay(TimeSpan.FromMilliseconds(500)).Wait();
                StopService();
                Task.Delay(TimeSpan.FromMilliseconds(1000)).Wait();
                StartService();
#else
                PS.ClearMessages();
                MainTaskConfigured = false;
#endif
            });
        }
        public void Message(string msg)
        {
            EventThread.Enqueue("Message", () =>
            {
                PS.SignalWarning<SactaProxy>(msg, History);
            });
        }

#region Data
        private Task MainTask { get; set; }
        private System.Threading.ManualResetEvent MainTaskSync { get; set; }
        private bool MainTaskConfigured { get; set; }
        private SactaProxyWebApp SactaProxyWebApp { get; set; }
        private readonly Dictionary<string, wasRestCallBack> webCallbacks = new Dictionary<string, wasRestCallBack>();

        private readonly ConfigurationManager cfgManager = new ConfigurationManager();
        private Configuration Cfg { get; set; }
        private readonly ProcessStatusControl PS = new ProcessStatusControl();
        private List<DependencyControl> Managers = new List<DependencyControl>();
        private DependencyControl MainManager => Managers.Where(d => d.IsMain).FirstOrDefault();
        private List<DependencyControl> DepManagers => Managers.Where(d => d.IsMain == false).ToList();
        private History History { get; set; }
        private bool ScvSectorizationAskPending { get; set; } = false;
        private NamedEventsSync ScvSectAskSync { get; set; }
        #endregion

#if DEBUG
        #region Pruebas Forzadas
        DateTime LastThrow = DateTime.Now;
        void ThrowExcepcionEveryMinute()
        {
            var elapsed = DateTime.Now - LastThrow;
            if (elapsed >= TimeSpan.FromMinutes(1))
            {
                LastThrow = DateTime.Now;
                throw new Exception("EveryMinuteException");
            }
        }
        #endregion Pruebas Forzadas
#endif
    }

}
