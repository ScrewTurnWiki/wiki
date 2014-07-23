
using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// Contains a Snippet.
	/// </summary>
	public class Snippet {

		/// <summary>
		/// The name of the Snippet.
		/// </summary>
		protected string name;
		/// <summary>
		/// The content of the Snippet.
		/// </summary>
		protected string content;
		/// <summary>
		/// The Provider that handles the Snippet.
		/// </summary>
		protected IPagesStorageProviderV30 provider;

		/// <summary>
		/// Initializes a new instance of the <b>Snippet</b> class.
		/// </summary>
		/// <param name="name">The Name of the Snippet.</param>
		/// <param name="content">The Content of the Snippet.</param>
		/// <param name="provider">The Provider of the Snippet.</param>
		public Snippet(string name, string content, IPagesStorageProviderV30 provider) {
			this.name = name;
			this.content = content;
			this.provider = provider;
		}

		/// <summary>
		/// Gets the Name of the Snippet.
		/// </summary>
		public string Name {
			get { return name; }
		}

		/// <summary>
		/// Gets or sets the Content of the Snippet.
		/// </summary>
		public string Content {
			get { return content; }
		}

		/// <summary>
		/// Gets the Provider of the Snippet.
		/// </summary>
		public IPagesStorageProviderV30 Provider {
			get { return provider; }
		}

	}

	/// <summary>
	/// Compares two <see cref="T:Snippet" /> objects.
	/// </summary>
	public class SnippetNameComparer : IComparer<Snippet> {

		/// <summary>
		/// Compares the name of two <see cref="T:Snippet" /> objects.
		/// </summary>
		/// <param name="x">The first <see cref="T:Snippet" />.</param>
		/// <param name="y">The second <see cref="T:Snippet" />.</param>
		/// <returns>The result of the comparison (1, 0 or -1).</returns>
		public int Compare(Snippet x, Snippet y) {
			return StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);
		}

	}

}
