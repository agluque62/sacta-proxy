using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;

using MySql.Data.MySqlClient;

using System.Net;
using System.Net.Sockets;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;

using sacta_proxy.model;

namespace sacta_proxy_tests
{
    [TestClass]
    public class DbTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            try
            {
                using (var connection = new MySqlConnection("Server=10.10.10.10;User ID=root;Password=cd40;Database=new_cd40;Connect Timeout=5"))
                {
                    connection.Open();

                    using (var command = new MySqlCommand("SELECT IdOperador,Clave,NivelAcceso FROM operadores WHERE IdSistema='departamento';", connection))
                    using (var reader = command.ExecuteReader())
                        while (reader.Read())
                        {
                            Debug.WriteLine($"user: {reader[0]}, clave: {reader[1]}, perfil: {reader[2]}");
                        }
                }
            }
            catch
            {
            }
        }
        [TestMethod]
        public void TestMethod2()
        {
            Assert.IsTrue(SystemUsers.Authenticate("1", "1"));
            Assert.IsFalse(SystemUsers.Authenticate("hola", ""));
        }
        [TestMethod]
        public void TestMethod3()
        {
            try
            {
                using (var connection = new MySqlConnection("Server=127.0.0.1;User ID=root;Password=cd40;Database=new_cd40;Connect Timeout=5"))
                {
                    connection.Open();
                    string sqlInsert = string.Format("INSERT INTO historicoincidencias (IdSistema, Scv, IdIncidencia, IdHw, TipoHw, FechaHora, Usuario, Descripcion) " +
                                                     "VALUES (\"{0}\",{1},{2},\"{3}\",{4},\"{5}\",\"{6}\",\"{7}\")",
                                                     "departamento", 0, 50, "ProxySacta", 4, 
                                                     String.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now), 
                                                     "USER",
                                                     "Descripcion del Evento...");
                    using (var command = new MySqlCommand(sqlInsert, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
            }
        }
        [TestMethod]
        public void TestMethod4()
        {
            var IpFrom = "127.0.0.1";
            var IpTo = "127.0.0.1";
            var community = "public";
            var baseOid = ".1.1.100.1";
            var code = 50;
            var msg = "Esto es un mensaje...";
            TrapFromTo(IpFrom, IpTo, community, $"{baseOid}.{code}", msg);
        }
        
        public void TrapFromTo(string ipFrom, string ipTo, string community, string oid, string val, Int32 port = 162, VersionCode snmpVersion = VersionCode.V2)
        {
            using (var sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                var variables = new List<Variable>() { new Variable(new ObjectIdentifier(oid), new OctetString(val)) };
                var endPointLocal = new IPEndPoint(IPAddress.Parse(ipFrom), 0);
                var receiver = new IPEndPoint(IPAddress.Parse(ipTo), port);

                sender.Bind(endPointLocal);

                TrapV2Message message = new TrapV2Message(0,
                    VersionCode.V2,
                    new OctetString(community),
                    new ObjectIdentifier(oid),
                    0,
                    variables);

                message.Send(receiver, sender);
            }
        }


    }
}
