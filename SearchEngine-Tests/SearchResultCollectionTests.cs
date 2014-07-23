
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace ScrewTurn.Wiki.SearchEngine.Tests {

	[TestFixture]
	public class SearchResultCollectionTests : TestsBase {

		[Test]
		public void Constructor_NoCapacity() {
			SearchResultCollection collection = new SearchResultCollection();
			Assert.AreEqual(0, collection.Count, "Wrong count (collection should be empty)");
		}

		[Test]
		public void Constructor_WithCapacity() {
			SearchResultCollection collection = new SearchResultCollection(15);
			Assert.AreEqual(0, collection.Count, "Wrong count (collection should be empty)");
			Assert.AreEqual(15, collection.Capacity, "Wrong capacity");
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void Constructor_InvalidCapacity() {
			SearchResultCollection collection = new SearchResultCollection(0);
		}

		[Test]
		public void AddAndCount() {
			SearchResultCollection collection = new SearchResultCollection();

			Assert.AreEqual(0, collection.Count);

			SearchResult res = new SearchResult(MockDocument("d", "d", "d", DateTime.Now));
			SearchResult res2 = new SearchResult(MockDocument("d2", "d", "d", DateTime.Now));

			collection.Add(res);
			collection.Add(res2);
			Assert.AreEqual(2, collection.Count, "Wrong count (collection should contain 2 items)");
			Assert.AreEqual(res, collection[0], "Wrong item at index 0");
			Assert.AreEqual(res2, collection[1], "Wrong item at index 1");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Add_NullItem() {
			SearchResultCollection collection = new SearchResultCollection();
			collection.Add(null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void Add_DuplicateItem() {
			SearchResultCollection collection = new SearchResultCollection();

			SearchResult res = new SearchResult(MockDocument("d", "d", "d", DateTime.Now));
			SearchResult res2 = new SearchResult(MockDocument("d2", "d", "d", DateTime.Now));

			collection.Add(res);
			collection.Add(res2);
			collection.Add(res);
		}

		[Test]
		public void Clear() {
			SearchResultCollection collection = new SearchResultCollection();

			SearchResult res = new SearchResult(MockDocument("d", "d", "d", DateTime.Now));

			collection.Add(res);
			Assert.AreEqual(1, collection.Count, "Wrong count (collection should contain 1 item)");

			collection.Clear();
			Assert.AreEqual(0, collection.Count, "Wrong count (collection should be empty)");
		}

		[Test]
		public void Contains() {
			SearchResultCollection collection = new SearchResultCollection();

			SearchResult res = new SearchResult(MockDocument("d", "d", "d", DateTime.Now));
			SearchResult res2 = new SearchResult(MockDocument("d2", "d", "d", DateTime.Now));

			collection.Add(res);
			Assert.IsTrue(collection.Contains(res), "Collection should contain item");
			Assert.IsFalse(collection.Contains(res2), "Collection should not contain item");

			Assert.IsFalse(collection.Contains(null), "Contains should return false");
		}

		[Test]
		public void GetSearchResult() {
			SearchResultCollection collection = new SearchResultCollection();

			IDocument doc1 = MockDocument("d", "d", "d", DateTime.Now);
			IDocument doc2 = MockDocument("d2", "d", "d", DateTime.Now);
			IDocument doc3 = MockDocument("d3", "d", "d", DateTime.Now);
			SearchResult res = new SearchResult(doc1);
			SearchResult res2 = new SearchResult(doc2);

			collection.Add(res);
			collection.Add(res2);

			Assert.AreEqual(res, collection.GetSearchResult(doc1), "Wrong search result object");
			Assert.IsNull(collection.GetSearchResult(doc3), "GetSearchResult should return null");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void GetSearchResult_NullDocument() {
			SearchResultCollection collection = new SearchResultCollection();
			collection.GetSearchResult(null);
		}

		[Test]
		public void CopyTo() {
			SearchResultCollection collection = new SearchResultCollection();

			SearchResult res = new SearchResult(MockDocument("d", "d", "d", DateTime.Now));
			SearchResult res2 = new SearchResult(MockDocument("d2", "d", "d", DateTime.Now));

			collection.Add(res);
			collection.Add(res2);

			SearchResult[] results = new SearchResult[2];
			collection.CopyTo(results, 0);

			Assert.AreEqual(res, results[0], "Wrong result item");
			Assert.AreEqual(res2, results[1], "Wrong result item");

			results = new SearchResult[3];
			collection.CopyTo(results, 0);

			Assert.AreEqual(res, results[0], "Wrong result item");
			Assert.AreEqual(res2, results[1], "Wrong result item");
			Assert.IsNull(results[2], "Non-null item");

			results = new SearchResult[3];
			collection.CopyTo(results, 1);

			Assert.IsNull(results[0], "Non-null item");
			Assert.AreEqual(res, results[1], "Wrong result item");
			Assert.AreEqual(res2, results[2], "Wrong result item");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void CopyTo_NullArray() {
			SearchResultCollection collection = new SearchResultCollection();

			collection.CopyTo(null, 0);
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void CopyTo_InvalidIndex_Negative() {
			SearchResultCollection collection = new SearchResultCollection();

			SearchResult[] results = new SearchResult[10];

			collection.CopyTo(results, -1);
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void CopyTo_InvalidIndex_TooBig() {
			SearchResultCollection collection = new SearchResultCollection();

			SearchResult[] results = new SearchResult[10];

			collection.CopyTo(results, 10);
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void CopyTo_ArrayTooSmall() {
			SearchResultCollection collection = new SearchResultCollection();

			SearchResult res = new SearchResult(MockDocument("d", "d", "d", DateTime.Now));
			SearchResult res2 = new SearchResult(MockDocument("d2", "d", "d", DateTime.Now));

			collection.Add(res);
			collection.Add(res2);

			SearchResult[] results = new SearchResult[1];

			collection.CopyTo(results, 0);
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void CopyTo_NoSpaceAtIndex() {
			SearchResultCollection collection = new SearchResultCollection();

			SearchResult res = new SearchResult(MockDocument("d", "d", "d", DateTime.Now));
			SearchResult res2 = new SearchResult(MockDocument("d2", "d", "d", DateTime.Now));

			collection.Add(res);
			collection.Add(res2);

			SearchResult[] results = new SearchResult[2];

			collection.CopyTo(results, 1);
		}

		[Test]
		public void ReadOnly() {
			SearchResultCollection collection = new SearchResultCollection();
			Assert.IsFalse(collection.IsReadOnly);
		}

		[Test]
		public void Remove() {
			SearchResultCollection collection = new SearchResultCollection();

			SearchResult res = new SearchResult(MockDocument("d", "d", "d", DateTime.Now));
			SearchResult res2 = new SearchResult(MockDocument("d2", "d", "d", DateTime.Now));
			SearchResult res3 = new SearchResult(MockDocument("d3", "d", "d", DateTime.Now));

			collection.Add(res);
			collection.Add(res2);

			Assert.IsTrue(collection.Remove(res), "Remove should return true");
			Assert.IsFalse(collection.Remove(res3), "Remove should return false");
			Assert.AreEqual(1, collection.Count, "Wrong count");
			Assert.AreEqual(res2, collection[0], "Wrong item at index 0");

		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Remove_NullItem() {
			SearchResultCollection collection = new SearchResultCollection();
			collection.Remove(null);
		}

		[Test]
		public void GetEnumerator() {
			SearchResultCollection collection = new SearchResultCollection();

			SearchResult res = new SearchResult(MockDocument("d", "d", "d", DateTime.Now));
			SearchResult res2 = new SearchResult(MockDocument("d2", "d", "d", DateTime.Now));

			collection.Add(res);
			collection.Add(res2);

			int count = 0;
			foreach(SearchResult r in collection) {
				count++;
			}
			Assert.AreEqual(2, count, "Wrong count - enumerator does not work");
		}

		[Test]
		public void Indexer() {
			SearchResultCollection collection = new SearchResultCollection();

			SearchResult res = new SearchResult(MockDocument("d", "d", "d", DateTime.Now));
			SearchResult res2 = new SearchResult(MockDocument("d2", "d", "d", DateTime.Now));

			collection.Add(res);
			collection.Add(res2);

			Assert.AreEqual(res, collection[0], "Wrong item");
			Assert.AreEqual(res2, collection[1], "Wrong item");
		}

		[Test]
		[ExpectedException(typeof(IndexOutOfRangeException))]
		public void Indexer_InvalidIndex_Negative() {
			SearchResultCollection collection = new SearchResultCollection();
			SearchResult i = collection[-1];
		}

		[Test]
		[ExpectedException(typeof(IndexOutOfRangeException))]
		public void Indexer_InvalidIndex_TooBig() {
			SearchResultCollection collection = new SearchResultCollection();
			SearchResult i = collection[1];
		}

	}

}
