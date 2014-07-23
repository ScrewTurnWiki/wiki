
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.SearchEngine;
using System.Data.Common;

namespace ScrewTurn.Wiki.Plugins.SqlCommon {

	/// <summary>
	/// Defines a connector between an index and a pages storage provider.
	/// </summary>
	public class IndexConnector : IIndexConnector {

		private GetWordFetcher getWordFetcher;
		private GetSize getSize;
		private GetCount getCount;
		private ClearIndex clearIndex;
		private DeleteDataForDocument deleteData;
		private SaveDataForDocument saveData;
		private TryFindWord tryFindWord;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:IndexConnector" /> class.
		/// </summary>
		/// <param name="getWordFetcher">The GetWordFetcher delegate.</param>
		/// <param name="getSize">The GetSize delegate.</param>
		/// <param name="getCount">The GetCount delegate.</param>
		/// <param name="clearIndex">The ClearIndex delegate.</param>
		/// <param name="deleteData">The DeleteDataForDocument delegate.</param>
		/// <param name="saveData">The SaveData delegate.</param>
		/// <param name="tryFindWord">The TryFindWord delegate.</param>
		public IndexConnector(GetWordFetcher getWordFetcher, GetSize getSize, GetCount getCount, ClearIndex clearIndex,
			DeleteDataForDocument deleteData, SaveDataForDocument saveData, TryFindWord tryFindWord) {

			if(getWordFetcher == null) throw new ArgumentNullException("getWordFetcher");
			if(getSize == null) throw new ArgumentNullException("getSize");
			if(getCount == null) throw new ArgumentNullException("getCount");
			if(clearIndex == null) throw new ArgumentNullException("clearIndex");
			if(deleteData == null) throw new ArgumentNullException("deleteData");
			if(saveData == null) throw new ArgumentNullException("saveData");
			if(tryFindWord == null) throw new ArgumentNullException("tryFindWord");

			this.getWordFetcher = getWordFetcher;
			this.getSize = getSize;
			this.getCount = getCount;
			this.clearIndex = clearIndex;
			this.deleteData = deleteData;
			this.saveData = saveData;
			this.tryFindWord = tryFindWord;
		}

		/// <summary>
		/// Invokes the GetWordFetcher delegate.
		/// </summary>
		/// <returns>The word fetcher.</returns>
		public IWordFetcher GetWordFetcher() {
			return getWordFetcher();
		}

		/// <summary>
		/// Defines a delegate for a method that gets the size of the approximate index in bytes.
		/// </summary>
		/// <returns>The size of the index, in bytes.</returns>
		public long GetSize() {
			return getSize();
		}

		/// <summary>
		/// Invokes the GetCount delegate.
		/// </summary>
		/// <param name="element">The element type.</param>
		/// <returns>The element count.</returns>
		public int GetCount(IndexElementType element) {
			return getCount(element);
		}

		/// <summary>
		/// Invokes the ClearIndex delegate.
		/// </summary>
		/// <param name="state">A state object passed from the index.</param>
		public void ClearIndex(object state) {
			clearIndex(state);
		}

		/// <summary>
		/// Invokes the DeleteDataForDocument delegate.
		/// </summary>
		/// <param name="document">The document.</param>
		/// <param name="state">A state object passed from the index.</param>
		public void DeleteDataForDocument(IDocument document, object state) {
			deleteData(document, state);
		}

		/// <summary>
		/// Invokes the SaveDataForDocument delegate.
		/// </summary>
		/// <param name="document">The document.</param>
		/// <param name="content">The content words.</param>
		/// <param name="title">The title words.</param>
		/// <param name="keywords">The keywords.</param>
		/// <param name="state">A state object passed from the index.</param>
		/// <returns>The number of stored occurrences.</returns>
		public int SaveDataForDocument(IDocument document, WordInfo[] content, WordInfo[] title, WordInfo[] keywords, object state) {
			return saveData(document, content, title, keywords, state);
		}

	}

}
