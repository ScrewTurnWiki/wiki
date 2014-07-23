
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using RC = Rhino.Mocks.Constraints;

namespace ScrewTurn.Wiki.Plugins.SqlCommon.Tests {

	[TestFixture]
	public class SqlIndexTests {

		[Test]
		public void Constructor_NullConnector() {
			Assert.Throws<ArgumentNullException>(() => {
				new SqlIndex(null);
			});
		}

		[Test]
		public void TotalDocuments_TotalWords_TotalOccurrences() {
			MockRepository mocks = new MockRepository();
			IIndexConnector conn = mocks.StrictMock<IIndexConnector>();

			Expect.Call(conn.GetCount(IndexElementType.Documents)).Return(12);
			Expect.Call(conn.GetCount(IndexElementType.Words)).Return(567);
			Expect.Call(conn.GetCount(IndexElementType.Occurrences)).Return(3456);

			mocks.ReplayAll();

			SqlIndex index = new SqlIndex(conn);

			Assert.AreEqual(12, index.TotalDocuments, "Wrong document count");
			Assert.AreEqual(567, index.TotalWords, "Wrong word count");
			Assert.AreEqual(3456, index.TotalOccurrences, "Wrong occurence count");

			mocks.VerifyAll();
		}

		[Test]
		public void Clear() {
			MockRepository mocks = new MockRepository();
			IIndexConnector conn = mocks.StrictMock<IIndexConnector>();
			
			string dummyState = "state";

			conn.ClearIndex(dummyState);
			LastCall.On(conn);

			mocks.ReplayAll();

			SqlIndex index = new SqlIndex(conn);

			index.Clear(dummyState);

			mocks.VerifyAll();
		}

		[Test]
		public void StoreDocument() {
			MockRepository mocks = new MockRepository();
			IIndexConnector conn = mocks.StrictMock<IIndexConnector>();
			ScrewTurn.Wiki.SearchEngine.IDocument doc = mocks.StrictMock<ScrewTurn.Wiki.SearchEngine.IDocument>();

			string dummyState = "state";

			string content = "This is some test content.";
			string title = "My Document";

			Expect.Call(doc.Title).Return(title).Repeat.AtLeastOnce();
			Expect.Call(doc.Tokenize(content)).Return(ScrewTurn.Wiki.SearchEngine.Tools.Tokenize(content, ScrewTurn.Wiki.SearchEngine.WordLocation.Content));
			Expect.Call(doc.Tokenize(title)).Return(ScrewTurn.Wiki.SearchEngine.Tools.Tokenize(title, ScrewTurn.Wiki.SearchEngine.WordLocation.Title));

			Predicate<ScrewTurn.Wiki.SearchEngine.WordInfo[]> contentPredicate = (array) => {
				return
					array.Length == 5 &&
					array[0].Text == "this" &&
					array[1].Text == "is" &&
					array[2].Text == "some" &&
					array[3].Text == "test" &&
					array[4].Text == "content";
			};
			Predicate<ScrewTurn.Wiki.SearchEngine.WordInfo[]> titlePredicate = (array) => {
				return
					array.Length == 2 &&
					array[0].Text == "my" &&
					array[1].Text == "document";
			};
			Predicate<ScrewTurn.Wiki.SearchEngine.WordInfo[]> keywordsPredicate = (array) => {
				return
					array.Length == 1 &&
					array[0].Text == "test";
			};

			conn.DeleteDataForDocument(doc, dummyState);
			LastCall.On(conn);
			Expect.Call(conn.SaveDataForDocument(null, null, null, null, null)).IgnoreArguments()
				.Constraints(RC.Is.Same(doc), RC.Is.Matching(contentPredicate), RC.Is.Matching(titlePredicate), RC.Is.Matching(keywordsPredicate), RC.Is.Same(dummyState))
				.Return(8);

			mocks.ReplayAll();

			SqlIndex index = new SqlIndex(conn);

			Assert.AreEqual(8, index.StoreDocument(doc, new string[] { "test" }, content, dummyState), "Wrong occurrence count");

			mocks.VerifyAll();
		}

		[Test]
		public void Search() {
			// Basic integration test: search algorithms are already extensively tested with InMemoryIndexBase

			MockRepository mocks = new MockRepository();

			IIndexConnector conn = mocks.StrictMock<IIndexConnector>();
			ScrewTurn.Wiki.SearchEngine.IWordFetcher fetcher = mocks.StrictMock<ScrewTurn.Wiki.SearchEngine.IWordFetcher>();
			
			ScrewTurn.Wiki.SearchEngine.Word dummy = null;
			Expect.Call(fetcher.TryGetWord("test", out dummy)).Return(false);
			Expect.Call(fetcher.TryGetWord("query", out dummy)).Return(false);
			fetcher.Dispose();
			LastCall.On(fetcher);

			Expect.Call(conn.GetWordFetcher()).Return(fetcher);

			mocks.ReplayAll();

			SqlIndex index = new SqlIndex(conn);

			Assert.AreEqual(0, index.Search(new ScrewTurn.Wiki.SearchEngine.SearchParameters("test query")).Count, "Wrong search result count");

			mocks.VerifyAll();
		}

	}

}
