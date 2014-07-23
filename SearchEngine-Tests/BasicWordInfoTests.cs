
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace ScrewTurn.Wiki.SearchEngine.Tests {

	[TestFixture]
	public class BasicWordInfoTests {

		[Test]
		public void Constructor() {
			BasicWordInfo info = new BasicWordInfo(2, 0, WordLocation.Content);
			Assert.AreEqual(2, info.FirstCharIndex, "Wrong start index");
			Assert.AreEqual(0, info.WordIndex, "Wrong word index");
		}

		[Test]
		public void Equals() {
			BasicWordInfo info1 = new BasicWordInfo(0, 0, WordLocation.Content);
			BasicWordInfo info2 = new BasicWordInfo(0, 0, WordLocation.Content);
			BasicWordInfo info3 = new BasicWordInfo(10, 1, WordLocation.Content);
			BasicWordInfo info4 = new BasicWordInfo(10, 1, WordLocation.Title);

			Assert.IsTrue(info1.Equals(info2), "info1 should equal info2");
			Assert.IsFalse(info1.Equals(info3), "info1 should not equal info3");
			Assert.IsFalse(info3.Equals(info4), "info3 should not equal info4");
			Assert.IsTrue(info1.Equals(info1), "info1 should equal itself");
			Assert.IsFalse(info1.Equals(null), "info1 should not equal null");
			Assert.IsFalse(info1.Equals("hello"), "info1 should not equal a string");
		}

		[Test]
		public void EqualityOperator() {
			BasicWordInfo info1 = new BasicWordInfo(0, 0, WordLocation.Content);
			BasicWordInfo info2 = new BasicWordInfo(0, 0, WordLocation.Content);
			BasicWordInfo info3 = new BasicWordInfo(10, 1, WordLocation.Content);
			BasicWordInfo info4 = new BasicWordInfo(10, 1, WordLocation.Title);

			Assert.IsTrue(info1 == info2, "info1 should equal info2");
			Assert.IsFalse(info1 == info3, "info1 should not equal info3");
			Assert.IsFalse(info3 == info4, "info3 should not equal info4");
			Assert.IsFalse(info1 == null, "info1 should not equal null");
		}

		[Test]
		public void InequalityOperator() {
			BasicWordInfo info1 = new BasicWordInfo(0, 0, WordLocation.Content);
			BasicWordInfo info2 = new BasicWordInfo(0, 0, WordLocation.Content);
			BasicWordInfo info3 = new BasicWordInfo(10, 1, WordLocation.Content);
			BasicWordInfo info4 = new BasicWordInfo(10, 1, WordLocation.Title);

			Assert.IsFalse(info1 != info2, "info1 should equal info2");
			Assert.IsTrue(info1 != info3, "info1 should not equal info3");
			Assert.IsTrue(info3 != info4, "info3 should not equal info4");
			Assert.IsTrue(info1 != null, "info1 should not equal null");
		}

		[Test]
		public void CompareTo() {
			BasicWordInfo info1 = new BasicWordInfo(0, 0, WordLocation.Content);
			BasicWordInfo info2 = new BasicWordInfo(0, 0, WordLocation.Content);
			BasicWordInfo info3 = new BasicWordInfo(10, 1, WordLocation.Content);
			BasicWordInfo info4 = new BasicWordInfo(10, 1, WordLocation.Title);

			Assert.AreEqual(0, info1.CompareTo(info2), "info1 should equal info2");
			Assert.AreEqual(-1, info1.CompareTo(info3), "info1 should be smaller than info3");
			Assert.AreEqual(2, info3.CompareTo(info4), "info3 should be greater than info4");
			Assert.AreEqual(1, info1.CompareTo(null), "info1 should be greater than null");
		}

	}

}
