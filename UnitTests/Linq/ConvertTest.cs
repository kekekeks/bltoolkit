﻿using System;
using System.Linq;

using NUnit.Framework;

using BLToolkit.Data.DataProvider;
using BLToolkit.Data.Linq;

namespace Data.Linq
{
	[TestFixture]
	public class ConvertTest : TestBase
	{
		[Test]
		public void Test1()
		{
			ForEachProvider(new[] { ProviderName.SQLite },
				db => Assert.AreEqual(3, (from t in db.Types where t.MoneyValue * t.ID == 9.99m  select t).Single().ID));
		}

		[Test]
		public void ToInt1()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select              Sql.ConvertTo<int>.From(t.MoneyValue),
				from t in db.Types select Sql.OnServer(Sql.ConvertTo<int>.From(t.MoneyValue))));
		}

		[Test]
		public void ToInt2()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select              Sql.Convert<int,decimal>(t.MoneyValue),
				from t in db.Types select Sql.OnServer(Sql.Convert<int,decimal>(t.MoneyValue))));
		}

		[Test]
		public void ToBigInt()
		{
			ForEachProvider(new[] { ProviderName.MySql }, db => AreEqual(
				from t in    Types select Sql.Convert(Sql.BigInt, t.MoneyValue),
				from t in db.Types select Sql.Convert(Sql.BigInt, t.MoneyValue)));
		}

		[Test]
		public void ToInt64()
		{
			ForEachProvider(new[] { ProviderName.MySql }, db => AreEqual(
				from p in from t in    Types select (Int64)t.MoneyValue where p > 0 select p,
				from p in from t in db.Types select (Int64)t.MoneyValue where p > 0 select p));
		}

		[Test]
		public void ToInt()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select Sql.Convert(Sql.Int, t.MoneyValue),
				from t in db.Types select Sql.Convert(Sql.Int, t.MoneyValue)));
		}

		[Test]
		public void ToInt32()
		{
			ForEachProvider(db => AreEqual(
				from p in from t in    Types select (Int32)t.MoneyValue where p > 0 select p,
				from p in from t in db.Types select (Int32)t.MoneyValue where p > 0 select p));
		}

		[Test]
		public void ToSmallInt()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select Sql.Convert(Sql.SmallInt, t.MoneyValue),
				from t in db.Types select Sql.Convert(Sql.SmallInt, t.MoneyValue)));
		}

		[Test]
		public void ToInt16()
		{
			ForEachProvider(db => AreEqual(
				from p in from t in    Types select (Int16)t.MoneyValue where p > 0 select p,
				from p in from t in db.Types select (Int16)t.MoneyValue where p > 0 select p));
		}

		[Test]
		public void ToTinyInt()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select Sql.Convert(Sql.TinyInt, t.MoneyValue),
				from t in db.Types select Sql.Convert(Sql.TinyInt, t.MoneyValue)));
		}

		[Test]
		public void ToByte()
		{
			ForEachProvider(db => AreEqual(
				from p in from t in    Types select (byte)t.MoneyValue where p > 0 select p,
				from p in from t in db.Types select (byte)t.MoneyValue where p > 0 select p));
		}

		[Test]
		public void ToDefaultDecimal()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select (int)Sql.Convert(Sql.DefaultDecimal, t.MoneyValue),
				from t in db.Types select (int)Sql.Convert(Sql.DefaultDecimal, t.MoneyValue)));
		}

		[Test]
		public void ToDecimal1()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select (int)Sql.Convert(Sql.Decimal(10), t.MoneyValue),
				from t in db.Types select (int)Sql.Convert(Sql.Decimal(10), t.MoneyValue)));
		}

		[Test]
		public void ToDecimal2()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select Sql.Convert(Sql.Decimal(10,4), t.MoneyValue),
				from t in db.Types select Sql.Convert(Sql.Decimal(10,4), t.MoneyValue)));
		}

		[Test]
		public void ToDecimal3()
		{
			ForEachProvider(db => AreEqual(
				from p in from t in    Types select (Decimal)t.MoneyValue where p > 0 select p,
				from p in from t in db.Types select (Decimal)t.MoneyValue where p > 0 select p));
		}

		[Test]
		public void ToMoney()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select (int)Sql.Convert(Sql.Money, t.MoneyValue),
				from t in db.Types select (int)Sql.Convert(Sql.Money, t.MoneyValue)));
		}

		[Test]
		public void ToSmallMoney()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select (decimal)Sql.Convert(Sql.SmallMoney, t.MoneyValue),
				from t in db.Types select (decimal)Sql.Convert(Sql.SmallMoney, t.MoneyValue)));
		}

		[Test]
		public void ToSqlFloat()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select (int)Sql.Convert(Sql.Float, t.MoneyValue),
				from t in db.Types select (int)Sql.Convert(Sql.Float, t.MoneyValue)));
		}

		[Test]
		public void ToDouble()
		{
			ForEachProvider(db => AreEqual(
				from p in from t in    Types select (int)(Double)t.MoneyValue where p > 0 select p,
				from p in from t in db.Types select (int)(Double)t.MoneyValue where p > 0 select p));
		}
	}
}
