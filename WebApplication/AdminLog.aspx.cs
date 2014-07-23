
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	public partial class AdminLog : BasePage {

		private const int MaxEntries = 100;

		protected void Page_Load(object sender, EventArgs e) {
			AdminMaster.RedirectToLoginIfNeeded();

			if(!AdminMaster.CanManageConfiguration(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames())) UrlTools.Redirect("AccessDenied.aspx");

			if(!Page.IsPostBack) {
				// Load log entries
				rptLog.DataBind();
			}
		}

		protected void chkFilter_CheckedChanged(object sender, EventArgs e) {
			rptLog.DataBind();
		}

		protected void btnNoLimit_Click(object sender, EventArgs e) {
			lblLimit.Visible = false;
			btnNoLimit.Visible = false;
			rptLog.DataBind();
		}

		protected void rptLog_DataBinding(object sender, EventArgs e) {
			List<LogEntry> entries = Log.ReadEntries();

			List<LogEntryRow> result = new List<LogEntryRow>(entries.Count);

			int maxEntries = btnNoLimit.Visible ? MaxEntries : int.MaxValue;

			foreach(LogEntry entry in entries) {
				if(IsEntryIncluded(entry.EntryType)) {
					result.Add(new LogEntryRow(entry));
				}
			}

			if(!Page.IsPostBack) {
				if(result.Count <= MaxEntries) {
					lblLimit.Visible = false;
					btnNoLimit.Visible = false;
				}
			}

			rptLog.DataSource = result.Take(maxEntries);
		}

		/// <summary>
		/// Decides whether a log entry must be displayed based on the current filtering options.
		/// </summary>
		/// <param name="type">The log entry type.</param>
		/// <returns><c>true</c> if the entry must be displayed, <c>false</c> otherwise.</returns>
		private bool IsEntryIncluded(EntryType type) {
			return type == EntryType.General && chkMessages.Checked ||
				type == EntryType.Warning && chkWarnings.Checked ||
				type == EntryType.Error && chkErrors.Checked;
		}

		protected void btnClearLog_Click(object sender, EventArgs e) {
			Log.ClearLog();
			rptLog.DataBind();
		}

	}

	/// <summary>
	/// Represents a log entry for display purposes.
	/// </summary>
	public class LogEntryRow {

		private string imageTag, dateTime, user, message, additionalClass;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:LogEntryRow" /> class.
		/// </summary>
		/// <param name="entry">The original log entry.</param>
		public LogEntryRow(LogEntry entry) {
			imageTag = entry.EntryType.ToString();
			dateTime = Preferences.AlignWithTimezone(entry.DateTime).ToString(Settings.DateTimeFormat).Replace(" ", "&nbsp;");
			user = entry.User.Replace(" ", "&nbsp;");
			message = entry.Message.Replace("&", "&amp;");

			if(entry.EntryType == EntryType.Error) additionalClass = " error";
			else if(entry.EntryType == EntryType.Warning) additionalClass = " warning";
			else additionalClass = "";
		}

		/// <summary>
		/// Gets the image tag.
		/// </summary>
		public string ImageTag {
			get { return imageTag; }
		}

		/// <summary>
		/// Gets the date/time.
		/// </summary>
		public string DateTime {
			get { return dateTime; }
		}

		/// <summary>
		/// Gets the user.
		/// </summary>
		public string User {
			get { return user; }
		}

		/// <summary>
		/// Gets the message.
		/// </summary>
		public string Message {
			get { return message; }
		}

		/// <summary>
		/// Gets the additional CSS class.
		/// </summary>
		public string AdditionalClass {
			get { return additionalClass; }
		}

	}

}
