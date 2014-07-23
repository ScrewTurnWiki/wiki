
using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace ScrewTurn.Wiki.SearchEngine {
	
	/// <summary>
	/// Represents a word in a document.
	/// </summary>
	/// <remarks>All instance and static members are <b>thread-safe</b>.</remarks>
	public class Word {

		/// <summary>
		/// The word text, lowercase.
		/// </summary>
		protected string text;
		/// <summary>
		/// The occurrences.
		/// </summary>
		protected OccurrenceDictionary occurrences;
		/// <summary>
		/// The word unique ID.
		/// </summary>
		protected uint id;

		/// <summary>
		/// Initializes a new instance of the <see cref="Word" /> class.
		/// </summary>
		/// <param name="id">The word ID.</param>
		/// <param name="text">The text of the word (lowercase).</param>
		/// <exception cref="ArgumentNullException">If <paramref name="text"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="text"/> is empty.</exception>
		public Word(uint id, string text)
			: this(id, text, new OccurrenceDictionary(10)) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Word" /> class.
		/// </summary>
		/// <param name="id">The word ID.</param>
		/// <param name="text">The text of the word (lowercase).</param>
		/// <param name="occurrences">The occurrences.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="text"/> or <paramref name="occurrences"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="text"/> is empty.</exception>
		public Word(uint id, string text, OccurrenceDictionary occurrences) {
			if(text == null) throw new ArgumentNullException("text");
			if(text.Length == 0) throw new ArgumentException("Text must contain at least one character", "text");
			if(occurrences == null) throw new ArgumentNullException("occurrences");

			this.text = Tools.RemoveDiacriticsAndPunctuation(text, true);
			//if(this.text.Length == 0) throw new InvalidOperationException();
			this.id = id;
			this.occurrences = occurrences;
		}

		/// <summary>
		/// Gets or sets the unique ID of the word.
		/// </summary>
		public uint ID {
			get {
				lock(this) {
					return id;
				}
			}
			set {
				lock(this) {
					id = value;
				}
			}
		}

		/// <summary>
		/// Gets the text of the word (lowercase).
		/// </summary>
		public string Text {
			get {
				// Read-only: no need to lock
				return text;
			}
		}

		/// <summary>
		/// Gets the occurrences.
		/// </summary>
		public OccurrenceDictionary Occurrences {
			get {
				lock(occurrences) {
					return occurrences;
				}
			}
		}

		/// <summary>
		/// Gets the total occurrences.
		/// </summary>
		/// <remarks>Computing the result is <b>O(n)</b>, where <b>n</b> is the number of 
		/// documents the word occurs in at least one time.</remarks>
		public int TotalOccurrences {
			get {
				int count = 0;
				lock(occurrences) {
					foreach(KeyValuePair<IDocument, SortedBasicWordInfoSet> pair in occurrences) {
						count += pair.Value.Count;
					}
				}
				return count;
			}
		}

		/// <summary>
		/// Stores an occurrence.
		/// </summary>
		/// <param name="document">The document the occurrence is referred to.</param>
		/// <param name="firstCharIndex">The index of the first character of the word in the document.</param>
		/// <param name="wordIndex">The index of the word in the document.</param>
		/// <param name="location">The location of the word.</param>
		/// <remarks>Adding an occurrence is <b>O(n)</b>, where <b>n</b> is the number of occurrences 
		/// of the word already stored for the same document. If there were no occurrences previously stored, 
		/// the operation is <b>O(1)</b>.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="document"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="firstCharIndex"/> or <paramref name="wordIndex"/> are less than zero.</exception>
		public void AddOccurrence(IDocument document, ushort firstCharIndex, ushort wordIndex, WordLocation location) {
			if(document == null) throw new ArgumentNullException("document");
			if(firstCharIndex < 0) throw new ArgumentOutOfRangeException("firstCharIndex", "Invalid first char index: must be greater than or equal to zero");
			if(wordIndex < 0) throw new ArgumentOutOfRangeException("wordIndex", "Invalid word index: must be greater than or equal to zero");

			lock(occurrences) {
				if(occurrences.ContainsKey(document)) {
					// Existing document
					occurrences[document].Add(new BasicWordInfo(firstCharIndex, wordIndex, location));
				}
				else {
					// New document
					SortedBasicWordInfoSet set = new SortedBasicWordInfoSet();
					set.Add(new BasicWordInfo(firstCharIndex, wordIndex, location));
					occurrences.Add(document, set);
				}
			}
		}

		/// <summary>
		/// Removes all the occurrences for a document.
		/// </summary>
		/// <param name="document">The document to remove the occurrences of.</param>
		/// <returns>The dumped word mappings for the removed word occurrences.</returns>
		/// <remarks>Removing the occurrences for the document is <b>O(1)</b>.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="document"/> is <c>null</c>.</exception>
		public List<DumpedWordMapping> RemoveOccurrences(IDocument document) {
			if(document == null) throw new ArgumentNullException("document");

			lock(occurrences) {
				if(occurrences.ContainsKey(document)) return occurrences.RemoveExtended(document, ID);
				else return new List<DumpedWordMapping>();
			}
		}

		/// <summary>
		/// Adds a bulk of occurrences of the word in a document, <b>removing all the old positions</b>, if any.
		/// </summary>
		/// <param name="document">The document.</param>
		/// <param name="positions">The positions.</param>
		/// <remarks>If <b>positions</b> is empty, the effect of the invocation of the method is equal to 
		/// that of <see cref="RemoveOccurrences" /> with the same <b>document</b>.
		/// Bulk-adding the occurrences is <b>O(1)</b>.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="document"/> or <paramref name="positions"/> are <c>null</c>.</exception>
		public void BulkAddOccurrences(IDocument document, SortedBasicWordInfoSet positions) {
			if(document == null) throw new ArgumentNullException("document");
			if(positions == null) throw new ArgumentNullException("positions");

			lock(occurrences) {
				if(occurrences.ContainsKey(document)) {
					if(positions.Count == 0) RemoveOccurrences(document);
					else occurrences[document] = positions;
				}
				else occurrences.Add(document, positions);
			}
		}

		/// <summary>
		/// Gets a string representation of the current instance.
		/// </summary>
		/// <returns>The string representation.</returns>
		public override string ToString() {
			return string.Format("{0} [x{1}]", text, TotalOccurrences);
		}

	}

}
