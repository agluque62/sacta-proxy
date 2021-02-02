using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;

using ClusterLib;
using Utilities;

namespace sacta_proxy_tests
{
    [TestClass]
    public class ClusterTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var Listener = new UdpSocket("127.0.0.1", 9000);
            Listener.NewDataEvent += (s, dg) =>
            {
                try
                {
                    if (dg.Data[0] == '{')
                    {
                        Debug.WriteLine("Llega un JSON...");
                    }
                    else
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        MemoryStream ms = new MemoryStream(dg.Data);
                        object msg = bf.Deserialize(ms);

                        if (msg is MsgType type)
                        {
                            switch (type)
                            {
                                case MsgType.Activate:
                                    Debug.WriteLine("Activate(true)");
                                    break;
                                case MsgType.Deactivate:
                                    Debug.WriteLine("Deactivate()");
                                    break;
                                case MsgType.GetState:
                                    Debug.WriteLine("SendState()");
                                    break;
                                default:
                                    Debug.WriteLine("Error 1");
                                    break;
                            }
                        }
                        else if (msg is NodeInfo)
                        {
                            Debug.WriteLine("NodeInfo...");
                        }
                        else
                        {
                            throw new Exception("Mensaje Desconocido");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Excepcion: {ex.Message}");
                }
            };
            Listener.BeginReceive();

            var Sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var EndpTo = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9000);

            using (MemoryStream ms = new MemoryStream())
                using(MemoryStream ms1=new MemoryStream())
            {
                var _State = new ClusterState();
                BinaryFormatter bf = new BinaryFormatter();

                try
                {
                    bf.Serialize(ms, _State.LocalNode);
                    Sender.SendTo(ms.ToArray(), EndpTo);
                    Task.Delay(1000).Wait();

                    bf.Serialize(ms1, MsgType.Activate);
                    Sender.SendTo(ms1.ToArray(), EndpTo);
                }
                catch (Exception ex)
                {
                }
            }

            Task.Delay(1000).Wait();

            var data1 = System.Text.Encoding.ASCII.GetBytes("{ \"res\": \"ok\"}");
            Sender.SendTo(data1, EndpTo);

            Task.Delay(1000).Wait();
        }
    }
}
