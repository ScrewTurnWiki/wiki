
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.SearchEngine {

	/// <summary>
	/// Implements a IDocument-SortedBasicWordInfo dictionary which treats IDocuments as value type.
	/// </summary>
	/// <remarks>All instance members are <b>not thread-safe</b>.</remarks>
	public class OccurrenceDictionary : IDictionary<IDocument, SortedBasicWordInfoSet> {

		private Dictionary<IDocument, SortedBasicWordInfoSet> dictionary;
		private Dictionary<string, IDocument> mappings; // Used for performance purposes

		/// <summary>
		/// Initializes a new instance of the <see cref="OccurrenceDictionary" /> class.
		/// </summary>
		/// <param name="capacity">The initial capacity of the dictionary.</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="capacity"/> is less than or equal to zero.</exception>
		public OccurrenceDictionary(int capacity) {
			if(capacity < 0) throw new ArgumentOutOfRangeException("capacity", "Capacity must be greater than zero");

			mappings = new Dictionary<string, IDocument>(capacity);
			dictionary = new Dictionary<IDocument, SortedBasicWordInfoSet>(capacity);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OccurrenceDictionary" /> class.
		/// </summary>
		public OccurrenceDictionary()
			: this(10) { }

		/// <summary>
		/// Gets the number of elements in the dictionary.
		/// </summary>
		public int Count {
			get { return dictionary.Count; }
		}

		/// <summary>
		/// Adds the specified key and value to the dictionary.
		/// </summary>
		/// <param name="key">The key of the element to add.</param>
		/// <param name="value">The value of the element to add.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="key"/> or <paramref name="value"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="key"/> is already present in the dictionary.</exception>
		public void Add(IDocument key, SortedBasicWordInfoSet value) {
			if(key == null) throw new ArgumentNullException("key");
			if(value == null) throw new ArgumentNullException("value");

			if(ContainsKey(key)) throw new ArgumentException("The specified key is already contained in the dictionary", "key");

			mappings.Add(key.Name, key);
			dictionary.Add(key, value);
		}

		/// <summary>
		/// Adds the specified key-value pair to the dictionary.
		/// </summary>
		/// <param name="item">The key-value pair to add.</param>
		public void Add(KeyValuePair<IDocument, SortedBasicWordInfoSet> item) {
			Add(item.Key, item.Value);
		}

		/// <summary>
		/// Determines whether the dictionary contains the specified key.
		/// </summary>
		/// <param name="key">The key to locate.</param>
		/// <returns><c>true</c> if the dictionary contains the specified key, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="key"/> is <c>null</c>.</exception>
		public bool ContainsKey(IDocument key) {
			if(key == null) throw new ArgumentNullException("key");

			return FindKey(key) != null;
		}

		/// <summary>
		/// Gets the keys of the dictionary.
		/// </summary>
		public ICollection<IDocument> Keys {
			get { return dictionary.Keys; }
		}

		/// <summary>
		/// Removes an element from the dictionary.
		/// </summary>
		/// <param name="key">The key of the element to remove.</param>
		/// <returns><c>true</c> if the element is removed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="key"/> is <c>null</c>.</exception>
		public bool Remove(IDocument key) {
			if(key == null) throw new ArgumentNullException("key");

			try {
				// The removal is considered to be successful even if no word mappings are removed
				RemoveExtended(key, uint.MaxValue);
				return true;
			}
			catch(KeyNotFoundException) {
				return false;
			}
		}

		/// <summary>
		/// Removes an element from the dictionary.
		/// </summary>
		/// <param name="item">The element to remove.</param>
		/// <returns><c>true</c> if the element is removed, <c>false</c> otherwise.</returns>
		public bool Remove(KeyValuePair<IDocument, SortedBasicWordInfoSet> item) {
			return Remove(item.Key);
		}

		/// <summary>
		/// Removes an element from the dictionary.
		/// </summary>
		/// <param name="key">The key of the element to remove.</param>
		/// <param name="wordId">The unique ID of the word that is being removed.</param>
		/// <returns>The list of removed words mappings, in dumpable format.</returns>
		/// <remarks>If <b>key</b> is not found, a <see cref="KeyNotFoundException" /> is thrown.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="key"/> is <c>null</c>.</exception>
		/// <exception cref="KeyNotFoundException">If <paramref name="key"/> is not present in the dictionary.</exception>
		public List<DumpedWordMapping> RemoveExtended(IDocument key, uint wordId) {
			if(key == null) throw new ArgumentNullException("key");

			IDocument target = FindKey(key);
			if(target == null) throw new KeyNotFoundException("Specified IDocument was not found");
			else {
				IDocument mappedDoc = mappings[key.Name];
				if(mappings.Remove(target.Name)) {
					// Prepare the list of DumpedWordMapping objects
					SortedBasicWordInfoSet set = dictionary[mappedDoc];
					List<DumpedWordMapping> dump = new List<DumpedWordMapping>(set.Count);
					foreach(BasicWordInfo w in set) {
						dump.Add(new DumpedWordMapping(wordId, key.ID, w));
					}

					if(!dictionary.Remove(target)) throw new InvalidOperationException("Internal data is broken");

					return dump;
				}
				else throw new InvalidOperationException("Internal data is broken");
			}
		}

		/// <summary>
		/// Finds an actual key in the mappings dictionary.
		/// </summary>
		/// <param name="key">The document.</param>
		/// <returns>The key, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="key"/> is <c>null</c>.</exception>
		private IDocument FindKey(IDocument key) {
			if(key == null) throw new ArgumentNullException("key");

			IDocument target = null;
			if(mappings.TryGetValue(key.Name, out target)) return target;
			else return null;
		}

		/// <summary>
		/// Tries to retrieve a value from the dictionary.
		/// </summary>
		/// <param name="key">The key of the value to retrieve.</param>
		/// <param name="value">The resulting value, or <c>null</c>.</param>
		/// <returns><c>true</c> if the value is retrieved, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="key"/> is <c>null</c>.</exception>
		public bool TryGetValue(IDocument key, out SortedBasicWordInfoSet value) {
			if(key == null) throw new ArgumentNullException("key");

			IDocument target = FindKey(key);
			if(target == null) {
				value = null;
				return false;
			}
			else {
				value = dictionary[target];
				return true;
			}
		}

		/// <summary>
		/// Gets the values of the dictionary.
		/// </summary>
		public ICollection<SortedBasicWordInfoSet> Values {
			get { return dictionary.Values; }
		}

		/// <summary>
		/// Gets or sets a value in the dictionary.
		/// </summary>
		/// <param name="key">The key of the value to ger or set.</param>
		/// <returns>The value.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="key"/> is <c>null</c>.</exception>
		/// <exception cref="IndexOutOfRangeException">If the key is not found.</exception>
		public SortedBasicWordInfoSet this[IDocument key] {
			get {
				if(key == null) throw new ArgumentNullException("key");

				IDocument target = FindKey(key);
				if(target == null) throw new IndexOutOfRangeException("The specified key was not found");
				else return dictionary[target];
			}
			set {
				if(key == null) throw new ArgumentNullException("key");
				if(value == null) throw new ArgumentNullException("value");

				IDocument target = FindKey(key);
				if(target == null) throw new IndexOutOfRangeException("The specified key was not found");
				else dictionary[target] = value;
			}
		}

		/// <summary>
		/// Clears the dictionary.
		/// </summary>
		public void Clear() {
			mappings.Clear();
			dictionary.Clear();
		}

		/// <summary>
		/// Determines whether the dictionary contains an element.
		/// </summary>
		/// <param name="item">The key-value pair representing the element.</param>
		/// <returns><c>true</c> if the dictionary contains the element, <c>false</c> otherwise.</returns>
		public bool Contains(KeyValuePair<IDocument, SortedBasicWordInfoSet> item) {
			return ContainsKey(item.Key);
		}

		/// <summary>
		/// Gets a value indicating whether the dictionary is read-only.
		/// </summary>
		public bool IsReadOnly {
			get { return false; }
		}

		/// <summary>
		/// Copies the content of the dictionary to a key-value pairs array.
		/// </summary>
		/// <param name="array">The output array.</param>
		/// <param name="arrayIndex">The index at which the copy begins.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="array"/> is <c>null</c>.</exception>
		public void CopyTo(KeyValuePair<IDocument, SortedBasicWordInfoSet>[] array, int arrayIndex) {
			if(array == null) throw new ArgumentNullException("array");

			int i = 0;
			foreach(KeyValuePair<IDocument, SortedBasicWordInfoSet> pair in dictionary) {
				array[arrayIndex + i] = pair;
				i++;
			}
		}

		/// <summary>
		/// Returns an enumerator that iterates through the elements in the dictionary.
		/// </summary>
		/// <returns>The iterator.</returns>
		public IEnumerator<KeyValuePair<IDocument, SortedBasicWordInfoSet>> GetEnumerator() {
			return dictionary.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through the elements in the dictionary.
		/// </summary>
		/// <returns>The iterator.</returns>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return dictionary.GetEnumerator();
		}

	}

}
