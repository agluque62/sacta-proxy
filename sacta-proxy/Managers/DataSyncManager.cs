using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using sacta_proxy.model;
using sacta_proxy.helpers;

namespace sacta_proxy.Managers
{
    public enum ItemStates { Aislado, JitterError, Sincronizado }
    public class FilesSyncManagerEventArgs : EventArgs
    {
        public string Error { get; set; }
        public FilesSyncManagerItem Item { get; set; }
    }
    public class FilesSyncManagerItem
    {
        public DateTime Date { get; set; }
        public string Name { get; set; }
        public string Data { get; set; }
        public ItemStates State { get; set; }
        public DateTime LastInfoReceived { get; set; }
    }
    class FilesSyncManagerDataSend : FilesSyncManagerItem
    {
        public DateTime When { get; set; }
    }

    /// <summary>
    /// Clase para sincronizar ficheros en el Cluster.
    /// </summary>
    public class DataSyncManager : IDisposable
    {
        #region Public
        public event EventHandler<FilesSyncManagerEventArgs> FileSyncEvent;
        public int SyncListenerSpvPeriod { get; set; } = 5;
        public int SyncSendingPeriod { get; set; } = 11;
        public double MaxJitter { get; set; } = 3;
        public int InternalDelay { get; set; } = 0;
        public DataSyncManager(bool enable, string ipl, string mcastg)
        {
            if (enable)
            {
                Logger.Info<DataSyncManager>($"{Id} Creating");

                var mcastgItems = mcastg.Split(':').ToList();
                var port = mcastgItems.Count != 2 ? 1030 : GenericHelper.ToInt(mcastgItems.ElementAt(1), 1030);
                var mcastIp = mcastgItems.Count != 2 ? "224.100.10.1" : mcastgItems.ElementAt(0);

                McastIp = IPAddress.Parse(mcastIp);
                McastIf = IPAddress.Parse(ipl);
                Listen = new IPEndPoint(McastIf, port);
                Sendto = new IPEndPoint(McastIp, port);
                ThreadSync = new EventQueue();

                ThreadSync.Start();
                ExecutiveThreadCancel = new CancellationTokenSource();
                ExecutiveThread = Task.Run(ExecutiveThreadRoutine, ExecutiveThreadCancel.Token);

                Logger.Info<DataSyncManager>($"{Id} Created");
            }
        }
        public void Dispose()
        {
            Logger.Info<DataSyncManager>($"{Id} Disposing SyncManager");

            ExecutiveThreadCancel?.Cancel();
            ExecutiveThread?.Wait(TimeSpan.FromSeconds(5));
            FileSyncEvent = null;
            ThreadSync?.Stop();

            Logger.Info<DataSyncManager>($"{Id} SyncManager Disposed");
        }
        public void MonitorsFile(string name, DateTime date, string data)
        {
            ThreadSync?.Enqueue("MonitorsFile", () =>
            {
                MonitoredFiles[name] = new FilesSyncManagerItem() { Name = name, Date = date, Data = data, State= ItemStates.Aislado, LastInfoReceived=DateTime.MaxValue };
                Logger.Info<DataSyncManager>($"{Id} SyncManager => Active");
            });
        }
        public object Status => new
        {
            std = MonitoredFiles.Values.Where(i=>i.State!= ItemStates.Sincronizado).Count()>0 ? 0 : 1,
            Items = MonitoredFiles.Values.Select(i => new
            {
                id  = $"{i.Name}",
                dt  = $"{i.Date}",
                std = $"{i.State}",
            })
        };
        #endregion

        #region Protected methods
        void OnDataReceived(object sender, DataGram dg)
        {
            ThreadSync?.Enqueue("OnDataReceived", () =>
            {
                if (dg.Data != null)
                {
                    if (dg.Client.Address.ToString() != McastIf.ToString())
                    {
                        ProcessReceivedData(dg.Data);
                    }
                }
                else
                {
                    Logger.Warn<DataSyncManager>($"{Id} Datagram NULL Received => Reseting LAN");
                    ResetLan();
                }
            });
        }
        void ExecutiveThreadRoutine()
        {
            DateTime lastListenerTime = DateTime.MinValue;
            DateTime lastRefreshTime = DateTime.MinValue;
            // Supervisar la cancelacion.
            while (ExecutiveThreadCancel.IsCancellationRequested == false)
            {
                Task.Delay(TimeSpan.FromMilliseconds(100)).Wait();
                if (DateTime.Now - lastListenerTime >= TimeSpan.FromSeconds(SyncListenerSpvPeriod))
                {
                    // Supervisar la disponibilidad del Listener.
                    ThreadSync.Enqueue("ExecutiveThreadRoutine. Listener Supervision", () =>
                    {
                        ListenerSupervision();
                    });
                    lastListenerTime = DateTime.Now;
                }

                if (DateTime.Now - lastRefreshTime >= TimeSpan.FromSeconds(SyncSendingPeriod))
                {
                    ThreadSync.Enqueue("ExecutiveThreadRoutine. Refresh", () =>
                    {
                        RefreshData();
                        ReceivedInfoSupervision();
                    });
                    lastRefreshTime = DateTime.Now;
                }
            }
        }
        void ListenerSupervision()
        {
            try
            {
                Logger.Debug<DataSyncManager>($"{Id} Listener Supervision Tick.");
                if (Listener == null)
                {
                    Logger.Info<DataSyncManager>($"{Id} Creating Listener");

                    Listener = new UdpSocket(Listen);
                    /** Para seleccionar correctamente la Interfaz de salida de las tramas MCAST */
                    Listener.Base.JoinMulticastGroup(McastIp, McastIf);
                    //Listener.Base.MulticastLoopback = false;
                    Listener.Base.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 16);
                    Listener.NewDataEvent += OnDataReceived;
                    Listener.BeginReceive();

                    Logger.Info<DataSyncManager>($"{Id} Listener Created");
                }
            }
            catch (Exception x)
            {
                Logger.Exception<DataSyncManager>(x, $"{Id}");
                ResetLan();
            }
        }
        void RefreshData()
        {
            foreach (var itemData in MonitoredFiles)
            {
                try
                {
                    if (Listener != null)
                    {
                        var DataSend = new FilesSyncManagerDataSend()
                        {
                            When = DateTime.Now + TimeSpan.FromSeconds(InternalDelay),
                            Name = itemData.Value.Name,
                            Date = itemData.Value.Date,
                            Data = itemData.Value.Data
                        };
                        var strData = JsonHelper.ToString(DataSend, false);
                        Logger.Debug<DataSyncManager>($"{Id} Sending Data for {itemData.Key} => {strData.Substring(0, 64)}...");

                        var bData = Encoding.ASCII.GetBytes(strData);
                        Listener?.Send(Sendto, bData);
                        Task.Delay(50).Wait();
                    }
                }
                catch (Exception x)
                {
                    Logger.Exception<DataSyncManager>(x, $"{Id}");
                    ResetLan();
                    return;
                }
            }
        }
        void ReceivedInfoSupervision()
        {
            foreach(var item in MonitoredFiles)
            {
                var Elapsed = DateTime.Now - item.Value.LastInfoReceived;
                if (Elapsed >= TimeSpan.FromSeconds(3*SyncSendingPeriod))
                {
                    item.Value.State = ItemStates.Aislado;
                    item.Value.LastInfoReceived = DateTime.MaxValue;
                }
            }
        }
        void ProcessReceivedData(byte[] dataReceived)
        {
            DateTime Now = DateTime.Now + TimeSpan.FromSeconds(InternalDelay);
            string data = System.Text.Encoding.Default.GetString(dataReceived, 0, dataReceived.Count());
            Logger.Trace<DataSyncManager>($"{Id} Processing Data Received => {data.Substring(0, 64)}...");

            try
            {
                var ItemRec = JsonHelper.Parse<FilesSyncManagerDataSend>(data);
                if (MonitoredFiles.ContainsKey(ItemRec.Name))
                {
                    var ItemLoc = MonitoredFiles[ItemRec.Name];
                    ItemLoc.LastInfoReceived = DateTime.Now;
                    var jitter = Math.Abs((Now - ItemRec.When).TotalSeconds);
                    if (jitter <= MaxJitter)
                    {
                        if (ItemRec.Date == ItemLoc.Date)
                        {
                            // Ficheros Sincronizados.
                            ItemLoc.State = ItemStates.Sincronizado;
                            Logger.Debug<DataSyncManager>($"{Id} Ficheros sincronizados");
                        }
                        else if (ItemRec.Date > ItemLoc.Date)
                        {
                            // Fichero Local desactualizado. Genero el Evento para que se actualice...
                            SafeLaunchEvent<FilesSyncManagerEventArgs>(FileSyncEvent, new FilesSyncManagerEventArgs()
                            {
                                Error = default,
                                Item = new FilesSyncManagerItem() { Date = ItemRec.Date, Name = ItemRec.Name, Data = ItemRec.Data }
                            });
                            Logger.Info<DataSyncManager>($"{Id} Event Launched for {ItemRec.Name} => {data.Substring(0, 64)}...");
                        }
                        else
                        {
                            // Fichero Remoto Desactualizado... Se actualizará en el proximo Polling...
                            Logger.Warn<DataSyncManager>($"{Id}  Fichero Remoto Desactualizado.");
                        }
                    }
                    else
                    {
                        // Los ordendores no estan sincronizados. Generar un evento de error...
                        var strError = $"{Id} Los ordenadores no estan sincronizados. Local => {Now}, Remote => {ItemRec.When}, Desviacion => {jitter} Seg.";
                        if (ItemLoc.State != ItemStates.JitterError)
                        {
                            ItemLoc.State = ItemStates.JitterError;
                            SafeLaunchEvent<FilesSyncManagerEventArgs>(FileSyncEvent, new FilesSyncManagerEventArgs() { Error = strError });
                        }
                        Logger.Warn<DataSyncManager>(strError);
                    }
                }
                else
                {
                    // El fichero no está en la lista... Generar un evento de error...
                    var strError = $"{Id} El fichero recibido no esta en la lista. Fichero recibido => {ItemRec.Name}";
                    Logger.Warn<DataSyncManager>(strError);
                    // SafeLaunchEvent<FilesSyncManagerEventArgs>(FileSyncEvent, new FilesSyncManagerEventArgs() { Error = strError });
                }
            }
            catch (Exception x)
            {
                Logger.Exception<DataSyncManager>(x, $"{Id}");
            }
        }
        void ResetLan()
        {
            Listener?.Dispose();
            Listener = null;
            foreach (var itemData in MonitoredFiles)
            {
                itemData.Value.State = ItemStates.Aislado;
            }
        }
        void SafeLaunchEvent<T>(EventHandler<T> handler, T args) => handler?.Invoke(this, args);
        string Id => $"On SYNC ({McastIf}):";
        #endregion

        #region Protected Properties
        Task ExecutiveThread { get; set; } = null;
        CancellationTokenSource ExecutiveThreadCancel { get; set; } = null;
        UdpSocket Listener { get; set; } = null;
        IPEndPoint Listen { get; set; } = null;
        IPEndPoint Sendto { get; set; } = null;
        IPAddress McastIp { get; set; } = default;
        IPAddress McastIf { get; set; } = default;
        private EventQueue ThreadSync { get; set; } = null;
        Dictionary<string, FilesSyncManagerItem> MonitoredFiles { get; set; } = new Dictionary<string, FilesSyncManagerItem>();
        #endregion
    }
}
