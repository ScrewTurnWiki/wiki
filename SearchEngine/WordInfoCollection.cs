
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.SearchEngine {

	/// <summary>
	/// Contains a collection of <b>MatchInfo</b> objects, sorted by index.
	/// </summary>
	public class WordInfoCollection : ICollection<WordInfo> {

		/// <summary>
		/// The items of the collection.
		/// </summary>
		protected List<WordInfo> items;

		/// <summary>
		/// Initializes a new instance of the <see cref="WordInfoCollection" /> class.
		/// </summary>
		public WordInfoCollection()
			: this(10) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="WordInfoCollection" /> class.
		/// </summary>
		/// <param name="capacity">The initial capacity of the collection.</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="capacity"/> is less than or equal to zero.</exception>
		public WordInfoCollection(int capacity) {
			if(capacity <= 0) throw new ArgumentOutOfRangeException("capacity", "Invalid capacity");

			items = new List<WordInfo>(capacity);
		}

		/// <summary>
		/// Adds an item to the collection.
		/// </summary>
		/// <param name="item">The item to add.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="item"/> is <c>null</c>.</exception>
		public void Add(WordInfo item) {
			if(item == null) throw new ArgumentNullException("item");

			items.Add(item);

			// Sort
			items.Sort(delegate(WordInfo mi1, WordInfo mi2) { return mi1.FirstCharIndex.CompareTo(mi2.FirstCharIndex); });
		}

		/// <summary>
		/// Clears the collection.
		/// </summary>
		public void Clear() {
			items.Clear();
		}

		/// <summary>
		/// Determines whether an item is in the collection.
		/// </summary>
		/// <param name="item">The item to check for.</param>
		/// <returns><c>true</c> if the item in the collection, <c>false</c> otherwise.</returns>
		public bool Contains(WordInfo item) {
			if(item == null) return false;
			return items.Contains(item);
		}

		/// <summary>
		/// Determines whether a word is in the collection.
		/// </summary>
		/// <param name="word">The word.</param>
		/// <returns><c>true</c> if the word is in the collection, <c>false</c> otherwise.</returns>
		public bool Contains(string word) {
			if(string.IsNullOrEmpty(word)) return false;

			foreach(WordInfo w in items) {
				if(w.Text == word) return true;
			}

			return false;
		}

		/// <summary>
		/// Determines whether a word occurrence is in the collection.
		/// </summary>
		/// <param name="word">The word.</param>
		/// <param name="firstCharIndex">The index of the first character.</param>
		/// <returns><c>True</c> if the collection contains the occurrence, <c>false</c> otherwise.</returns>
		public bool ContainsOccurrence(string word, int firstCharIndex) {
			if(string.IsNullOrEmpty(word)) return false;
			if(firstCharIndex < 0) return false;

			foreach(WordInfo w in items) {
				if(w.Text == word && w.FirstCharIndex == firstCharIndex) return true;
			}

			return false;
		}

		/// <summary>
		/// Copies the collection items to an array.
		/// </summary>
		/// <param name="array">The destination array.</param>
		/// <param name="arrayIndex">The zero-based array index at which the copy begins.</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="arrayIndex"/> is not within the bounds of <paramref name="array"/>.</exception>
		public void CopyTo(WordInfo[] array, int arrayIndex) {
			if(array == null) throw new ArgumentNullException("array");
			if(arrayIndex < 0 || arrayIndex > array.Length - 1)
				throw new ArgumentOutOfRangeException("arrayIndex", "Index should be greater than or equal to zero and less than the number of items in the array");

			if(array.Length - arrayIndex < items.Count)
				throw new ArgumentOutOfRangeException("arrayIndex", "Not enough space for copying the items starting at the specified index");

			items.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Gets the number of items in the collection.
		/// </summary>
		public int Count {
			get { return items.Count; }
		}

		/// <summary>
		/// Gets the capacity of the collection.
		/// </summary>
		public int Capacity {
			get { return items.Capacity; }
		}

		/// <summary>
		/// Gets a value indicating whether the collection is read-only.
		/// </summary>
		public bool IsReadOnly {
			get { return false; }
		}

		/// <summary>
		/// Removes an item from the collection.
		/// </summary>
		/// <param name="item">The item to remove.</param>
		/// <returns><c>true</c> if <b>item</b> is removed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="item"/> is <c>null</c>.</exception>
		public bool Remove(WordInfo item) {
			if(item == null) throw new ArgumentNullException("item");

			return items.Remove(item);
		}

		/// <summary>
		/// Returns an enumerator that iterates through the items of the collection.
		/// </summary>
		/// <returns>The enumerator.</returns>
		public IEnumerator<WordInfo> GetEnumerator() {
			return items.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through the items of the collection.
		/// </summary>
		/// <returns>The enumerator.</returns>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return items.GetEnumerator();
		}

		/// <summary>
		/// Gets an item of the collection.
		/// </summary>
		/// <param name="index">The index of the item to retrieve.</param>
		/// <returns>The item.</returns>
		/// <exception cref="IndexOutOfRangeException">If <paramref name="index"/> is outside the bounds of the collection.</exception>
		public WordInfo this[int index] {
			get {
				if(index < 0 || index > items.Count - 1)
					throw new IndexOutOfRangeException("Index should be greater than or equal to zero and less than the number of items in the collection");

				return items[index];
			}
		}

		/// <summary>
		/// Returns a string representation of the current instance.
		/// </summary>
		/// <returns>The string representation.</returns>
		public override string ToString() {
			return items.Count.ToString() + " matches";
		}

	}

}
