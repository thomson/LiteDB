﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using LiteDB.Engine;

namespace LiteDB.Tests.Query
{
    [TestClass]
    public class Select_Tests
    {
        private LiteEngine db;
        private BsonDocument[] zip;

        [TestInitialize]
        public void Init()
        {
            db = new LiteEngine();
            zip = DataGen.Zip().Take(20).ToArray();

            db.EnsureIndex("zip", "city");
            db.Insert("zip", zip);
        }

        [TestCleanup]
        public void CleanUp()
        {
            db.Dispose();
        }

        [TestMethod]
        public void Query_Select_Key_Only()
        {
            // must orderBy mem data because index will be sorted
            var r0 = zip
                .Select(x => x["city"].AsString)
                .OrderBy(x => x)
                .ToArray();

            // this query will not deserialize document, using only index key
            var r1 = db.Query("zip")
                .Index(Index.All("city"))
                .Select(true)
                .ToValues()
                // key only return document { key: value }
                .Select(x => x["city"].AsString)
                .ToArray();

            Assert.AreEqual(
                string.Join(",", r0),
                string.Join(",", r1));
        }

        [TestMethod]
        public void Query_Select_New_Document()
        {
            var r0 = zip
                .Select(x => new { city = x["city"].AsString.ToUpper(), lat = x["loc"][0].AsDouble, lng = x["loc"][1].AsDouble })
                .ToArray();

            var r1 = db.Query("zip")
                .Select("{ city: UPPER(city), lat: loc[0], lng: loc[1] }")
                .ToArray();


            for (var i = 0; i < r0.Length; i++)
            {
                Assert.AreEqual(r0[i].city, r1[i]["city"].AsString);
                Assert.AreEqual(r0[i].lat, r1[i]["lat"].AsDouble);
                Assert.AreEqual(r0[i].lng, r1[i]["lng"].AsDouble);
            }
        }

        [TestMethod]
        public void Query_Select_Values()
        {
            // return 1 row per result in loc
            var r1 = db.Query("zip")
                .Select("ITEMS(loc)")
                .ToValues()
                .ToArray();

            // all loc array contains 2 values (lat,lng)
            Assert.AreEqual(zip.Length * 2, r1.Length);
        }
    }
}