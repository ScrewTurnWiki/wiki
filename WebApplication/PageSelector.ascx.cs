using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ScrewTurn.Wiki {

	public partial class PageSelector : System.Web.UI.UserControl {

		private const string ItemCountName = "IC";
		private const string PageSizeName = "PS";
		private const string SelectedPageName = "SP";

		private int itemCount = 0;
		private int pageSize = 50;
		private int selectedPage = 0;

		protected PageSelector() {
			pageSize = Settings.ListSize;
		}

		protected void Page_Load(object sender, EventArgs e) {
			// Load data from ViewState
			object temp = ViewState[ItemCountName];
			if(temp != null) {
				itemCount = (int)temp;
			}
			temp = ViewState[PageSizeName];
			if(temp != null) {
				pageSize = (int)temp;
			}
			temp = ViewState[SelectedPageName];
			if(temp != null) {
				selectedPage = (int)temp;
			}

			RenderPages();
		}

		/// <summary>
		/// Gets or sets the item count.
		/// </summary>
		public int ItemCount {
			get { return itemCount; }
			set {
				if(value < 0) throw new ArgumentException("Item Count must be greater than or equal to zero", "value");
				itemCount = value;
				ViewState[ItemCountName] = value;
				RenderPages();
			}
		}

		/// <summary>
		/// Gets or sets the page size.
		/// </summary>
		public int PageSize {
			get { return pageSize; }
			set {
				if(value <= 0) throw new ArgumentException("Page Size must be greater than zero", "value");
				pageSize = value;
				ViewState[PageSizeName] = value;
				RenderPages();
			}
		}

		/// <summary>
		/// Gets or sets the selected page (0..N).
		/// </summary>
		public int SelectedPage {
			get { return selectedPage; }
			private set {
				int dummy;
				if(value < 0 || value > CountPages(out dummy) - 1) throw new ArgumentException("Invalid Page", "value");
				selectedPage = value;
				ViewState[SelectedPageName] = value;
			}
		}

		/// <summary>
		/// Gets the size of the selected page.
		/// </summary>
		public int SelectedPageSize {
			get {
				int pageCount;
				int lastPageSize;

				pageCount = CountPages(out lastPageSize);

				int selectedPageItemCount = (selectedPage == pageCount - 1) ? lastPageSize : pageSize;

				return selectedPageItemCount;
			}
		}

		/// <summary>
		/// Gets the number of pages needed to display the current items.
		/// </summary>
		/// <param name="lastPageSize">The size of the last page.</param>
		/// <returns>The number of pages.</returns>
		private int CountPages(out int lastPageSize) {
			int pageCount = itemCount / pageSize;
			lastPageSize = itemCount - (pageCount * pageSize);

			return pageCount + 1;
		}

		/// <summary>
		/// Renders the pages.
		/// </summary>
		private void RenderPages() {
			int pageCount;
			int lastPageSize;
			pageCount = CountPages(out lastPageSize);

			List<ItemBlockRow> result = new List<ItemBlockRow>(pageCount);
			for(int i = 0; i < pageCount - 1; i++) {
				result.Add(new ItemBlockRow(i, i * pageSize, pageSize, selectedPage == i));
			}
			if(itemCount > (pageCount - 1) * pageSize) {
				result.Add(new ItemBlockRow(pageCount - 1, (pageCount - 1) * pageSize, lastPageSize, selectedPage == pageCount - 1));
			}

			// Don't display anything if there is only one page
			if(result.Count > 1) {
				rptPages.DataSource = result;
			}
			else {
				rptPages.DataSource = new List<ItemBlockRow>();
			}
			rptPages.DataBind();
		}

		/// <summary>
		/// Selects a page.
		/// </summary>
		/// <param name="page">The page (0..N).</param>
		public void SelectPage(int page) {
			rptPages_ItemCommand(this, new CommandEventArgs("Select", page.ToString()));
		}

		protected void rptPages_ItemCommand(object sender, CommandEventArgs e) {
			if(e.CommandName == "Select") {
				int selectedPage = int.Parse((string)e.CommandArgument);

				int pageCount;
				int lastPageSize;
				pageCount = CountPages(out lastPageSize);

				int selectedPageItemCount = (selectedPage == pageCount - 1) ? lastPageSize : pageSize;

				SelectedPage = selectedPage;

				RenderPages();

				if(SelectedPageChanged != null) {
					SelectedPageChanged(this, new SelectedPageChangedEventArgs(selectedPage, selectedPageItemCount));
				}
			}
		}

		/// <summary>
		/// Event fired when the selected page has changed.
		/// </summary>
		public event EventHandler<SelectedPageChangedEventArgs> SelectedPageChanged;

	}

	/// <summary>
	/// Represents a page of items for display purposes.
	/// </summary>
	public class ItemBlockRow {

		private string page;
		private string text;
		private bool selected;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:ItemBlockRow" /> class.
		/// </summary>
		/// <param name="page">The page number.</param>
		/// <param name="begin">The number of the first item</param>
		/// <param name="size">The number of items.</param>
		/// <param name="selected">A value indicating whether the page is selected.</param>
		public ItemBlockRow(int page, int begin, int size, bool selected) {
			this.page = page.ToString();
			this.text = (begin + 1).ToString() + "-" + (begin + size).ToString();
			this.selected = selected;
		}

		/// <summary>
		/// Gets the page.
		/// </summary>
		public string Page {
			get { return page; }
		}

		/// <summary>
		/// Gets the text.
		/// </summary>
		public string Text {
			get { return text; }
		}

		/// <summary>
		/// Gets a value indicating whether the page is selected.
		/// </summary>
		public bool Selected {
			get { return selected; }
		}

	}

	/// <summary>
	/// Contains arguments for the SelectedPageChanged event.
	/// </summary>
	public class SelectedPageChangedEventArgs : EventArgs {

		private int selectedPage;
		private int itemCount;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:PageChangedEventArgs" /> class.
		/// </summary>
		/// <param name="selectedPage">The sepected page (0..N).</param>
		/// <param name="itemCount">The number of items in the page.</param>
		public SelectedPageChangedEventArgs(int selectedPage, int itemCount) {
			this.selectedPage = selectedPage;
			this.itemCount = itemCount;
		}

		/// <summary>
		/// Gets the selected page (0..N).
		/// </summary>
		public int SelectedPage {
			get { return selectedPage; }
		}

		/// <summary>
		/// Gets the item count.
		/// </summary>
		public int ItemCount {
			get { return itemCount; }
		}

	}

}
