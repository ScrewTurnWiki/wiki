
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.SearchEngine;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {
	
	/// <summary>
	/// Represents a page attachment document.
	/// </summary>
	public class PageAttachmentDocument : IDocument {

		/// <summary>
		/// The type tag for a <see cref="T:PageAttachmentDocument" />.
		/// </summary>
		public const string StandardTypeTag = "A";

		private uint id;
		private string name;
		private string title;
		private string typeTag = StandardTypeTag;
		private DateTime dateTime;
		private PageInfo page;
		private string provider;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:PageAttachmentDocument" /> class.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="name">The attachment name.</param>
		/// <param name="provider">The file provider.</param>
		/// <param name="dateTime">The modification date/time.</param>
		public PageAttachmentDocument(PageInfo page, string name, string provider, DateTime dateTime) {
			if(page == null) throw new ArgumentNullException("page");
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");
			if(provider == null) throw new ArgumentNullException("provider");
			if(provider.Length == 0) throw new ArgumentException("Provider cannot be empty", "provider");

			this.name = page.FullName + "|" + provider + "|" + name;
			id = Tools.HashDocumentNameForTemporaryIndex(this.name);
			title = name;
			this.dateTime = dateTime;
			this.page = page;
			this.provider = provider;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:PageAttachmentDocument" /> class.
		/// </summary>
		/// <param name="doc">The dumped document.</param>
		public PageAttachmentDocument(DumpedDocument doc) {
			string[] fields = doc.Name.Split('|');

			id = doc.ID;
			name = doc.Name;
			title = doc.Title;
			dateTime = doc.DateTime;
			provider = fields[0];
			page = Pages.FindPage(fields[1]);
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
		/// <returns>The extracted words and their positions (always an empty array).</returns>
		public WordInfo[] Tokenize(string content) {
			return ScrewTurn.Wiki.SearchEngine.Tools.Tokenize(content);
		}

		/// <summary>
		/// Gets the page.
		/// </summary>
		public PageInfo Page {
			get { return page; }
		}

		/// <summary>
		/// Gets the provider.
		/// </summary>
		public string Provider {
			get { return provider; }
		}

	}

}
