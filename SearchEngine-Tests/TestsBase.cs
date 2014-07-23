
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;

namespace ScrewTurn.Wiki.SearchEngine.Tests {
	
	public class TestsBase {

		/// <summary>
		/// A general purpose mock repository, initalized dusing test fixture setup.
		/// </summary>
		private MockRepository looseMocks;

		/// <summary>
		/// Demo content for a plain-text document.
		/// </summary>
		protected const string PlainTextDocumentContent = "This is some content.";

		/// <summary>
		/// Demo content for a plain-text document.
		/// </summary>
		protected const string PlainTextDocumentContent2 = "Dummy text used for testing purposes.";

		/// <summary>
		/// Demo content for a plain-text document.
		/// </summary>
		protected const string PlainTextDocumentContent3 = "Todo.";

		/// <summary>
		/// Demo content for a plain-text document.
		/// </summary>
		protected const string PlainTextDocumentContent4 = "Content with repeated content.";

		/// <summary>
		/// The words contained in the demo content (<b>PlainTextDocumentContent</b>).
		/// </summary>
		protected WordInfo[] PlainTextDocumentWords = new WordInfo[] {
			new WordInfo("This", 0, 0, WordLocation.Content),
			new WordInfo("is", 5, 1, WordLocation.Content),
			new WordInfo("some", 8, 2, WordLocation.Content),
			new WordInfo("content", 13, 3, WordLocation.Content)
		};

		/// <summary>
		/// The words contained in the demo content (<b>PlainTextDocumentContent2</b>).
		/// </summary>
		protected WordInfo[] PlainTextDocumentWords2 = new WordInfo[] {
			new WordInfo("Dummy", 0, 0, WordLocation.Content),
			new WordInfo("text", 6, 1, WordLocation.Content),
			new WordInfo("used", 11, 2, WordLocation.Content),
			new WordInfo("for", 16, 3, WordLocation.Content),
			new WordInfo("testing", 20, 4, WordLocation.Content),
			new WordInfo("purposes", 28, 5, WordLocation.Content)
		};

		/// <summary>
		/// The words contained in the demo content (<b>PlainTextDocumentContent3</b>).
		/// </summary>
		protected WordInfo[] PlainTextDocumentWords3 = new WordInfo[] {
			new WordInfo("Todo", 0, 0, WordLocation.Content)
		};

		/// <summary>
		/// The words contained in the demo content (<b>PlainTextDocumentContent4</b>).
		/// </summary>
		protected WordInfo[] PlainTextDocumentWords4 = new WordInfo[] {
			new WordInfo("Content", 0, 0, WordLocation.Content),
			new WordInfo("with", 8, 1, WordLocation.Content),
			new WordInfo("repeated", 13, 2, WordLocation.Content),
			new WordInfo("content", 22, 3, WordLocation.Content)
		};

		[SetUp]
		public void SetUp() {
			looseMocks = new MockRepository();
		}

		/// <summary>
		/// Mocks an index, inheriting from IndexBase.
		/// </summary>
		/// <returns>The index.</returns>
		public IInMemoryIndex MockInMemoryIndex() {
			InMemoryIndexBase index = looseMocks.DynamicMock<InMemoryIndexBase>();

			looseMocks.Replay(index);

			return index;
		}

		/// <summary>
		/// Mocks a document with a fixed content.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="title">The title.</param>
		/// <param name="typeTag">The type tag.</param>
		/// <param name="dateTime">The date/time.</param>
		/// <returns>The mocked document.</returns>
		public IDocument MockDocument(string name, string title, string typeTag, DateTime dateTime) {
			IDocument doc = looseMocks.DynamicMock<IDocument>();
			Expect.Call(doc.Name).Return(name).Repeat.Any();
			Expect.Call(doc.ID).Return(1).Repeat.Any();
			Expect.Call(doc.Title).Return(title).Repeat.Any();
			Expect.Call(doc.TypeTag).Return(typeTag).Repeat.Any();
			Expect.Call(doc.DateTime).Return(dateTime).Repeat.Any();

			Expect.Call(doc.Tokenize(title)).Return(Tools.Tokenize(title, WordLocation.Title)).Repeat.Any();
			Expect.Call(doc.Tokenize(null)).IgnoreArguments().Return(PlainTextDocumentWords).Repeat.Any();

			looseMocks.Replay(doc);

			return doc;
		}

		/// <summary>
		/// Mocks a document with a fixed content.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="title">The title.</param>
		/// <param name="typeTag">The type tag.</param>
		/// <param name="dateTime">The date/time.</param>
		/// <returns>The mocked document.</returns>
		public IDocument MockDocument2(string name, string title, string typeTag, DateTime dateTime) {
			IDocument doc = looseMocks.DynamicMock<IDocument>();
			Expect.Call(doc.Name).Return(name).Repeat.Any();
			Expect.Call(doc.ID).Return(2).Repeat.Any();
			Expect.Call(doc.Title).Return(title).Repeat.Any();
			Expect.Call(doc.TypeTag).Return(typeTag).Repeat.Any();
			Expect.Call(doc.DateTime).Return(dateTime).Repeat.Any();

			Expect.Call(doc.Tokenize(title)).Return(Tools.Tokenize(title, WordLocation.Title)).Repeat.Any();
			Expect.Call(doc.Tokenize(null)).IgnoreArguments().Return(PlainTextDocumentWords2).Repeat.Any();

			looseMocks.Replay(doc);

			return doc;
		}

		/// <summary>
		/// Mocks a document with a fixed content.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="title">The title.</param>
		/// <param name="typeTag">The type tag.</param>
		/// <param name="dateTime">The date/time.</param>
		/// <returns>The mocked document.</returns>
		public IDocument MockDocument3(string name, string title, string typeTag, DateTime dateTime) {
			IDocument doc = looseMocks.DynamicMock<IDocument>();
			Expect.Call(doc.Name).Return(name).Repeat.Any();
			Expect.Call(doc.ID).Return(3).Repeat.Any();
			Expect.Call(doc.Title).Return(title).Repeat.Any();
			Expect.Call(doc.TypeTag).Return(typeTag).Repeat.Any();
			Expect.Call(doc.DateTime).Return(dateTime).Repeat.Any();

			Expect.Call(doc.Tokenize(title)).Return(Tools.Tokenize(title, WordLocation.Title)).Repeat.Any();
			Expect.Call(doc.Tokenize(null)).IgnoreArguments().Return(PlainTextDocumentWords3).Repeat.Any();

			looseMocks.Replay(doc);

			return doc;
		}

		/// <summary>
		/// Mocks a document with a fixed content.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="title">The title.</param>
		/// <param name="typeTag">The type tag.</param>
		/// <param name="dateTime">The date/time.</param>
		/// <returns>The mocked document.</returns>
		public IDocument MockDocument4(string name, string title, string typeTag, DateTime dateTime) {
			IDocument doc = looseMocks.DynamicMock<IDocument>();
			Expect.Call(doc.Name).Return(name).Repeat.Any();
			Expect.Call(doc.ID).Return(4).Repeat.Any();
			Expect.Call(doc.Title).Return(title).Repeat.Any();
			Expect.Call(doc.TypeTag).Return(typeTag).Repeat.Any();
			Expect.Call(doc.DateTime).Return(dateTime).Repeat.Any();

			Expect.Call(doc.Tokenize(title)).Return(Tools.Tokenize(title, WordLocation.Title)).Repeat.Any();
			Expect.Call(doc.Tokenize(null)).IgnoreArguments().Return(PlainTextDocumentWords4).Repeat.Any();

			looseMocks.Replay(doc);

			return doc;
		}

		public uint FreeDocumentId = 1;
		public uint FreeWordId = 1;

		public void AutoHandlerForDocumentStorage(object sender, IndexChangedEventArgs e) {
			List<WordId> ids = new List<WordId>();
			if(e.ChangeData != null && e.ChangeData.Words != null) {
				foreach(DumpedWord w in e.ChangeData.Words) {
					ids.Add(new WordId(w.Text, FreeWordId));
					FreeWordId++;
				}
			}

			e.Result = new IndexStorerResult(FreeDocumentId, ids);
			FreeDocumentId++;
		}

	}

}
