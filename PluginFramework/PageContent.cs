
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// Contains the Content of a Page.
	/// </summary>
	public class PageContent {

		/// <summary>
		/// The PageInfo object.
		/// </summary>
		protected PageInfo pageInfo;
		/// <summary>
		/// The Title of the Page.
		/// </summary>
		protected string title;
		/// <summary>
		/// The Username of the user who modified the Page.
		/// </summary>
		protected string user;
		/// <summary>
		/// The Date/Time of the last modification.
		/// </summary>
		protected DateTime lastModified;
		/// <summary>
		/// The comment of the editor, about this revision.
		/// </summary>
		protected string comment;
		/// <summary>
		/// The Content of the Page (WikiMarkup).
		/// </summary>
		protected string content;
		/// <summary>
		/// The keywords, usually used for SEO.
		/// </summary>
		protected string[] keywords;
		/// <summary>
		/// The page description, usually used for SEO.
		/// </summary>
		protected string description;
		/// <summary>
		/// The Pages linked in this Page (both existent and inexistent).
		/// </summary>
		protected string[] linkedPages = new string[0];

		/// <summary>
		/// Initializes a new instance of the <see cref="T:PageContent"/> class.
		/// </summary>
		/// <param name="pageInfo">The PageInfo object.</param>
		/// <param name="title">The Title.</param>
		/// <param name="user">The User that last modified the Page.</param>
		/// <param name="lastModified">The last modification Date and Time.</param>
		/// <param name="comment">The Comment of the editor, about this revision.</param>
		/// <param name="content">The <b>unparsed</b> Content.</param>
		/// <param name="keywords">The keywords, usually used for SEO, or <c>null</c>.</param>
		/// <param name="description">The description, usually used for SEO, or <c>null</c>.</param>
		public PageContent(PageInfo pageInfo, string title, string user, DateTime lastModified, string comment, string content,
			string[] keywords, string description) {

			this.pageInfo = pageInfo;
			this.title = title;
			this.user = user;
			this.lastModified = lastModified;
			this.content = content;
			this.comment = comment;
			this.keywords = keywords != null ? keywords : new string[0];
			this.description = description;
		}

		/// <summary>
		/// Gets the PageInfo.
		/// </summary>
		public PageInfo PageInfo {
			get { return pageInfo; }
		}

		/// <summary>
		/// Gets the Title.
		/// </summary>
		public string Title {
			get { return title; }
		}

		/// <summary>
		/// Gets the User.
		/// </summary>
		public string User {
			get { return user; }
		}

		/// <summary>
		/// Gets the last modification Date and Time.
		/// </summary>
		public DateTime LastModified {
			get { return lastModified; }
		}

		/// <summary>
		/// Gets the Comment of the editor, about this revision.
		/// </summary>
		public string Comment {
			get { return comment; }
		}

		/// <summary>
		/// Gets the <b>unformatted</b> Content.
		/// </summary>
		public string Content {
			get { return content; }
		}

		/// <summary>
		/// Gets the keywords, usually used for SEO.
		/// </summary>
		public string[] Keywords {
			get { return keywords; }
		}

		/// <summary>
		/// Gets the description, usually used for SEO.
		/// </summary>
		public string Description {
			get { return description; }
		}

		/// <summary>
		/// Gets or sets the Linked Pages, both existent and inexistent.
		/// </summary>
		public string[] LinkedPages {
			get { return linkedPages; }
			set { linkedPages = value; }
		}

		/// <summary>
		/// Determines whether the current instance was built using <see cref="M:GetEmpty"/>.
		/// </summary>
		/// <returns><c>True</c> if the instance is empty, <c>false</c> otherwise.</returns>
		public bool IsEmpty() {
			return this is EmptyPageContent;
		}

		/// <summary>
		/// Gets an empty instance of <see cref="T:PageContent" />.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <returns>The instance.</returns>
		public static PageContent GetEmpty(PageInfo page) {
			return new EmptyPageContent(page);
		}

		/// <summary>
		/// Represents an empty page content.
		/// </summary>
		private class EmptyPageContent : PageContent {

			/// <summary>
			/// Initializes a new instance of the <see cref="T:EmptyPageContent"/> class.
			/// </summary>
			/// <param name="page">The page the content refers to.</param>
			public EmptyPageContent(PageInfo page)
				: base(page, "", "", DateTime.MinValue, "", "", null, "") { }

		}

	}

}
