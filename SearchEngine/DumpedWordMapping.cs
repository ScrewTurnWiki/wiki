
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.SearchEngine {
	
	/// <summary>
	/// Contains a word mapping data, structured for easy dumping on disk or database.
	/// </summary>
	/// <remarks>The class is <b>not thread-safe</b>.</remarks>
	public class DumpedWordMapping {

		/// <summary>
		/// The word unique ID.
		/// </summary>
		protected uint wordId;
		/// <summary>
		/// The document unique ID.
		/// </summary>
		protected uint documentId;
		/// <summary>
		/// The index of the character of the word.
		/// </summary>
		protected ushort firstCharIndex;
		/// <summary>
		/// The index of the word in the original document.
		/// </summary>
		protected ushort wordIndex;
		/// <summary>
		/// The location identifier.
		/// </summary>
		protected byte location;

		/// <summary>
		/// Initializes a new instance of the <see cref="DumpedWordMapping" /> class.
		/// </summary>
		/// <param name="wordId">The word unique ID.</param>
		/// <param name="documentId">The document unique ID.</param>
		/// <param name="firstCharIndex">The index of the first character the word.</param>
		/// <param name="wordIndex">The index of the word in the original index.</param>
		/// <param name="location">The location identifier.</param>
		public DumpedWordMapping(uint wordId, uint documentId, ushort firstCharIndex, ushort wordIndex, byte location) {
			this.wordId = wordId;
			this.documentId = documentId;
			this.firstCharIndex = firstCharIndex;
			this.wordIndex = wordIndex;
			this.location = location;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DumpedWordMapping" /> class.
		/// </summary>
		/// <param name="wordId">The word unique ID.</param>
		/// <param name="documentId">The document unique ID.</param>
		/// <param name="info">The <see cref="BasicWordInfo" />.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="info"/> is <c>null</c>.</exception>
		public DumpedWordMapping(uint wordId, uint documentId, BasicWordInfo info) {
			if(info == null) throw new ArgumentNullException("info");

			this.wordId = wordId;
			this.documentId = documentId;
			this.firstCharIndex = info.FirstCharIndex;
			this.wordIndex = info.WordIndex;
			this.location = info.Location.Location;
		}

		/// <summary>
		/// Gets or sets the word unique ID.
		/// </summary>
		public uint WordID {
			get { return wordId; }
			set { wordId = value; }
		}

		/// <summary>
		/// Gets the document unique ID.
		/// </summary>
		public uint DocumentID {
			get { return documentId; }
		}

		/// <summary>
		/// Gets the index of the first character of the word.
		/// </summary>
		public ushort FirstCharIndex {
			get { return firstCharIndex; }
		}

		/// <summary>
		/// Gets the index of the word in the original document.
		/// </summary>
		public ushort WordIndex {
			get { return wordIndex; }
		}

		/// <summary>
		/// Gets the location identifier.
		/// </summary>
		public byte Location {
			get { return location; }
		}

	}

}
