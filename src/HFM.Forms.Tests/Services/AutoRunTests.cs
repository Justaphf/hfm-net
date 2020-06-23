
using System;

using NUnit.Framework;

using HFM.Core.Logging;

namespace HFM.Forms
{
   [TestFixture]
   public class AutoRunTests
   {
      [Test]
      public void AutoRun_SetFilePath_Test()
      {
         var autoRun = new RegistryAutoRun(NullLogger.Instance);
         autoRun.SetFilePath(System.Reflection.Assembly.GetExecutingAssembly().Location);
         Assert.AreEqual(true, autoRun.IsEnabled());
         autoRun.SetFilePath(String.Empty);
         Assert.AreEqual(false, autoRun.IsEnabled());
      }
   }
}
