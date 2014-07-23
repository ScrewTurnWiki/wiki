
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace ScrewTurn.Wiki.SearchEngine.Tests {

	[TestFixture]
	public class IndexChangedEventArgsTests : TestsBase {

		[Test]
		public void Constructor() {
			IDocument doc = MockDocument("Doc", "Document", "ptdoc", DateTime.Now);
			DumpedChange change = new DumpedChange(new DumpedDocument(doc), new List<DumpedWord>(),
				new List<DumpedWordMapping>(new DumpedWordMapping[] { new DumpedWordMapping(1, 1, 1, 1, 1) }));

			IndexChangedEventArgs args = new IndexChangedEventArgs(doc, IndexChangeType.DocumentAdded, change, null);

			Assert.AreSame(doc, args.Document, "Invalid document instance");
			Assert.AreEqual(IndexChangeType.DocumentAdded, args.Change, "Wrong change");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Constructor_NullDocument() {
			IDocument doc = MockDocument("Doc", "Document", "ptdoc", DateTime.Now);
			DumpedChange change = new DumpedChange(new DumpedDocument(doc), new List<DumpedWord>(),
				new List<DumpedWordMapping>(new DumpedWordMapping[] { new DumpedWordMapping(1, 1, 1, 1, 1) }));

			IndexChangedEventArgs args = new IndexChangedEventArgs(null, IndexChangeType.DocumentAdded, change, null);
		}

		[Test]
		public void Constructor_IndexCleared() {
			IndexChangedEventArgs args = new IndexChangedEventArgs(null, IndexChangeType.IndexCleared, null, null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Constructor_NullChangeData() {
			IndexChangedEventArgs args = new IndexChangedEventArgs(
				MockDocument("Doc", "Document", "ptdoc", DateTime.Now), IndexChangeType.DocumentAdded, null, null);
		}

	}

}
