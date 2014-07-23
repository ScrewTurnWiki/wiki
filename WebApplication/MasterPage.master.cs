
using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	public partial class MasterPage : System.Web.UI.MasterPage {

		private string currentNamespace = null;
		private PageInfo currentPage = null;

		protected void Page_Load(object sender, EventArgs e) {
			// Try to detect current namespace and page
			currentNamespace = Tools.DetectCurrentNamespace();
			currentPage = Tools.DetectCurrentPageInfo(true);

			lblStrings.Text = string.Format("<script type=\"text/javascript\">\r\n<!--\r\n__BaseName = \"{0}\";\r\n__ConfirmMessage = \"{1}\";\r\n// -->\r\n</script>",
				CphMaster.ClientID + "_", Properties.Messages.ConfirmOperation);

			PrintHtmlHead();
			PrintHeader();
			PrintSidebar();
			PrintFooter();
			PrintPageHeaderAndFooter();
		}

		/// <summary>
		/// Gets the pseudo-cache item name based on the current namespace.
		/// </summary>
		/// <param name="name">The item name.</param>
		/// <returns>The namespace-qualified item name.</returns>
		private string GetPseudoCacheItemName(string name) {
			if(string.IsNullOrEmpty(currentNamespace)) return name;
			else return currentNamespace + "." + name;
		}

		/// <summary>
		/// Prints the page header and page footer.
		/// </summary>
		public void PrintPageHeaderAndFooter() {
			string h = Content.GetPseudoCacheValue(GetPseudoCacheItemName("PageHeader"));
			if(h == null) {
				h = Settings.Provider.GetMetaDataItem(MetaDataItem.PageHeader, currentNamespace);
				h = @"<div id=""PageInternalHeaderDiv"">" + FormattingPipeline.FormatWithPhase1And2(h, false, FormattingContext.PageHeader, currentPage) + "</div>";
				Content.SetPseudoCacheValue(GetPseudoCacheItemName("PageHeader"), h);
			}
			lblPageHeaderDiv.Text = FormattingPipeline.FormatWithPhase3(h, FormattingContext.PageHeader, currentPage);

			h = Content.GetPseudoCacheValue(GetPseudoCacheItemName("PageFooter"));
			if(h == null) {
				h = Settings.Provider.GetMetaDataItem(MetaDataItem.PageFooter, currentNamespace);
				h = @"<div id=""PageInternalFooterDiv"">" + FormattingPipeline.FormatWithPhase1And2(h, false, FormattingContext.PageFooter, currentPage) + "</div>";
				Content.SetPseudoCacheValue(GetPseudoCacheItemName("PageFooter"), h);
			}
			lblPageFooterDiv.Text = FormattingPipeline.FormatWithPhase3(h, FormattingContext.PageFooter, currentPage);
		}

		/// <summary>
		/// Prints the HTML head tag.
		/// </summary>
		public void PrintHtmlHead() {
			string h = Content.GetPseudoCacheValue(GetPseudoCacheItemName("Head"));
			if(h == null) {
				StringBuilder sb = new StringBuilder(100);

				if(Settings.RssFeedsMode != RssFeedsMode.Disabled) {
					sb.AppendFormat(@"<link rel=""alternate"" title=""{0}"" href=""{1}######______NAMESPACE______######RSS.aspx"" type=""application/rss+xml"" />",
						Settings.WikiTitle, Settings.MainUrl);
					sb.Append("\n");
					sb.AppendFormat(@"<link rel=""alternate"" title=""{0}"" href=""{1}######______NAMESPACE______######RSS.aspx?Discuss=1"" type=""application/rss+xml"" />",
						Settings.WikiTitle + " - Discussions", Settings.MainUrl);
					sb.Append("\n");
				}

				sb.Append("######______INCLUDES______######");
				h = sb.ToString();
				Content.SetPseudoCacheValue(GetPseudoCacheItemName("Head"), h);
			}

			// Use a Control to allow 3rd party plugins to programmatically access the Page header
			string nspace = currentNamespace;
			if(nspace == null) nspace = "";
			else if(nspace.Length > 0) nspace += ".";

			Literal c = new Literal();
			c.Text = h.Replace("######______INCLUDES______######", Tools.GetIncludes(currentNamespace)).Replace("######______NAMESPACE______######", nspace);
			Page.Header.Controls.Add(c);
		}

		/// <summary>
		/// Prints the header.
		/// </summary>
		public void PrintHeader() {
			string h = Content.GetPseudoCacheValue(GetPseudoCacheItemName("Header"));
			if(h == null) {
				h = FormattingPipeline.FormatWithPhase1And2(Settings.Provider.GetMetaDataItem(MetaDataItem.Header, currentNamespace),
					false, FormattingContext.Header, currentPage);
				Content.SetPseudoCacheValue(GetPseudoCacheItemName("Header"), h);
			}
			lblHeaderDiv.Text = FormattingPipeline.FormatWithPhase3(h, FormattingContext.Header, currentPage);
		}

		/// <summary>
		/// Prints the sidebar.
		/// </summary>
		public void PrintSidebar() {
			string s = Content.GetPseudoCacheValue(GetPseudoCacheItemName("Sidebar"));
			if(s == null) {
				s = FormattingPipeline.FormatWithPhase1And2(Settings.Provider.GetMetaDataItem(MetaDataItem.Sidebar, currentNamespace),
					false, FormattingContext.Sidebar, currentPage);
				Content.SetPseudoCacheValue(GetPseudoCacheItemName("Sidebar"), s);
			}
			lblSidebarDiv.Text = FormattingPipeline.FormatWithPhase3(s, FormattingContext.Sidebar, currentPage);
		}

		/// <summary>
		/// Prints the footer.
		/// </summary>
		public void PrintFooter() {
			string f = Content.GetPseudoCacheValue(GetPseudoCacheItemName("Footer"));
			if(f == null) {
				f = FormattingPipeline.FormatWithPhase1And2(Settings.Provider.GetMetaDataItem(MetaDataItem.Footer, currentNamespace),
					false, FormattingContext.Footer, currentPage);
				Content.SetPseudoCacheValue(GetPseudoCacheItemName("Footer"), f);
			}
			lblFooterDiv.Text = FormattingPipeline.FormatWithPhase3(f, FormattingContext.Footer, currentPage);
		}

	}

}
