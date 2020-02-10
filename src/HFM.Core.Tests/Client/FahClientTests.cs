﻿/*
 * HFM.NET - Fah Client Class Tests
 * Copyright (C) 2009-2015 Ryan Harlamert (harlam357)
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
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
 */

using System;
using System.Collections.Generic;

using NUnit.Framework;
using Rhino.Mocks;

using HFM.Client;
using HFM.Core.Data.SQLite;
using HFM.Core.DataTypes;
using HFM.Core.WorkUnits;
using HFM.Log;
using HFM.Proteins;

namespace HFM.Core.Client
{
   [TestFixture]
   public class FahClientTests
   {
      [Test]
      public void FahClient_ArgumentNullException_Test()
      {
         Assert.Throws<ArgumentNullException>(() => new FahClient(null));
      }

      [Test]
      public void FahClient_UpdateBenchmarkData_Test()
      {
         // setup
         var benchmarkCollection = new ProteinBenchmarkService();
         var database = MockRepository.GenerateMock<IUnitInfoDatabase>();
         var fahClient = new FahClient(MockRepository.GenerateStub<IMessageConnection>()) { BenchmarkService = benchmarkCollection, UnitInfoDatabase = database };

         var unitInfo1 = new WorkUnit();
         unitInfo1.OwningClientName = "Owner";
         unitInfo1.OwningClientPath = "Path";
         unitInfo1.OwningSlotId = 0;
         unitInfo1.ProjectID = 2669;
         unitInfo1.ProjectRun = 1;
         unitInfo1.ProjectClone = 2;
         unitInfo1.ProjectGen = 3;
         unitInfo1.FinishedTime = new DateTime(2010, 1, 1);
         unitInfo1.QueueIndex = 0;
         var currentUnitInfo = new UnitInfoModel { CurrentProtein = new Protein(), WorkUnitData = unitInfo1 };

         var unitInfo1Clone = unitInfo1.DeepClone();
         unitInfo1Clone.FramesObserved = 4;
         var frameDataDictionary = new Dictionary<int, WorkUnitFrameData>()
            .With(new WorkUnitFrameData { Duration = TimeSpan.FromMinutes(0), ID = 0 },
                  new WorkUnitFrameData { Duration = TimeSpan.FromMinutes(5), ID = 1 },
                  new WorkUnitFrameData { Duration = TimeSpan.FromMinutes(5), ID = 2 },
                  new WorkUnitFrameData { Duration = TimeSpan.FromMinutes(5), ID = 3 });
         unitInfo1Clone.FrameData = frameDataDictionary;
         unitInfo1Clone.UnitResult = WorkUnitResult.FinishedUnit;
         var unitInfoLogic1 = new UnitInfoModel { CurrentProtein = new Protein(), WorkUnitData = unitInfo1Clone };

         var parsedUnits = new[] { unitInfoLogic1 };

         // arrange
         database.Stub(x => x.Connected).Return(true);
         database.Expect(x => x.Insert(null)).IgnoreArguments().Repeat.Times(1);

         var benchmarkClient = new ProteinBenchmarkSlotIdentifier("Owner Slot 00", "Path");

         // assert before act
         Assert.AreEqual(false, benchmarkCollection.Contains(benchmarkClient));
         Assert.AreEqual(false, new List<int>(benchmarkCollection.GetBenchmarkProjects(benchmarkClient)).Contains(2669));
         Assert.IsNull(benchmarkCollection.GetBenchmark(currentUnitInfo.WorkUnitData));

         // act
         fahClient.UpdateBenchmarkData(currentUnitInfo, parsedUnits);

         // assert after act
         Assert.AreEqual(true, benchmarkCollection.Contains(benchmarkClient));
         Assert.AreEqual(true, new List<int>(benchmarkCollection.GetBenchmarkProjects(benchmarkClient)).Contains(2669));
         Assert.AreEqual(TimeSpan.FromMinutes(5), benchmarkCollection.GetBenchmark(currentUnitInfo.WorkUnitData).AverageFrameTime);

         database.VerifyAllExpectations();
      }
   }
}
