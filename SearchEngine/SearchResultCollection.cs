
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.SearchEngine {

	/// <summary>
	/// Contains a collection of <b>SearchResults</b>, without duplicates.
	/// </summary>
	/// <remarks>Instance and static members are <b>not thread-safe</b>.</remarks>
	public class SearchResultCollection : ICollection<SearchResult> {

		/// <summary>
		/// Contains the collection items.
		/// </summary>
		protected List<SearchResult> items;

		/// <summary>
		/// Initializes a new instance of the <see cref="SearchResultCollection" /> Class.
		/// </summary>
		public SearchResultCollection()
			: this(10) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="SearchResultCollection" /> Class.
		/// </summary>
		/// <param name="capacity">The initial capacity of the collection.</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="capacity"/> is less than or equal to zero.</exception>
		public SearchResultCollection(int capacity) {
			if(capacity <= 0) throw new ArgumentOutOfRangeException("Invalid capacity", "capacity");

			items = new List<SearchResult>(capacity);
		}

		/// <summary>
		/// Adds an item to the collection.
		/// </summary>
		/// <param name="item">The item to add.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="item"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="item"/> is laredy present in the collection.</exception>
		public void Add(SearchResult item) {
			if(item == null) throw new ArgumentNullException("item");

			foreach(SearchResult r in items) {
				if(r.Document == item.Document)
					throw new ArgumentException("Item is already present in the collection", "item");
			}

			items.Add(item);
		}

		/// <summary>
		/// Clears the collection.
		/// </summary>
		public void Clear() {
			items.Clear();
		}

		/// <summary>
		/// Determines whether or not the collection contains an item.
		/// </summary>
		/// <param name="item">The item to check for.</param>
		/// <returns><c>true</c> if the collection contains <b>item</b>, <c>false</c> otherwise.</returns>
		public bool Contains(SearchResult item) {
			if(item == null) return false;
			else return items.Contains(item);
		}

		/// <summary>
		/// Retrieves the search result for a document (looking at document names).
		/// </summary>
		/// <param name="document">The document.</param>
		/// <returns>The <b>SearchResult</b> object, if any, <c>null</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="document"/> is <c>null</c>.</exception>
		public SearchResult GetSearchResult(IDocument document) {
			if(document == null) throw new ArgumentNullException("document");

			foreach(SearchResult r in items) {
				if(r.Document.Name == document.Name) return r;
			}
			return null;
		}

		/// <summary>
		/// Copies the collection to an array.
		/// </summary>
		/// <param name="array">The destination array.</param>
		/// <param name="arrayIndex">The zero-based array index at which the copy begins.</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="arrayIndex"/> is outside the bounds of <paramref name="array"/>.</exception>
		public void CopyTo(SearchResult[] array, int arrayIndex) {
			if(array == null) throw new ArgumentNullException("array");
			if(arrayIndex < 0 || arrayIndex > array.Length - 1)
				throw new ArgumentOutOfRangeException("arrayIndex", "Index should be greater than or equal to zero and less than the number of items in the array");

			if(array.Length - arrayIndex < items.Count)
				throw new ArgumentOutOfRangeException("arrayIndex", "Not enough space for copying the items starting at the specified index");

			items.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Gets the total number of items in the collection.
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
		/// <returns><c>true</c> if the item is removed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="item"/> is <c>null</c>.</exception>
		public bool Remove(SearchResult item) {
			if(item == null) throw new ArgumentNullException("item");

			return items.Remove(item);
		}

		/// <summary>
		/// Returns an enumerator that iterates through the items in the collection.
		/// </summary>
		/// <returns>The enumerator.</returns>
		public IEnumerator<SearchResult> GetEnumerator() {
			return items.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through the items in the collection.
		/// </summary>
		/// <returns>The enumerator.</returns>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return items.GetEnumerator();
		}

		/// <summary>
		/// Gets an item from the collection.
		/// </summary>
		/// <param name="index">The index of the item.</param>
		/// <returns>The item.</returns>
		/// <exception cref="IndexOutOfRangeException">If <paramref name="index"/> is outside the bounds of the collection.</exception>
		public SearchResult this[int index] {
			get {
				if(index < 0 || index > items.Count - 1) throw new IndexOutOfRangeException("Index should be greater than or equal to zero and less than the number of items in the collection");

				return items[index];
			}
		}

	}

}
