
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.SearchEngine;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// Represents a message for use with the search engine.
	/// </summary>
	public class MessageDocument : IDocument {

		/// <summary>
		/// The type tag for a <see cref="T:MessageDocument" />.
		/// </summary>
		public const string StandardTypeTag = "M";

		/// <summary>
		/// Gets the document name for a message.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="messageID">The message ID.</param>
		/// <returns>The document name.</returns>
		public static string GetDocumentName(PageInfo page, int messageID) {
			if(page == null) throw new ArgumentNullException("page");
			return page.FullName + "..." + messageID.ToString();
		}

		/// <summary>
		/// Gets the page name and message ID from a document name.
		/// </summary>
		/// <param name="documentName">The document name.</param>
		/// <param name="pageName">The page name.</param>
		/// <param name="messageID">The message ID.</param>
		public static void GetMessageDetails(string documentName, out string pageName, out int messageID) {
			if(documentName == null) throw new ArgumentNullException("documentName");
			if(documentName.Length == 0) throw new ArgumentException("Document Name cannot be empty", "documentName");

			int lastThreeDotsIndex = documentName.LastIndexOf("...");
			if(lastThreeDotsIndex == -1) throw new ArgumentException("Document Name has an invalid format", "documentName");

			pageName = documentName.Substring(0, lastThreeDotsIndex);
			messageID = int.Parse(documentName.Substring(lastThreeDotsIndex + 3));
		}

		private uint id;
		private string name, title, typeTag;
		private DateTime dateTime;
		private int messageID;
		private PageInfo pageInfo;

		private Tokenizer tokenizer;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:MessageDocument" /> class.
		/// </summary>
		/// <param name="pageInfo">The page.</param>
		/// <param name="messageID">The message ID.</param>
		/// <param name="dumpedDocument">The dumped document data.</param>
		/// <param name="tokenizer">The tokenizer.</param>
		public MessageDocument(PageInfo pageInfo, int messageID, DumpedDocument dumpedDocument, Tokenizer tokenizer) {
			if(dumpedDocument == null) throw new ArgumentNullException("dumpedDocument");
			if(tokenizer == null) throw new ArgumentNullException("tokenizer");

			this.pageInfo = pageInfo;
			this.messageID = messageID;
			id = dumpedDocument.ID;
			name = dumpedDocument.Name;
			typeTag = dumpedDocument.TypeTag;
			title = dumpedDocument.Title;
			dateTime = dumpedDocument.DateTime;
			this.tokenizer = tokenizer;
		}

		/// <summary>
		/// Gets or sets the globally unique ID of the document.
		/// </summary>
		public uint ID {
			get { return id; }
			set { id = value; }
		}

		/// <summary>
		/// Gets the globally-unique name of the document.
		/// </summary>
		public string Name {
			get { return name; }
		}

		/// <summary>
		/// Gets the title of the document, if any.
		/// </summary>
		public string Title {
			get { return title; }
		}

		/// <summary>
		/// Gets the tag for the document type.
		/// </summary>
		public string TypeTag {
			get { return typeTag; }
		}

		/// <summary>
		/// Gets the document date/time.
		/// </summary>
		public DateTime DateTime {
			get { return dateTime; }
		}

		/// <summary>
		/// Performs the tokenization of the document content.
		/// </summary>
		/// <param name="content">The content to tokenize.</param>
		/// <returns>The extracted words and their positions.</returns>
		public WordInfo[] Tokenize(string content) {
			return tokenizer(content);
		}

		/// <summary>
		/// Gets the message ID.
		/// </summary>
		public int MessageID {
			get { return messageID; }
		}

		/// <summary>
		/// Gets the page information.
		/// </summary>
		public PageInfo PageInfo {
			get { return pageInfo; }
		}

	}

}
