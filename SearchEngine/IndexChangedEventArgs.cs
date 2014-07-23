
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.SearchEngine {

	/// <summary>
	/// Contains arguments for the <b>IndexChanged</b> event of the <see cref="IInMemoryIndex" /> interface.
	/// </summary>
	public class IndexChangedEventArgs : EventArgs {

		private IDocument document;
		private IndexChangeType change;
		private DumpedChange changeData;
		private object state;

		private IndexStorerResult result = null;

		/// <summary>
		/// Initializes a new instance of the <see cref="IndexChangedEventArgs" /> class.
		/// </summary>
		/// <param name="document">The affected document.</param>
		/// <param name="change">The change performed.</param>
		/// <param name="changeData">The dumped change data.</param>
		/// <param name="state">A state object that is passed to the IndexStorer SaveDate/DeleteData function.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="change"/> is not <see cref="IndexChangeType.IndexCleared"/> and <paramref name="document"/> or <paramref name="changeData"/> are <c>null</c>.</exception>
		public IndexChangedEventArgs(IDocument document, IndexChangeType change, DumpedChange changeData, object state) {
			if(change != IndexChangeType.IndexCleared) {
				if(document == null) throw new ArgumentNullException("document");
				if(changeData == null) throw new ArgumentNullException("changeData");
			}

			this.document = document;
			this.change = change;
			this.changeData = changeData;
			this.state = state;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="IndexChangedEventArgs" /> class.
		/// </summary>
		/// <param name="document">The affected document.</param>
		/// <param name="change">The change performed.</param>
		/// <param name="changeData">The dumped change data.</param>
		/// <param name="state">A state object that is passed to the IndexStorer SaveDate/DeleteData function.</param>
		/// <param name="result">The storer result, if any.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="change"/> is not <see cref="IndexChangeType.IndexCleared"/> and <paramref name="document"/> or <paramref name="changeData"/> are <c>null</c>.</exception>
		public IndexChangedEventArgs(IDocument document, IndexChangeType change, DumpedChange changeData, object state, IndexStorerResult result)
			: this(document, change, changeData, state) {

			this.result = result;
		}

		/// <summary>
		/// Gets the affected document.
		/// </summary>
		public IDocument Document {
			get { return document; }
		}

		/// <summary>
		/// Gets the change performed.
		/// </summary>
		public IndexChangeType Change {
			get { return change; }
		}

		/// <summary>
		/// Gets the dumped change data.
		/// </summary>
		public DumpedChange ChangeData {
			get { return changeData; }
		}

		/// <summary>
		/// Gets the state object that is passed to the IndexStorer SaveDate/DeleteData function.
		/// </summary>
		public object State {
			get { return state; }
		}

		/// <summary>
		/// Gets or sets the index storer result, if any.
		/// </summary>
		public IndexStorerResult Result {
			get { return result; }
			set { result = value; }
		}

	}

	/// <summary>
	/// Lists valid index changes.
	/// </summary>
	public enum IndexChangeType {
		/// <summary>
		/// A document is added.
		/// </summary>
		DocumentAdded,
		/// <summary>
		/// A document is removed.
		/// </summary>
		DocumentRemoved,
		/// <summary>
		/// The index is cleared.
		/// </summary>
		IndexCleared
	}

}
