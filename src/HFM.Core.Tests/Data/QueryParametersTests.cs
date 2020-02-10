﻿/*
 * HFM.NET - Query Parameters Class Tests
 * Copyright (C) 2009-2013 Ryan Harlamert (harlam357)
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

using NUnit.Framework;

namespace HFM.Core.Data
{
   [TestFixture]
   public class QueryParametersTests
   {
      [Test]
      public void CreateTest()
      {
         var param = new QueryParameters();
         Assert.AreEqual(QueryParameters.SelectAll.Name, param.Name);
         Assert.AreEqual(0, param.Fields.Count);
      }

      [Test]
      public void DeepCopyTest()
      {
         var param = new QueryParameters();
         param.Name = "Test";
         param.Fields.Add(new QueryField { Name = QueryFieldName.Name, Type = QueryFieldType.Equal, Value = "Test Instance" });
         param.Fields.Add(new QueryField { Name = QueryFieldName.DownloadDateTime, Type = QueryFieldType.GreaterThan, Value = new DateTime(2000, 1, 1) });

         var copy = param.DeepClone();
         Assert.AreNotSame(param, copy);
         Assert.AreEqual(param.Name, copy.Name);
         for (int i = 0; i < param.Fields.Count; i++)
         {
            Assert.AreEqual(param.Fields[i].Name, copy.Fields[i].Name);
            Assert.AreEqual(param.Fields[i].Type, copy.Fields[i].Type);
            Assert.AreEqual(param.Fields[i].Value, copy.Fields[i].Value);
         }
      }

      [Test]
      public void CompareTest1()
      {
         var param1 = new QueryParameters();
         var param2 = new QueryParameters();
         Assert.AreEqual(0, param1.CompareTo(param2));
      }

      [Test]
      public void CompareTest2()
      {
         var param1 = new QueryParameters { Name = "Name" };
         var param2 = new QueryParameters();
         Assert.AreEqual(1, param1.CompareTo(param2));
      }

      [Test]
      public void CompareTest3()
      {
         var param1 = new QueryParameters();
         var param2 = new QueryParameters { Name = "Name" };
         Assert.AreEqual(-1, param1.CompareTo(param2));
      }

      [Test]
      public void CompareTest4()
      {
         var param1 = new QueryParameters { Name = null };
         var param2 = new QueryParameters();
         Assert.AreEqual(-1, param1.CompareTo(param2));
      }

      [Test]
      public void CompareTest5()
      {
         var param1 = new QueryParameters { Name = null };
         var param2 = new QueryParameters { Name = null };
         Assert.AreEqual(0, param1.CompareTo(param2));
      }

      [Test]
      public void CompareTest6()
      {
         var param1 = new QueryParameters { Name = "A" };
         var param2 = new QueryParameters { Name = "B" };
         Assert.AreEqual(-1, param1.CompareTo(param2));
      }

      [Test]
      public void CompareTest7()
      {
         var param1 = new QueryParameters { Name = "B" };
         var param2 = new QueryParameters { Name = "A" };
         Assert.AreEqual(1, param1.CompareTo(param2));
      }

      [Test]
      public void CompareTest8()
      {
         var param1 = new QueryParameters();
         Assert.AreEqual(1, param1.CompareTo(null));
      }

      #region Query Field

      [Test]
      public void QueryFieldCreateTest()
      {
         var field = new QueryField();
         Assert.AreEqual(QueryFieldName.ProjectID, field.Name);
         Assert.AreEqual(QueryFieldType.Equal, field.Type);
      }

      [Test]
      public void QueryFieldValueTest1()
      {
         var field = new QueryField();
         field.Value = "Value";
         Assert.AreEqual("Value", field.Value);
         field.Value = null;
         Assert.IsNull(field.Value);
      }

      [Test]
      public void QueryFieldValueTest2()
      {
         var field = new QueryField();
         field.Value = new DateTime(2000, 1, 1);
         Assert.AreEqual(new DateTime(2000, 1, 1), field.Value);
         field.Value = null;
         Assert.IsNull(field.Value);
      }

      [Test]
      public void QueryFieldValueTest3()
      {
         var field = new QueryField();
         field.Value = 6900;
         Assert.AreEqual("6900", field.Value);
         field.Value = null;
         Assert.IsNull(field.Value);
      }

      [Test]
      public void QueryField_Operator_Default_Test()
      {
         var field = new QueryField();
         Assert.AreEqual("=", field.Operator);
      }

      [Test]
      public void QueryField_Operator_Equal_Test()
      {
         var field = new QueryField { Type = QueryFieldType.Equal };
         Assert.AreEqual("=", field.Operator);
      }

      [Test]
      public void QueryField_Operator_GreaterThan_Test()
      {
         var field = new QueryField { Type = QueryFieldType.GreaterThan };
         Assert.AreEqual(">", field.Operator);
      }

      [Test]
      public void QueryField_Operator_GreaterThanOrEqual_Test()
      {
         var field = new QueryField { Type = QueryFieldType.GreaterThanOrEqual };
         Assert.AreEqual(">=", field.Operator);
      }

      [Test]
      public void QueryField_Operator_LessThan_Test()
      {
         var field = new QueryField { Type = QueryFieldType.LessThan };
         Assert.AreEqual("<", field.Operator);
      }

      [Test]
      public void QueryField_Operator_LessThanOrEqual_Test()
      {
         var field = new QueryField { Type = QueryFieldType.LessThanOrEqual };
         Assert.AreEqual("<=", field.Operator);
      }

      [Test]
      public void QueryField_Operator_Like_Test()
      {
         var field = new QueryField { Type = QueryFieldType.Like };
         Assert.AreEqual("LIKE", field.Operator);
      }

      [Test]
      public void QueryField_Operator_NotLike_Test()
      {
         var field = new QueryField { Type = QueryFieldType.NotLike };
         Assert.AreEqual("NOT LIKE", field.Operator);
      }

      [Test]
      public void QueryField_Operator_NotEqual_Test()
      {
         var field = new QueryField { Type = QueryFieldType.NotEqual };
         Assert.AreEqual("!=", field.Operator);
      }

      #endregion
   }
}
