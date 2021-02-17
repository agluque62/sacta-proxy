using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

//using MySqlConnector;
using MySql.Data.MySqlClient;

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
    }
}
