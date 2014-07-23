
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.SearchEngine {

	/// <summary>
	/// Represents a change occurred to the index, structured for easy dumping to disk or database.
	/// </summary>
	/// <remarks>The class is <b>not thread-safe</b>.</remarks>
	public class DumpedChange {

		/// <summary>
		/// The dumped document data.
		/// </summary>
		protected DumpedDocument document;
		/// <summary>
		/// The list of dumped words data.
		/// </summary>
		protected List<DumpedWord> words;
		/// <summary>
		/// The list of dumped mappings data.
		/// </summary>
		protected List<DumpedWordMapping> mappings;

		/// <summary>
		/// Initializes a new instance of the <see cref="DumpedChange" /> class.
		/// </summary>
		/// <param name="document">The dumped document data.</param>
		/// <param name="words">The list of dumped words data.</param>
		/// <param name="mappings">The list of dumped mappings data.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="document"/>, <paramref name="words"/> or <paramref name="mappings"/> are <c>null</c>.</exception>
		public DumpedChange(DumpedDocument document, List<DumpedWord> words, List<DumpedWordMapping> mappings) {
			if(document == null) throw new ArgumentNullException("document");
			if(words == null) throw new ArgumentNullException("words");
			if(mappings == null) throw new ArgumentNullException("mappings");

			// mappings can be empty if the document did not have any indexable content
			//if(mappings.Count == 0) throw new ArgumentException("Mappings cannot be empty", "mappings");

			this.document = document;
			this.words = words;
			this.mappings = mappings;
		}

		/// <summary>
		/// Gets the dumped document data.
		/// </summary>
		public DumpedDocument Document {
			get { return document; }
		}

		/// <summary>
		/// Gets the list of dumped words data.
		/// </summary>
		public List<DumpedWord> Words {
			get { return words; }
		}

		/// <summary>
		/// Gets the list of dumped mappings data.
		/// </summary>
		public List<DumpedWordMapping> Mappings {
			get { return mappings; }
		}

	}

}
