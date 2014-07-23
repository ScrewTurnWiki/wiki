
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.SearchEngine {

	/// <summary>
	/// Contains search parameters.
	/// </summary>
	public class SearchParameters {

		private string query;
		private string[] documentTypeTags;
		private SearchOptions options;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SearchParameters" /> class.
		/// </summary>
		/// <param name="query">The search query.</param>
		/// <param name="documentTypeTags">The document type tags to include in the search, or <c>null</c>.</param>
		/// <param name="options">The search options.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="query"/> or one of the elements of <paramref name="documentTypeTags"/> (when the array is not <c>null</c>) are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="query"/> or one of the elements of <paramref name="documentTypeTags"/> (when the array is not <c>null</c>) are empty.</exception>
		public SearchParameters(string query, string[] documentTypeTags, SearchOptions options) {
			if(query == null) throw new ArgumentNullException("query");
			if(query.Length == 0) throw new ArgumentException("Query cannot be empty", "query");
			if(documentTypeTags != null) {
				if(documentTypeTags.Length == 0) throw new ArgumentException("DocumentTypeTags cannot be empty", "documentTypeTags");
				foreach(string dtt in documentTypeTags) {
					if(dtt == null) throw new ArgumentNullException("documentTypeTags");
					if(dtt.Length == 0) throw new ArgumentException("DocumentTypeTag cannot be empty", "documentTypeTag");
				}
			}

			this.query = PrepareQuery(query);
			this.documentTypeTags = documentTypeTags;
			this.options = options;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SearchParameters" /> class.
		/// </summary>
		/// <param name="query">The search query.</param>
		public SearchParameters(string query)
			: this(query, null, SearchOptions.AtLeastOneWord) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SearchParameters" /> class.
		/// </summary>
		/// <param name="query">The search query.</param>
		/// <param name="documentTypeTags">The document type tags to include in the search, or <c>null</c>.</param>
		public SearchParameters(string query, params string[] documentTypeTags)
			: this(query, documentTypeTags, SearchOptions.AtLeastOneWord) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SearchParameters" /> class.
		/// </summary>
		/// <param name="query">The search query.</param>
		/// <param name="options">The search options.</param>
		public SearchParameters(string query, SearchOptions options)
			: this(query, null, options) { }

		/// <summary>
		/// Prepares a query for searching.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <returns>The prepared query.</returns>
		private static string PrepareQuery(string query) {
			StringBuilder sb = new StringBuilder(query.Length);

			// This behavior is slightly different from RemoveDiacriticsAndPunctuation
			foreach(char c in query) {
				if(!ScrewTurn.Wiki.SearchEngine.Tools.IsSplitChar(c)) sb.Append(c);
				else sb.Append(" ");
			}

			string normalized = Tools.RemoveDiacriticsAndPunctuation(sb.ToString(), false);
			return normalized;
		}

		/// <summary>
		/// Gets or sets the query.
		/// </summary>
		public string Query {
			get { return query; }
			set { query = PrepareQuery(value); }
		}

		/// <summary>
		/// Gets or sets the document type tags to include in the search, or <c>null</c>.
		/// </summary>
		public string[] DocumentTypeTags {
			get { return documentTypeTags; }
			set { documentTypeTags = value; }
		}

		/// <summary>
		/// Gets or sets the search options.
		/// </summary>
		public SearchOptions Options {
			get { return options; }
			set { options = value; }
		}

	}

}
