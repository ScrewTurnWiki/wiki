
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.SearchEngine;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Represents a file document.
	/// </summary>
	public class FileDocument : IDocument {

		/// <summary>
		/// The type tag for a <see cref="T:FileDocument" />.
		/// </summary>
		public const string StandardTypeTag = "F";

		private uint id;
		private string name;
		private string title;
		private string typeTag = StandardTypeTag;
		private DateTime dateTime;
		private string provider;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:FileDocument" /> class.
		/// </summary>
		/// <param name="fullName">The file full name.</param>
		/// <param name="provider">The file provider.</param>
		/// <param name="dateTime">The modification date/time.</param>
		public FileDocument(string fullName, string provider, DateTime dateTime) {
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(fullName.Length == 0) throw new ArgumentException("Full Name cannot be empty", "fullName");
			if(provider == null) throw new ArgumentNullException("provider");
			if(provider.Length == 0) throw new ArgumentException("Provider cannot be empty", "provider");

			id = Tools.HashDocumentNameForTemporaryIndex(fullName);
			name = provider + "|" + fullName;
			title = fullName.Substring(Tools.GetDirectoryName(fullName).Length);
			this.dateTime = dateTime;
			this.provider = provider;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:FileDocument" /> class.
		/// </summary>
		/// <param name="doc">The dumped document.</param>
		public FileDocument(DumpedDocument doc) {
			string[] fields = doc.Name.Split('|');

			id = doc.ID;
			name = doc.Name;
			title = doc.Title;
			dateTime = doc.DateTime;
			provider = fields[0];
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
		/// Gets the provider.
		/// </summary>
		public string Provider {
			get { return provider; }
		}

	}

}
