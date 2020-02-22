﻿
using System.Runtime.Serialization;

namespace HFM.Core.SlotXml
{
   [DataContract(Namespace = "")]
   public class LogLine
   {
      [DataMember(Order = 1)]
      public int Index { get; set; }

      [DataMember(Order = 2)]
      public string Raw { get; set; }

      public override string ToString()
      {
         return Raw;
      }
   }
}
