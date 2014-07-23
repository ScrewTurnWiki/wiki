
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.SearchEngine {
	
	/// <summary>
	/// Defines the interface a generic document.
	/// </summary>
	public interface IDocument {

		/// <summary>
		/// Gets or sets the globally unique ID of the document.
		/// </summary>
		uint ID { get; set; }

		/// <summary>
		/// Gets the globally-unique name of the document.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the title of the document, if any.
		/// </summary>
		string Title { get; }

		/// <summary>
		/// Gets the tag for the document type.
		/// </summary>
		string TypeTag { get; }

		/// <summary>
		/// Gets the document date/time.
		/// </summary>
		DateTime DateTime { get; }

		/// <summary>
		/// Performs the tokenization of the document content.
		/// </summary>
		/// <param name="content">The content to tokenize.</param>
		/// <returns>The extracted words and their positions.</returns>
		WordInfo[] Tokenize(string content);

	}

}
