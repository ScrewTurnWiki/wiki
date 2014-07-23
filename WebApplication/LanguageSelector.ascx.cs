
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ScrewTurn.Wiki {

	public partial class LanguageSelector : System.Web.UI.UserControl {

		protected void Page_Load(object sender, EventArgs e) {
			if(!Page.IsPostBack && lstLanguages.Items.Count == 0) {
				LoadLanguages();
			}
		}

		/// <summary>
		/// Loads the languages.
		/// </summary>
		public void LoadLanguages() {
			string[] c = Tools.AvailableCultures;
			lstLanguages.Items.Clear();
			for(int i = 0; i < c.Length; i++) {
				lstLanguages.Items.Add(new ListItem(c[i].Split('|')[1], c[i].Split('|')[0]));
			}
		}

		/// <summary>
		/// Gets or sets the selected language.
		/// </summary>
		public string SelectedLanguage {
			get {
				return lstLanguages.SelectedValue;
			}
			set {
				lstLanguages.SelectedIndex = -1;
				foreach(ListItem item in lstLanguages.Items) {
					if(item.Value == value) {
						item.Selected = true;
						break;
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets the selected timezone.
		/// </summary>
		public string SelectedTimezone {
			get {
				return lstTimezones.SelectedValue;
			}
			set {
				lstTimezones.SelectedIndex = -1;
				foreach(ListItem item in lstTimezones.Items) {
					if(item.Value == value) {
						item.Selected = true;
						break;
					}
				}
			}
		}

	}

}
