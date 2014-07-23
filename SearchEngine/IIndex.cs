
using System;

namespace ScrewTurn.Wiki.SearchEngine {

	/// <summary>
	/// A delegate that is used for converting a <see cref="DumpedDocument" /> to an instance of a class implementing <see cref="IDocument" />, 
	/// while reading index data from a permanent storage.
	/// </summary>
	/// <param name="document">The <see cref="DumpedDocument" /> to convert.</param>
	/// <returns>The converted document implementing <see cref="IDocument" /> or <c>null</c> if no document is found.</returns>
	public delegate IDocument BuildDocument(DumpedDocument document);
	
	/// <summary>
	/// Defines an interface for a search engine index.
	/// </summary>
	public interface IIndex {

		/// <summary>
		/// Gets or sets the stop words to be used while indexing new content.
		/// </summary>
		string[] StopWords { get; set; }

		/// <summary>
		/// Gets the total count of unique words.
		/// </summary>
		/// <remarks>Computing the result is <n>O(1)</n>.</remarks>
		int TotalWords { get; }

		/// <summary>
		/// Gets the total count of documents.
		/// </summary>
		/// <remarks>Computing the result is <b>O(n*m)</b>, where <b>n</b> is the number of 
		/// words in the index and <b>m</b> is the number of documents.</remarks>
		int TotalDocuments { get; }

		/// <summary>
		/// Gets the total number of occurrences (count of words in each document).
		/// </summary>
		/// <remarks>Computing the result is <b>O(n)</b>, 
		/// where <b>n</b> is the number of words in the index.</remarks>
		int TotalOccurrences { get; }

		/// <summary>
		/// Completely clears the index (stop words are not affected).
		/// </summary>
		/// <param name="state">A state object that is passed to the IndexStorer SaveDate/DeleteData function.</param>
		void Clear(object state);

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
		/// <exception cref="ArgumentNullException">If <paramref name="document"/> or <paramref name="content"/> are <c>null</c>.</exception>
		int StoreDocument(IDocument document, string[] keywords, string content, object state);

		/// <summary>
		/// Removes a document from the index.
		/// </summary>
		/// <param name="document">The document to remove.</param>
		/// <param name="state">A state object that is passed to the IndexStorer SaveDate/DeleteData function.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="document"/> is <c>null</c>.</exception>
		void RemoveDocument(IDocument document, object state);

		/// <summary>
		/// Performs a search in the index.
		/// </summary>
		/// <param name="parameters">The search parameters.</param>
		/// <returns>The results.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="parameters"/> is <c>null</c>.</exception>
		SearchResultCollection Search(SearchParameters parameters);

	}

	/// <summary>
	/// Defines an interface for an in-memory index.
	/// </summary>
	public interface IInMemoryIndex : IIndex {

		/// <summary>
		/// An event fired when the index is changed.
		/// </summary>
		event EventHandler<IndexChangedEventArgs> IndexChanged;

		/// <summary>
		/// Sets the delegate used for converting a <see cref="DumpedDocument" /> to an instance of a class implementing <see cref="IDocument" />, 
		/// while reading index data from a permanent storage.
		/// </summary>
		/// <param name="buildDocument">The delegate (cannot be <c>null</c>).</param>
		/// <remarks>This method must be called before invoking <see cref="InitializeData" />.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="buildDocument"/> is <c>null</c>.</exception>
		void SetBuildDocumentDelegate(BuildDocument buildDocument);

		/// <summary>
		/// Initializes index data by completely emptying the index catalog and storing the specified data.
		/// </summary>
		/// <param name="documents">The documents.</param>
		/// <param name="words">The words.</param>
		/// <param name="mappings">The mappings.</param>
		/// <remarks>The method <b>does not</b> check the consistency of the data passed as arguments.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="documents"/>, <paramref name="words"/> or <paramref name="mappings"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">If <see cref="M:SetBuildDocumentDelegate"/> was not called.</exception>
		void InitializeData(DumpedDocument[] documents, DumpedWord[] words, DumpedWordMapping[] mappings);

	}

	/// <summary>
	/// Lists legal search options.
	/// </summary>
	public enum SearchOptions {
		/// <summary>
		/// Search for at least one word of the search query.
		/// </summary>
		AtLeastOneWord,
		/// <summary>
		/// Search for all the words of the search query, in any order.
		/// </summary>
		AllWords,
		/// <summary>
		/// Search for an exact phrase.
		/// </summary>
		ExactPhrase
	}

}
