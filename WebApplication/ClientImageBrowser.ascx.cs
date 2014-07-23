
using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Text;

namespace ScrewTurn.Wiki {

	public partial class ClientImageBrowser : System.Web.UI.UserControl {

		private const string ClientBrowserItems = "ClientBrowserItems";

		private string leafCssClass = "";
		private string nodeCssClass = "";
		private string nodeContent = "";
		private string upCssClass = "";
		private string upLevelContent = "";

		protected void Page_Load(object sender, EventArgs e) {
			Render();
		}

		/// <summary>
		/// Removes all the items in the browser and re-populates it.
		/// </summary>
		public void PopulateBrowser() {
			if(Populate == null) ViewState[ClientBrowserItems] = new List<TreeElement>();
			else ViewState[ClientBrowserItems] = Populate(this, new PopulateEventArgs());

			Render();
		}

		private void Render() {
			lblStrings.Text = string.Format("<script type=\"text/javascript\">\r\n<!--\r\n\tvar CurrentDivId = \"{0}\";\r\n// -->\r\n</script>", BuildSubTreeContainerID(0));

			List<TreeElement> items = (List<TreeElement>)ViewState[ClientBrowserItems];
			if(items == null) return;

			StringBuilder sb = new StringBuilder();
			sb.Append(@"<div class=""browsercontainer"">");
			int iteration = 0;
			RenderSubTree(items, sb, ref iteration, BuildSubTreeContainerID(0));
			sb.Append("</div>");
			lblContent.Text = sb.ToString();
		}

		private void RenderSubTree(List<TreeElement> items, StringBuilder sb, ref int iteration, string parentId) {
			// This method generates the client markup and JavaScript that contains a tree of images
			// The sub-trees are NOT rendered as nested elements

			StringBuilder temp = new StringBuilder();

			string id = BuildSubTreeContainerID(iteration);

			sb.AppendFormat(@"<div id=""{0}""{1}>", id, (iteration != 0 ? @" style=""display: none;""" : ""));

			if(iteration != 0) {
				sb.AppendFormat(@"<div class=""{0}""><a href=""#"" class=""{0}"" onclick=""javascript:return DisplayDiv('{1}');"" title=""{2}"">{3}</a></div>",
					upCssClass, parentId, Properties.Messages.UpLevel, upLevelContent + Properties.Messages.UpLevel);
			}

			foreach(TreeElement item in items) {
				iteration++; // Before invoking RenderSubTree recursively!
				// Render item
				RenderItem(item, sb, iteration);
				if(item.SubItems.Count > 0) {
					RenderSubTree(item.SubItems, temp, ref iteration, id);
				}
			}

			sb.Append("</div>");

			sb.Append(temp.ToString());
		}

		private void RenderItem(TreeElement item, StringBuilder sb, int iteration) {
			if(item.SubItems.Count > 0) {
				// Expanding link
				string containerId = BuildSubTreeContainerID(iteration);
				sb.AppendFormat(@"<div class=""{0}""><a href=""#"" class=""{0}"" onclick=""javascript:return DisplayDiv('{1}');"" title=""{2}"">{3}</a></div>",
					nodeCssClass, containerId, item.Name, nodeContent + item.Text);
			}
			else {
				// Action link
				sb.AppendFormat(@"<div class=""{0}""><a href=""#"" class=""{0}"" onclick=""{1}"" title=""{2}"">{3}</a></div>",
					leafCssClass, item.OnClientClick, item.Name, item.Text);
			}
		}

		private string BuildSubTreeContainerID(int iteration) {
			return string.Format("sub_{0}_{1}", ID, iteration);
		}

		/// <summary>
		/// Gets or sets the CSS Class for leaf items.
		/// </summary>
		public string LeafCssClass {
			get { return leafCssClass; }
			set { leafCssClass = value; }
		}

		/// <summary>
		/// Gets or sets the CSS Class for folder items.
		/// </summary>
		public string NodeCssClass {
			get { return nodeCssClass; }
			set { nodeCssClass = value; }
		}

		/// <summary>
		/// Gets or sets the content for folder items.
		/// </summary>
		public string NodeContent {
			get { return nodeContent; }
			set { nodeContent = value; }
		}

		/// <summary>
		/// Gets or sets the CSS Class for "Up one level" nodes.
		/// </summary>
		public string UpCssClass {
			get { return upCssClass; }
			set { upCssClass = value; }
		}

		/// <summary>
		/// Gets or sets the content for "Up one level" nodes.
		/// </summary>
		public string UpLevelContent {
			get { return upLevelContent; }
			set { upLevelContent = value; }
		}

		/// <summary>
		/// Delegate used for handling the Populate event.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">The event arguments.</param>
		/// <returns>A list of items contained in the expanded sub-tree.</returns>
		public delegate List<TreeElement> PopulateEventHandler(object sender, PopulateEventArgs e);

		/// <summary>
		/// Occurs when a sub-tree is populated.
		/// </summary>
		public event PopulateEventHandler Populate;

	}

}
