﻿using System;
using System.IO;
using System.Web.Caching;
using System.Web.UI;
using System.Web.UI.WebControls;
using Moq;
using NUnit.Framework;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Profiling;
using umbraco;
using Umbraco.Core.Configuration;
using Umbraco.Core.Models;
using Umbraco.Tests.TestHelpers;
using Umbraco.Web.Macros;
using File = System.IO.File;
using Macro = umbraco.cms.businesslogic.macro.Macro;

namespace Umbraco.Tests.Macros
{
    [TestFixture]
    public class MacroTests
    {

        [SetUp]
        public void Setup()
        {
            //we DO want cache enabled for these tests
            var cacheHelper = new CacheHelper(
                new ObjectCacheRuntimeCacheProvider(),
                new StaticCacheProvider(),
                new NullCacheProvider(),
                new IsolatedRuntimeCache(type => new ObjectCacheRuntimeCacheProvider()));
            ApplicationContext.Current = new ApplicationContext(cacheHelper, new ProfilingLogger(Mock.Of<ILogger>(), Mock.Of<IProfiler>()));

            UmbracoConfig.For.SetUmbracoSettings(SettingsForTests.GetDefault());
        }

        [TearDown]
        public void TearDown()
        {
            ApplicationContext.Current.ApplicationCache.RuntimeCache.ClearAllCache();
            ApplicationContext.Current.DisposeIfDisposable();
            ApplicationContext.Current = null;
        }

        [TestCase("123", "IntProp", typeof(int))]
        [TestCase("Hello", "StringProp", typeof(string))]
        [TestCase("123456789.01", "DoubleProp", typeof(double))]
        [TestCase("1234567", "FloatProp", typeof(float))]
        [TestCase("1", "BoolProp", typeof(bool))]
        [TestCase("true", "BoolProp", typeof(bool))]
        [TestCase("0", "BoolProp", typeof(bool))]
        [TestCase("false", "BoolProp", typeof(bool))]
        [TestCase("2001-05-10", "DateProp", typeof(DateTime))]
        [TestCase("123px", "UnitProp", typeof(Unit))]
        [TestCase("456pt", "UnitProp", typeof(Unit))]
        [TestCase("CEC063D3-D73E-4B7D-93ED-7F69CA9BF2D0", "GuidProp", typeof(Guid))]
        [TestCase("CEC063D3D73E4B7D93ED7F69CA9BF2D0", "GuidProp", typeof(Guid))]
        [TestCase("", "NullDateProp", typeof(DateTime?))]
        [TestCase("2001-05-10", "NullDateProp", typeof(DateTime?))]
        [TestCase("", "NullUnitProp", typeof(Unit?))]
        [TestCase("456pt", "NullUnitProp", typeof(Unit?))]
        public void SetUserControlProperty(string val, string macroPropName, Type convertTo)
        {
            var ctrl = new UserControlTest();
            var macroModel = new MacroModel
            {
                Name = "test",
                Alias = "test",
                ScriptName = "~/usercontrols/menu.ascx"
            };
            macroModel.Properties.Add(new MacroPropertyModel(macroPropName, val));

            UserControlMacroEngine.UpdateControlProperties(ctrl, macroModel);

            var ctrlType = ctrl.GetType();
            var prop = ctrlType.GetProperty(macroPropName);
            var converted = val.TryConvertTo(convertTo);

            Assert.IsTrue(converted.Success);
            Assert.NotNull(prop);
            Assert.AreEqual(converted.Result, prop.GetValue(ctrl));
        }

        [TestCase("text.xslt", "", "", "Xslt")]
        [TestCase("", "~/Views/MacroPartials/test.cshtml", "", "PartialView")]
        [TestCase("", "~/App_Plugins/MyPackage/Views/MacroPartials/test.cshtml", "", "PartialView")]
        [TestCase("", "", "~/usercontrols/menu.ascx", "UserControl")]
        [TestCase("", "", "~/usercontrols/Header.ASCX", "UserControl")]
        [TestCase("", "", "", "Unknown")]
        public void Determine_Macro_Type(string xslt, string scriptFile, string scriptType, string expectedType)
        {
            var expected = Enum<MacroTypes>.Parse(expectedType);
            Assert.AreEqual(expected, Macro.FindMacroType(xslt, scriptFile, scriptType));
        }

        [TestCase("text.xslt", "", "", "~/xslt/text.xslt")]
        //[TestCase("", "razor-script.cshtml", "", "~/macroScripts/razor-script.cshtml")] // gone in v8
        [TestCase("", "~/Views/MacroPartials/test.cshtml", "", "~/Views/MacroPartials/test.cshtml")]
        [TestCase("", "~/App_Plugins/MyPackage/Views/MacroPartials/test.cshtml", "", "~/App_Plugins/MyPackage/Views/MacroPartials/test.cshtml")]
        [TestCase("", "", "~/usercontrols/menu.ascx", "~/usercontrols/menu.ascx")]
        public void Get_Macro_File(string xslt, string scriptFile, string scriptType, string expectedResult)
        {
            var model = new MacroModel
            {
                Name = "Test",
                Alias = "test",
                TypeName = scriptType,
                Xslt = xslt,
                ScriptName = scriptFile,
                MacroType = MacroModel.FindMacroType(xslt, scriptFile, scriptType)
            };
            var file = MacroRenderer.GetMacroFileName(model);
            Assert.AreEqual(expectedResult, file);
        }

        [TestCase("Xslt", true)]
        [TestCase("PartialView", true)]
        [TestCase("UserControl", true)]
        [TestCase("Unknown", false)]
        public void Macro_Is_File_Based(string macroTypeString, bool expectedNonNull)
        {
            var macroType = Enum<MacroTypes>.Parse(macroTypeString);
            var model = new MacroModel
            {
                MacroType = macroType,
                Xslt = "anything",
                ScriptName = "anything",
                TypeName = "anything"
            };
            var filename = MacroRenderer.GetMacroFileName(model);
            if (expectedNonNull)
                Assert.IsNotNull(filename);
            else
                Assert.IsNull(filename);
        }

        //[TestCase(-5, true)] //the cache DateTime will be older than the file date
        //[TestCase(5, false)] //the cache DateTime will be newer than the file date
        public void Macro_Needs_Removing_Based_On_Macro_File(int minutesToNow, bool expectedNull)
        {
            // macro has been refactored, and macro.GetMacroContentFromCache() will
            // take care of the macro file, if any. It requires a web environment,
            // so we cannot really test this anymore.
        }

        public void Get_Macro_Cache_Identifier()
        {
            //var asdf  = macro.GetCacheIdentifier()
        }

        private class UserControlTest : UserControl
        {
            public int IntProp { get; set; }
            public string StringProp { get; set; }
            public double DoubleProp { get; set; }
            public float FloatProp { get; set; }
            public bool BoolProp { get; set; }
            public DateTime DateProp { get; set; }
            public Unit UnitProp { get; set; }
            public Guid GuidProp { get; set; }
            public DateTime? NullDateProp { get; set; }
            public Unit? NullUnitProp { get; set; }
        }
    }
}
