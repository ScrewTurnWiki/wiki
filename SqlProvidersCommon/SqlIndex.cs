
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.SearchEngine;

namespace ScrewTurn.Wiki.Plugins.SqlCommon {
	
	/// <summary>
	/// Implements a SQL-based search engine index.
	/// </summary>
	public class SqlIndex : IIndex {

		private IIndexConnector connector;

		/// <summary>
		/// The stop words to be used while indexing new content.
		/// </summary>
		protected string[] stopWords = null;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SqlIndex" /> class.
		/// </summary>
		/// <param name="connector">The connection object.</param>
		public SqlIndex(IIndexConnector connector) {
			if(connector == null) throw new ArgumentNullException("connector");

			this.connector = connector;
			this.stopWords = new string[0];
		}

		/// <summary>
		/// Gets or sets the stop words to be used while indexing new content.
		/// </summary>
		/// <value></value>
		public string[] StopWords {
			get {
				lock(this) {
					return stopWords;
				}
			}
			set {
				if(value == null) throw new ArgumentNullException("value", "Stop words cannot be null");
				lock(this) {
					stopWords = value;
				}
			}
		}

		/// <summary>
		/// Gets the total count of unique words.
		/// </summary>
		/// <remarks>Computing the result is <n>O(1)</n>.</remarks>
		public int TotalWords {
			get {
				return connector.GetCount(IndexElementType.Words);
			}
		}

		/// <summary>
		/// Gets the total count of documents.
		/// </summary>
		/// <remarks>Computing the result is <b>O(n*m)</b>, where <b>n</b> is the number of
		/// words in the index and <b>m</b> is the number of documents.</remarks>
		public int TotalDocuments {
			get {
				return connector.GetCount(IndexElementType.Documents);
			}
		}

		/// <summary>
		/// Gets the total number of occurrences (count of words in each document).
		/// </summary>
		/// <remarks>Computing the result is <b>O(n)</b>,
		/// where <b>n</b> is the number of words in the index.</remarks>
		public int TotalOccurrences {
			get {
				return connector.GetCount(IndexElementType.Occurrences);
			}
		}

		/// <summary>
		/// Completely clears the index (stop words are not affected).
		/// </summary>
		/// <param name="state">A state object that is passed to the IndexStorer SaveDate/DeleteData function.</param>
		public void Clear(object state) {
			connector.ClearIndex(state);
		}

		/// <summary>
		/// Stores a document in the index.
		/// </summary>
		/// <param name="document">The document.</param>
		/// <param name="keywords">The document keywords, if any, an empty array or <c>null</c> otherwise.</param>
		/// <param name="content">The content of the document.</param>
		/// <param name="state">A state object that is passed to the IndexStorer SaveDate/DeleteData function.</param>
		/// <returns>The number of indexed words (including duplicates).</returns>
		/// <remarks>Indexing the content of the document is <b>O(n)</b>,
		/// where <b>n</b> is the total number of words in the document.</remarks>
		public int StoreDocument(IDocument document, string[] keywords, string content, object state) {
			if(document == null) throw new ArgumentNullException("document");
			if(keywords == null) keywords = new string[0];
			if(content == null) throw new ArgumentNullException("content");

			RemoveDocument(document, state);

			keywords = ScrewTurn.Wiki.SearchEngine.Tools.CleanupKeywords(keywords);

			// Prepare content words
			WordInfo[] contentWords = document.Tokenize(content);
			contentWords = ScrewTurn.Wiki.SearchEngine.Tools.RemoveStopWords(contentWords, stopWords);

			// Prepare title words
			WordInfo[] titleWords = document.Tokenize(document.Title);
			titleWords = ScrewTurn.Wiki.SearchEngine.Tools.RemoveStopWords(titleWords, stopWords);
			for(int i = 0; i < titleWords.Length; i++) {
				titleWords[i] = new WordInfo(titleWords[i].Text, titleWords[i].FirstCharIndex, titleWords[i].WordIndex, WordLocation.Title);
			}

			// Prepare keywords
			WordInfo[] words = new WordInfo[keywords.Length];
			int count = 0;
			for(int i = 0; i < words.Length; i++) {
				words[i] = new WordInfo(keywords[i], (ushort)count, (ushort)i, WordLocation.Keywords);
				count += 1 + keywords[i].Length;
			}

			return connector.SaveDataForDocument(document, contentWords, titleWords, words, state);
		}

		/// <summary>
		/// Removes a document from the index.
		/// </summary>
		/// <param name="document">The document to remove.</param>
		/// <param name="state">A state object that is passed to the IndexStorer SaveDate/DeleteData function.</param>
		public void RemoveDocument(IDocument document, object state) {
			if(document == null) throw new ArgumentNullException("document");

			connector.DeleteDataForDocument(document, state);
		}

		/// <summary>
		/// Performs a search in the index.
		/// </summary>
		/// <param name="parameters">The search parameters.</param>
		/// <returns>The results.</returns>
		public SearchResultCollection Search(SearchParameters parameters) {
			if(parameters == null) throw new ArgumentNullException("parameters");

			using(IWordFetcher fetcher = connector.GetWordFetcher()) {
				if(parameters.DocumentTypeTags == null) {
					return ScrewTurn.Wiki.SearchEngine.Tools.SearchInternal(parameters.Query, null, false, parameters.Options, fetcher);
				}
				else {
					return ScrewTurn.Wiki.SearchEngine.Tools.SearchInternal(parameters.Query, parameters.DocumentTypeTags, true, parameters.Options, fetcher);
				}
			}
		}

	}

	/// <summary>
	/// Implements a word fetcher for a SQL-based index.
	/// </summary>
	public class SqlWordFetcher : IWordFetcher {

		private DbConnection connection;
		private TryFindWord implementation;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SqlWordFetcher" /> class.
		/// </summary>
		/// <param name="connection">An open database connection.</param>
		/// <param name="implementation">The method implementation.</param>
		public SqlWordFetcher(DbConnection connection, TryFindWord implementation) {
			if(connection == null) throw new ArgumentNullException("connection");
			if(implementation == null) throw new ArgumentNullException("implementation");

			this.connection = connection;
			this.implementation = implementation;
		}

		/// <summary>
		/// Tries to get a word.
		/// </summary>
		/// <param name="text">The text of the word.</param>
		/// <param name="word">The found word, if any, <c>null</c> otherwise.</param>
		/// <returns><c>true</c> if the word is found, <c>false</c> otherwise.</returns>
		public bool TryGetWord(string text, out Word word) {
			return implementation(text, out word, connection);
		}

		#region IDisposable Members

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose() {
			try {
				connection.Close();
			}
			catch { }
		}

		#endregion

	}

}
