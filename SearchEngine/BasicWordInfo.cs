
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.SearchEngine {

	/// <summary>
	/// Contains basic information about a word in a document.
	/// </summary>
	public class BasicWordInfo : IEquatable<BasicWordInfo>, IComparable<BasicWordInfo> {

		/// <summary>
		/// The index in the original document the word starts at.
		/// </summary>
		protected ushort firstCharIndex;
		/// <summary>
		/// The index of the word in the document.
		/// </summary>
		protected ushort wordIndex;
		/// <summary>
		/// The location of the word in the document.
		/// </summary>
		protected WordLocation location;

		/// <summary>
		/// Initializes a new instance of the <see cref="BasicWordInfo" /> class.
		/// </summary>
		/// <param name="firstCharIndex">The index of the first character of the word in the document.</param>
		/// <param name="wordIndex">The index of the word in the document.</param>
		/// <param name="location">The location of the word in the document.</param>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="firstCharIndex"/> or <paramref name="wordIndex"/> are less than zero.</exception>
		public BasicWordInfo(ushort firstCharIndex, ushort wordIndex, WordLocation location) {
			if(firstCharIndex < 0) throw new ArgumentOutOfRangeException("firstCharIndex", "Invalid first char index: must be greater than or equal to zero");
			if(wordIndex < 0) throw new ArgumentOutOfRangeException("wordIndex", "Invalid word index: must be greater than or equal to zero");

			this.firstCharIndex = firstCharIndex;
			this.wordIndex = wordIndex;
			this.location = location;
		}

		/// <summary>
		/// Gets the index of the first character of the word in the document.
		/// </summary>
		public ushort FirstCharIndex {
			get { return firstCharIndex; }
		}

		/// <summary>
		/// Gets the index of the word in the document.
		/// </summary>
		public ushort WordIndex {
			get { return wordIndex; }
		}

		/// <summary>
		/// Gets the location of the word in the document.
		/// </summary>
		public WordLocation Location {
			get { return location; }
		}

		/// <summary>
		/// Determinates whether the current instance is equal to an instance of an object.
		/// </summary>
		/// <param name="other">The instance of the object.</param>
		/// <returns><c>true</c> if the instances are value-equal, <c>false</c> otherwise.</returns>
		public override bool Equals(object other) {
			if(other is BasicWordInfo) return Equals((BasicWordInfo)other);
			else return false;
		}

		/// <summary>
		/// Determinates whether the current instance is value-equal to another.
		/// </summary>
		/// <param name="other">The other instance.</param>
		/// <returns><c>true</c> if the instances are value-equal, <c>false</c> otherwise.</returns>
		public bool Equals(BasicWordInfo other) {
			if(object.ReferenceEquals(other, null)) return false;

			return other.FirstCharIndex == firstCharIndex && other.WordIndex == wordIndex &&
				other.Location == location;
		}

		/// <summary>
		/// Applies the value-equality operator to two <see cref="T:BasicWordInfo" /> objects.
		/// </summary>
		/// <param name="x">The first object.</param>
		/// <param name="y">The second object.</param>
		/// <returns><c>true</c> if the objects are value-equal, <c>false</c> otherwise.</returns>
		public static bool operator ==(BasicWordInfo x, BasicWordInfo y) {
			if(object.ReferenceEquals(x, null) && !object.ReferenceEquals(y, null) ||
				!object.ReferenceEquals(x, null) && object.ReferenceEquals(y, null)) return false;
			
			if(object.ReferenceEquals(x, null) && object.ReferenceEquals(y, null)) return true;

			return x.Equals(y);
		}

		/// <summary>
		/// Applies the value-inequality operator to two <see cref="T:BasicWordInfo" /> objects.
		/// </summary>
		/// <param name="x">The first object.</param>
		/// <param name="y">The second object.</param>
		/// <returns><c>true</c> if the objects are not value-equal, <c>false</c> otherwise.</returns>
		public static bool operator !=(BasicWordInfo x, BasicWordInfo y) {
			return !(x == y);
		}

		/// <summary>
		/// Gets the hash code of the current instance.
		/// </summary>
		/// <returns>The hash code.</returns>
		public override int GetHashCode() {
			return location.GetHashCode() + firstCharIndex * 10 + wordIndex * 100000;
		}

		/// <summary>
		/// Compares the current instance with another instance.
		/// </summary>
		/// <param name="other">The other instance.</param>
		/// <returns>The comparison result.</returns>
		/// <remarks><b>The First Char Index does not partecipate to the comparison.</b></remarks>
		public int CompareTo(BasicWordInfo other) {
			if(other == null) return 1;

			int res = location.CompareTo(other.Location) * 2;
			return res + wordIndex.CompareTo(other.WordIndex);
		}

	}

}
