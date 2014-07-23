
using System;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Contains extended information about a Page.
	/// </summary>
	public class ExtendedPageInfo {

		private PageInfo pageInfo;
		private string title, creator, lastAuthor;
		private DateTime modificationDateTime;
		private int messageCount;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:ExtendedPageInfo" /> class.
		/// </summary>
		/// <param name="pageInfo">The <see cref="T:PageInfo" /> object.</param>
		/// <param name="title">The title of the page.</param>
		/// <param name="modificationDateTime">The modification date/time.</param>
		/// <param name="creator">The creator.</param>
		/// <param name="lastAuthor">The last author.</param>
		public ExtendedPageInfo(PageInfo pageInfo, string title, DateTime modificationDateTime, string creator, string lastAuthor) {
			this.pageInfo = pageInfo;
			this.title = FormattingPipeline.PrepareTitle(title, false, FormattingContext.PageContent, pageInfo);
			this.modificationDateTime = modificationDateTime;
			this.creator = creator;
			this.lastAuthor = lastAuthor;
			this.messageCount = Pages.GetMessageCount(pageInfo);
		}

		/// <summary>
		/// Gets the PageInfo object.
		/// </summary>
		public PageInfo PageInfo {
			get { return pageInfo; }
		}

		/// <summary>
		/// Gets the title of the page.
		/// </summary>
		public string Title {
			get { return title; }
		}

		/// <summary>
		/// Gets the creation date/time.
		/// </summary>
		public DateTime CreationDateTime {
			get { return pageInfo.CreationDateTime; }
		}

		/// <summary>
		/// Gets the modification date/time.
		/// </summary>
		public DateTime ModificationDateTime {
			get { return modificationDateTime; }
		}

		/// <summary>
		/// Gets the creator.
		/// </summary>
		public string Creator {
			get { return creator; }
		}

		/// <summary>
		/// Gets the last author.
		/// </summary>
		public string LastAuthor {
			get { return lastAuthor; }
		}

		/// <summary>
		/// Gets the number of messages.
		/// </summary>
		public int MessageCount {
			get { return messageCount; }
		}

	}

}
