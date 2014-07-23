
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace ScrewTurn.Wiki.SearchEngine.Tests {
	
	[TestFixture]
	public class SortedBasicWordInfoSetTests : TestsBase {

		[Test]
		public void Constructor_Default() {
			SortedBasicWordInfoSet set = new SortedBasicWordInfoSet();
			Assert.AreEqual(0, set.Count, "Wrong count (set should be empty)");
		}

		[Test]
		public void Constructor_WithCapacity() {
			SortedBasicWordInfoSet set = new SortedBasicWordInfoSet(10);
			Assert.AreEqual(0, set.Count, "Wrong count (set should be empty)");
			Assert.AreEqual(10, set.Capacity, "Wrong capacity (capacity should be ensured)");
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void Constructor_InvalidCapacity() {
			SortedBasicWordInfoSet set = new SortedBasicWordInfoSet(0);
		}

		[Test]
		public void Add_NewItem() {
			SortedBasicWordInfoSet set = new SortedBasicWordInfoSet();
			Assert.IsTrue(set.Add(new BasicWordInfo(10, 1, WordLocation.Content)), "Add should return true (adding new item)");
			Assert.AreEqual(1, set.Count, "Wrong count (set should contain 1 item)");
		}

		[Test]
		public void Add_ExistingItem() {
			SortedBasicWordInfoSet set = new SortedBasicWordInfoSet();
			Assert.IsTrue(set.Add(new BasicWordInfo(2, 0, WordLocation.Content)), "Add should return true (adding new item)");
			Assert.AreEqual(1, set.Count, "Wrong count (set should contain 1 item)");
			Assert.IsFalse(set.Add(new BasicWordInfo(2, 0, WordLocation.Content)), "Add should return false (adding existing item)");
			Assert.AreEqual(1, set.Count, "Wrong count (set should contain 1 item)");
		}

		[Test]
		public void Contains() {
			SortedBasicWordInfoSet set = new SortedBasicWordInfoSet();
			Assert.IsFalse(set.Contains(new BasicWordInfo(1, 0, WordLocation.Content)), "Contains should return false (inexistent item)");
			Assert.IsTrue(set.Add(new BasicWordInfo(1, 0, WordLocation.Content)), "Add should return true (adding new item)");
			Assert.IsTrue(set.Contains(new BasicWordInfo(1, 0, WordLocation.Content)), "Contains should return true (item exists)");
			Assert.AreEqual(1, set.Count, "Wrong count (set should contain 1 item");
			Assert.IsFalse(set.Contains(new BasicWordInfo(10, 2, WordLocation.Content)), "Contains should return false (inexistent item)");
		}

		[Test]
		public void Remove() {
			SortedBasicWordInfoSet set = new SortedBasicWordInfoSet();
			Assert.IsFalse(set.Remove(new BasicWordInfo(1, 0, WordLocation.Content)), "Remove should return false (removing inexistent item");
			Assert.IsTrue(set.Add(new BasicWordInfo(1, 0, WordLocation.Content)), "Add should return true (adding new item)");
			Assert.AreEqual(1, set.Count, "Wrong count (set should contain 1 item)");
			Assert.IsTrue(set.Contains(new BasicWordInfo(1, 0, WordLocation.Content)), "Contains should return true (item exists)");
			Assert.IsTrue(set.Remove(new BasicWordInfo(1, 0, WordLocation.Content)), "Remove should return true (removing existing item)");
			Assert.IsFalse(set.Contains(new BasicWordInfo(1, 0, WordLocation.Content)), "Contains should return false (inexistent item)");
			Assert.AreEqual(0, set.Count, "Wrong count (set should be empty)");
		}

		[Test]
		public void Clear() {
			SortedBasicWordInfoSet set = new SortedBasicWordInfoSet();
			Assert.IsTrue(set.Add(new BasicWordInfo(10, 2, WordLocation.Content)), "Add should return true (adding new item)");
			Assert.IsTrue(set.Add(new BasicWordInfo(2, 1, WordLocation.Content)), "Add should return true (adding new item)");
			Assert.AreEqual(2, set.Count, "Wrong count (set should contain 2 items)");
			set.Clear();
			Assert.AreEqual(0, set.Count, "Wrong count (set should be empty)");
			Assert.IsFalse(set.Contains(new BasicWordInfo(10, 2, WordLocation.Content)), "Contains should return false (empty set)");
			Assert.IsFalse(set.Contains(new BasicWordInfo(2, 1, WordLocation.Content)), "Contains should return false (empty set)");
		}

		[Test]
		public void GetEnumerator() {
			SortedBasicWordInfoSet set = new SortedBasicWordInfoSet();
			Assert.IsTrue(set.Add(new BasicWordInfo(1, 0, WordLocation.Content)), "Add should return true (adding new item)");
			Assert.IsTrue(set.Add(new BasicWordInfo(3, 1, WordLocation.Content)), "Add should return true (adding new item)");
			Assert.AreEqual(2, set.Count);
			int count = 0;
			foreach(BasicWordInfo item in set) {
				if(count == 0) Assert.AreEqual(1, item.FirstCharIndex, "Wrong start index for current item");
				if(count == 0) Assert.AreEqual(0, item.WordIndex, "Wrong word index for current item");
				if(count == 1) Assert.AreEqual(3, item.FirstCharIndex, "Wrong start index for current item");
				if(count == 1) Assert.AreEqual(1, item.WordIndex, "Wrong word index for current item");
				count++;
			}
			Assert.AreEqual(2, count);
		}

		[Test]
		public void Indexer() {
			SortedBasicWordInfoSet set = new SortedBasicWordInfoSet();
			Assert.IsTrue(set.Add(new BasicWordInfo(1, 0, WordLocation.Content)), "Add should return true (adding new item)");
			Assert.IsTrue(set.Add(new BasicWordInfo(10, 1, WordLocation.Content)), "Add should return true (adding new item)");
			Assert.IsTrue(set.Add(new BasicWordInfo(3, 2, WordLocation.Content)), "Add should return true (adding new item)");
			Assert.AreEqual(3, set.Count);
			Assert.AreEqual(1, set[0].FirstCharIndex, "Wrong start index at index 0");
			Assert.AreEqual(0, set[0].WordIndex, "Wrong word index at index 0");
			Assert.AreEqual(10, set[1].FirstCharIndex, "Wrong start index at index 1");
			Assert.AreEqual(1, set[1].WordIndex, "Wrong word index at index 1");
			Assert.AreEqual(3, set[2].FirstCharIndex, "Wrong start index at index 2");
			Assert.AreEqual(2, set[2].WordIndex, "Wrong word index at index 2");
		}

		[Test]
		[ExpectedException(typeof(IndexOutOfRangeException))]
		public void Indexer_InvalidIndex_Negative() {
			SortedBasicWordInfoSet set = new SortedBasicWordInfoSet();
			BasicWordInfo i = set[-1];
		}

		[Test]
		[ExpectedException(typeof(IndexOutOfRangeException))]
		public void Indexer_InvalidIndex_TooBig() {
			SortedBasicWordInfoSet set = new SortedBasicWordInfoSet();
			BasicWordInfo i = set[1];
		}

	}

}
