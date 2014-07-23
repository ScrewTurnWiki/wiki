
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScrewTurn.Wiki.SearchEngine.Tests;
using NUnit.Framework;
using ScrewTurn.Wiki.SearchEngine;

namespace ScrewTurn.Wiki.Tests {

	[TestFixture]
	public class IndexStorerTests : TestsBase {

		private string documentsFile = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "__SE_Documents.dat");
		private string wordsFile = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "__SE_Words.dat");
		private string mappingsFile = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "__SE_Mappings.dat");

		[TearDown]
		public void RemoveFiles() {
			try {
				File.Delete(documentsFile);
			}
			catch { }
			try {
				File.Delete(wordsFile);
			}
			catch { }
			try {
				File.Delete(mappingsFile);
			}
			catch { }
		}

		[Test]
		public void Constructor() {
			IInMemoryIndex index = MockInMemoryIndex();
			IndexStorer storer = new IndexStorer(documentsFile, wordsFile, mappingsFile, index);
			Assert.AreEqual(index, storer.Index, "Wrong index");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void Constructor_InvalidDocumentsFile(string file) {
			IndexStorer storer = new IndexStorer(file, wordsFile, mappingsFile, MockInMemoryIndex());
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void Constructor_InvalidWordsFile(string file) {
			IndexStorer storer = new IndexStorer(documentsFile, file, mappingsFile, MockInMemoryIndex());
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void Constructor_InvalidMappingsFile(string file) {
			IndexStorer storer = new IndexStorer(documentsFile, wordsFile, file, MockInMemoryIndex());
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Constructor_NullIndex() {
			IndexStorer storer = new IndexStorer(documentsFile, wordsFile, mappingsFile, null);
		}

		[Test]
		public void Size() {
			IInMemoryIndex index = MockInMemoryIndex();
			index.SetBuildDocumentDelegate(delegate(DumpedDocument d) { return null; });

			IndexStorer storer = new IndexStorer(documentsFile, wordsFile, mappingsFile, index);
			storer.LoadIndex();

			Assert.IsTrue(storer.Size > 0, "Size should be greater than zero");
		}

		[Test]
		public void LoadIndexAndModifications() {
			IInMemoryIndex index = MockInMemoryIndex();
			IDocument doc1 = MockDocument("doc1", "Document", "doc", DateTime.Now);
			IDocument doc2 = MockDocument2("doc2", "Article", "doc", DateTime.Now);
			
			BuildDocument buildDoc = new BuildDocument(delegate(DumpedDocument d) {
				return d.Name == doc1.Name ? doc1 : doc2;
			});

			index.SetBuildDocumentDelegate(buildDoc);

			IndexStorer storer = new IndexStorer(documentsFile, wordsFile, mappingsFile, index);
			storer.LoadIndex();

			index.StoreDocument(doc1, null, "", null);
			index.StoreDocument(doc2, null, "", null);

			Assert.AreEqual(2, index.TotalDocuments, "Wrong document count");
			Assert.AreEqual(12, index.TotalWords, "Wrong word count");
			Assert.AreEqual(12, index.TotalOccurrences, "Wrong occurrence count");

			storer.Dispose();
			storer = null;

			index = MockInMemoryIndex();
			index.SetBuildDocumentDelegate(buildDoc);

			storer = new IndexStorer(documentsFile, wordsFile, mappingsFile, index);
			storer.LoadIndex();

			Assert.AreEqual(2, index.TotalDocuments, "Wrong document count");
			Assert.AreEqual(12, index.TotalWords, "Wrong word count");
			Assert.AreEqual(12, index.TotalOccurrences, "Wrong occurrence count");

			SearchResultCollection res = index.Search(new SearchParameters("document content article"));
			Assert.AreEqual(2, res.Count, "Wrong result count");
			Assert.AreEqual(2, res[0].Matches.Count, "Wrong matches count");
			Assert.AreEqual(1, res[1].Matches.Count, "Wrong matches count");

			Assert.AreEqual("document", res[0].Matches[0].Text, "Wrong match text");
			Assert.AreEqual(0, res[0].Matches[0].FirstCharIndex, "Wrong match first char index");
			Assert.AreEqual(0, res[0].Matches[0].WordIndex, "Wrong match word index");
			Assert.AreEqual(WordLocation.Title, res[0].Matches[0].Location, "Wrong match location");

			Assert.AreEqual("content", res[0].Matches[1].Text, "Wrong match text");
			Assert.AreEqual(13, res[0].Matches[1].FirstCharIndex, "Wrong match first char index");
			Assert.AreEqual(3, res[0].Matches[1].WordIndex, "Wrong match word index");
			Assert.AreEqual(WordLocation.Content, res[0].Matches[1].Location, "Wrong match location");

			Assert.AreEqual("article", res[1].Matches[0].Text, "Wrong match text");
			Assert.AreEqual(0, res[1].Matches[0].FirstCharIndex, "Wrong match first char index");
			Assert.AreEqual(0, res[1].Matches[0].WordIndex, "Wrong match word index");
			Assert.AreEqual(WordLocation.Title, res[1].Matches[0].Location, "Wrong match location");

			index.RemoveDocument(doc1, null);

			storer.Dispose();
			storer = null;

			index = MockInMemoryIndex();
			index.SetBuildDocumentDelegate(buildDoc);

			storer = new IndexStorer(documentsFile, wordsFile, mappingsFile, index);
			storer.LoadIndex();

			Assert.AreEqual(1, index.TotalDocuments, "Wrong document count");
			Assert.AreEqual(7, index.TotalWords, "Wrong word count");
			Assert.AreEqual(7, index.TotalOccurrences, "Wrong occurrence count");

			res = index.Search(new SearchParameters("document content article"));
			Assert.AreEqual(1, res.Count, "Wrong result count");
			Assert.AreEqual(1, res[0].Matches.Count, "Wrong matches count");

			Assert.AreEqual("article", res[0].Matches[0].Text, "Wrong match text");
			Assert.AreEqual(0, res[0].Matches[0].FirstCharIndex, "Wrong match first char index");
			Assert.AreEqual(0, res[0].Matches[0].WordIndex, "Wrong match word index");
			Assert.AreEqual(WordLocation.Title, res[0].Matches[0].Location, "Wrong match location");

			index.Clear(null);

			storer.Dispose();
			storer = null;

			index = MockInMemoryIndex();
			index.SetBuildDocumentDelegate(buildDoc);

			storer = new IndexStorer(documentsFile, wordsFile, mappingsFile, index);
			storer.LoadIndex();

			Assert.AreEqual(0, index.TotalDocuments, "Wrong document count");
			Assert.AreEqual(0, index.TotalWords, "Wrong word count");
			Assert.AreEqual(0, index.TotalOccurrences, "Wrong occurrence count");
			Assert.AreEqual(0, index.Search(new SearchParameters("document")).Count, "Wrong result count");
		}

		[Test]
		public void DataCorrupted() {
			IInMemoryIndex index = MockInMemoryIndex();
			IDocument doc1 = MockDocument("doc1", "Document", "doc", DateTime.Now);
			IDocument doc2 = MockDocument2("doc2", "Article", "doc", DateTime.Now);
			index.SetBuildDocumentDelegate(delegate(DumpedDocument d) { return d.Name == doc1.Name ? doc1 : doc2; });

			IndexStorer storer = new IndexStorer(documentsFile, wordsFile, mappingsFile, index);
			storer.LoadIndex();

			index.StoreDocument(doc1, null, "", null);
			index.StoreDocument(doc2, null, "", null);

			Assert.AreEqual(2, index.TotalDocuments, "Wrong document count");
			Assert.AreEqual(12, index.TotalWords, "Wrong word count");
			Assert.AreEqual(12, index.TotalOccurrences, "Wrong occurrence count");

			storer.Dispose();
			storer = null;

			File.Create(documentsFile).Close();

			index = MockInMemoryIndex();

			storer = new IndexStorer(documentsFile, wordsFile, mappingsFile, index);
			storer.LoadIndex();

			Assert.IsTrue(storer.DataCorrupted, "DataCorrupted should be true");
			Assert.AreEqual(0, index.TotalDocuments, "Wrong document count");
		}

		[Test]
		public void ReplaceDocument() {
			// This test checks that IndexStorer properly discards the old document when a new copy is stored,
			// even when the title and date/time are different

			IInMemoryIndex index = MockInMemoryIndex();
			DateTime dt1 = DateTime.Now.AddDays(-1);
			IDocument doc1 = MockDocument("doc1", "Document", "doc", dt1);
			IDocument doc2 = MockDocument2("doc1", "Article", "doc", DateTime.Now);
			index.SetBuildDocumentDelegate(delegate(DumpedDocument d) { return d.DateTime == dt1 ? doc1 : doc2; });

			IndexStorer storer = new IndexStorer(documentsFile, wordsFile, mappingsFile, index);
			storer.LoadIndex();

			index.StoreDocument(doc1, null, "", null);
			Assert.AreEqual(1, index.TotalDocuments, "Wrong document count");
			index.StoreDocument(doc2, null, "", null);
			Assert.AreEqual(1, index.TotalDocuments, "Wrong document count");

			storer.Dispose();
			storer = null;

			index = MockInMemoryIndex();
			index.SetBuildDocumentDelegate(delegate(DumpedDocument d) { return d.DateTime == dt1 ? doc1 : doc2; });

			storer = new IndexStorer(documentsFile, wordsFile, mappingsFile, index);
			storer.LoadIndex();

			Assert.AreEqual(1, index.TotalDocuments, "Wrong document count");
		}

	}

}
