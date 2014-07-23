
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;

namespace ScrewTurn.Wiki.SearchEngine {

	/// <summary>
	/// Represents a search result.
	/// </summary>
	/// <remarks>Instance and static members are <b>not thread-safe</b>.</remarks>
	public class SearchResult {

		/// <summary>
		/// The document the result refers to.
		/// </summary>
		protected IDocument document;
		/// <summary>
		/// The matches in the document.
		/// </summary>
		protected WordInfoCollection matches;
		/// <summary>
		/// The result relevance.
		/// </summary>
		protected Relevance relevance;

		/// <summary>
		/// Initializes a new instance of the <see cref="SearchResult" /> class.
		/// </summary>
		/// <param name="document">The document the result refers to.</param>
		/// <remarks>The relevance is initially set to <b>0</b>.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="document"/> is <c>null</c>.</exception>
		public SearchResult(IDocument document) {
			if(document == null) throw new ArgumentNullException("document");

			this.document = document;
			this.matches = new WordInfoCollection();
			this.relevance = new Relevance(0);
		}

		/// <summary>
		/// Gets the document the result refers to.
		/// </summary>
		public IDocument Document {
			get { return document; }
		}

		/// <summary>
		/// Gets the matches in the document.
		/// </summary>
		public WordInfoCollection Matches {
			get { return matches; }
		}

		/// <summary>
		/// Gets the relevance of the search result.
		/// </summary>
		public Relevance Relevance {
			get { return relevance; }
		}

		/// <summary>
		/// Gets a string representation of the current instance.
		/// </summary>
		/// <returns>The string representation.</returns>
		public override string ToString() {
			return document.Name + "(" + matches.Count.ToString() + " matches)";
		}

	}

}
