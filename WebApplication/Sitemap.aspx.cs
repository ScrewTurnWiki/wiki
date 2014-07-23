
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using ScrewTurn.Wiki.PluginFramework;
using System.Text;

namespace ScrewTurn.Wiki {

	public partial class Sitemap : System.Web.UI.Page {

		protected void Page_Load(object sender, EventArgs e) {
			Response.ClearContent();
			Response.ContentType = "text/xml;charset=UTF-8";
			Response.ContentEncoding = System.Text.UTF8Encoding.UTF8;

			string mainUrl = Settings.MainUrl;
			string rootDefault = Settings.DefaultPage.ToLowerInvariant();

			using(XmlWriter writer = XmlWriter.Create(Response.OutputStream)) {
				writer.WriteStartDocument();

				writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
				writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
				writer.WriteAttributeString("xsi", "schemaLocation", null, "http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/09/sitemap.xsd");

				string user = SessionFacade.GetCurrentUsername();
				string[] groups = SessionFacade.GetCurrentGroupNames();

				foreach(PageInfo page in Pages.GetPages(null)) {
					if(AuthChecker.CheckActionForPage(page, Actions.ForPages.ReadPage, user, groups)) {
						WritePage(mainUrl, page, page.FullName.ToLowerInvariant() == rootDefault, writer);
					}
				}
				foreach(NamespaceInfo nspace in Pages.GetNamespaces()) {
					string nspaceDefault = nspace.DefaultPage.FullName.ToLowerInvariant();

					foreach(PageInfo page in Pages.GetPages(nspace)) {
						if(AuthChecker.CheckActionForPage(page, Actions.ForPages.ReadPage, user, groups)) {
							WritePage(mainUrl, page, page.FullName.ToLowerInvariant() == nspaceDefault, writer);
						}
					}
				}

				writer.WriteEndElement();
				writer.WriteEndDocument();
			}
		}

		/// <summary>
		/// Writes a page to the output XML writer.
		/// </summary>
		/// <param name="mainUrl">The main wiki URL.</param>
		/// <param name="page">The page.</param>
		/// <param name="isDefault">A value indicating whether the page is the default of its namespace.</param>
		/// <param name="writer">The writer.</param>
		private void WritePage(string mainUrl, PageInfo page, bool isDefault, XmlWriter writer) {
			writer.WriteStartElement("url");
			writer.WriteElementString("loc", mainUrl + Tools.UrlEncode(page.FullName) + Settings.PageExtension);
			writer.WriteElementString("priority", isDefault ? "0.75" : "0.5");
			writer.WriteElementString("changefreq", "daily");
			writer.WriteEndElement();
		}

	}

}
