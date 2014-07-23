
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;

namespace ScrewTurn.Wiki.SearchEngine.Tests {

	public abstract class IndexBaseTests : TestsBase {

		// These tests treat instances of IInMemoryIndex as special cases
		// They are tested correctly, properly handling the IndexChanged event

		/// <summary>
		/// Gets the instance of the index to test.
		/// </summary>
		/// <returns>The instance of the index.</returns>
		protected abstract IIndex GetIndex();

		[Test]
		public void StopWordsProperty() {
			IIndex index = GetIndex();
			Assert.AreEqual(0, index.StopWords.Length, "Wrong stop words count");

			index.StopWords = new string[] { "the", "those" };
			Assert.AreEqual(2, index.StopWords.Length, "Wrong stop words count");
			Assert.AreEqual("the", index.StopWords[0], "Wrong stop word at index 0");
			Assert.AreEqual("those", index.StopWords[1], "Wrong stop word at index 1");
		}

		[Test]
		public void Statistics() {
			IIndex index = GetIndex();

			Assert.AreEqual(0, index.TotalWords, "Wrong total words count");
			Assert.AreEqual(0, index.TotalDocuments, "Wrong total documents count");
			Assert.AreEqual(0, index.TotalOccurrences, "Wrong total occurrences count");
		}

		[Test]
		public void Clear() {
			IIndex index = GetIndex();
			IInMemoryIndex imIndex = index as IInMemoryIndex;

			IDocument doc = MockDocument("Doc", "Document", "ptdoc", DateTime.Now);

			if(imIndex != null) imIndex.IndexChanged += AutoHandlerForDocumentStorage;
			index.StoreDocument(doc, null, PlainTextDocumentContent, null);

			bool eventFired = false;
			if(imIndex != null) {
				imIndex.IndexChanged += delegate(object sender, IndexChangedEventArgs e) {
					eventFired = true;
				};
			}

			index.Clear(null);

			if(imIndex != null) Assert.IsTrue(eventFired, "IndexChanged event not fired");
			Assert.AreEqual(0, index.TotalDocuments, "Wrong document count");
			Assert.AreEqual(0, index.TotalWords, "Wrong word count");
			Assert.AreEqual(0, index.TotalOccurrences, "Wrong occurrence count");
			Assert.AreEqual(0, index.Search(new SearchParameters("document")).Count, "Wrong result count");
		}

		[Test]
		public void StoreDocument() {
			IIndex index = GetIndex();
			IInMemoryIndex imIndex = index as IInMemoryIndex;

			IDocument doc = MockDocument("Doc", "Document", "ptdoc", DateTime.Now);

			bool eventFired = false;
			if(imIndex != null) {
				imIndex.IndexChanged += delegate(object sender, IndexChangedEventArgs e) {
					eventFired = e.Document == doc && e.Change == IndexChangeType.DocumentAdded;
					if(eventFired) {
						Assert.AreEqual("Doc", e.ChangeData.Document.Name, "Wrong document");
						Assert.AreEqual(5, e.ChangeData.Words.Count, "Wrong count");
						Assert.AreEqual(5, e.ChangeData.Mappings.Count, "Wrong count");
						Assert.IsTrue(e.ChangeData.Mappings.Find(delegate(DumpedWordMapping m) { return m.WordIndex == 0 && m.Location == WordLocation.Content.Location; }) != null, "Mappings does not contain a word");
						Assert.IsTrue(e.ChangeData.Mappings.Find(delegate(DumpedWordMapping m) { return m.WordIndex == 1 && m.Location == WordLocation.Content.Location; }) != null, "Mappings does not contain a word");
						Assert.IsTrue(e.ChangeData.Mappings.Find(delegate(DumpedWordMapping m) { return m.WordIndex == 2 && m.Location == WordLocation.Content.Location; }) != null, "Mappings does not contain a word");
						Assert.IsTrue(e.ChangeData.Mappings.Find(delegate(DumpedWordMapping m) { return m.WordIndex == 3 && m.Location == WordLocation.Content.Location; }) != null, "Mappings does not contain a word");
						Assert.IsTrue(e.ChangeData.Mappings.Find(delegate(DumpedWordMapping m) { return m.WordIndex == 0 && m.Location == WordLocation.Title.Location; }) != null, "Mappings does not contain a word");
					}
				};

				imIndex.IndexChanged += AutoHandlerForDocumentStorage;
			}

			Assert.AreEqual(5, index.StoreDocument(doc, null, PlainTextDocumentContent, null), "Wrong number of indexed words");
			Assert.AreEqual(5, index.TotalWords, "Wrong total words count");
			Assert.AreEqual(5, index.TotalOccurrences, "Wrong total occurrences count");
			Assert.AreEqual(1, index.TotalDocuments, "Wrong total documents count");
			if(imIndex != null) Assert.IsTrue(eventFired, "Event not fired");
		}

		[Test]
		public void StoreDocument_ExistingDocument() {
			IIndex index = GetIndex();
			IInMemoryIndex imIndex = index as IInMemoryIndex;

			if(imIndex != null) imIndex.IndexChanged += AutoHandlerForDocumentStorage;

			IDocument doc = MockDocument("Doc", "Document", "ptdoc", DateTime.Now);

			Assert.AreEqual(5, index.StoreDocument(doc, null, PlainTextDocumentContent, null), "Wrong number of indexed words");
			Assert.AreEqual(5, index.TotalWords, "Wrong total words count");
			Assert.AreEqual(5, index.TotalOccurrences, "Wrong total occurrences count");
			Assert.AreEqual(1, index.TotalDocuments, "Wrong total documents count");

			doc = MockDocument2("Doc", "Document", "ptdoc", DateTime.Now);

			Assert.AreEqual(7, index.StoreDocument(doc, null, PlainTextDocumentContent2, null), "Wrong number of indexed words");
			Assert.AreEqual(7, index.TotalWords, "Wrong total words count");
			Assert.AreEqual(7, index.TotalOccurrences, "Wrong total occurrences count");
			Assert.AreEqual(1, index.TotalDocuments, "Wrong total documents count");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void StoreDocument_NullDocument() {
			IIndex index = GetIndex();
			index.StoreDocument(null, null, "blah", null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void StoreDocument_NullContent() {
			IIndex index = GetIndex();
			index.StoreDocument(MockDocument("Doc", "Document", "ptdoc", DateTime.Now), null, null, null);
		}

		[Test]
		public void RemoveDocument() {
			IIndex index = GetIndex();
			IInMemoryIndex imIndex = index as IInMemoryIndex;

			IDocument doc1 = MockDocument("Doc1", "Document 1", "ptdoc", DateTime.Now);
			IDocument doc2 = MockDocument("Doc2", "Document 2", "ptdoc", DateTime.Now);

			if(imIndex != null) imIndex.IndexChanged += AutoHandlerForDocumentStorage;

			index.StoreDocument(doc1, null, "", null);
			index.StoreDocument(doc2, null, "", null);

			bool eventFired = false;

			if(imIndex != null) {
				imIndex.IndexChanged += delegate(object sender, IndexChangedEventArgs e) {
					eventFired = e.Document == doc1 && e.Change == IndexChangeType.DocumentRemoved;
					if(eventFired) {
						Assert.AreEqual("Doc1", e.ChangeData.Document.Name, "Wrong document");
						Assert.AreEqual(1, e.ChangeData.Words.Count, "Wrong count");
						Assert.AreEqual(6, e.ChangeData.Mappings.Count, "Wrong count");
						Assert.IsTrue(e.ChangeData.Mappings.Find(delegate(DumpedWordMapping m) { return m.WordIndex == 0 && m.Location == WordLocation.Content.Location; }) != null, "Mappings does not contain a word");
						Assert.IsTrue(e.ChangeData.Mappings.Find(delegate(DumpedWordMapping m) { return m.WordIndex == 1 && m.Location == WordLocation.Content.Location; }) != null, "Mappings does not contain a word");
						Assert.IsTrue(e.ChangeData.Mappings.Find(delegate(DumpedWordMapping m) { return m.WordIndex == 2 && m.Location == WordLocation.Content.Location; }) != null, "Mappings does not contain a word");
						Assert.IsTrue(e.ChangeData.Mappings.Find(delegate(DumpedWordMapping m) { return m.WordIndex == 3 && m.Location == WordLocation.Content.Location; }) != null, "Mappings does not contain a word");
						Assert.IsTrue(e.ChangeData.Mappings.Find(delegate(DumpedWordMapping m) { return m.WordIndex == 0 && m.Location == WordLocation.Title.Location; }) != null, "Mappings does not contain a word");
						Assert.IsTrue(e.ChangeData.Mappings.Find(delegate(DumpedWordMapping m) { return m.WordIndex == 1 && m.Location == WordLocation.Title.Location; }) != null, "Mappings does not contain a word");
					}
				};
			}

			Assert.AreEqual(2, index.TotalDocuments, "Wrong document count");

			index.RemoveDocument(doc1, null);
			Assert.AreEqual(1, index.TotalDocuments, "Wrong document count");

			IDocument doc3 = MockDocument("Doc1", "Document 1", "ptdoc", DateTime.Now);
			index.StoreDocument(doc3, null, "", null);
			Assert.AreEqual(2, index.TotalDocuments, "Wrong document count");

			index.RemoveDocument(doc1, null);
			Assert.AreEqual(1, index.TotalDocuments, "Wrong document count");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemoveDocument_NullDocument() {
			IIndex index = GetIndex();
			index.RemoveDocument(null, null);
		}

		private static bool AreDocumentsEqual(IDocument doc1, IDocument doc2) {
			return
				//doc1.ID == doc2.ID && // ID can be different (new instance / loaded from storage)
				doc1.Name == doc2.Name &&
				doc1.Title == doc2.Title &&
				doc1.TypeTag == doc2.TypeTag &&
				doc1.DateTime.ToString("yyMMddHHmmss") == doc2.DateTime.ToString("yyMMddHHmmss");
		}

		[Test]
		public void Search_Basic() {
			IIndex index = GetIndex();
			IInMemoryIndex imIndex = index as IInMemoryIndex;

			// The mocked document has a default content
			IDocument doc1 = MockDocument("Doc1", "Document 1", "ptdoc", DateTime.Now);
			IDocument doc2 = MockDocument("Doc2", "Document 2", "ptdoc", DateTime.Now);
			IDocument doc3 = MockDocument("Doc3", "Document 3", "ptdoc", DateTime.Now);

			if(imIndex != null) imIndex.IndexChanged += AutoHandlerForDocumentStorage;

			index.StoreDocument(doc1, null, "", null);
			index.StoreDocument(doc2, null, "", null);
			index.StoreDocument(doc3, null, "", null);

			SearchResultCollection res1 = index.Search(new SearchParameters("specifications"));
			SearchResultCollection res2 = index.Search(new SearchParameters("this"));

			Assert.AreEqual(0, res1.Count, "Wrong result count");
			Assert.AreEqual(3, res2.Count, "Wrong result count");

			// Matches are in unpredictable order
			bool doc1Found = false, doc2Found = false, doc3Found = false;
			foreach(SearchResult r in res2) {
				if(AreDocumentsEqual(r.Document, doc1)) doc1Found = true;
				else if(AreDocumentsEqual(r.Document, doc2)) doc2Found = true;
				else if(AreDocumentsEqual(r.Document, doc3)) doc3Found = true;

				Assert.AreEqual(1, r.Matches.Count, "Wrong match count");
				Assert.AreEqual(0, r.Matches[0].FirstCharIndex, "Wrong start index");
				Assert.AreEqual(4, r.Matches[0].Text.Length, "Wrong length");
				Assert.AreEqual(33.3333F, r.Relevance.Value, 0.01F, "Wrong relevance value");
			}

			Assert.IsTrue(doc1Found, "Doc1 not found in results");
			Assert.IsTrue(doc2Found, "Doc2 not found in results");
			Assert.IsTrue(doc3Found, "Doc3 not found in results");
		}

		[Test]
		public void Search_Basic_SingleResultWord() {
			IIndex index = GetIndex();
			IInMemoryIndex imIndex = index as IInMemoryIndex;

			IDocument doc = MockDocument3("Doc", "Document", "ptdoc", DateTime.Now);

			if(imIndex != null) imIndex.IndexChanged += AutoHandlerForDocumentStorage;

			index.StoreDocument(doc, null, "", null);

			SearchResultCollection res = index.Search(new SearchParameters("todo"));

			Assert.AreEqual(1, res.Count, "Wrong result count");
			Assert.IsTrue(AreDocumentsEqual(doc, res[0].Document), "Wrong document");
			Assert.AreEqual(100, res[0].Relevance.Value, "Wrong relevance");
			Assert.AreEqual(1, res[0].Matches.Count, "Wrong match count");
			Assert.AreEqual(0, res[0].Matches[0].FirstCharIndex, "Wrong first char index");
			Assert.AreEqual(0, res[0].Matches[0].WordIndex, "Wrong word index");
			Assert.AreEqual("todo", res[0].Matches[0].Text, "Wrong word text");
		}

		[Test]
		public void Search_Basic_MultipleWords() {
			IIndex index = GetIndex();
			IInMemoryIndex imIndex = index as IInMemoryIndex;

			// The mocked document has a default content
			IDocument doc1 = MockDocument("Doc1", "Document 1", "ptdoc", DateTime.Now);

			if(imIndex != null) imIndex.IndexChanged += AutoHandlerForDocumentStorage;

			index.StoreDocument(doc1, null, "", null);

			SearchResultCollection res = index.Search(new SearchParameters("this content"));

			Assert.AreEqual(1, res.Count, "Wrong result count");
			Assert.AreEqual(100F, res[0].Relevance.Value, 0.01F, "Wrong relevance value");
			Assert.IsTrue(AreDocumentsEqual(doc1, res[0].Document), "Wrong document");
			Assert.AreEqual(2, res[0].Matches.Count, "Wrong matches count");
			Assert.AreEqual(0, res[0].Matches[0].FirstCharIndex, "Wrong start index for match at position 0");
			Assert.AreEqual(4, res[0].Matches[0].Text.Length, "Wrong length for match at position 0");
			Assert.AreEqual(13, res[0].Matches[1].FirstCharIndex, "Wrong start index for match at position 1");
			Assert.AreEqual(7, res[0].Matches[1].Text.Length, "Wrong length for match at position 1");
		}

		[Test]
		public void Search_Filtered() {
			IIndex index = GetIndex();
			IInMemoryIndex imIndex = index as IInMemoryIndex;

			// The mocked document has a default content
			IDocument doc1 = MockDocument("Doc1", "Document 1", "ptdoc", DateTime.Now);
			IDocument doc2 = MockDocument("Doc2", "Document 2", "htmldoc", DateTime.Now);
			IDocument doc3 = MockDocument("Doc3", "Document 3", "odoc", DateTime.Now);

			if(imIndex != null) imIndex.IndexChanged += AutoHandlerForDocumentStorage;

			index.StoreDocument(doc1, null, "", null);
			index.StoreDocument(doc2, null, "", null);
			index.StoreDocument(doc3, null, "", null);

			SearchResultCollection res = index.Search(new SearchParameters("this", "ptdoc", "htmldoc"));

			Assert.AreEqual(2, res.Count, "Wrong result count");

			// Matches are in unpredictable order
			bool doc1Found = false, doc2Found = false, doc3Found = false;
			foreach(SearchResult r in res) {
				if(AreDocumentsEqual(r.Document, doc1)) doc1Found = true;
				else if(AreDocumentsEqual(r.Document, doc2)) doc2Found = true;
				else if(AreDocumentsEqual(r.Document, doc3)) doc3Found = true;

				Assert.AreEqual(1, r.Matches.Count, "Wrong match count");
				Assert.AreEqual(0, r.Matches[0].FirstCharIndex, "Wrong start index");
				Assert.AreEqual(4, r.Matches[0].Text.Length, "Wrong length");
				Assert.AreEqual(50F, r.Relevance.Value, 0.01F, "Wrong relevance value");
			}

			Assert.IsTrue(doc1Found, "Doc1 not found in results");
			Assert.IsTrue(doc2Found, "Doc2 not found in results");
			Assert.IsFalse(doc3Found, "Doc3 found in results");
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void Search_Filtered_EmptyDocumentTags() {
			IIndex index = GetIndex();
			index.Search(new SearchParameters("hello", new string[0]));
		}

		[Test]
		public void Search_WithOptions_AtLeastOneWord() {
			IIndex index = GetIndex();
			IInMemoryIndex imIndex = index as IInMemoryIndex;

			IDocument doc1 = MockDocument("Doc1", "Document 1", "ptdoc", DateTime.Now);

			if(imIndex != null) imIndex.IndexChanged += AutoHandlerForDocumentStorage;

			index.StoreDocument(doc1, null, "", null);

			SearchResultCollection res = index.Search(new SearchParameters("this content", SearchOptions.AllWords));

			Assert.AreEqual(1, res.Count, "Wrong result count");
			Assert.AreEqual(0, res[0].Matches[0].FirstCharIndex, "Wrong start index for 'this'");
			Assert.AreEqual(4, res[0].Matches[0].Text.Length, "Wrong length for 'index'");
			Assert.AreEqual(13, res[0].Matches[1].FirstCharIndex, "Wrong start index for 'content'");
			Assert.AreEqual(7, res[0].Matches[1].Text.Length, "Wrong length for 'content'");

			res = index.Search(new SearchParameters("this stuff", SearchOptions.AtLeastOneWord));

			Assert.AreEqual(1, res.Count, "Wrong result count");
		}

		[TestCase("content", 1)]
		[TestCase("this content", 1)]
		[TestCase("this stuff", 0)]
		[TestCase("blah", 0)]
		public void Search_WithOptions_AllWords(string query, int results) {
			IIndex index = GetIndex();
			IInMemoryIndex imIndex = index as IInMemoryIndex;

			IDocument doc1 = MockDocument("Doc1", "Document 1", "ptdoc", DateTime.Now);

			if(imIndex != null) imIndex.IndexChanged += AutoHandlerForDocumentStorage;

			index.StoreDocument(doc1, null, "", null);

			SearchResultCollection res = index.Search(new SearchParameters(query, SearchOptions.AllWords));

			Assert.AreEqual(results, res.Count, "Wrong result count");
		}

		[TestCase("content", 1)]
		[TestCase("this is some content", 1)]
		[TestCase("THIS SOME content is", 0)]
		[TestCase("this is test content", 0)]
		[TestCase("blah", 0)]
		public void Search_WithOptions_ExactPhrase(string query, int results) {
			IIndex index = GetIndex();
			IInMemoryIndex imIndex = index as IInMemoryIndex;

			IDocument doc1 = MockDocument("Doc1", "Document 1", "ptdoc", DateTime.Now);
			
			if(imIndex != null) imIndex.IndexChanged += AutoHandlerForDocumentStorage;

			index.StoreDocument(doc1, null, "", null);

			SearchResultCollection res = index.Search(new SearchParameters(query, SearchOptions.ExactPhrase));

			Assert.AreEqual(results, res.Count, "Wrong result count");
		}

		[Test]
		public void Search_ExactPhrase() {
			IIndex index = GetIndex();
			IInMemoryIndex imIndex = index as IInMemoryIndex;

			IDocument doc = MockDocument4("Doc", "Document", "ptdoc", DateTime.Now);

			if(imIndex != null) imIndex.IndexChanged += AutoHandlerForDocumentStorage;

			index.StoreDocument(doc, null, "", null);

			SearchResultCollection res = index.Search(new SearchParameters("content repeated content blah blah", SearchOptions.ExactPhrase));
			Assert.AreEqual(0, res.Count, "Wrong result count");

			res = index.Search(new SearchParameters("repeated content", SearchOptions.ExactPhrase));
			Assert.AreEqual(1, res.Count, "Wrong result count");
		}

		[Test]
		public void Search_Basic_Location() {
			IIndex index = GetIndex();
			IInMemoryIndex imIndex = index as IInMemoryIndex;

			IDocument doc1 = MockDocument("Doc1", "Document 1", "ptdoc", DateTime.Now);

			if(imIndex != null) imIndex.IndexChanged += AutoHandlerForDocumentStorage;

			index.StoreDocument(doc1, new string[] { "development" }, "", null);

			SearchResultCollection res = index.Search(new SearchParameters("document"));

			Assert.AreEqual(1, res.Count, "Wrong result count");
			Assert.AreEqual(1, res[0].Matches.Count, "Wrong matches count");
			Assert.AreEqual(WordLocation.Title, res[0].Matches[0].Location, "Wrong location");

			res = index.Search(new SearchParameters("content"));

			Assert.AreEqual(1, res.Count, "Wrong result count");
			Assert.AreEqual(1, res[0].Matches.Count, "Wrong matches count");
			Assert.AreEqual(WordLocation.Content, res[0].Matches[0].Location, "Wrong location");

			res = index.Search(new SearchParameters("development"));

			Assert.AreEqual(1, res.Count, "Wrong result count");
			Assert.AreEqual(1, res[0].Matches.Count, "Wrong matches count");
			Assert.AreEqual(WordLocation.Keywords, res[0].Matches[0].Location, "Wrong location");

			IDocument doc2 = MockDocument2("Doc2", "Text 2", "ptdoc", DateTime.Now);
			index.StoreDocument(doc2, new string[] { "document" }, "", null);

			res = index.Search(new SearchParameters("document"));

			Assert.AreEqual(2, res.Count, "Wrong result count");
			Assert.AreEqual(1, res[0].Matches.Count, "Wrong matches count");
			Assert.AreEqual(1, res[1].Matches.Count, "Wrong matches count");
		}

		[Test]
		public void Search_Basic_LocationRelevance_1() {
			IIndex index = GetIndex();
			IInMemoryIndex imIndex = index as IInMemoryIndex;

			IDocument doc1 = MockDocument("Doc1", "Document 1", "ptdoc", DateTime.Now);
			IDocument doc2 = MockDocument2("Doc2", "Text 2", "ptdoc", DateTime.Now);

			if(imIndex != null) imIndex.IndexChanged += AutoHandlerForDocumentStorage;

			index.StoreDocument(doc1, null, "", null);
			index.StoreDocument(doc2, null, "", null);

			// "dummy" is only present in the content of doc2
			// "document" is only present in the title of doc1
			SearchResultCollection res = index.Search(new SearchParameters("dummy document"));

			Assert.AreEqual(2, res.Count, "Wrong result count");
			Assert.AreEqual(1, res[0].Matches.Count, "Wrong matches count");
			Assert.AreEqual(1, res[1].Matches.Count, "Wrong matches count");
			foreach(SearchResult r in res) {
				if(r.Matches[0].Location == WordLocation.Content) Assert.AreEqual(33.3, r.Relevance.Value, 0.1, "Wrong relevance for content");
				else if(r.Matches[0].Location == WordLocation.Title) Assert.AreEqual(66.6, r.Relevance.Value, 0.1, "Wrong relevance for title");
			}
		}

		[Test]
		public void Search_Basic_LocationRelevance_2() {
			IIndex index = GetIndex();
			IInMemoryIndex imIndex = index as IInMemoryIndex;

			IDocument doc1 = MockDocument("Doc1", "Document 1", "ptdoc", DateTime.Now);
			IDocument doc2 = MockDocument2("Doc2", "Text 2", "ptdoc", DateTime.Now);

			if(imIndex != null) imIndex.IndexChanged += AutoHandlerForDocumentStorage;

			index.StoreDocument(doc1, null, "", null);
			index.StoreDocument(doc2, new string[] { "blah" }, "", null);

			// "blah" is only present in the keywords of doc2
			// "content" is only present in the content of doc1
			SearchResultCollection res = index.Search(new SearchParameters("content blah"));

			Assert.AreEqual(2, res.Count, "Wrong result count");
			Assert.AreEqual(1, res[0].Matches.Count, "Wrong matches count");
			Assert.AreEqual(1, res[1].Matches.Count, "Wrong matches count");
			foreach(SearchResult r in res) {
				if(r.Matches[0].Location == WordLocation.Content) Assert.AreEqual(40.0, r.Relevance.Value, 0.1, "Wrong relevance for content");
				else if(r.Matches[0].Location == WordLocation.Keywords) Assert.AreEqual(60.0, r.Relevance.Value, 0.1, "Wrong relevance for keywords");
			}
		}

		[Test]
		public void Search_Basic_LocationRelevance_3() {
			IIndex index = GetIndex();
			IInMemoryIndex imIndex = index as IInMemoryIndex;

			IDocument doc1 = MockDocument("Doc1", "Document 1", "ptdoc", DateTime.Now);
			IDocument doc2 = MockDocument2("Doc2", "Text 2", "ptdoc", DateTime.Now);

			if(imIndex != null) imIndex.IndexChanged += AutoHandlerForDocumentStorage;

			index.StoreDocument(doc1, null, "", null);
			index.StoreDocument(doc2, new string[] { "blah" }, "", null);

			// "blah" is only present in the keywords of doc2
			// "document" is only present in the title of doc1
			SearchResultCollection res = index.Search(new SearchParameters("document blah"));

			Assert.AreEqual(2, res.Count, "Wrong result count");
			Assert.AreEqual(1, res[0].Matches.Count, "Wrong matches count");
			Assert.AreEqual(1, res[1].Matches.Count, "Wrong matches count");
			foreach(SearchResult r in res) {
				if(r.Matches[0].Location == WordLocation.Keywords) Assert.AreEqual(42.8, r.Relevance.Value, 0.1, "Wrong relevance for content");
				else if(r.Matches[0].Location == WordLocation.Title) Assert.AreEqual(57.1, r.Relevance.Value, 0.1, "Wrong relevance for keywords");
			}
		}

	}

}
