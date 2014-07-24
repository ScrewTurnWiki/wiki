
using System;
using ScrewTurn.Wiki.SearchEngine;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki
{

	/// <summary>
	/// Represents a page attachment document.
	/// </summary>
	public class PageAttachmentDocument : IDocument
	{

		/// <summary>
		/// The type tag for a <see cref="T:PageAttachmentDocument" />.
		/// </summary>
		public const string StandardTypeTag = "A";

		/// <summary>
		/// Initializes a new instance of the <see cref="T:PageAttachmentDocument" /> class.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="name">The attachment name.</param>
		/// <param name="provider">The file provider.</param>
		/// <param name="dateTime">The modification date/time.</param>
		public PageAttachmentDocument( PageInfo page, string name, string provider, DateTime dateTime )
		{
			if ( page == null ) throw new ArgumentNullException( "page" );
			if ( name == null ) throw new ArgumentNullException( "name" );
			if ( name.Length == 0 ) throw new ArgumentException( "Name cannot be empty", "name" );
			if ( provider == null ) throw new ArgumentNullException( "provider" );
			if ( provider.Length == 0 ) throw new ArgumentException( "Provider cannot be empty", "provider" );

			Name = page.FullName + "|" + provider + "|" + name;
			ID = Tools.HashDocumentNameForTemporaryIndex( Name );
			Title = name;
			DateTime = dateTime;
			Page = page;
			Provider = provider;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:PageAttachmentDocument" /> class.
		/// </summary>
		/// <param name="doc">The dumped document.</param>
		public PageAttachmentDocument( DumpedDocument doc )
		{
			string[ ] fields = doc.Name.Split( '|' );

			ID = doc.ID;
			Name = doc.Name;
			Title = doc.Title;
			DateTime = doc.DateTime;
			Provider = fields[ 0 ];
			Page = Pages.FindPage( fields[ 1 ] );
		}

		/// <summary>
		/// Gets or sets the globally unique ID of the document.
		/// </summary>
		public uint ID { get; set; }

		/// <summary>
		/// Gets the globally-unique name of the document.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the title of the document, if any.
		/// </summary>
		public string Title { get; private set; }

		/// <summary>
		/// Gets the tag for the document type.
		/// </summary>
		public string TypeTag { get { return "A"; } }

		/// <summary>
		/// Gets the document date/time.
		/// </summary>
		public DateTime DateTime { get; private set; }

		/// <summary>
		/// Performs the tokenization of the document content.
		/// </summary>
		/// <param name="content">The content to tokenize.</param>
		/// <returns>The extracted words and their positions (always an empty array).</returns>
		public WordInfo[ ] Tokenize( string content )
		{
			return SearchEngine.Tools.Tokenize( content );
		}

		/// <summary>
		/// Gets the page.
		/// </summary>
		public PageInfo Page { get; private set; }

		/// <summary>
		/// Gets the provider.
		/// </summary>
		public string Provider { get; private set; }
	}

}
