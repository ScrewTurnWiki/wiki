
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.SearchEngine {

	/// <summary>
	/// Implements a sorted set of integers which can be accessed by index.
	/// </summary>
	/// <remarks>Instance members are <b>not thread-safe</b>.</remarks>
	public class SortedBasicWordInfoSet : IEnumerable<BasicWordInfo> {

		private List<BasicWordInfo> items;

		/// <summary>
		/// Initializes a new instance of the <see cref="SortedBasicWordInfoSet"/> class.
		/// </summary>
		public SortedBasicWordInfoSet()
			: this(10) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="SortedBasicWordInfoSet"/> class.
		/// </summary>
		/// <param name="capacity">The initial capacity.</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="capacity"/> is <c>null</c>.</exception>
		public SortedBasicWordInfoSet(int capacity) {
			if(capacity <= 0) throw new ArgumentOutOfRangeException("Invalid capacity", "capacity");

			items = new List<BasicWordInfo>(capacity);
		}

		/// <summary>
		/// Gets the count if the items in the set.
		/// </summary>
		public int Count {
			get { return items.Count; }
		}

		/// <summary>
		/// Gets the capacity of the set.
		/// </summary>
		public int Capacity {
			get { return items.Capacity; }
		}

		/// <summary>
		/// Adds a new item to the set.
		/// </summary>
		/// <param name="item">The item to add.</param>
		/// <returns><c>true</c> if the item is added, <c>false</c> otherwise.</returns>
		/// <remarks>Adding an item is <b>O(log n)</b> if the item is already in the set,
		/// <b>O(n)</b> otherwise, where <b>n</b> is the number of items in the set.</remarks>
		public bool Add(BasicWordInfo item) {
			int idx = items.BinarySearch(item);

			if(idx < 0) {
				// Item does not exist, insert it to the right position to avoid explicit sorting
				// Sort (quick sort) is O(nlogn) on average, O(n^2) worst, Insert is O(n)
				items.Insert(~idx, item);
				return true;
			}
			else return false;
		}

		/// <summary>
		/// Determines whether the set contains an item.
		/// </summary>
		/// <param name="item">The item to look for.</param>
		/// <returns><c>true</c> if the set contains the specified item, <c>false</c> otherwise.</returns>
		/// <remarks>The operation is <b>O(log n)</b>, where <b>n</b> is the number of items in the set.</remarks>
		public bool Contains(BasicWordInfo item) {
			if(items.Count == 0) return false;

			return items.BinarySearch(item) >= 0;
		}

		/// <summary>
		/// Removes an item from the set.
		/// </summary>
		/// <param name="item">The item to remove.</param>
		/// <returns><c>true</c> if the item is removed, <c>false</c> otherwise.</returns>
		/// <remarks>The operation is <b>O(n)</b>, where <b>n</b> is the number of items in the set.</remarks>
		public bool Remove(BasicWordInfo item) {
			if(items.Count == 0) return false;

			// Remove and RemoveAt are both O(n)
			return items.Remove(item);
		}

		/// <summary>
		/// Clears the set.
		/// </summary>
		/// <remarks>The operation is <b>O(1)</b>.</remarks>
		public void Clear() {
			items.Clear();
		}

		/// <summary>
		/// Returns an enumerator that iterates through the set.
		/// </summary>
		/// <returns>The enumerator</returns>
		IEnumerator<BasicWordInfo> IEnumerable<BasicWordInfo>.GetEnumerator() {
			return items.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through the set.
		/// </summary>
		/// <returns>The enumerator</returns>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return items.GetEnumerator();
		}

		/// <summary>
		/// Gets an item from the set at a specific index.
		/// </summary>
		/// <param name="index">The zero-based index of the element to get.</param>
		/// <returns>The item at the specified index.</returns>
		/// <exception cref="IndexOutOfRangeException">If <paramref name="index"/> is outside the bounds of the collection.</exception>
		public BasicWordInfo this[int index] {
			get {
				if(index < 0 || index > items.Count - 1) throw new IndexOutOfRangeException("Index should be greater than or equal to zero and less than the number of items in the set");
				return items[index];
			}
		}

		/// <summary>
		/// Gets a string representation of the current instance.
		/// </summary>
		/// <returns>The string representation.</returns>
		public override string ToString() {
			StringBuilder sb = new StringBuilder(50);
			for(int i = 0; i < items.Count; i++) {
				sb.AppendFormat("{0}", items[i]);
				if(i != items.Count - 1) sb.Append(", ");
			}
			return sb.ToString();
		}

	}

}
