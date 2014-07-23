
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace ScrewTurn.Wiki.SearchEngine.Tests {

	[TestFixture]
	public class DumpedDocumentTests : TestsBase {

		[Test]
		public void Constructor_WithDocument() {
			IDocument doc = MockDocument("name", "Title", "doc", DateTime.Now);
			DumpedDocument ddoc = new DumpedDocument(doc);

			Assert.AreEqual(doc.ID, ddoc.ID, "Wrong ID");
			Assert.AreEqual("name", ddoc.Name, "Wrong name");
			Assert.AreEqual("Title", ddoc.Title, "Wrong title");
			Assert.AreEqual(doc.DateTime, ddoc.DateTime, "Wrong date/time");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Constructor_WithDocument_NullDocument() {
			DumpedDocument ddoc = new DumpedDocument(null);
		}

		[Test]
		public void Constructor_WithParameters() {
			IDocument doc = MockDocument("name", "Title", "doc", DateTime.Now);
			DumpedDocument ddoc = new DumpedDocument(doc.ID, doc.Name, doc.Title, doc.TypeTag, doc.DateTime);

			Assert.AreEqual(doc.ID, ddoc.ID, "Wrong ID");
			Assert.AreEqual("name", ddoc.Name, "Wrong name");
			Assert.AreEqual("Title", ddoc.Title, "Wrong title");
			Assert.AreEqual(doc.DateTime, ddoc.DateTime, "Wrong date/time");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void Constructor_WithParameters_InvalidName(string name) {
			DumpedDocument ddoc = new DumpedDocument(10, name, "Title", "doc", DateTime.Now);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void Constructor_WithParameters_InvalidTitle(string title) {
			DumpedDocument ddoc = new DumpedDocument(1, "name", title, "doc", DateTime.Now);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void Constructor_WithParameters_InvalidTypeTag(string typeTag) {
			DumpedDocument ddoc = new DumpedDocument(1, "name", "Title", typeTag, DateTime.Now);
		}

	}

}
