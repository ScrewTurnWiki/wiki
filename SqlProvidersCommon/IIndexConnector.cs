
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.SearchEngine;

namespace ScrewTurn.Wiki.Plugins.SqlCommon {

	/// <summary>
	/// Defines the interface for a connector between an index to a pages storage provider.
	/// </summary>
	public interface IIndexConnector {

		/// <summary>
		/// Invokes the GetWordFetcher delegate.
		/// </summary>
		/// <returns>The word fetcher.</returns>
		IWordFetcher GetWordFetcher();

		/// <summary>
		/// Defines a delegate for a method that gets the size of the approximate index in bytes.
		/// </summary>
		/// <returns>The size of the index, in bytes.</returns>
		long GetSize();

		/// <summary>
		/// Invokes the GetCount delegate.
		/// </summary>
		/// <param name="element">The element type.</param>
		/// <returns>The element count.</returns>
		int GetCount(IndexElementType element);

		/// <summary>
		/// Invokes the ClearIndex delegate.
		/// </summary>
		/// <param name="state">A state object passed from the index.</param>
		void ClearIndex(object state);

		/// <summary>
		/// Invokes the DeleteDataForDocument delegate.
		/// </summary>
		/// <param name="document">The document.</param>
		/// <param name="state">A state object passed from the index.</param>
		void DeleteDataForDocument(IDocument document, object state);

		/// <summary>
		/// Invokes the SaveDataForDocument delegate.
		/// </summary>
		/// <param name="document">The document.</param>
		/// <param name="content">The content words.</param>
		/// <param name="title">The title words.</param>
		/// <param name="keywords">The keywords.</param>
		/// <param name="state">A state object passed from the index.</param>
		/// <returns>The number of stored occurrences.</returns>
		int SaveDataForDocument(IDocument document, WordInfo[] content, WordInfo[] title, WordInfo[] keywords, object state);

	}

	/// <summary>
	/// Defines a delegate for a method for getting a word fetcher.
	/// </summary>
	/// <returns>The word fetcher.</returns>
	public delegate IWordFetcher GetWordFetcher();

	/// <summary>
	/// Defines a delegate for a method that gets the size of the approximate index in bytes.
	/// </summary>
	/// <returns>The size of the index, in bytes.</returns>
	public delegate long GetSize();

	/// <summary>
	/// Defines a delegate for a method that counts elements in the index.
	/// </summary>
	/// <param name="element">The element type.</param>
	/// <returns>The element count.</returns>
	public delegate int GetCount(IndexElementType element);

	/// <summary>
	/// Defines a delegate for method that clears the index.
	/// </summary>
	/// <param name="state">A state object passed from the index.</param>
	public delegate void ClearIndex(object state);

	/// <summary>
	/// Defines a delegate for a method for saving data. The passed data is the whole document data, as it was tokenized.
	/// </summary>
	/// <param name="document">The document.</param>
	/// <param name="content">The content words.</param>
	/// <param name="title">The title words.</param>
	/// <param name="keywords">The keywords.</param>
	/// <param name="state">A state object passed from the index.</param>
	/// <returns>The number of stored occurrences.</returns>
	public delegate int SaveDataForDocument(IDocument document, WordInfo[] content, WordInfo[] title, WordInfo[] keywords, object state);

	/// <summary>
	/// Defines a delegate for a method for deleting a document's data.
	/// </summary>
	/// <param name="document">The document.</param>
	/// <param name="state">A state object passed from the index.</param>
	public delegate void DeleteDataForDocument(IDocument document, object state);

	/// <summary>
	/// Defines a delegate for a method for finding a word.
	/// </summary>
	/// <param name="text">The word text.</param>
	/// <param name="word">The returned word.</param>
	/// <param name="connection">An open database connection.</param>
	/// <returns><c>true</c> if the word is found, <c>false</c> otherwise.</returns>
	public delegate bool TryFindWord(string text, out Word word, DbConnection connection);

	/// <summary>
	/// Defines legal index element types.
	/// </summary>
	public enum IndexElementType {
		/// <summary>
		/// The documents.
		/// </summary>
		Documents,
		/// <summary>
		/// The words.
		/// </summary>
		Words,
		/// <summary>
		/// The occurrences.
		/// </summary>
		Occurrences
	}

}
