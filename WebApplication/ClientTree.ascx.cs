
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

	public partial class ClientTree : System.Web.UI.UserControl {

		private const string ClientTreeItems = "ClientTreeItems";

		private string leafCssClass = "";
		private string nodeCssClass = "";
		private string containerCssClass = "";

		protected void Page_Load(object sender, EventArgs e) {
			Render();
		}

		/// <summary>
		/// Removes all the items in the tree and re-populates it.
		/// </summary>
		public void PopulateTree() {
			// Use ViewState to cache data
			if(Populate == null) ViewState[ClientTreeItems] = new List<TreeElement>();
			else ViewState[ClientTreeItems] = Populate(this, new PopulateEventArgs());

			Render();
		}

		private void Render() {
			List<TreeElement> items = (List<TreeElement>)ViewState[ClientTreeItems];
			if(items == null) return;

			StringBuilder sb = new StringBuilder();
			sb.Append(@"<div class=""treecontainer"">");
			int iteration = 0;
			RenderSubTree(items, sb, ref iteration);
			sb.Append("</div>");
			lblContent.Text = sb.ToString();
		}

		private void RenderSubTree(List<TreeElement> items, StringBuilder sb, ref int iteration) {
			// This method generates the client markup and JavaScript that contains a tree of items
			// The sub-trees are rendered as nested elements (DIVs)
			foreach(TreeElement item in items) {
				iteration++; // Before invoking RenderSubTree recursively!
				// Render item
				if(item.SubItems.Count > 0) {
					// Expanding link
					string containerId = BuildSubTreeContainerID(iteration);
					sb.AppendFormat(@"<a href=""#"" class=""{0}"" onclick=""javascript:return ToggleDiv('{1}');"" title=""{2}"">{3}</a>",
						nodeCssClass, containerId, item.Name, item.Text);
					sb.AppendFormat(@"<div id=""{0}"" class=""{1}"" style=""display: none;"">", containerId, containerCssClass);
					RenderSubTree(item.SubItems, sb, ref iteration);
					sb.Append("</div>");
				}
				else {
					// Action link
					sb.AppendFormat(@"<a href=""#"" class=""{0}"" onclick=""{1}"" title=""{2}"">{3}</a>",
						leafCssClass, item.OnClientClick, item.Name, item.Text);
				}
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
		/// Gets or sets the CSS Class for containers.
		/// </summary>
		public string ContainerCssClass {
			get { return containerCssClass; }
			set { containerCssClass = value; }
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
