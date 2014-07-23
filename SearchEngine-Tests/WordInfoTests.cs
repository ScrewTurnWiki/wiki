
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace ScrewTurn.Wiki.SearchEngine.Tests {
	
	[TestFixture]
	public class WordInfoTests : TestsBase {

		[Test]
		public void Constructor() {
			WordInfo info = new WordInfo("continuous", 2, 0, WordLocation.Content);
			Assert.AreEqual(2, info.FirstCharIndex, "Wrong start index");
			Assert.AreEqual(10, info.Text.Length, "Wrong length");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Constructor_NullText() {
			WordInfo info = new WordInfo(null, 0, 0, WordLocation.Content);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void Constructor_EmptyText() {
			WordInfo info = new WordInfo("", 0, 0, WordLocation.Content);
		}

		[Test]
		public void Equals() {
			WordInfo info1 = new WordInfo("word", 0, 0, WordLocation.Content);
			WordInfo info2 = new WordInfo("word", 0, 0, WordLocation.Content);
			WordInfo info3 = new WordInfo("word", 10, 1, WordLocation.Content);
			WordInfo info4 = new WordInfo("word", 10, 1, WordLocation.Title);
			WordInfo info5 = new WordInfo("word2", 0, 0, WordLocation.Content);

			Assert.IsTrue(info1.Equals(info2), "info1 should equal info2");
			Assert.IsFalse(info1.Equals(info3), "info1 should not equal info3");
			Assert.IsFalse(info3.Equals(info4), "info3 should not equal info4");
			Assert.IsTrue(info1.Equals(info1), "info1 should equal itself");
			Assert.IsFalse(info1.Equals(null), "info1 should not equal null");
			Assert.IsFalse(info1.Equals("hello"), "info1 should not equal a string");
			Assert.IsFalse(info1.Equals(info5), "info1 should not equal info5");
		}

		[Test]
		public void EqualityOperator() {
			WordInfo info1 = new WordInfo("word", 0, 0, WordLocation.Content);
			WordInfo info2 = new WordInfo("word", 0, 0, WordLocation.Content);
			WordInfo info3 = new WordInfo("word", 10, 1, WordLocation.Content);
			WordInfo info4 = new WordInfo("word", 10, 1, WordLocation.Title);
			WordInfo info5 = new WordInfo("word2", 0, 0, WordLocation.Content);

			Assert.IsTrue(info1 == info2, "info1 should equal info2");
			Assert.IsFalse(info1 == info3, "info1 should not equal info3");
			Assert.IsFalse(info3 == info4, "info3 should not equal info4");
			Assert.IsFalse(info1 == null, "info1 should not equal null");
			Assert.IsFalse(info1 == info5, "info1 should not equal info5");
		}

		[Test]
		public void InequalityOperator() {
			WordInfo info1 = new WordInfo("word", 0, 0, WordLocation.Content);
			WordInfo info2 = new WordInfo("word", 0, 0, WordLocation.Content);
			WordInfo info3 = new WordInfo("word", 10, 1, WordLocation.Content);
			WordInfo info4 = new WordInfo("word", 10, 1, WordLocation.Title);
			WordInfo info5 = new WordInfo("word2", 0, 0, WordLocation.Content);

			Assert.IsFalse(info1 != info2, "info1 should equal info2");
			Assert.IsTrue(info1 != info3, "info1 should not equal info3");
			Assert.IsTrue(info3 != info4, "info3 should not equal info4");
			Assert.IsTrue(info1 != null, "info1 should not equal null");
			Assert.IsTrue(info1 != info5, "info1 should not equal info5");
		}

		[Test]
		public void CompareTo() {
			WordInfo info1 = new WordInfo("word", 0, 0, WordLocation.Content);
			WordInfo info2 = new WordInfo("word", 0, 0, WordLocation.Content);
			WordInfo info3 = new WordInfo("word", 10, 1, WordLocation.Content);
			WordInfo info4 = new WordInfo("word", 10, 1, WordLocation.Title);
			WordInfo info5 = new WordInfo("word2", 0, 0, WordLocation.Content);

			Assert.AreEqual(0, info1.CompareTo(info2), "info1 should equal info2");
			Assert.AreEqual(-3, info1.CompareTo(info3), "info1 should be smaller than info3");
			Assert.AreEqual(2, info3.CompareTo(info4), "info3 should be greater than info4");
			Assert.AreEqual(1, info1.CompareTo(null), "info1 should be greater than null");
			Assert.AreEqual(-1, info1.CompareTo(info5), "info1 should be smaller than info5");
		}

	}

}
