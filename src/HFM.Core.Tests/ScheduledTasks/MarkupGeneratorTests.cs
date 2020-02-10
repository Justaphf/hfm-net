﻿/*
 * HFM.NET
 * Copyright (C) 2009-2017 Ryan Harlamert (harlam357)
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; version 2
 * of the License. See the included file GPLv2.TXT.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using AutoMapper;

using NUnit.Framework;
using Rhino.Mocks;

using HFM.Core.Client;
using HFM.Core.Configuration;
using HFM.Core.DataTypes;
using HFM.Log;
using HFM.Preferences;

namespace HFM.Core.ScheduledTasks
{
   [TestFixture]
   public class MarkupGeneratorTests
   {
      [SetUp]
      public void Init()
      {
         Mapper.Initialize(c => c.AddProfile<AutoMapperProfile>());
      }

      [Test]
      public void GenerateTest()
      {
         IPreferenceSet prefs = CreatePreferenceSet();
         prefs.Stub(x => x.Get<bool>(Preference.WebGenCopyXml)).Return(true);
         prefs.Stub(x => x.Get<bool>(Preference.WebGenCopyHtml)).Return(true);

         var slots = CreateSlotModelCollection();
         var markupGenerator = new MarkupGenerator(prefs);
         markupGenerator.Generate(slots);

         Assert.IsNotNull(markupGenerator.XmlFilePaths);
         Assert.IsNotNull(markupGenerator.HtmlFilePaths);
      }

      [Test]
      public void GenerateXmlOnlyTest()
      {
         IPreferenceSet prefs = CreatePreferenceSet();
         prefs.Stub(x => x.Get<bool>(Preference.WebGenCopyXml)).Return(true);
         prefs.Stub(x => x.Get<bool>(Preference.WebGenCopyHtml)).Return(false);

         var slots = CreateSlotModelCollection();
         var markupGenerator = new MarkupGenerator(prefs);
         markupGenerator.Generate(slots);

         Assert.IsNotNull(markupGenerator.XmlFilePaths);
         Assert.IsNull(markupGenerator.HtmlFilePaths);
      }

      [Test]
      public void DontGenerateTest()
      {
         IPreferenceSet prefs = CreatePreferenceSet();
         prefs.Stub(x => x.Get<bool>(Preference.WebGenCopyXml)).Return(false);
         prefs.Stub(x => x.Get<bool>(Preference.WebGenCopyHtml)).Return(false);

         var slots = CreateSlotModelCollection();
         var markupGenerator = new MarkupGenerator(prefs);
         markupGenerator.Generate(slots);

         Assert.IsNull(markupGenerator.XmlFilePaths);
         Assert.IsNull(markupGenerator.HtmlFilePaths);
      }

      [Test]
      public void GenerateArgumentNullTest1()
      {
         var prefs = CreatePreferenceSet();

         var markupGenerator = new MarkupGenerator(prefs);
         Assert.Throws(typeof(ArgumentNullException), () => markupGenerator.Generate(null));
      }

      [Test]
      public void GenerateXmlTest()
      {
         IPreferenceSet prefs = CreatePreferenceSet();
         var slots = CreateSlotModelCollection();

         var markupGenerator = new MarkupGenerator(prefs);
         markupGenerator.GenerateXml(slots);

         string folderPath = Path.Combine(Path.GetTempPath(), "hfm-" + Environment.UserName);
         Assert.AreEqual(3, markupGenerator.XmlFilePaths.Count());
         Assert.AreEqual(Path.Combine(folderPath, "SlotSummary.xml"), markupGenerator.XmlFilePaths.ElementAt(0));
         Assert.AreEqual(Path.Combine(folderPath, "Test2.xml"), markupGenerator.XmlFilePaths.ElementAt(1));
         Assert.AreEqual(Path.Combine(folderPath, "Test1.xml"), markupGenerator.XmlFilePaths.ElementAt(2));
      }

      [Test]
      public void GenerateHtmlTest()
      {
         IPreferenceSet prefs = CreatePreferenceSet();
         var slots = CreateSlotModelCollection();

         var markupGenerator = new MarkupGenerator(prefs);
         markupGenerator.GenerateHtml(slots);

         string folderPath = Path.Combine(Path.GetTempPath(), "hfm-" + Environment.UserName);
         Assert.AreEqual(6, markupGenerator.HtmlFilePaths.Count());
         Assert.AreEqual(Path.Combine(folderPath, "index.html"), markupGenerator.HtmlFilePaths.ElementAt(0));
         Assert.AreEqual(Path.Combine(folderPath, "mobile.html"), markupGenerator.HtmlFilePaths.ElementAt(1));
         Assert.AreEqual(Path.Combine(folderPath, "summary.html"), markupGenerator.HtmlFilePaths.ElementAt(2));
         Assert.AreEqual(Path.Combine(folderPath, "mobilesummary.html"), markupGenerator.HtmlFilePaths.ElementAt(3));
         Assert.AreEqual(Path.Combine(folderPath, "Test2.html"), markupGenerator.HtmlFilePaths.ElementAt(4));
         Assert.AreEqual(Path.Combine(folderPath, "Test1.html"), markupGenerator.HtmlFilePaths.ElementAt(5));
      }

      private static IPreferenceSet CreatePreferenceSet()
      {
         var prefs = MockRepository.GenerateStub<IPreferenceSet>();
         prefs.Stub(x => x.Get<string>(Preference.ApplicationPath)).Return(@"..\..\..\HFM");
         prefs.Stub(x => x.Get<int>(Preference.DecimalPlaces)).Return(0);
         prefs.Stub(x => x.Get<string>(Preference.WebOverview)).Return("WebOverview.xslt");
         prefs.Stub(x => x.Get<string>(Preference.WebMobileOverview)).Return("WebMobileOverview.xslt");
         prefs.Stub(x => x.Get<string>(Preference.WebSummary)).Return("WebSummary.xslt");
         prefs.Stub(x => x.Get<string>(Preference.WebMobileSummary)).Return("WebMobileSummary.xslt");
         prefs.Stub(x => x.Get<string>(Preference.WebSlot)).Return("WebSlot.xslt");
         return prefs;
      }

      private static ICollection<SlotModel> CreateSlotModelCollection()
      {
         // setup stubs
         var prefs = MockRepository.GenerateStub<IPreferenceSet>();

         var slots = new List<SlotModel>();

         // setup slot
         var slot = new SlotModel();
         slot.Prefs = prefs;
         // set concrete values
         slot.Settings = new ClientSettings { Name = "Test2" };
         var logLines = new List<LogLine>
         {
            new LogLine { LineType = LogLineType.LogHeader, Index = 1, Raw = "Header" }
         };
         slot.CurrentLogLines = logLines;
         slot.WorkUnit.LogLines = logLines;
         slots.Add(slot);

         // setup slot
         slot = new SlotModel();
         slot.Prefs = prefs;
         // Test For - Issue 201 - Web Generation Fails when a Client with no CurrentLogLines is encountered.
         // Make sure we return null for CurrentLogLines in the second SlotModel.
         slot.Settings = new ClientSettings { Name = "Test1" };
         slot.CurrentLogLines = null;
         slots.Add(slot);

         return slots;
      }
   }
}
