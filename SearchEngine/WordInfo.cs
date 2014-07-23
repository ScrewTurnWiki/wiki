
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.SearchEngine {

	/// <summary>
	/// Contains full information about a word in a document.
	/// </summary>
	public class WordInfo : BasicWordInfo, IEquatable<WordInfo>, IComparable<WordInfo> {

		/// <summary>
		/// The word text.
		/// </summary>
		protected string text;

		/// <summary>
		/// Initializes a new instance of the <see cref="WordInfo" /> class.
		/// </summary>
		/// <param name="text">The text of the word.</param>
		/// <param name="firstCharIndex">The index of the first character of the word in the document.</param>
		/// <param name="wordIndex">The index of the word in the document.</param>
		/// <param name="location">The location of the word in the document.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="text"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="text"/> is empty.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="firstCharIndex"/> or <paramref name="wordIndex"/> are less than zero.</exception>
		public WordInfo(string text, ushort firstCharIndex, ushort wordIndex, WordLocation location)
			: base(firstCharIndex, wordIndex, location) {
			if(text == null) throw new ArgumentNullException("text");
			if(text.Length == 0) throw new ArgumentException("Invalid text", "text");

			this.text = Tools.RemoveDiacriticsAndPunctuation(text, true);
			//if(this.text.Length == 0) throw new InvalidOperationException();
		}

		/// <summary>
		/// Gets the text of the word.
		/// </summary>
		public string Text {
			get { return text; }
		}

		/// <summary>
		/// Gets a string representation of the current instance.
		/// </summary>
		/// <returns>The string representation.</returns>
		public override string ToString() {
			return string.Format("{0}:{1}", text, firstCharIndex);
		}
		/// <summary>
		/// Determinates whether the current instance is equal to an instance of an object.
		/// </summary>
		/// <param name="other">The instance of the object.</param>
		/// <returns><c>true</c> if the instances are value-equal, <c>false</c> otherwise.</returns>
		public override bool Equals(object other) {
			if(other is WordInfo) return Equals((WordInfo)other);
			else return false;
		}

		/// <summary>
		/// Determinates whether the current instance is value-equal to another.
		/// </summary>
		/// <param name="other">The other instance.</param>
		/// <returns><c>true</c> if the instances are value-equal, <c>false</c> otherwise.</returns>
		public bool Equals(WordInfo other) {
			if(object.ReferenceEquals(other, null)) return false;

			return other.FirstCharIndex == firstCharIndex && other.WordIndex == wordIndex &&
				other.Location == location && other.text == text;
		}

		/// <summary>
		/// Applies the value-equality operator to two <see cref="T:WordInfo" /> objects.
		/// </summary>
		/// <param name="x">The first object.</param>
		/// <param name="y">The second object.</param>
		/// <returns><c>true</c> if the objects are value-equal, <c>false</c> otherwise.</returns>
		public static bool operator ==(WordInfo x, WordInfo y) {
			if(object.ReferenceEquals(x, null) && !object.ReferenceEquals(y, null) ||
				!object.ReferenceEquals(x, null) && object.ReferenceEquals(y, null)) return false;

			if(object.ReferenceEquals(x, null) && object.ReferenceEquals(y, null)) return true;

			return x.Equals(y);
		}

		/// <summary>
		/// Applies the value-equality operator to two <see cref="T:WordInfo" /> objects.
		/// </summary>
		/// <param name="x">The first object.</param>
		/// <param name="y">The second object.</param>
		/// <returns><c>true</c> if the objects are not value-equal, <c>false</c> otherwise.</returns>
		public static bool operator !=(WordInfo x, WordInfo y) {
			return !(x == y);
		}

		/// <summary>
		/// Gets the hash code of the current instance.
		/// </summary>
		/// <returns>The hash code.</returns>
		public override int GetHashCode() {
			return base.GetHashCode() ^ text.GetHashCode();
		}

		/// <summary>
		/// Compares the current instance with another instance.
		/// </summary>
		/// <param name="other">The other instance.</param>
		/// <returns>The comparison result.</returns>
		/// <remarks><b>The First Char Index does not partecipate to the comparison.</b></remarks>
		public int CompareTo(WordInfo other) {
			if(other == null) return 1;

			// text has a distance module of 1
			// location has a distance module of 2
			// wordIndex has a distance module of 3

			int res = location.CompareTo(other.Location) * 2;
			int res2 = wordIndex.CompareTo(other.WordIndex) * 3;
			return res + res2 + text.CompareTo(other.Text);
		}

	}

}
