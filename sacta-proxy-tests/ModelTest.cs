using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using sacta_proxy.model;

namespace sacta_proxy_tests
{
    using SectMap = Dictionary<string, int>;

    [TestClass]
    public class ModelTest
    {
        SectMap map1 = new SectMap()
        {
            {"001", 1 },
            {"002", 2 },
            {"003", 3 },
        };
        SectMap map2 = new SectMap()
        {
            {"011", 11 },
            {"012", 12 },
            {"013", 13 },
            {"014", 15 },
        };

        [TestMethod]
        public void TestSectorizationModel()
        {
            SectorizationPersistence.Set("map1", map1);
            SectorizationPersistence.Set("map2", map2);
            SectorizationPersistence.Set("map3", map2);

            SectorizationPersistence.Get("map3", (map) =>
            {

            });

            SectorizationPersistence.Sanitize(new List<string>() { "map1", "map3" });
        }
    }
}
