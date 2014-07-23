
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	public partial class Editor : System.Web.UI.UserControl {

		private PageInfo currentPage = null;
		private bool inWYSIWYG = false;

		protected void Page_Load(object sender, EventArgs e) {
			if(!Page.IsPostBack) {
				NamespaceInfo currentNamespace = Tools.DetectCurrentNamespaceInfo();
				string currentNamespaceName = currentNamespace != null ? currentNamespace.Name + "." : "";
				StringBuilder sb = new StringBuilder(200);
				sb.Append("<script type=\"text/javascript\">\r\n<!--\r\n");
				sb.AppendFormat("\tvar MarkupControl = \"{0}\";\r\n", txtMarkup.ClientID);
				sb.AppendFormat("\tvar VisualControl = \"{0}\";\r\n", lblWYSIWYG.ClientID);
				sb.AppendFormat("\tvar CurrentPage = \"{0}\";\r\n", (currentPage != null ? Tools.UrlEncode(currentPage.FullName) : ""));
				sb.AppendFormat("\tvar CurrentNamespace = \"{0}\";\r\n", Tools.UrlEncode(currentNamespaceName));
				sb.Append("// -->\r\n</script>");
				lblStrings.Text = sb.ToString();

				if(ViewState["ToolbarVisible"] == null) ViewState["ToolbarVisible"] = true;

				InitToolbar();
			}

			if(mlvEditor.ActiveViewIndex == 1) inWYSIWYG = true;
			else inWYSIWYG = false;

			//SelectTab(0);
			if(ViewState["Tab"] != null) SelectTab((int)ViewState["Tab"]);

			LoadSnippets();

			PrintCustomSpecialTags();
		}

		private void InitToolbar() {
			if(!ToolbarVisible) {
				lblToolbarInit.Text = "<script type=\"text/javascript\">\n<!--\nHideToolbarButtons();\n// -->\n</script>";
			}
			else lblToolbarInit.Text = "";
		}

		#region Tabs Management

		/// <summary>
		/// Gets a value indicating whether the editor is in WikiMarkup mode.
		/// </summary>
		/// <returns><c>true</c> if the editor is in WikiMarkup mode, <c>false</c> otherwise.</returns>
		public bool IsInWikiMarkup() {
			// Quick and dirty
			return btnWikiMarkup.CssClass == "tabbuttonactive";
		}

		/// <summary>
		/// Selects the active tab.
		/// </summary>
		/// <param name="index">The index of the active tab:
		/// - 0: WikiMarkup
		/// - 1: Visual
		/// - 2: Preview.</param>
		private void SelectTab(int index) {
			btnWikiMarkup.CssClass = "tabbutton";
			btnVisual.CssClass = "tabbutton";
			btnPreview.CssClass = "tabbutton";

			btnWikiMarkup.Enabled = true;
			btnVisual.Enabled = true;
			btnPreview.Enabled = true;

			mlvEditor.ActiveViewIndex = index;
			switch(index) {
				case 0:
					btnWikiMarkup.CssClass = "tabbuttonactive";
					btnWikiMarkup.Enabled = false;
					break;
				case 1:
					btnVisual.CssClass = "tabbuttonactive";
					btnVisual.Enabled = false;
					break;
				case 2:
					btnPreview.CssClass = "tabbuttonactive";
					btnPreview.Enabled = false;
					break;
			}
			ViewState["Tab"] = index;

			if(SelectedTabChanged != null) SelectedTabChanged(this, new SelectedTabChangedEventArgs());
		}

		/// <summary>
		/// Fired when the selected tab changes.
		/// </summary>
		public event EventHandler<SelectedTabChangedEventArgs> SelectedTabChanged;

		/// <summary>
		/// Gets or sets a value indicating wherher the Visual tab is enabled.
		/// </summary>
		public bool VisualVisible {
			get { return btnVisual.Visible; }
			set { btnVisual.Visible = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the Preview tab is enabled.
		/// </summary>
		public bool PreviewVisible {
			get { return btnPreview.Visible; }
			set { btnPreview.Visible = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the toolbar is visible.
		/// </summary>
		public bool ToolbarVisible {
			get { return (bool)ViewState["ToolbarVisible"]; }
			set {
				ViewState["ToolbarVisible"] = value;
				InitToolbar();
			}
		}

		protected void btnWikiMarkup_Click(object sender, EventArgs e) {
			SelectTab(0);

			//added for WYSIWYG
			//if last view was WYSIWYG take text from WYSIWYG to Markup
			if(inWYSIWYG)
				txtMarkup.Text = ReverseFormatter.ReverseFormat(lblWYSIWYG.Text);
			//end
		}

		protected void btnVisual_Click(object sender, EventArgs e) {
			SelectTab(1);

			//added for WYSIWYG
			//lblWYSIWYG.Text = FormattingPipeline.FormatWithPhase1And2(txtMarkup.Text, null);
			string[] links = null;
			lblWYSIWYG.Text = Formatter.Format(txtMarkup.Text,
				false, FormattingContext.Unknown, null, out links, true);
			//end
		}

		protected void btnPreview_Click(object sender, EventArgs e) {
			SelectTab(2);

			//added for WYSIWYG
			//if last view was WYSIWYG take text from WYSIWYG to Preview
			//in both cases I need to synchronize WYSIWYG and Markup view
			if(inWYSIWYG) {
				lblPreview.Text = lblWYSIWYG.Text.Replace("&lt;", "<").Replace("&gt;", ">");
				txtMarkup.Text = ReverseFormatter.ReverseFormat(lblWYSIWYG.Text);
			}
			else {
				lblPreview.Text = FormattingPipeline.FormatWithPhase3(FormattingPipeline.FormatWithPhase1And2(txtMarkup.Text, false, FormattingContext.Unknown, null),
					FormattingContext.Unknown, null);
				//lblWYSIWYG.Text = lblPreview.Text;
				string[] links = null;
				lblWYSIWYG.Text = Formatter.Format(txtMarkup.Text, false, FormattingContext.Unknown, null, out links, true);
			}
			//end
		}

		#endregion

		#region Menus Management

		/// <summary>
		/// Prints the custom special tags.
		/// </summary>
		private void PrintCustomSpecialTags() {
			Dictionary<string, CustomToolbarItem> tags = Host.Instance.CustomSpecialTags;
			StringBuilder sb = new StringBuilder(100);
			foreach(string key in tags.Keys) {
				switch(tags[key].Item) {
					case ToolbarItem.SpecialTag:
						sb.AppendFormat("<a href=\"#\" onclick=\"javascript:return InsertMarkup('{0}');\" class=\"menulink\">{1}</a>",
							tags[key].Value, key);
						break;
					case ToolbarItem.SpecialTagWrap:
						string[] t = tags[key].Value.Split('|');
						sb.AppendFormat("<a href=\"#\" onclick=\"javascript:return WrapSelectedMarkup('{0}', '{1}');\" class=\"menulink\">{2}</a>",
							t[0], t[1], key);
						break;
				}
			}
			lblCustomSpecialTags.Text = sb.ToString();
		}

		/// <summary>
		/// Loads and prints the snippets.
		/// </summary>
		private void LoadSnippets() {
			StringBuilder sb = new StringBuilder(1000);
			foreach(Snippet s in Snippets.GetSnippets()) {
				string[] parameters = Snippets.ExtractParameterNames(s);
				int paramCount = parameters.Length;
				string label;
				if(paramCount == 0) {
					label = s.Name;
					sb.AppendFormat(@"<a href=""#"" title=""{0}"" onclick=""javascript:return InsertMarkup('&#0123;s:{1}&#0125;');"" class=""menulink"">{0}</a>",
						label, s.Name);
				}
				else {
					bool isPositional = IsSnippetPositional(parameters);
					label = string.Format("{0} ({1} {2})", s.Name, paramCount, Properties.Messages.Parameters);
					sb.AppendFormat(@"<a href=""#"" title=""{0}"" onclick=""javascript:return InsertMarkup('&#0123;s:{1}{2}{3}&#0125;');"" class=""menulink"">{0}</a>",
						label, s.Name, isPositional ? "" : "\\r\\n", GetParametersPlaceHolders(parameters, isPositional));
				}
			}
			if(sb.Length == 0) sb.Append("<i>" + Properties.Messages.NoSnippets + "</i>");
			lblSnippets.Text = sb.ToString();
		}

		/// <summary>
		/// Determines whether the parameters of a snippet are positional or not.
		/// </summary>
		/// <param name="parameters">The parameters.</param>
		/// <returns><c>true</c> if the parameters are positional, <c>false</c> otherwise.</returns>
		private static bool IsSnippetPositional(string[] parameters) {
			int dummy;
			for(int i = 0; i < parameters.Length; i++) {
				if(!int.TryParse(parameters[i], out dummy)) return false;
				if(dummy != i + 1) return false;
			}
			return true;
		}

		/// <summary>
		/// Gets the placeholder for snippet parameters.
		/// </summary>
		/// <param name="parameters">The parameters.</param>
		/// <param name="isSnippetPositional">A value indicating whether the snippet parameters are positional.</param>
		/// <returns>The snippet placeholder/template.</returns>
		private static string GetParametersPlaceHolders(string[] parameters, bool isSnippetPositional) {
			if(parameters.Length == 0) return "";
			else {
				StringBuilder sb = new StringBuilder(20);
				foreach(string param in parameters) {
					if(isSnippetPositional) sb.AppendFormat("|PLACE YOUR VALUE HERE ({0})", param);
					else sb.AppendFormat("| {0} = PLACE YOUR VALUE HERE\\r\\n", param);
				}
				/*for(int i = 1; i <= paramCount; i++) {
					sb.Append("|P");
					sb.Append(i.ToString());
				}*/
				return sb.ToString();
			}
		}

		#endregion

		#region Public I/O

		/// <summary>
		/// Sets the edited content.
		/// </summary>
		/// <param name="content">The content.</param>
		/// <param name="useVisual"><c>true</c> if the visual editor must be used, <c>false</c> otherwise.</param>
		public void SetContent(string content, bool useVisual) {
			inWYSIWYG = useVisual;
			lblWYSIWYG.Text = "";
			txtMarkup.Text = content;
			if(useVisual) btnVisual_Click(this, null);
			else btnWikiMarkup_Click(this, null);
		}

		/// <summary>
		/// Gets the edited content.
		/// </summary>
		/// <returns>The content.</returns>
		public string GetContent() {
			if(inWYSIWYG) return ReverseFormatter.ReverseFormat(lblWYSIWYG.Text);
			else return txtMarkup.Text;
		}

		/// <summary>
		/// Gets or sets the current Page (if any), i.e. the page that is being edited.
		/// </summary>
		/// <remarks>This property is used for enabling the "link attachment" feature in the editor.
		/// If the current Page is null, the feature is disabled.</remarks>
		public PageInfo CurrentPage {
			get { return currentPage; }
			set { currentPage = value; }
		}

		#endregion

	}

	/// <summary>
	/// Contains arguments for the Selected Tab Changed event.
	/// </summary>
	public class SelectedTabChangedEventArgs : EventArgs {
	}

}
