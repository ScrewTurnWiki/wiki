
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace ScrewTurn.Wiki.SearchEngine.Tests {

	[TestFixture]
	public class SearchResultTests : TestsBase {

		[Test]
		public void Constructor() {
			IDocument doc = MockDocument("Doc", "Document", "ptdoc", DateTime.Now);
			SearchResult res = new SearchResult(doc);

			Assert.AreEqual(doc, res.Document, "Wrong document");
			Assert.AreEqual(0, res.Relevance.Value, "Wrong initial relevance value");
			Assert.IsFalse(res.Relevance.IsFinalized, "Initial relevance value should not be finalized");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Constructor_NullDocument() {
			SearchResult res = new SearchResult(null);
		}

	}

}
