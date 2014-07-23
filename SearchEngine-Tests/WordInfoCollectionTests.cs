
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace ScrewTurn.Wiki.SearchEngine.Tests {
	
	[TestFixture]
	public class WordInfoCollectionTests {

		[Test]
		public void Constructor_NoCapacity() {
			WordInfoCollection collection = new WordInfoCollection();
			Assert.AreEqual(0, collection.Count, "Wrong count (collection should be empty)");
		}

		[Test]
		public void Constructor_WithCapacity() {
			WordInfoCollection collection = new WordInfoCollection(15);
			Assert.AreEqual(0, collection.Count, "Wrong count (collection should be empty)");
			Assert.AreEqual(15, collection.Capacity, "Wrong capacity");
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void Constructor_InvalidCapacity() {
			WordInfoCollection collection = new WordInfoCollection(0);
		}

		[Test]
		public void AddAndCount() {
			WordInfoCollection collection = new WordInfoCollection();

			WordInfo mi1 = new WordInfo("continuous", 0, 0, WordLocation.Content);
			WordInfo mi2 = new WordInfo("taskbar", 21, 1, WordLocation.Content);

			Assert.AreEqual(0, collection.Count, "Wrong count (collection should be empty)");
			collection.Add(mi2);
			collection.Add(mi1);
			Assert.AreEqual(2, collection.Count, "Wrong count (collection should contain 2 items)");
			Assert.AreEqual(mi1, collection[0], "Wrong item at index 0");
			Assert.AreEqual(mi2, collection[1], "Wrong item at index 1");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Add_NullItem() {
			WordInfoCollection collection = new WordInfoCollection();
			collection.Add(null);
		}

		[Test]
		public void Clear() {
			WordInfoCollection collection = new WordInfoCollection();

			WordInfo mi1 = new WordInfo("continuous", 0, 0, WordLocation.Content);
			WordInfo mi2 = new WordInfo("taskbar", 21, 1, WordLocation.Content);

			collection.Add(mi1);
			collection.Add(mi2);
			Assert.AreEqual(2, collection.Count, "Wrong count (collection should contain 2 items)");

			collection.Clear();
			Assert.AreEqual(0, collection.Count, "Wrong count (collection should be empty)");
		}

		[Test]
		public void Contains_WordInfo() {
			WordInfoCollection collection = new WordInfoCollection();

			WordInfo mi1 = new WordInfo("continuous", 0, 0, WordLocation.Content);
			WordInfo mi2 = new WordInfo("taskbar", 21, 0, WordLocation.Content);

			collection.Add(mi1);

			Assert.IsTrue(collection.Contains(mi1), "Collection should contain item");
			Assert.IsFalse(collection.Contains(mi2), "Collection should not contain item");

			Assert.IsFalse(collection.Contains(null as WordInfo), "Contains should return false");
		}

		[Test]
		public void Contains_String() {
			WordInfoCollection collection = new WordInfoCollection();

			WordInfo mi1 = new WordInfo("continuous", 0, 0, WordLocation.Content);

			collection.Add(mi1);

			Assert.IsTrue(collection.Contains("continuous"), "Collection should contain string");
			Assert.IsFalse(collection.Contains("taskbar"), "Collection should not contain string");

			Assert.IsFalse(collection.Contains(null as string), "Contains should return false");
			Assert.IsFalse(collection.Contains(""), "Contains should return false");
		}

		[Test]
		public void ContainsOccurrence() {
			WordInfoCollection collection = new WordInfoCollection();

			WordInfo mi1 = new WordInfo("continuous", 7, 0, WordLocation.Content);

			collection.Add(mi1);

			Assert.IsTrue(collection.ContainsOccurrence("continuous", 7), "Collection should contain occurrence");
			Assert.IsFalse(collection.ContainsOccurrence("continuous2", 7), "Collection should not contain occurrence");
			Assert.IsFalse(collection.ContainsOccurrence("continuous", 6), "Collection should not contain occurrence");
			Assert.IsFalse(collection.ContainsOccurrence("continuous", 8), "Collection should not contain occurrence");
			Assert.IsFalse(collection.ContainsOccurrence("continuous2", 6), "Collection should not contain occurrence");

			Assert.IsFalse(collection.ContainsOccurrence("continuous2", -1), "Contains should return false");
			Assert.IsFalse(collection.ContainsOccurrence("", 7), "Contains should return false");
			Assert.IsFalse(collection.ContainsOccurrence(null, 7), "Contains should return false");
		}

		[Test]
		public void CopyTo() {
			WordInfoCollection collection = new WordInfoCollection();

			WordInfo mi1 = new WordInfo("continuous", 0, 0, WordLocation.Content);
			WordInfo mi2 = new WordInfo("goose", 34, 0, WordLocation.Content);

			collection.Add(mi1);
			collection.Add(mi2);

			WordInfo[] matches = new WordInfo[2];
			collection.CopyTo(matches, 0);

			Assert.AreEqual(mi1, matches[0], "Wrong match item");
			Assert.AreEqual(mi2, matches[1], "Wrong match item");

			matches = new WordInfo[3];
			collection.CopyTo(matches, 0);

			Assert.AreEqual(mi1, matches[0], "Wrong match item");
			Assert.AreEqual(mi2, matches[1], "Wrong match item");
			Assert.IsNull(matches[2], "Non-null item");

			matches = new WordInfo[3];
			collection.CopyTo(matches, 1);

			Assert.IsNull(matches[0], "Non-null item");
			Assert.AreEqual(mi1, matches[1], "Wrong match item");
			Assert.AreEqual(mi2, matches[2], "Wrong match item");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void CopyTo_NullArray() {
			WordInfoCollection collection = new WordInfoCollection();

			collection.CopyTo(null, 0);
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void CopyTo_InvalidIndex_Negative() {
			WordInfoCollection collection = new WordInfoCollection();

			WordInfo[] results = new WordInfo[10];

			collection.CopyTo(results, -1);
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void CopyTo_InvalidIndex_TooBig() {
			WordInfoCollection collection = new WordInfoCollection();

			WordInfo[] results = new WordInfo[10];

			collection.CopyTo(results, 10);
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void CopyTo_ArrayTooSmall() {
			WordInfoCollection collection = new WordInfoCollection();

			WordInfo mi1 = new WordInfo("home", 0, 0, WordLocation.Content);
			WordInfo mi2 = new WordInfo("taskbar", 100, 0, WordLocation.Content);

			collection.Add(mi1);
			collection.Add(mi2);

			WordInfo[] matches = new WordInfo[1];

			collection.CopyTo(matches, 0);
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void CopyTo_NoSpaceAtIndex() {
			WordInfoCollection collection = new WordInfoCollection();

			WordInfo mi1 = new WordInfo("home", 0, 0, WordLocation.Content);
			WordInfo mi2 = new WordInfo("taskbar", 100, 0, WordLocation.Content);

			collection.Add(mi1);
			collection.Add(mi2);

			WordInfo[] matches = new WordInfo[2];

			collection.CopyTo(matches, 1);
		}

		[Test]
		public void ReadOnly() {
			WordInfoCollection collection = new WordInfoCollection();
			Assert.IsFalse(collection.IsReadOnly, "Collection should not be read-only");
		}

		[Test]
		public void Remove() {
			WordInfoCollection collection = new WordInfoCollection();

			WordInfo mi1 = new WordInfo("goose", 1, 0, WordLocation.Content);
			WordInfo mi2 = new WordInfo("hello", 12, 0, WordLocation.Content);

			collection.Add(mi1);
			Assert.IsTrue(collection.Contains(mi1), "Collection should contain item");
			Assert.IsFalse(collection.Contains(mi2), "Collection should not contain item");

			Assert.IsFalse(collection.Contains(null as WordInfo), "Contains should return false");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Remove_NullItem() {
			WordInfoCollection collection = new WordInfoCollection();
			collection.Remove(null);
		}

		[Test]
		public void GetEnumerator() {
			WordInfoCollection collection = new WordInfoCollection();

			WordInfo mi1 = new WordInfo("goose", 1, 0, WordLocation.Content);
			WordInfo mi2 = new WordInfo("hello", 12, 0, WordLocation.Content);

			collection.Add(mi2);
			collection.Add(mi1);

			int count = 0;
			foreach(WordInfo r in collection) {
				if(count == 0) Assert.AreEqual(mi1, r, "Wrong item at position 0");
				if(count == 1) Assert.AreEqual(mi2, r, "Wrong item at position 1");
				count++;
			}
			Assert.AreEqual(2, count, "Wrong count - enumerator does not work");
		}

		[Test]
		public void Indexer() {
			WordInfoCollection collection = new WordInfoCollection();

			WordInfo mi1 = new WordInfo("taskbar", 1, 0, WordLocation.Content);
			WordInfo mi2 = new WordInfo("goose", 12, 0, WordLocation.Content);

			collection.Add(mi2);
			collection.Add(mi1);

			Assert.AreEqual(mi1, collection[0], "Wrong item at position 0");
			Assert.AreEqual(mi2, collection[1], "Wrong item at position 0");
		}

		[Test]
		[ExpectedException(typeof(IndexOutOfRangeException))]
		public void Indexer_InvalidIndex_Negative() {
			WordInfoCollection collection = new WordInfoCollection();
			WordInfo mi = collection[-1];
		}

		[Test]
		[ExpectedException(typeof(IndexOutOfRangeException))]
		public void Indexer_InvalidIndex_TooBig() {
			WordInfoCollection collection = new WordInfoCollection();
			WordInfo mi = collection[0];
		}

	}

}
