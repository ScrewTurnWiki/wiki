
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ScrewTurn.Wiki {

	public partial class AclActionsSelector : System.Web.UI.UserControl {

		protected void Page_Load(object sender, EventArgs e) {
		}

		/// <summary>
		/// Gets or sets the name of the resource of which to display the actions.
		/// </summary>
		public AclResources CurrentResource {
			get {
				object temp = ViewState["CR"];
				if(temp == null) return AclResources.Globals;
				else return (AclResources)temp;
			}
			set {
				ViewState["CR"] = value;
				Render();
			}
		}

		/// <summary>
		/// Renders the items in the list.
		/// </summary>
		private void Render() {
			AclResources res = CurrentResource;

			string[] temp = null;
			switch(res) {
				case AclResources.Globals:
					temp = Actions.ForGlobals.All;
					break;
				case AclResources.Namespaces:
					temp = Actions.ForNamespaces.All;
					break;
				case AclResources.Pages:
					temp = Actions.ForPages.All;
					break;
				case AclResources.Directories:
					temp = Actions.ForDirectories.All;
					break;
				default:
					throw new NotSupportedException("ACL Resource not supported");
			}

			// Add full-control action
			string[] actions = new string[temp.Length + 1];
			actions[0] = Actions.FullControl;
			Array.Copy(temp, 0, actions, 1, temp.Length);

			lstActionsGrant.Items.Clear();
			lstActionsDeny.Items.Clear();
			foreach(string action in actions) {
				ListItem item = new ListItem(GetName(res, action), action);
				lstActionsGrant.Items.Add(item);
				ListItem itemBlank = new ListItem("&nbsp;", action);
				lstActionsDeny.Items.Add(itemBlank);
			}
		}

		private string GetName(AclResources res, string action) {
			switch(res) {
				case AclResources.Globals:
					return Actions.ForGlobals.GetFullName(action);
				case AclResources.Namespaces:
					return Actions.ForNamespaces.GetFullName(action);
				case AclResources.Pages:
					return Actions.ForPages.GetFullName(action);
				case AclResources.Directories:
					return Actions.ForDirectories.GetFullName(action);
				default:
					throw new NotSupportedException("ACL Resource not supported");
			}
		}

		/// <summary>
		/// Gets or sets the list of the granted actions.
		/// </summary>
		public string[] GrantedActions {
			get {
				List<string> actions = new List<string>();
				foreach(ListItem item in lstActionsGrant.Items) {
					if(item.Selected) actions.Add(item.Value);
				}
				return actions.ToArray();
			}
			set {
				if(value == null) throw new ArgumentNullException("value");

				SelectActions(value, DeniedActions);
			}
		}

		/// <summary>
		/// Gets or sets the list of the denied actions.
		/// </summary>
		public string[] DeniedActions {
			get {
				List<string> actions = new List<string>();
				foreach(ListItem item in lstActionsDeny.Items) {
					if(item.Selected) actions.Add(item.Value);
				}
				return actions.ToArray();
			}
			set {
				if(value == null) throw new ArgumentNullException("value");

				SelectActions(GrantedActions, value);
			}
		}

		private void SelectActions(string[] granted, string[] denied) {
			// Deselect and enable all items first
			foreach(ListItem item in lstActionsGrant.Items) {
				item.Selected = false;
				item.Enabled = true;
			}
			foreach(ListItem item in lstActionsDeny.Items) {
				item.Selected = false;
				item.Enabled = true;
			}

			// Select specific ones
			foreach(ListItem item in lstActionsGrant.Items) {
				item.Selected = Array.Find(granted, delegate(string s) { return s == item.Value; }) != null;
			}
			foreach(ListItem item in lstActionsDeny.Items) {
				item.Selected = Array.Find(denied, delegate(string s) { return s == item.Value; }) != null;
			}

			SetupCheckBoxes(null);
		}

		protected void lstActions_SelectedIndexChanged(object sender, EventArgs e) {
			SetupCheckBoxes(sender as Anthem.CheckBoxList);
		}

		private void SetupCheckBoxes(Anthem.CheckBoxList list) {
			// Setup the checkboxes so that full-control takes over the others,
			// and there cannot be an action that is both granted and denied
			// The list parameter determines the last checkbox list that changed status,
			// allowing to switch the proper checkbox pair
			if(lstActionsGrant.Items.Count > 0) {
				if(list == null) list = lstActionsGrant;
				Anthem.CheckBoxList other = list == lstActionsGrant ? lstActionsDeny : lstActionsGrant;

				// Verify whether full-control is checked
				// If so, disable all other checkboxes
				for(int i = 1; i < list.Items.Count; i++) {
					if(list.Items[0].Selected) {
						list.Items[i].Selected = false;
						list.Items[i].Enabled = false;
						other.Items[i].Enabled = true;
					}
					else {
						list.Items[i].Enabled = true;
					}
				}

				// Switch status of other list checkboxes
				for(int i = 0; i < other.Items.Count; i++) {
					if(i > 0 && other.Items[0].Selected) {
						other.Items[i].Selected = false;
						other.Items[i].Enabled = false;
					}
					else {
						if(other.Items[i].Enabled && list.Items[i].Enabled && list.Items[i].Selected) {
							other.Items[i].Selected = false;
						}
					}
				}
			}
		}

	}

	/// <summary>
	/// Lists legal ACL resources.
	/// </summary>
	public enum AclResources {
		/// <summary>
		/// Global resources.
		/// </summary>
		Globals,
		/// <summary>
		/// Namespaces.
		/// </summary>
		Namespaces,
		/// <summary>
		/// Pages.
		/// </summary>
		Pages,
		/// <summary>
		/// Directories.
		/// </summary>
		Directories
	}

}
