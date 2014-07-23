using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Net;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using ScrewTurn.Wiki.ImportWiki;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	public partial class Import : BasePage {

		private HttpWebRequest request;
		private delegate void delegatePageDownload();

		protected void Page_Load(object sender, EventArgs e) {
			if(Array.Find(SessionFacade.GetCurrentGroupNames(), delegate(string g) { return g == Settings.AdministratorsGroup; }) == null) {
				UrlTools.Redirect("AccessDenied.aspx");
			}

			Page.Title = "Import - " + Settings.WikiTitle;
		}

		protected void lstOperation_SelectedIndexChanged(object sender, EventArgs e) {
			switch(lstOperation.SelectedValue.ToUpperInvariant()) {
				case "PAGE":
					mlwImport.ActiveViewIndex = 0;
					break;
				case "WIKI":
					mlwImport.ActiveViewIndex = 1;
					break;
				case "TEXT":
					mlwImport.ActiveViewIndex = 2;
					break;
			}
		}

		protected void lstWiki_SelectedIndexChanged(object sender, EventArgs e) {
			switch(lstWiki.SelectedValue.ToUpperInvariant()) {
				case "MEDIA":
					lblWikiUrl.Text = "Wiki URL, in the form http://www.yourserver.com/w/index.php";
					lblPageUrl.Text = "Wiki URL, in the form http://www.yourserver.com/w/index.php";
					break;
				case "FLEX":
					lblWikiUrl.Text = "Wiki URL, in the form http://www.yourserver.com/";
					lblPageUrl.Text = "Wiki URL, in the form http://www.yourserver.com/";
					break;
			}
		}

		protected void btnGo_click(object sender, EventArgs e) {
			switch(lstOperation.SelectedValue.ToUpperInvariant()) {
				case "PAGE":
					PageAsyncTask task = new PageAsyncTask(
						new BeginEventHandler(BeginPageRequest),
						new EndEventHandler(EndPageRequest),
						new EndEventHandler(TimeoutPageRequest),
						null);
					RegisterAsyncTask(task);
					break;
				case "WIKI":
					AddOnPreRenderCompleteAsync(
						new BeginEventHandler(BeginPagesListRequest),
						new EndEventHandler(EndPagesListRequest)
					);
					break;
				case "TEXT":
					mlwImport.ActiveViewIndex = 3;
					ITranslator translator = new Translator();
					txtTranslated.Text = translator.Translate(txtText.Text);
					break;
			}
		}

		private string PageRequest(string url) {
			request = (HttpWebRequest)WebRequest.Create(url);
			SetProxyAndUserAgent(request);
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			StreamReader reader = new StreamReader(response.GetResponseStream());
			return reader.ReadToEnd();
		}

		private void SetProxyAndUserAgent(HttpWebRequest req) {
			req.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.8.0.7) Gecko/20060909 Firefox/1.5.0.7";
			string addr = null;
			int port = -1;
			if(txtProxyAddress.Text.Length > 0) addr = txtProxyAddress.Text;
			if(txtProxyPort.Text.Length > 0) port = int.Parse(txtProxyPort.Text);

			if(addr != null) {
				if(port > 0) req.Proxy = new WebProxy(addr, port);
				else req.Proxy = new WebProxy(addr);
			}
			else req.Proxy = null;
		}

		private void savePage(string text) {
			string pageName = txtPageName.Text.Replace(":", "_").Replace("/", "_").Replace(@"\", "_").Replace('?', '_');
			string pageTitle = txtPageName.Text;
			Log.LogEntry("Page " + pageName + " created with import whole wiki", EntryType.General, "import");

			PageInfo pg = Pages.FindPage(pageName);
			SaveMode saveMode = SaveMode.Backup;
			if(pg == null) {
				Pages.CreatePage(null as string, pageName);
				pg = Pages.FindPage(pageName);
				saveMode = SaveMode.Normal;
			}
			Log.LogEntry("Page update requested for " + pageName, EntryType.General, "import");
			Pages.ModifyPage(pg, pageTitle, "import", DateTime.Now, "", text, null, null, saveMode);
		}

		#region TranslateAll

		protected void btnTranslateAll_click(object sender, EventArgs e) {
			AddOnPreRenderCompleteAsync(
				new BeginEventHandler(PageDownload),
				new EndEventHandler(endPageDownload)
			);
		}

		private IAsyncResult PageDownload(object sender, EventArgs e, AsyncCallback ac, object state) {
			IAsyncResult ar = null;
			return ac.BeginInvoke(ar, ac, null);
		}

		private void endPageDownload(IAsyncResult ar) {
			for(int i = 0; i < pageList.Items.Count; i++) {
				if(pageList.Items[i].Selected) {
					string url = "";
					Regex textarea = null;
					Match match = null;
					ITranslator translator = null;
					if(lstWiki.SelectedValue.ToUpperInvariant() == "MEDIA") {
						url = txtWikiUrl.Text + "?title=" + pageList.Items[i].Value + "&action=edit";
						textarea = new Regex(@"(?<=(\<textarea([^>])*?)\>)(.|\s)+?(?=(\<\/textarea\>))");
						translator = new Translator();
					}
					if(lstWiki.SelectedValue.ToUpperInvariant() == "FLEX") {
						if(txtWikiUrl.Text.EndsWith("/")) url = txtWikiUrl.Text + "wikiedit.aspx?topic=" + pageList.Items[i].Value;
						else url = txtWikiUrl.Text + "/wikiedit.aspx?topic=" + pageList.Items[i].Value;
						textarea = new Regex(@"(?<=(\<textarea class=\'EditBox\'([^>])*?)\>)(.|\s)+?(?=(\<\/textarea\>))");
						translator = new TranslatorFlex();
					}
					try {
						match = textarea.Match(PageRequest(url));
						if(match.Success) {
							string text = translator.Translate(match.Value.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", @""""));
							string pageName = pageList.Items[i].Value.Replace(":", "_").Replace("/", "_").Replace(@"\", "_").Replace('?', '_');
							string pageTitle = pageList.Items[i].Value;
							Log.LogEntry("Page " + pageName + " created with import whole wiki", EntryType.General, "import");
							PageInfo pg = Pages.FindPage(pageName);
							SaveMode saveMode = SaveMode.Backup;
							if(pg == null) {
								Pages.CreatePage(null as string, pageName);
								pg = Pages.FindPage(pageName);
								saveMode = SaveMode.Normal;
							}
							Log.LogEntry("Page create requested for " + pageName, EntryType.General, "import");
							Pages.ModifyPage(pg, pageTitle, "import", DateTime.Now, "", text, null, null, saveMode);
							pageList.Items.Remove(pageList.Items[i]);
						}
					}
					catch(WebException) { }
				}
			}
			pageList.Visible = true;
			lblPageList.Text = "Import completed!";
		}

		#endregion

		#region SinglePageRequest

		private IAsyncResult BeginPageRequest(object sender, EventArgs e, AsyncCallback cb, object state) {
			string editPageUrl = "";
			if(lstWiki.SelectedValue.ToUpperInvariant() == "MEDIA") editPageUrl = txtPageUrl.Text + "?title=" + txtPageName.Text + "&action=edit";
			if(lstWiki.SelectedValue.ToUpperInvariant() == "FLEX") {
				if(txtPageUrl.Text.EndsWith("/")) editPageUrl = txtPageUrl.Text + "wikiedit.aspx?topic=" + txtPageName.Text;
				else editPageUrl = txtPageUrl.Text + "/wikiedit.aspx?topic=" + txtPageName.Text;
			}
			request = (HttpWebRequest)WebRequest.Create(editPageUrl);
			SetProxyAndUserAgent(request);
			return request.BeginGetResponse(cb, state);
		}

		private void EndPageRequest(IAsyncResult ar) {
			try {
				HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(ar);
				StreamReader reader = new StreamReader(response.GetResponseStream());
				if(lstWiki.SelectedValue.ToUpperInvariant() == "MEDIA") {
					Regex textarea = new Regex(@"(?<=(\<textarea([^>])*?)\>)(.|\s)+?(?=(\<\/textarea\>))");
					Match match = textarea.Match(reader.ReadToEnd());
					if(match.Success) {
						Translator translator = new Translator();
						string text = translator.Translate(match.Value.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", @""""));
						savePage(text);
					}
				}
				if(lstWiki.SelectedValue.ToUpperInvariant() == "FLEX") {
					Regex textarea = new Regex(@"(?<=(\<textarea class=\'EditBox\'([^>])*?)\>)(.|\s)+?(?=(\<\/textarea\>))");
					Match match = textarea.Match(reader.ReadToEnd());
					if(match.Success) {
						Translator translator = new Translator();
						string text = translator.Translate(match.Value.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", @""""));
						savePage(text);
					}
				}
				UrlTools.Redirect(UrlTools.BuildUrl(txtPageName.Text.Replace(":", "_").Replace("/", "_").Replace(@"\", "_").Replace('?', '_'), ".ashx"));
			}
			catch(WebException) {
				lblResult.Text = "Web exception";
			}
		}

		private void TimeoutPageRequest(IAsyncResult ar) {
			mlwImport.ActiveViewIndex = 3;
			txtTranslated.Text = "Requested page temporarily unavailable";
		}

		#endregion

		#region WholeWikiRequest

		private IAsyncResult BeginPagesListRequest(object sender, EventArgs e, AsyncCallback cb, object state) {
			IAsyncResult ar = null;
			List<string> pages = new List<string>();
			try {
				if(lstWiki.SelectedValue.ToUpperInvariant() == "MEDIA") {
					//Download all namespaces
					string url = txtWikiUrl.Text + "?title=Special:Allpages&namespace=0";

					List<string> namespaces = GetNamespaces(PageRequest(url));

					//Download list of pages
					for(int k = 0; k < namespaces.Count; k = k + 2) {
						string pageText = PageRequest(txtWikiUrl.Text + "?title=Special:Allpages&namespace=" + namespaces[k]);

						Regex allPagesTable = new Regex(@"(?<=(\<table\ class=\'allpageslist\'([^>])*?)\>)(.|\s)+?(?=(\<\/table\>))");
						Match table = allPagesTable.Match(pageText);
						if(table.Success) {
							string tableStr = table.Value;
							Regex allPagesData = new Regex(@"(?<=(\<a([^>])*?)\>)(.|\s)+?(?=(\<\/a\>))");
							Match data = allPagesData.Match(tableStr);
							int i = 2;
							while(data.Success) {
								i++;
								if(i == 3) {
									pages.AddRange(PartialList(PageRequest(txtWikiUrl.Text + "?title=Special:Allpages&from=" + data.Value + "&namespace=" + namespaces[k]), namespaces[k + 1]));
									i = 0;
								}
								tableStr = tableStr.Substring(data.Index + data.Length + 5);
								data = allPagesData.Match(tableStr);
							}
						}
						else
							pages.AddRange(PartialList(pageText, namespaces[k + 1]));
					}
				}
				else {
					string pageText = null;
					if(txtWikiUrl.Text.EndsWith("/")) pageText = PageRequest(txtWikiUrl.Text + @"search.aspx?search=&namespace=%5BAll%5D");
					else pageText = PageRequest(txtWikiUrl.Text + @"/search.aspx?search=&namespace=%5BAll%5D");
					Regex pageTitle = new Regex(@"<div class='searchHitHead'>(.|\s)+?</div>");
					Match match = pageTitle.Match(pageText);
					while(match.Success) {
						pages.Add(match.Value.Substring(37, match.Value.IndexOf('"', 37) - 37));
						match = pageTitle.Match(pageText, match.Index + match.Length - 1);
					}
				}
				pageList.Items.Clear();

				for(int i = 0; i < pages.Count; i++) {
					pageList.Items.Add(new ListItem(pages[i], pages[i]));
					pageList.Items[i].Selected = true;
				}
				if(pages.Count > 1000) {
					pageList.Visible = false;
					pageList_div.Visible = false;
					lblPageList.Text = "Too many pages. Click Translate button to import whole wiki.";
				}
				else {
					pageList.Visible = true;
					pageList_div.Visible = true;
					lblPageList.Text = "To exclude pages, uselect them";
				}
			}
			catch(WebException) {
				pageList.Visible = false;
				pageList_div.Visible = false;
				btnTranslateAll.Visible = false;
				lblPageList.Text = "Web Exception";
			}

			return cb.BeginInvoke(ar, cb, null);
		}

		private List<string> GetNamespaces(string p) {
			List<string> namespacesList = new List<string>();
			Regex namespaces = new Regex(@"(?<=(\<select\ id=\'namespace\'([^>])*?)\>)(.|\s)+?(?=(\<\/select\>))");
			Match match = namespaces.Match(p);
			if(match.Success) {
				Regex namespaceValue = new Regex(@"(?<=(\<option\ value=\""))\d+?(?=(\""))");
				Match match1 = namespaceValue.Match(match.Value);
				while(match1.Success) {
					namespacesList.Add(match1.Value);
					if(match1.Value != "0")
						namespacesList.Add(match.Value.Substring(match.Value.IndexOf(">", match1.Index) + 1, match.Value.IndexOf("<", match.Value.IndexOf(">", match1.Index)) - match.Value.IndexOf(">", match1.Index) - 1) + ":");
					else namespacesList.Add("");
					match1 = namespaceValue.Match(match.Value, match1.Index + 1);
				}
			}
			return namespacesList;
		}

		private void EndPagesListRequest(IAsyncResult ar) {

		}

		private List<string> PartialList(string pageText, string nameSpace) {
			Regex allPagesTable = new Regex(@"(?<=(\<hr\ \/\>\<table([^>])*?)\>)(.|\s)*?(?=(\<\/table\>))");
			Match table = allPagesTable.Match(pageText);
			string tableStr = table.Value;
			Regex allPagesData = new Regex(@"(?<=(\<a([^>])*?)\>)(.|\s)+?(?=(\<\/a\>))");
			Match data = allPagesData.Match(tableStr);
			List<string> pages = new List<string>();
			while(data.Success) {
				pages.Add(nameSpace + data.Value);
				tableStr = tableStr.Substring(data.Index + data.Length + 5);
				data = allPagesData.Match(tableStr);
			}
			return pages;
		}

		#endregion

	}

}