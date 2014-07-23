
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.SearchEngine {

	/// <summary>
	/// Contains the results of an index storer operation.
	/// </summary>
	public class IndexStorerResult {

		private uint? documentId;
		private List<WordId> wordIds;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:IndexStorerResult" /> class.
		/// </summary>
		/// <param name="documentId">The ID of the document just stored, if any.</param>
		/// <param name="wordIds">The IDs of the words just stored, if any.</param>
		public IndexStorerResult(uint? documentId, List<WordId> wordIds) {
			this.documentId = documentId;
			this.wordIds = wordIds;
		}

		/// <summary>
		/// Gets or sets the ID of the document just stored, if any.
		/// </summary>
		public uint? DocumentID {
			get { return documentId; }
			set { documentId = value; }
		}

		/// <summary>
		/// Gets or sets the IDs of the words
		/// </summary>
		public List<WordId> WordIDs {
			get { return wordIds; }
			set { wordIds = value; }
		}

	}

	/// <summary>
	/// Describes the ID of a word.
	/// </summary>
	public class WordId {

		private string text;
		private uint id;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:WordId" /> class.
		/// </summary>
		/// <param name="text">The word text, lowercase.</param>
		/// <param name="id">The word ID.</param>
		public WordId(string text, uint id) {
			this.text = text;
			this.id = id;
		}

		/// <summary>
		/// Gets the word text.
		/// </summary>
		public string Text {
			get { return text; }
		}

		/// <summary>
		/// Gets the word ID.
		/// </summary>
		public uint ID {
			get { return id; }
		}

	}

}
