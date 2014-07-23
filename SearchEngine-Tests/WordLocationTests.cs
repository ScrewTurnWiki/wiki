
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace ScrewTurn.Wiki.SearchEngine.Tests {

	[TestFixture]
	public class WordLocationTests {

		[Test]
		public void StaticInstances_Title() {
			WordLocation loc1 = WordLocation.Title;
			WordLocation loc2 = WordLocation.Title;

			Assert.AreEqual("Title", loc1.ToString(), "Invalid string representation");
			Assert.AreEqual("Title", loc2.ToString(), "Invalid string representation");

			Assert.IsTrue(loc1 == loc2, "loc1 should equal loc2");
			Assert.IsTrue(loc1.Equals(loc2), "loc1 should equal loc2");
			Assert.AreNotSame(loc2, loc1, "loc1 should not be the same object as loc2");
		}

		[Test]
		public void StaticInstances_Content() {
			WordLocation loc1 = WordLocation.Content;
			WordLocation loc2 = WordLocation.Content;

			Assert.AreEqual("Content", loc1.ToString(), "Invalid string representation");
			Assert.AreEqual("Content", loc2.ToString(), "Invalid string representation");

			Assert.IsTrue(loc1 == loc2, "loc1 should equal loc2");
			Assert.IsTrue(loc1.Equals(loc2), "loc1 should equal loc2");
			Assert.AreNotSame(loc2, loc1, "loc1 should not be the same object as loc2");
		}

		[Test]
		public void StaticInstances_Keywords() {
			WordLocation loc1 = WordLocation.Keywords;
			WordLocation loc2 = WordLocation.Keywords;

			Assert.AreEqual("Keywords", loc1.ToString(), "Invalid string representation");
			Assert.AreEqual("Keywords", loc2.ToString(), "Invalid string representation");

			Assert.IsTrue(loc1 == loc2, "loc1 should equal loc2");
			Assert.IsTrue(loc1.Equals(loc2), "loc1 should equal loc2");
			Assert.AreNotSame(loc2, loc1, "loc1 should not be the same object as loc2");
		}

		[Test]
		public void StaticInstances_CompareTo() {
			Assert.AreEqual(0, WordLocation.Title.CompareTo(WordLocation.Title), "Title should equal Title");
			Assert.AreEqual(1, WordLocation.Content.CompareTo(WordLocation.Title), "Content should be greater than Title");
			Assert.AreEqual(-1, WordLocation.Title.CompareTo(WordLocation.Content), "Title should be smaller than Content");
		}

		[Test]
		public void StaticInstances_RelativeRelevance() {
			Assert.IsTrue(WordLocation.Title.RelativeRelevance > WordLocation.Keywords.RelativeRelevance, "Wrong relevance relationship");
			Assert.IsTrue(WordLocation.Keywords.RelativeRelevance > WordLocation.Content.RelativeRelevance, "Wrong relevance relationship");
		}

		[Test]
		public void StaticMethods_GetInstance() {
			Assert.AreEqual(WordLocation.Title, WordLocation.GetInstance(1), "Wrong instance");
			Assert.AreEqual(WordLocation.Keywords, WordLocation.GetInstance(2), "Wrong instance");
			Assert.AreEqual(WordLocation.Content, WordLocation.GetInstance(3), "Wrong instance");
		}

		[TestCase((byte)0, ExpectedException = typeof(ArgumentOutOfRangeException))]
		[TestCase((byte)4, ExpectedException = typeof(ArgumentOutOfRangeException))]
		public void StaticMethods_GetInstance_InvalidLocation(byte location) {
			WordLocation.GetInstance(location);
		}

	}

}
