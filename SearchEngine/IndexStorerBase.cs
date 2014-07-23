
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.SearchEngine {

	/// <summary>
	/// Implements a base class for an index storer.
	/// </summary>
	/// <remarks>Instance and static members are <b>thread-safe</b>.</remarks>
	public abstract class IndexStorerBase : IDisposable {

		/// <summary>
		/// Indicates whether the object was disposed.
		/// </summary>
		protected bool disposed = false;

		/// <summary>
		/// The index bound to this storer.
		/// </summary>
		protected IInMemoryIndex index = null;

		/// <summary>
		/// <c>true</c> if the index data seems corrupted.
		/// </summary>
		protected bool dataCorrupted = false;

		/// <summary>
		/// Contains the exception occurred during index setup.
		/// </summary>
		protected Exception reasonForDataCorruption = null;

		/// <summary>
		/// The event handler for the <see cref="IInMemoryIndex.IndexChanged" /> of the bound index.
		/// </summary>
		protected EventHandler<IndexChangedEventArgs> indexChangedHandler;

		/// <summary>
		/// Initializes a new instance of the <see cref="IndexStorerBase" /> class.
		/// </summary>
		/// <param name="index">The index to manage.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="index"/> is <c>null</c>.</exception>
		public IndexStorerBase(IInMemoryIndex index) {
			if(index == null) throw new ArgumentNullException("index");

			this.index = index;
			indexChangedHandler = new EventHandler<IndexChangedEventArgs>(IndexChangedHandler);
			this.index.IndexChanged += indexChangedHandler;
		}

		/// <summary>
		/// Gets the index.
		/// </summary>
		public IInMemoryIndex Index {
			get {
				lock(this) {
					return index;
				}
			}
		}

		/// <summary>
		/// Loads the index from the data store the first time.
		/// </summary>
		/// <param name="documents">The dumped documents.</param>
		/// <param name="words">The dumped words.</param>
		/// <param name="mappings">The dumped word mappings.</param>
		protected abstract void LoadIndexInternal(out DumpedDocument[] documents, out DumpedWord[] words, out DumpedWordMapping[] mappings);

		/// <summary>
		/// Gets the approximate size, in bytes, of the search engine index.
		/// </summary>
		public abstract long Size { get; }

		/// <summary>
		/// Gets a value indicating whether the index data seems corrupted and cannot be used.
		/// </summary>
		public bool DataCorrupted {
			get {
				lock(this) {
					return dataCorrupted;
				}
			}
		}

		/// <summary>
		/// Gets the exception that caused data corruption.
		/// </summary>
		public Exception ReasonForDataCorruption {
			get { return reasonForDataCorruption; }
		}

		/// <summary>
		/// Loads the index from the data store the first time.
		/// </summary>
		public void LoadIndex() {
			lock(this) {
				DumpedDocument[] documents = null;
				DumpedWord[] words = null;
				DumpedWordMapping[] mappings = null;

				dataCorrupted = false;

				try {
					LoadIndexInternal(out documents, out words, out mappings);
				}
				catch(Exception ex) {
					reasonForDataCorruption = ex;
					dataCorrupted = true;
				}

				if(!dataCorrupted) {
					try {
						index.InitializeData(documents, words, mappings);
					}
					catch(Exception ex) {
						reasonForDataCorruption = ex;
						dataCorrupted = true;
					}
				}
			}
		}

		/// <summary>
		/// Handles the <see cref="IInMemoryIndex.IndexChanged" /> events.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event arguments.</param>
		protected void IndexChangedHandler(object sender, IndexChangedEventArgs e) {
			lock(this) {
				if(disposed) return;

				switch(e.Change) {
					case IndexChangeType.IndexCleared:
						InitDataStore(e.State);
						break;
					case IndexChangeType.DocumentAdded:
						if(!dataCorrupted) {
							IndexStorerResult result = SaveData(e.ChangeData, e.State);
							e.Result = result;
						}
						break;
					case IndexChangeType.DocumentRemoved:
						if(!dataCorrupted) DeleteData(e.ChangeData, e.State);
						break;
					default:
						throw new NotSupportedException("Invalid Change Type");
				}
			}
		}

		/// <summary>
		/// Initializes the data storage.
		/// </summary>
		/// <param name="state">A state object passed from the index.</param>
		protected abstract void InitDataStore(object state);

		/// <summary>
		/// Deletes data from the data storage.
		/// </summary>
		/// <param name="data">The data to delete.</param>
		/// <param name="state">A state object passed from the index.</param>
		protected abstract void DeleteData(DumpedChange data, object state);

		/// <summary>
		/// Stores new data into the data storage.
		/// </summary>
		/// <param name="data">The data to store.</param>
		/// <param name="state">A state object passed by the index.</param>
		/// <returns>The storer result, if any.</returns>
		/// <remarks>When saving a new document, the document ID in data.Mappings must be
		/// replaced with the currect document ID, generated by the concrete implementation of
		/// this method. data.Words should have IDs numbered from uint.MaxValue downwards. 
		/// The method re-numbers the words appropriately.</remarks>
		protected abstract IndexStorerResult SaveData(DumpedChange data, object state);

		/// <summary>
		/// Disposes the current object.
		/// </summary>
		public void Dispose() {
			lock(this) {
				if(!disposed) {
					disposed = true;
					index.IndexChanged -= indexChangedHandler;
				}
			}
		}

		/// <summary>
		/// Determines whether a <see cref="DumpedWordMapping" /> is contained in a list.
		/// </summary>
		/// <param name="mapping">The mapping.</param>
		/// <param name="list">The list.</param>
		/// <returns><c>true</c> if the mapping is contained in the list, <c>false</c> otherwise.</returns>
		protected static bool Find(DumpedWordMapping mapping, IEnumerable<DumpedWordMapping> list) {
			foreach(DumpedWordMapping m in list) {
				if(m.WordID == mapping.WordID &&
					m.DocumentID == mapping.DocumentID &&
					m.FirstCharIndex == mapping.FirstCharIndex &&
					m.WordIndex == mapping.WordIndex &&
					m.Location == mapping.Location) return true;
			}
			return false;
		}

		/// <summary>
		/// Determines whether a <see cref="DumpedWord" /> is contained in a list.
		/// </summary>
		/// <param name="word">The word.</param>
		/// <param name="list">The list.</param>
		/// <returns><c>true</c> if the word is contained in the list, <c>false</c> orherwise.</returns>
		protected static bool Find(DumpedWord word, IEnumerable<DumpedWord> list) {
			foreach(DumpedWord w in list) {
				if(w.ID == word.ID && w.Text == word.Text) return true;
			}
			return false;
		}

	}

}
