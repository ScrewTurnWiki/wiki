
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;
using System.Text;

namespace ScrewTurn.Wiki {

	public partial class MasterPageSA : System.Web.UI.MasterPage {

		private string currentNamespace = null;

		protected void Page_Load(object sender, EventArgs e) {
			// Try to detect current namespace
			currentNamespace = Tools.DetectCurrentNamespace();

			lblStrings.Text = string.Format("<script type=\"text/javascript\">\r\n<!--\r\n__BaseName = \"{0}\";\r\n__ConfirmMessage = \"{1}\";\r\n// -->\r\n</script>",
				CphMasterSA.ClientID + "_", Properties.Messages.ConfirmOperation);

			string nspace = currentNamespace;
			if(string.IsNullOrEmpty(nspace)) nspace = "";
			else nspace += ".";
			lnkMainPage.NavigateUrl = nspace + "Default.aspx";

			if(!Page.IsPostBack) {
				string referrer = Request.UrlReferrer != null ? Request.UrlReferrer.FixHost().ToString() : "";
				if(!string.IsNullOrEmpty(referrer)) {
					lnkPreviousPage.Visible = true;
					lnkPreviousPage.NavigateUrl = referrer;
				}
				else lnkPreviousPage.Visible = false;
			}

			PrintHtmlHead();
			PrintHeader();
			PrintFooter();
		}

		/// <summary>
		/// Prints the HTML head tag.
		/// </summary>
		public void PrintHtmlHead() {
			Literal c = new Literal();
			c.Text = Tools.GetIncludes(Tools.DetectCurrentNamespace());
			Page.Header.Controls.Add(c);
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
		/// Prints the header.
		/// </summary>
		public void PrintHeader() {
			string h = Content.GetPseudoCacheValue(GetPseudoCacheItemName("Header"));
			if(h == null) {
				h = FormattingPipeline.FormatWithPhase1And2(Settings.Provider.GetMetaDataItem(MetaDataItem.Header, currentNamespace),
					false, FormattingContext.Header, null);
				Content.SetPseudoCacheValue(GetPseudoCacheItemName("Header"), h);
			}
			lblHeaderDiv.Text = FormattingPipeline.FormatWithPhase3(h, FormattingContext.Header, null);
		}

		/// <summary>
		/// Prints the footer.
		/// </summary>
		public void PrintFooter() {
			string f = Content.GetPseudoCacheValue(GetPseudoCacheItemName("Footer"));
			if(f == null) {
				f = FormattingPipeline.FormatWithPhase1And2(Settings.Provider.GetMetaDataItem(MetaDataItem.Footer, currentNamespace),
					false, FormattingContext.Footer, null);
				Content.SetPseudoCacheValue(GetPseudoCacheItemName("Footer"), f);
			}
			lblFooterDiv.Text = FormattingPipeline.FormatWithPhase3(f, FormattingContext.Footer, null);
		}

	}

}
