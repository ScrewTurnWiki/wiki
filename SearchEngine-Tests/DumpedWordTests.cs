
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace ScrewTurn.Wiki.SearchEngine.Tests {

	[TestFixture]
	public class DumpedWordTests {

		[Test]
		public void Constructor_WithParameters() {
			DumpedWord w = new DumpedWord(12, "word");
			Assert.AreEqual(12, w.ID, "Wrong ID");
			Assert.AreEqual("word", w.Text, "Wrong text");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void Constructor_WithParameters_InvalidText(string text) {
			DumpedWord w = new DumpedWord(5, text);
		}

		[Test]
		public void Constructor_Word() {
			DumpedWord w = new DumpedWord(new Word(23, "text"));
			Assert.AreEqual(23, w.ID, "Wrong ID");
			Assert.AreEqual("text", w.Text, "Wrong text");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Constructor_Word_NullWord() {
			DumpedWord w = new DumpedWord(null);
		}

	}

}
