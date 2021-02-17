using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace sacta_proxy.Helpers
{
    public struct DataGram
    {
        public byte[] Data;
        public IPEndPoint Client;
    }
    public class UdpSocket : IDisposable
    {
        #region Public
        public event EventHandler<DataGram> NewDataEvent;
        public UdpClient Base
        {
            get { return _Udp; }
        }
        public int MaxReceiveThreads
        {
            get { return _MaxReceiveThreads; }
            set { _MaxReceiveThreads = value; }
        }
        public UdpSocket(int port)
            : this(null, port)
        {
        }
        public UdpSocket(string ip, int port)
        {
            _Udp = new UdpClient(new IPEndPoint(ip != null ? IPAddress.Parse(ip) : IPAddress.Any, port));

            if (Environment.OSVersion.Platform != PlatformID.Unix)
            {
                uint SIO_UDP_CONNRESET = 0x9800000C;
                byte[] inValue = new byte[] { 0, 0, 0, 0 }; // == false
                byte[] outValue = new byte[] { 0, 0, 0, 0 }; // initialize to 0

                _Udp.Client.IOControl((int)SIO_UDP_CONNRESET, inValue, outValue);
            }

            _Datagrams = new Queue<DataGram>();
        }
        ~UdpSocket()
        {
            Dispose(false);
        }

        #region IDisposable Members
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }
        #endregion

        public void BeginReceive()
        {
            ClearReceiveBuffer();
            _Udp.BeginReceive(ReceiveCallback, null);
        }
        public void ClearReceiveBuffer()
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            while (_Udp.Available > 0)
            {
                _Udp.Receive(ref ep);
            }
        }
        public void Send(IPEndPoint remoteEP, byte[] msg)
        {
            _Udp.Send(msg, msg.Length, remoteEP);
        }
        #endregion

        #region Private

        UdpClient _Udp;
        Queue<DataGram> _Datagrams;
        int _MaxReceiveThreads = 10;
        int _NumRecevieThreads;
        bool _Disposed;
        void Dispose(bool bDispose)
        {
            if (!_Disposed)
            {
                _Disposed = true;

                if (bDispose)
                {
                    _Udp.Close();
                    _Udp = null;

                    lock (_Datagrams)
                    {
                        _Datagrams.Clear();
                        _Datagrams = null;
                    }
                }
            }
        }
        void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                DataGram dg = new DataGram();
                IPEndPoint client = new IPEndPoint(IPAddress.Any, 0);
                bool processData = false;
                if (_Udp == null) return;
                dg.Data = _Udp.EndReceive(ar, ref client);
                dg.Client = client;

                SafeLaunchEvent(this, dg);
                lock (_Datagrams)
                {
                    while (_Udp.Available > 0)
                    {
                        DataGram dgToEnqueue = new DataGram();
                        client = new IPEndPoint(IPAddress.Any, 0);

                        dgToEnqueue.Data = _Udp.Receive(ref client);
                        dgToEnqueue.Client = client;

                        _Datagrams.Enqueue(dgToEnqueue);
                    }

                    if (_NumRecevieThreads < _MaxReceiveThreads)
                    {
                        processData = true;
                        _NumRecevieThreads++;
                    }
                }

                _Udp.BeginReceive(ReceiveCallback, null);
                while (processData)
                {
                    lock (_Datagrams)
                    {
                        if ((_Datagrams.Count > 0) && (_NumRecevieThreads <= _MaxReceiveThreads))
                        {
                            dg = _Datagrams.Dequeue();
                            SafeLaunchEvent(this, dg);
                        }
                        else
                        {
                            _NumRecevieThreads--;
                            processData = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Exception<UdpSocket>(ex);
            }
        }

        protected void SafeLaunchEvent(object sender, DataGram data)
        {
            NewDataEvent?.Invoke(sender, data);
        }

        #endregion
    }

}
