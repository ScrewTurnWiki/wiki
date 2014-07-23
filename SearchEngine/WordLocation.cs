
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.SearchEngine {

	/// <summary>
	/// Describes the location of a word in a document.
	/// </summary>
	public class WordLocation : IComparable<WordLocation>, IEquatable<WordLocation> {

		private byte location;
		private string label;
		private float relativeRelevance;

		/// <summary>
		/// Initializes a new instance of the <see cref="WordLocation" /> class.
		/// </summary>
		/// <param name="location">A number representing the location.</param>
		/// <param name="label">The label of the instance.</param>
		/// <param name="relativeRelevance">The relative relevance of the instance.</param>
		protected WordLocation(byte location, string label, float relativeRelevance) {
			this.location = location;
			this.label = label;
			this.relativeRelevance = relativeRelevance;
		}

		/// <summary>
		/// Gets the location identifier.
		/// </summary>
		/// <remarks>This property should only be used for serialization purposes.</remarks>
		public byte Location {
			get { return location; }
		}

		/// <summary>
		/// Gets the relative relevance of the word location.
		/// </summary>
		public float RelativeRelevance {
			get { return relativeRelevance; }
		}

		/// <summary>
		/// Gets a string representation of the current instance.
		/// </summary>
		/// <returns>The string representation.</returns>
		public override string ToString() {
			return label;
		}

		/// <summary>
		/// Represents a word that is in the title of a document.
		/// </summary>
		public static WordLocation Title {
			get { return new WordLocation(1, "Title", 2); }
		}

		/// <summary>
		/// Represents a word that is in the keywords of a document.
		/// </summary>
		public static WordLocation Keywords {
			get { return new WordLocation(2, "Keywords", 1.5F); }
		}

		/// <summary>
		/// Represents a word that is in the content of a document.
		/// </summary>
		public static WordLocation Content {
			get { return new WordLocation(3, "Content", 1); }
		}

		/// <summary>
		/// Gets the correct <see cref="WordLocation" /> instance from the location identifier.
		/// </summary>
		/// <param name="location">The location identifier.</param>
		/// <returns>The correct instance.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="location"/> is different from 1, 2, 3.</exception>
		public static WordLocation GetInstance(byte location) {
			switch(location) {
				case 1:
					return Title;
				case 2:
					return Keywords;
				case 3:
					return Content;
				default:
					throw new ArgumentOutOfRangeException("Invalid location", "location");
			}
		}

		/// <summary>
		/// Compares the current instance to another.
		/// </summary>
		/// <param name="other">The other instance.</param>
		/// <returns>The comparison result.</returns>
		public int CompareTo(WordLocation other) {
			if(object.ReferenceEquals(other, null)) return 1;

			if(location > other.location) return 1;
			else if(location < other.location) return -1;
			else return 0;
			//return location.CompareTo(other.location);
		}

		/// <summary>
		/// Determines whether the current instance equals an object.
		/// </summary>
		/// <param name="obj">The object.</param>
		/// <returns><c>true</c> if the current instance equals the object, <c>false</c> otherwise.</returns>
		public override bool Equals(object obj) {
			if(obj is WordLocation) return Equals((WordLocation)obj);
			else return false;
		}

		/// <summary>
		/// Returns the hash code of the current instance.
		/// </summary>
		/// <returns>The hash code.</returns>
		public override int GetHashCode() {
			return location;
		}

		/// <summary>
		/// Determines whether the current instance equals another.
		/// </summary>
		/// <param name="other">The other instance.</param>
		/// <returns><c>true</c> if the current instance equals the other, <c>false</c> otherwise.</returns>
		public bool Equals(WordLocation other) {
			if(object.ReferenceEquals(other, null)) return false;
			else return location == other.location;
		}

		/// <summary>
		/// Applies the value-equality operator to two <see cref="T:WordLocation" /> objects.
		/// </summary>
		/// <param name="x">The first object.</param>
		/// <param name="y">The second object.</param>
		/// <returns><c>true</c> if the objects are value-equal, <c>false</c> otherwise.</returns>
		public static bool operator ==(WordLocation x, WordLocation y) {
			if(object.ReferenceEquals(x, null) && !object.ReferenceEquals(y, null)) return false;
			if(!object.ReferenceEquals(x, null) && object.ReferenceEquals(y, null)) return false;
			if(object.ReferenceEquals(x, null) && object.ReferenceEquals(y, null)) return true;
			return x.Equals(y);
		}

		/// <summary>
		/// Applies the value-inequality operator to two <see cref="T:WordLocation" /> objects.
		/// </summary>
		/// <param name="x">The first object.</param>
		/// <param name="y">The second object.</param>
		/// <returns><c>true</c> if the objects are not value-equal, <c>false</c> otherwise.</returns>
		public static bool operator !=(WordLocation x, WordLocation y) {
			return !(x == y);
		}

	}

}
