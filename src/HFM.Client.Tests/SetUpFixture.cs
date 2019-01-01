﻿
using System;

using NUnit.Framework;

namespace HFM.Client.Tests
{
   [SetUpFixture]
   public class SetUpFixture
   {
      [OneTimeSetUp]
      public void SetEnvironmentCurrentDirectory()
      {
         Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
      }
   }
}
