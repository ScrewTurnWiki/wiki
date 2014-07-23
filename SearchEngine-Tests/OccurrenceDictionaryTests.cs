
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace ScrewTurn.Wiki.SearchEngine.Tests {

	[TestFixture]
	public class OccurrenceDictionaryTests : TestsBase {

		[Test]
		public void Constructor_NoCapacity() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			Assert.AreEqual(0, dic.Count, "Wrong count (Dictionary should be empty)");
		}

		[Test]
		public void Constructor_WithCapacity() {
			OccurrenceDictionary dic = new OccurrenceDictionary(10);
			Assert.AreEqual(0, dic.Count, "Wrong count (Dictionary should be empty)");
		}

		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void Constructor_InvalidCapacity() {
			OccurrenceDictionary dic = new OccurrenceDictionary(-1);
		}

		[Test]
		public void Add_KV() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			dic.Add(MockDocument("Doc1", "Doc 1", "d", DateTime.Now), new SortedBasicWordInfoSet());
			Assert.AreEqual(1, dic.Count, "Wrong count");
			dic.Add(MockDocument("Doc2", "Doc 2", "d", DateTime.Now), new SortedBasicWordInfoSet());
			Assert.AreEqual(2, dic.Count, "Wrong count");
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void Add_KV_ExistingKey() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			dic.Add(MockDocument("Doc1", "Doc 1", "d", DateTime.Now), new SortedBasicWordInfoSet());
			dic.Add(MockDocument("Doc1", "Doc 2", "d2", DateTime.Now.AddHours(1)), new SortedBasicWordInfoSet());
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Add_KV_NullKey() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			dic.Add(null, new SortedBasicWordInfoSet());
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Add_KV_NullValue() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			dic.Add(MockDocument("Doc1", "Doc 1", "d", DateTime.Now), null);
		}

		[Test]
		public void Add_Pair() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			dic.Add(new KeyValuePair<IDocument, SortedBasicWordInfoSet>(
				MockDocument("Doc1", "Doc 1", "d", DateTime.Now), new SortedBasicWordInfoSet()));
			Assert.AreEqual(1, dic.Count, "Wrong count");
			dic.Add(MockDocument("Doc2", "Doc 2", "d", DateTime.Now), new SortedBasicWordInfoSet());
			Assert.AreEqual(2, dic.Count, "Wrong count");
		}

		[Test]
		public void ContainsKey() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			IDocument doc = MockDocument("Doc", "Doc", "d", DateTime.Now);
			Assert.IsFalse(dic.ContainsKey(doc), "ContainsKey should return false");
			dic.Add(doc, new SortedBasicWordInfoSet());
			Assert.IsTrue(dic.ContainsKey(doc), "ContainsKey should return true");
			Assert.IsFalse(dic.ContainsKey(MockDocument("Doc2", "Doc 2", "d", DateTime.Now)), "ContainsKey should return false");

			IDocument doc2 = MockDocument("Doc", "Doc", "d", DateTime.Now);
			Assert.IsTrue(dic.ContainsKey(doc2), "ContainsKey should return true");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ContainsKey_NullKey() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			dic.ContainsKey(null);
		}

		[Test]
		public void Keys() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			IDocument doc1 = MockDocument("Doc1", "Doc1", "d", DateTime.Now);
			IDocument doc2 = MockDocument("Doc2", "Doc2", "d", DateTime.Now);
			dic.Add(doc1, new SortedBasicWordInfoSet());
			dic.Add(doc2, new SortedBasicWordInfoSet());

			Assert.AreEqual(2, dic.Keys.Count, "Wrong key count");

			bool doc1Found = false, doc2Found = false;
			foreach(IDocument d in dic.Keys) {
				if(d.Name == "Doc1") doc1Found = true;
				if(d.Name == "Doc2") doc2Found = true;
			}

			Assert.IsTrue(doc1Found, "Doc1 not found");
			Assert.IsTrue(doc2Found, "Doc2 not found");
		}

		[Test]
		public void Remove_KV() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			SortedBasicWordInfoSet set = new SortedBasicWordInfoSet();
			set.Add(new BasicWordInfo(5, 0, WordLocation.Content));
			dic.Add(MockDocument("Doc1", "Doc1", "d", DateTime.Now), set);
			dic.Add(MockDocument("Doc2", "Doc2", "d", DateTime.Now), new SortedBasicWordInfoSet());
			Assert.AreEqual(2, dic.Count, "Wrong initial count");
			Assert.IsFalse(dic.Remove(MockDocument("Doc3", "Doc3", "d", DateTime.Now)), "Remove should return false");
			Assert.IsTrue(dic.Remove(MockDocument("Doc1", "Doc1", "d", DateTime.Now)), "Remove should return true");
			Assert.AreEqual(1, dic.Count, "Wrong count");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Remove_KV_NullKey() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			dic.Remove(null as IDocument);
		}

		[Test]
		public void Remove_Pair() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			dic.Add(MockDocument("Doc1", "Doc1", "d", DateTime.Now), new SortedBasicWordInfoSet());
			dic.Add(MockDocument("Doc2", "Doc2", "d", DateTime.Now), new SortedBasicWordInfoSet());
			Assert.AreEqual(2, dic.Count, "Wrong initial count");
			Assert.IsFalse(dic.Remove(
				new KeyValuePair<IDocument, SortedBasicWordInfoSet>(MockDocument("Doc3", "Doc3", "d", DateTime.Now), new SortedBasicWordInfoSet())),
				"Remove should return false");
			Assert.IsTrue(dic.Remove(
				new KeyValuePair<IDocument, SortedBasicWordInfoSet>(MockDocument("Doc2", "Doc2", "d", DateTime.Now), new SortedBasicWordInfoSet())),
				"Remove should return true");
			Assert.AreEqual(1, dic.Count, "Wrong count");
		}

		[Test]
		public void RemoveExtended() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			SortedBasicWordInfoSet set1 = new SortedBasicWordInfoSet();
			set1.Add(new BasicWordInfo(5, 0, WordLocation.Content));
			set1.Add(new BasicWordInfo(12, 1, WordLocation.Keywords));
			SortedBasicWordInfoSet set2 = new SortedBasicWordInfoSet();
			set2.Add(new BasicWordInfo(1, 0, WordLocation.Content));
			set2.Add(new BasicWordInfo(4, 1, WordLocation.Title));
			dic.Add(MockDocument("Doc1", "Doc", "doc", DateTime.Now), set1);
			dic.Add(MockDocument("Doc2", "Doc", "doc", DateTime.Now), set2);

			List<DumpedWordMapping> dm = dic.RemoveExtended(MockDocument("Doc1", "Doc", "doc", DateTime.Now), 1);
			Assert.AreEqual(2, dm.Count, "Wrong count");

			Assert.IsTrue(dm.Find(delegate(DumpedWordMapping m) {
				return m.WordID == 1 && m.DocumentID == 1 &&
					m.FirstCharIndex == 5 && m.WordIndex == 0 &&
					m.Location == WordLocation.Content.Location;
			}) != null, "Mapping not found");

			Assert.IsTrue(dm.Find(delegate(DumpedWordMapping m) {
				return m.WordID == 1 && m.DocumentID == 1 &&
					m.FirstCharIndex == 12 && m.WordIndex == 1 &&
					m.Location == WordLocation.Keywords.Location;
			}) != null, "Mapping not found");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemoveExtended_NullDocument() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			dic.RemoveExtended(null, 1);
		}

		[Test]
		public void TryGetValue() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			IDocument doc1 = MockDocument("Doc1", "Doc1", "d", DateTime.Now);
			IDocument doc2 = MockDocument("Doc2", "Doc2", "d", DateTime.Now);

			SortedBasicWordInfoSet set = null;

			Assert.IsFalse(dic.TryGetValue(doc1, out set), "TryGetValue should return false");
			Assert.IsNull(set, "Set should be null");

			dic.Add(doc1, new SortedBasicWordInfoSet());
			Assert.IsTrue(dic.TryGetValue(MockDocument("Doc1", "Doc1", "d", DateTime.Now), out set), "TryGetValue should return true");
			Assert.IsNotNull(set, "Set should not be null");

			Assert.IsFalse(dic.TryGetValue(doc2, out set), "TryGetValue should return false");
			Assert.IsNull(set, "Set should have been set to null");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TryGetValue_NullKey() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			SortedBasicWordInfoSet set = null;
			dic.TryGetValue(null, out set);
		}

		[Test]
		public void Values() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			IDocument doc1 = MockDocument("Doc1", "Doc1", "d", DateTime.Now);
			IDocument doc2 = MockDocument("Doc2", "Doc2", "d", DateTime.Now);
			SortedBasicWordInfoSet set1 = new SortedBasicWordInfoSet();
			set1.Add(new BasicWordInfo(0, 0, WordLocation.Content));
			SortedBasicWordInfoSet set2 = new SortedBasicWordInfoSet();
			set2.Add(new BasicWordInfo(1, 1, WordLocation.Title));
			dic.Add(doc1, set1);
			dic.Add(doc2, set2);

			Assert.AreEqual(2, dic.Values.Count, "Wrong value count");

			bool set1Found = false, set2Found = false;
			foreach(SortedBasicWordInfoSet set in dic.Values) {
				if(set[0].FirstCharIndex == 0) set1Found = true;
				if(set[0].FirstCharIndex == 1) set2Found = true;
			}

			Assert.IsTrue(set1Found, "Set1 not found");
			Assert.IsTrue(set2Found, "Set2 not found");
		}

		[Test]
		public void Indexer_Get() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			IDocument doc1 = MockDocument("Doc1", "Doc1", "d", DateTime.Now);
			SortedBasicWordInfoSet set1 = new SortedBasicWordInfoSet();
			set1.Add(new BasicWordInfo(1, 1, WordLocation.Content));

			dic.Add(doc1, set1);

			SortedBasicWordInfoSet output = dic[MockDocument("Doc1", "Doc1", "d", DateTime.Now)];
			Assert.IsNotNull(output, "Output should not be null");
			Assert.AreEqual(1, set1.Count, "Wrong count");
			Assert.AreEqual(1, set1[0].FirstCharIndex, "Wrong first char index");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Indexer_Get_NullIndex() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			SortedBasicWordInfoSet set = dic[null];
		}

		[Test]
		[ExpectedException(typeof(IndexOutOfRangeException))]
		public void Indexer_Get_InexistentIndex() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			SortedBasicWordInfoSet set = dic[MockDocument("Doc", "Doc", "d", DateTime.Now)];
		}

		[Test]
		public void Indexer_Set() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			dic.Add(MockDocument("Doc1", "Doc1", "d", DateTime.Now), new SortedBasicWordInfoSet());

			SortedBasicWordInfoSet set1 = new SortedBasicWordInfoSet();
			set1.Add(new BasicWordInfo(1, 1, WordLocation.Content));

			dic[MockDocument("Doc1", "Doc1", "d", DateTime.Now)] = set1;

			SortedBasicWordInfoSet output = dic[MockDocument("Doc1", "Doc1", "d", DateTime.Now)];
			Assert.AreEqual(1, output.Count, "Wrong count");
			Assert.AreEqual(1, output[0].FirstCharIndex, "Wrong first char index");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Indexer_Set_NullIndex() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			dic[null] = new SortedBasicWordInfoSet();
		}

		[Test]
		[ExpectedException(typeof(IndexOutOfRangeException))]
		public void Indexer_Set_InexistentIndex() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			dic[MockDocument("Doc", "Doc", "d", DateTime.Now)] = new SortedBasicWordInfoSet();
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Indexer_Set_NullValue() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			dic.Add(MockDocument("Doc1", "Doc1", "d", DateTime.Now), new SortedBasicWordInfoSet());
			dic[MockDocument("Doc1", "Doc1", "d", DateTime.Now)] = null;
		}

		[Test]
		public void Clear() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			dic.Add(MockDocument("Doc1", "Doc1", "d", DateTime.Now), new SortedBasicWordInfoSet());
			dic.Clear();
			Assert.AreEqual(0, dic.Count, "Wrong count");
		}

		[Test]
		public void Contains() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			IDocument doc = MockDocument("Doc", "Doc", "d", DateTime.Now);
			Assert.IsFalse(dic.Contains(new KeyValuePair<IDocument, SortedBasicWordInfoSet>(doc, new SortedBasicWordInfoSet())), "Contains should return false");
			dic.Add(doc, new SortedBasicWordInfoSet());
			Assert.IsTrue(dic.Contains(new KeyValuePair<IDocument, SortedBasicWordInfoSet>(doc, new SortedBasicWordInfoSet())), "Contains should return true");
			Assert.IsFalse(dic.Contains(new KeyValuePair<IDocument, SortedBasicWordInfoSet>(MockDocument("Doc2", "Doc 2", "d", DateTime.Now), new SortedBasicWordInfoSet())), "Contains should return false");

			IDocument doc2 = MockDocument("Doc", "Doc", "d", DateTime.Now);
			Assert.IsTrue(dic.Contains(new KeyValuePair<IDocument, SortedBasicWordInfoSet>(doc, new SortedBasicWordInfoSet())), "Contains should return true");
		}

		[Test]
		public void IsReadOnly() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			Assert.IsFalse(dic.IsReadOnly, "IsReadOnly should always return false");
		}

		[Test]
		public void CopyTo() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			SortedBasicWordInfoSet set = new SortedBasicWordInfoSet();
			set.Add(new BasicWordInfo(1, 1, WordLocation.Title));
			dic.Add(MockDocument("Doc", "Doc", "d", DateTime.Now), set);
			KeyValuePair<IDocument, SortedBasicWordInfoSet>[] array = new KeyValuePair<IDocument, SortedBasicWordInfoSet>[1];
			dic.CopyTo(array, 0);

			Assert.IsNotNull(array[0], "Array[0] should not be null");
			Assert.AreEqual("Doc", array[0].Key.Name, "Wrong array item");
			Assert.AreEqual(1, array[0].Value.Count, "Wrong count");
			Assert.AreEqual(1, array[0].Value[0].FirstCharIndex, "Wrong first char index");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void CopyTo_NullArray() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			dic.CopyTo(null, 0);
		}

		[Test]
		[ExpectedException(typeof(IndexOutOfRangeException))]
		public void CopyTo_ShortArray() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			dic.Add(MockDocument("Doc", "Doc", "d", DateTime.Now), new SortedBasicWordInfoSet());
			KeyValuePair<IDocument, SortedBasicWordInfoSet>[] array = new KeyValuePair<IDocument,SortedBasicWordInfoSet>[0];
			dic.CopyTo(array, 0);
		}

		[Test]
		[ExpectedException(typeof(IndexOutOfRangeException))]
		public void CopyTo_InvalidIndex_Negative() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			dic.Add(MockDocument("Doc", "Doc", "d", DateTime.Now), new SortedBasicWordInfoSet());
			KeyValuePair<IDocument, SortedBasicWordInfoSet>[] array = new KeyValuePair<IDocument, SortedBasicWordInfoSet>[1];
			dic.CopyTo(array, -1);
		}

		[Test]
		[ExpectedException(typeof(IndexOutOfRangeException))]
		public void CopyTo_InvalidIndex_TooBig() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			dic.Add(MockDocument("Doc", "Doc", "d", DateTime.Now), new SortedBasicWordInfoSet());
			KeyValuePair<IDocument, SortedBasicWordInfoSet>[] array = new KeyValuePair<IDocument, SortedBasicWordInfoSet>[1];
			dic.CopyTo(array, 1);
		}

		[Test]
		public void GetEnumerator() {
			OccurrenceDictionary dic = new OccurrenceDictionary();
			IDocument doc1 = MockDocument("Doc1", "Doc1", "d", DateTime.Now);
			IDocument doc2 = MockDocument("Doc2", "Doc2", "d", DateTime.Now);
			dic.Add(doc1, new SortedBasicWordInfoSet());
			dic.Add(doc2, new SortedBasicWordInfoSet());

			Assert.IsNotNull(dic.GetEnumerator(), "GetEnumerator should not return null");

			int count = 0;
			foreach(KeyValuePair<IDocument, SortedBasicWordInfoSet> pair in dic) {
				count++;
			}

			Assert.AreEqual(2, count, "Wrong count");
		}

	}

}
