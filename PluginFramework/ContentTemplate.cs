
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// Contains a template for page content.
	/// </summary>
	public class ContentTemplate {

		/// <summary>
		/// The name of the template.
		/// </summary>
		protected string name;
		/// <summary>
		/// The content of the template.
		/// </summary>
		protected string content;
		/// <summary>
		/// The provider handling the template.
		/// </summary>
		protected IPagesStorageProviderV30 provider;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:ContentTemplate" /> class.
		/// </summary>
		/// <param name="name">The name of the template.</param>
		/// <param name="content">The content of the template.</param>
		/// <param name="provider">The provider handling the template.</param>
		public ContentTemplate(string name, string content, IPagesStorageProviderV30 provider) {
			this.name = name;
			this.content = content;
			this.provider = provider;
		}

		/// <summary>
		/// Gets the name of the template.
		/// </summary>
		public string Name {
			get { return name; }
		}

		/// <summary>
		/// Gets the content of the template.
		/// </summary>
		public string Content {
			get { return content; }
		}

		/// <summary>
		/// Gets the provider handling the template.
		/// </summary>
		public IPagesStorageProviderV30 Provider {
			get { return provider; }
		}

	}

	/// <summary>
	/// Compares two <see cref="T:ContentTemplate" /> objects.
	/// </summary>
	public class ContentTemplateNameComparer : IComparer<ContentTemplate> {

		/// <summary>
		/// Compares the name of two <see cref="T:ContentTemplate" /> objects.
		/// </summary>
		/// <param name="x">The first <see cref="T:ContentTemplate" />.</param>
		/// <param name="y">The second <see cref="T:ContentTemplate" />.</param>
		/// <returns>The result of the comparison (1, 0 or -1).</returns>
		public int Compare(ContentTemplate x, ContentTemplate y) {
			return StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);
		}

	}

}
