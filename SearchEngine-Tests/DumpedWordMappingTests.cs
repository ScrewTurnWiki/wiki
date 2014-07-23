
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace ScrewTurn.Wiki.SearchEngine.Tests {
	
	[TestFixture]
	public class DumpedWordMappingTests : TestsBase {

		[Test]
		public void Constructor_Integers() {
			DumpedWordMapping map = new DumpedWordMapping(5, 2, 3, 4, WordLocation.Keywords.Location);
			Assert.AreEqual(5, map.WordID, "Wrong word ID");
			Assert.AreEqual(2, map.DocumentID, "Wrong document ID");
			Assert.AreEqual(3, map.FirstCharIndex, "Wrong first char index");
			Assert.AreEqual(4, map.WordIndex, "Wrong word index");
			Assert.AreEqual(WordLocation.Keywords.Location, map.Location, "Wrong word location");
		}

		[Test]
		public void Constructor_WithBasicWordInfo() {
			DumpedWordMapping map = new DumpedWordMapping(5, 2, new BasicWordInfo(3, 4, WordLocation.Keywords));
			Assert.AreEqual(5, map.WordID, "Wrong word ID");
			Assert.AreEqual(2, map.DocumentID, "Wrong document ID");
			Assert.AreEqual(3, map.FirstCharIndex, "Wrong first char index");
			Assert.AreEqual(4, map.WordIndex, "Wrong word index");
			Assert.AreEqual(WordLocation.Keywords.Location, map.Location, "Wrong word location");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Constructor_WithBasicWordInfo_NullInfo() {
			DumpedWordMapping map = new DumpedWordMapping(10, 12, null);
		}

	}

}
