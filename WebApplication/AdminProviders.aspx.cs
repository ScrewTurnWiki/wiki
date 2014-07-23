
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;
using System.Net;
using System.Linq;
using System.IO;

namespace ScrewTurn.Wiki {

	public partial class AdminProviders : BasePage {

		protected void Page_Load(object sender, EventArgs e) {
			AdminMaster.RedirectToLoginIfNeeded();

			if(!AdminMaster.CanManageProviders(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames())) UrlTools.Redirect("AccessDenied.aspx");

			if(!Page.IsPostBack) {
				// Load providers and related data
				rptProviders.DataBind();
				
				LoadDlls();

				LoadSourceProviders();
			}
		}

		#region Providers List

		protected void rdo_CheckedChanged(object sender, EventArgs e) {
			ResetEditor();
			rptProviders.DataBind();
		}

		/// <summary>
		/// Resets the editor.
		/// </summary>
		private void ResetEditor() {
			pnlProviderDetails.Visible = false;
			btnAutoUpdateProviders.Visible = true;
			txtCurrentProvider.Value = "";
			lblResult.CssClass = "";
			lblResult.Text = "";
		}

		protected void rptProviders_DataBinding(object sender, EventArgs e) {
			List<IProviderV30> providers = new List<IProviderV30>(5);

			int enabledCount = 0;

			if(rdoPages.Checked) {
				enabledCount = Collectors.PagesProviderCollector.AllProviders.Length;
				providers.AddRange(Collectors.PagesProviderCollector.AllProviders);
				providers.AddRange(Collectors.DisabledPagesProviderCollector.AllProviders);
			}
			else if(rdoUsers.Checked) {
				enabledCount = Collectors.UsersProviderCollector.AllProviders.Length;
				providers.AddRange(Collectors.UsersProviderCollector.AllProviders);
				providers.AddRange(Collectors.DisabledUsersProviderCollector.AllProviders);
			}
			else if(rdoFiles.Checked) {
				enabledCount = Collectors.FilesProviderCollector.AllProviders.Length;
				providers.AddRange(Collectors.FilesProviderCollector.AllProviders);
				providers.AddRange(Collectors.DisabledFilesProviderCollector.AllProviders);
			}
			else if(rdoCache.Checked) {
				enabledCount = Collectors.CacheProviderCollector.AllProviders.Length;
				providers.AddRange(Collectors.CacheProviderCollector.AllProviders);
				providers.AddRange(Collectors.DisabledCacheProviderCollector.AllProviders);
			}
			else if(rdoFormatter.Checked) {
				enabledCount = Collectors.FormatterProviderCollector.AllProviders.Length;
				providers.AddRange(Collectors.FormatterProviderCollector.AllProviders);
				providers.AddRange(Collectors.DisabledFormatterProviderCollector.AllProviders);
			}

			List<ProviderRow> result = new List<ProviderRow>(providers.Count);

			for(int i = 0; i < providers.Count; i++) {
				IProviderV30 prov = providers[i];
				result.Add(new ProviderRow(prov.Information,
					prov.GetType().FullName,
					GetUpdateStatus(prov.Information),
					i > enabledCount - 1,
					txtCurrentProvider.Value == prov.GetType().FullName));
			}

			rptProviders.DataSource = result;
		}

		/// <summary>
		/// Gets the update status of a provider.
		/// </summary>
		/// <param name="info">The component information.</param>
		/// <returns>The update status.</returns>
		private string GetUpdateStatus(ComponentInformation info) {
			if(!Settings.DisableAutomaticVersionCheck) {
				if(string.IsNullOrEmpty(info.UpdateUrl)) return "n/a";
				else {
					string newVersion = null;
					string newAssemblyUrl = null;
					UpdateStatus status = Tools.GetUpdateStatus(info.UpdateUrl, info.Version, out newVersion, out newAssemblyUrl);

					if(status == UpdateStatus.Error) {
						return "<span class=\"resulterror\">" + Properties.Messages.Error + "</span>";
					}
					else if(status == UpdateStatus.NewVersionFound) {
						return "<span class=\"resulterror\">" + Properties.Messages.NewVersion + " <b>" + newVersion + "</b>" +
							(string.IsNullOrEmpty(newAssemblyUrl) ? "" : " (" + Properties.Messages.AutoUpdateAvailable + ")") + "</span>";
					}
					else if(status == UpdateStatus.UpToDate) {
						return "<span class=\"resultok\">" + Properties.Messages.UpToDate + "</span>";
					}
					else throw new NotSupportedException();
				}
			}
			else return "n/a";
		}

		/// <summary>
		/// Gets the currently selected provider.
		/// </summary>
		/// <returns>The provider.</returns>
		/// <param name="enabled">A value indicating whether the returned provider is enabled.</param>
		/// <param name="canDisable">A value indicating whether the returned provider can be disabled.</param>
		private IProviderV30 GetCurrentProvider(out bool enabled, out bool canDisable) {
			return Collectors.FindProvider(txtCurrentProvider.Value, out enabled, out canDisable);
		}

		protected void rptProviders_ItemCommand(object sender, CommandEventArgs e) {
			txtCurrentProvider.Value = e.CommandArgument as string;

			if(e.CommandName == "Select") {
				bool enabled;
				bool canDisable;
				IProviderV30 provider = GetCurrentProvider(out enabled, out canDisable);

				// Cannot disable the provider that handles the default page of the root namespace
				if(Pages.FindPage(Settings.DefaultPage).Provider == provider) canDisable = false;

				pnlProviderDetails.Visible = true;
				lblProviderName.Text = provider.Information.Name + " (" + provider.Information.Version + ")";
				string dll = provider.GetType().Assembly.FullName;
				lblProviderDll.Text = dll.Substring(0, dll.IndexOf(",")) + ".dll";
				txtConfigurationString.Text = ProviderLoader.LoadConfiguration(provider.GetType().FullName);
				if(provider.ConfigHelpHtml != null) {
					lblProviderConfigHelp.Text = provider.ConfigHelpHtml;
				}
				else {
					lblProviderConfigHelp.Text = Properties.Messages.NoConfigurationRequired;
				}

				btnEnable.Visible = !enabled;
				btnAutoUpdateProviders.Visible = false;
				btnDisable.Visible = enabled;
				btnDisable.Enabled = canDisable;
				lblCannotDisable.Visible = !canDisable;
				btnUnload.Enabled = !enabled;

				rptProviders.DataBind();
			}
		}

		/// <summary>
		/// Performs all the actions that are needed after a provider status is changed.
		/// </summary>
		private void PerformPostProviderChangeActions() {
			Content.InvalidateAllPages();
			Content.ClearPseudoCache();
		}

		protected void btnSave_Click(object sender, EventArgs e) {
			bool enabled, canDisable;
			IProviderV30 prov = GetCurrentProvider(out enabled, out canDisable);
			Log.LogEntry("Configuration change requested for Provider " + prov.Information.Name, EntryType.General, SessionFacade.CurrentUsername);

			string error;
			if(ProviderLoader.TryChangeConfiguration(txtCurrentProvider.Value, txtConfigurationString.Text, out error)) {
				PerformPostProviderChangeActions();

				lblResult.CssClass = "resultok";
				lblResult.Text = Properties.Messages.ProviderConfigurationSaved;

				ResetEditor();
				rptProviders.DataBind();
			}
			else {
				lblResult.CssClass = "resulterror";
				lblResult.Text = Properties.Messages.ProviderRejectedConfiguration +
					(string.IsNullOrEmpty(error) ? "" : (": " + error));
			}
		}

		protected void btnDisable_Click(object sender, EventArgs e) {
			bool enabled, canDisable;
			IProviderV30 prov = GetCurrentProvider(out enabled, out canDisable);
			Log.LogEntry("Deactivation requested for Provider " + prov.Information.Name, EntryType.General, SessionFacade.CurrentUsername);

			ProviderLoader.DisableProvider(txtCurrentProvider.Value);
			PerformPostProviderChangeActions();

			lblResult.CssClass = "resultok";
			lblResult.Text = Properties.Messages.ProviderDisabled;

			ResetEditor();
			rptProviders.DataBind();
			LoadSourceProviders();
			ReloadDefaultProviders();
		}

		protected void btnEnable_Click(object sender, EventArgs e) {
			bool enabled, canDisable;
			IProviderV30 prov = GetCurrentProvider(out enabled, out canDisable);
			Log.LogEntry("Activation requested for provider provider " + prov.Information.Name, EntryType.General, SessionFacade.CurrentUsername);

			ProviderLoader.EnableProvider(txtCurrentProvider.Value);
			PerformPostProviderChangeActions();

			lblResult.CssClass = "resultok";
			lblResult.Text = Properties.Messages.ProviderEnabled;

			ResetEditor();
			rptProviders.DataBind();
			LoadSourceProviders();
			ReloadDefaultProviders();
		}

		protected void btnUnload_Click(object sender, EventArgs e) {
			bool enabled, canDisable;
			IProviderV30 prov = GetCurrentProvider(out enabled, out canDisable);
			Log.LogEntry("Unloading requested for provider provider " + prov.Information.Name, EntryType.General, SessionFacade.CurrentUsername);

			ProviderLoader.UnloadProvider(txtCurrentProvider.Value);
			PerformPostProviderChangeActions();

			lblResult.CssClass = "resultok";
			lblResult.Text = Properties.Messages.ProviderUnloaded;

			ResetEditor();
			rptProviders.DataBind();
			LoadSourceProviders();
			ReloadDefaultProviders();
		}

		protected void btnCancel_Click(object sender, EventArgs e) {
			ResetEditor();
			rptProviders.DataBind();
		}

		protected void btnAutoUpdateProviders_Click(object sender, EventArgs e) {
			lblAutoUpdateResult.CssClass = "";
			lblAutoUpdateResult.Text = "";

			Log.LogEntry("Providers auto-update requested", EntryType.General, SessionFacade.CurrentUsername);

			ProviderUpdater updater = new ProviderUpdater(Settings.Provider,
				Collectors.FileNames,
				Collectors.PagesProviderCollector.AllProviders,
				Collectors.DisabledPagesProviderCollector.AllProviders,
				Collectors.UsersProviderCollector.AllProviders,
				Collectors.DisabledUsersProviderCollector.AllProviders,
				Collectors.FilesProviderCollector.AllProviders,
				Collectors.DisabledFilesProviderCollector.AllProviders,
				Collectors.CacheProviderCollector.AllProviders,
				Collectors.DisabledCacheProviderCollector.AllProviders,
				Collectors.FormatterProviderCollector.AllProviders,
				Collectors.DisabledFormatterProviderCollector.AllProviders);

			int count = updater.UpdateAll();

			lblAutoUpdateResult.CssClass = "resultok";
			if(count > 0) lblAutoUpdateResult.Text = Properties.Messages.ProvidersUpdated;
			else lblAutoUpdateResult.Text = Properties.Messages.NoProvidersToUpdate;

			rptProviders.DataBind();
		}

		#endregion

		#region Defaults

		/// <summary>
		/// Reloads the default providers.
		/// </summary>
		private void ReloadDefaultProviders() {
			lstPagesProvider.Reload();
			lstUsersProvider.Reload();
			lstFilesProvider.Reload();
		}

		protected void btnSaveDefaultProviders_Click(object sender, EventArgs e) {
			Log.LogEntry("Default providers change requested", EntryType.General, SessionFacade.CurrentUsername);

			Settings.BeginBulkUpdate();
			Settings.DefaultPagesProvider = lstPagesProvider.SelectedProvider;
			Settings.DefaultUsersProvider = lstUsersProvider.SelectedProvider;
			Settings.DefaultFilesProvider = lstFilesProvider.SelectedProvider;
			Settings.DefaultCacheProvider = lstCacheProvider.SelectedProvider;
			Settings.EndBulkUpdate();

			lblDefaultProvidersResult.CssClass = "resultok";
			lblDefaultProvidersResult.Text = Properties.Messages.DefaultProvidersSaved;

			ResetEditor();
			rptProviders.DataBind();
		}

		#endregion

		#region DLLs

		/// <summary>
		/// Loads all the providers' DLLs.
		/// </summary>
		private void LoadDlls() {
			string[] files = Settings.Provider.ListPluginAssemblies();
			lstDlls.Items.Clear();
			lstDlls.Items.Add(new ListItem("- " + Properties.Messages.SelectAndDelete + " -", ""));
			for(int i = 0; i < files.Length; i++) {
				lstDlls.Items.Add(new ListItem(files[i], files[i]));
			}
		}

		protected void lstDlls_SelectedIndexChanged(object sender, EventArgs e) {
			btnDeleteDll.Enabled = lstDlls.SelectedIndex >= 0 && !string.IsNullOrEmpty(lstDlls.SelectedValue);
		}

		protected void btnDeleteDll_Click(object sender, EventArgs e) {
			if(Settings.Provider.DeletePluginAssembly(lstDlls.SelectedValue)) {
				LoadDlls();
				lstDlls_SelectedIndexChanged(sender, e);
				lblDllResult.CssClass = "resultok";
				lblDllResult.Text = Properties.Messages.DllDeleted;
			}
			else {
				lblDllResult.CssClass = "resulterror";
				lblDllResult.Text = Properties.Messages.CouldNotDeleteDll;
			}
		}

		protected void btnUpload_Click(object sender, EventArgs e) {
			string file = upDll.FileName;

			string ext = System.IO.Path.GetExtension(file);
			if(ext != null) ext = ext.ToLowerInvariant();
			if(ext != ".dll") {
				lblUploadResult.CssClass = "resulterror";
				lblUploadResult.Text = Properties.Messages.VoidOrInvalidFile;
				return;
			}

			Log.LogEntry("Provider DLL upload requested " + upDll.FileName, EntryType.General, SessionFacade.CurrentUsername);

			string[] asms = Settings.Provider.ListPluginAssemblies();
			if(Array.Find<string>(asms, delegate(string v) {
				if(v.Equals(file)) return true;
				else return false;
			}) != null) {
				// DLL already exists
				lblUploadResult.CssClass = "resulterror";
				lblUploadResult.Text = Properties.Messages.DllAlreadyExists;
				return;
			}
			else {
				Settings.Provider.StorePluginAssembly(file, upDll.FileBytes);

				int count = ProviderLoader.LoadFromAuto(file);

				lblUploadResult.CssClass = "resultok";
				lblUploadResult.Text = Properties.Messages.LoadedProviders.Replace("###", count.ToString());
				upDll.Attributes.Add("value", "");

				PerformPostProviderChangeActions();

				LoadDlls();
				ResetEditor();
				rptProviders.DataBind();
				LoadSourceProviders();
			}
		}

		#endregion

		#region Data Migration

		/// <summary>
		/// Loads source providers for data migration.
		/// </summary>
		private void LoadSourceProviders() {
			lstPagesSource.Items.Clear();
			lstPagesSource.Items.Add(new ListItem("", ""));
			foreach(IPagesStorageProviderV30 prov in Collectors.PagesProviderCollector.AllProviders) {
				if(!prov.ReadOnly) {
					lstPagesSource.Items.Add(new ListItem(prov.Information.Name, prov.GetType().ToString()));
				}
			}

			lstUsersSource.Items.Clear();
			lstUsersSource.Items.Add(new ListItem("", ""));
			foreach(IUsersStorageProviderV30 prov in Collectors.UsersProviderCollector.AllProviders) {
				if(IsUsersProviderFullWriteEnabled(prov)) {
					lstUsersSource.Items.Add(new ListItem(prov.Information.Name, prov.GetType().ToString()));
				}
			}

			lstFilesSource.Items.Clear();
			lstFilesSource.Items.Add(new ListItem("", ""));
			foreach(IFilesStorageProviderV30 prov in Collectors.FilesProviderCollector.AllProviders) {
				if(!prov.ReadOnly) {
					lstFilesSource.Items.Add(new ListItem(prov.Information.Name, prov.GetType().ToString()));
				}
			}

			lblSettingsSource.Text = Settings.Provider.Information.Name;
			lstSettingsDestination.Items.Clear();
			lstSettingsDestination.Items.Add(new ListItem("", ""));
			if(Settings.Provider.GetType().FullName != typeof(SettingsStorageProvider).FullName) {
				lstSettingsDestination.Items.Add(new ListItem(SettingsStorageProvider.ProviderName, typeof(SettingsStorageProvider).FullName));
			}
			foreach(ISettingsStorageProviderV30 prov in ProviderLoader.LoadAllSettingsStorageProviders(Settings.Provider)) {
				if(prov.GetType().FullName != Settings.Provider.GetType().FullName) {
					lstSettingsDestination.Items.Add(new ListItem(prov.Information.Name, prov.GetType().FullName));
				}
			}
		}

		protected void lstPagesSource_SelectedIndexChanged(object sender, EventArgs e) {
			lstPagesDestination.Items.Clear();
			if(lstPagesSource.SelectedValue != "") {
				foreach(IPagesStorageProviderV30 prov in Collectors.PagesProviderCollector.AllProviders) {
					if(!prov.ReadOnly && lstPagesSource.SelectedValue != prov.GetType().ToString()) {
						lstPagesDestination.Items.Add(new ListItem(prov.Information.Name, prov.GetType().ToString()));
					}
				}
			}
			btnMigratePages.Enabled = lstPagesDestination.Items.Count > 0;
		}

		protected void lstUsersSource_SelectedIndexChanged(object sender, EventArgs e) {
			lstUsersDestination.Items.Clear();
			if(lstUsersSource.SelectedValue != "") {
				foreach(IUsersStorageProviderV30 prov in Collectors.UsersProviderCollector.AllProviders) {
					if(IsUsersProviderFullWriteEnabled(prov) && lstUsersSource.SelectedValue != prov.GetType().ToString()) {
						lstUsersDestination.Items.Add(new ListItem(prov.Information.Name, prov.GetType().ToString()));
					}
				}
			}
			btnMigrateUsers.Enabled = lstUsersDestination.Items.Count > 0;
		}

		protected void lstFilesSource_SelectedIndexChanged(object sender, EventArgs e) {
			lstFilesDestination.Items.Clear();
			if(lstFilesSource.SelectedValue != "") {
				foreach(IFilesStorageProviderV30 prov in Collectors.FilesProviderCollector.AllProviders) {
					if(!prov.ReadOnly && lstFilesSource.SelectedValue != prov.GetType().ToString()) {
						lstFilesDestination.Items.Add(new ListItem(prov.Information.Name, prov.GetType().ToString()));
					}
				}
			}
			btnMigrateFiles.Enabled = lstFilesDestination.Items.Count > 0;
		}

		protected void lstSettingsDestination_SelectedIndexChanged(object sender, EventArgs e) {
			btnCopySettings.Enabled = lstSettingsDestination.SelectedValue != "";
		}

		protected void btnMigratePages_Click(object sender, EventArgs e) {
			IPagesStorageProviderV30 from = Collectors.PagesProviderCollector.GetProvider(lstPagesSource.SelectedValue);
			IPagesStorageProviderV30 to = Collectors.PagesProviderCollector.GetProvider(lstPagesDestination.SelectedValue);

			Log.LogEntry("Pages data migration requested from " + from.Information.Name + " to " + to.Information.Name, EntryType.General, SessionFacade.CurrentUsername);

			DataMigrator.MigratePagesStorageProviderData(from, to);

			lblMigratePagesResult.CssClass = "resultok";
			lblMigratePagesResult.Text = Properties.Messages.DataMigrated;
		}

		protected void btnMigrateUsers_Click(object sender, EventArgs e) {
			IUsersStorageProviderV30 from = Collectors.UsersProviderCollector.GetProvider(lstUsersSource.SelectedValue);
			IUsersStorageProviderV30 to = Collectors.UsersProviderCollector.GetProvider(lstUsersDestination.SelectedValue);

			Log.LogEntry("Users data migration requested from " + from.Information.Name + " to " + to.Information.Name, EntryType.General, SessionFacade.CurrentUsername);

			DataMigrator.MigrateUsersStorageProviderData(from, to, true);

			lblMigrateUsersResult.CssClass = "resultok";
			lblMigrateUsersResult.Text = Properties.Messages.DataMigrated;
		}

		protected void btnMigrateFiles_Click(object sender, EventArgs e) {
			IFilesStorageProviderV30 from = Collectors.FilesProviderCollector.GetProvider(lstFilesSource.SelectedValue);
			IFilesStorageProviderV30 to = Collectors.FilesProviderCollector.GetProvider(lstFilesDestination.SelectedValue);

			Log.LogEntry("Files data migration requested from " + from.Information.Name + " to " + to.Information.Name, EntryType.General, SessionFacade.CurrentUsername);

			DataMigrator.MigrateFilesStorageProviderData(from, to, Settings.Provider);

			lblMigrateFilesResult.CssClass = "resultok";
			lblMigrateFilesResult.Text = Properties.Messages.DataMigrated;
		}

		protected void btnCopySettings_Click(object sender, EventArgs e) {
			ISettingsStorageProviderV30 to = null;

			ISettingsStorageProviderV30[] allProviders = ProviderLoader.LoadAllSettingsStorageProviders(Settings.Provider);
			foreach(ISettingsStorageProviderV30 prov in allProviders) {
				if(prov.GetType().ToString() == lstSettingsDestination.SelectedValue) {
					to = prov;
					break;
				}
			}

			Log.LogEntry("Settings data copy requested to " + to.Information.Name, EntryType.General, SessionFacade.CurrentUsername);

			try {
				to.Init(Host.Instance, txtSettingsDestinationConfig.Text);
			}
			catch(InvalidConfigurationException ex) {
				Log.LogEntry("Provider rejected configuration: " + ex.ToString(), EntryType.Error, Log.SystemUsername);
				lblCopySettingsResult.CssClass = "resulterror";
				lblCopySettingsResult.Text = Properties.Messages.ProviderRejectedConfiguration;
				return;
			}

			// Find namespaces
			List<string> namespaces = new List<string>(5);
			foreach(NamespaceInfo ns in Pages.GetNamespaces()) {
				namespaces.Add(ns.Name);
			}

			DataMigrator.CopySettingsStorageProviderData(Settings.Provider, to, namespaces.ToArray(), Collectors.GetAllProviders());

			lblCopySettingsResult.CssClass = "resultok";
			lblCopySettingsResult.Text = Properties.Messages.DataCopied;
		}

		#endregion

		protected void btnExport_Click(object sender, EventArgs e) {
			Log.LogEntry("Data export requested.", EntryType.General, SessionFacade.GetCurrentUsername());

			string tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
			Directory.CreateDirectory(tempDir);
			string zipFileName = Path.Combine(tempDir, "Backup.zip");

			bool backupFileSucceded = BackupRestore.BackupRestore.BackupAll(zipFileName, Settings.Provider.ListPluginAssemblies(),
				Settings.Provider,
				(from p in Collectors.PagesProviderCollector.AllProviders where !p.ReadOnly select p).ToArray(),
				(from p in Collectors.UsersProviderCollector.AllProviders where IsUsersProviderFullWriteEnabled(p) select p).ToArray(),
				(from p in Collectors.FilesProviderCollector.AllProviders where !p.ReadOnly select p).ToArray());

			FileInfo file = new FileInfo(zipFileName);
			Response.Clear();
			Response.AddHeader("content-type", GetMimeType(zipFileName));
			Response.AddHeader("content-disposition", "attachment;filename=Backup.zip");
			Response.AddHeader("content-length", file.Length.ToString());

			Response.TransmitFile(zipFileName);
			Response.Flush();

			Directory.Delete(tempDir, true);
			Log.LogEntry("Data export completed.", EntryType.General, SessionFacade.GetCurrentUsername());
		}

		private string GetMimeType(string ext) {
			string mime = "";
			if(MimeTypes.Types.TryGetValue(ext, out mime)) return mime;
			else return "application/octet-stream";
		}

		/// <summary>
		/// Detects whether a users storage provider fully supports writing to all managed data.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <returns><c>true</c> if the provider fully supports writing all managed data, <c>false</c> otherwise.</returns>
		private static bool IsUsersProviderFullWriteEnabled(IUsersStorageProviderV30 provider) {
			return
				!provider.UserAccountsReadOnly &&
				!provider.UserGroupsReadOnly &&
				!provider.GroupMembershipReadOnly &&
				!provider.UsersDataReadOnly;
		}

	}

	/// <summary>
	/// Represents a provider for display purposes.
	/// </summary>
	public class ProviderRow {

		private string name, typeName, version, author, authorUrl, updateStatus, additionalClass;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:ProviderRow" /> class.
		/// </summary>
		/// <param name="info">The original component information.</param>
		/// <param name="typeName">The type name.</param>
		/// <param name="disabled">A value indicating whether the provider is disabled.</param>
		/// <param name="selected">A value indicating whether the provider is selected.</param>
		public ProviderRow(ComponentInformation info, string typeName, string updateStatus, bool disabled, bool selected) {
			name = info.Name;
			this.typeName = typeName;
			version = info.Version;
			author = info.Author;
			authorUrl = info.Url;
			this.updateStatus = updateStatus;
			additionalClass = disabled ? " disabled" : "";
			additionalClass += selected ? " selected" : "";
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		public string Name {
			get { return name; }
		}

		/// <summary>
		/// Gets the type name.
		/// </summary>
		public string TypeName {
			get { return typeName; }
		}

		/// <summary>
		/// Gets the version.
		/// </summary>
		public string Version {
			get { return version; }
		}

		/// <summary>
		/// Gets the author.
		/// </summary>
		public string Author {
			get { return author; }
		}

		/// <summary>
		/// Gets the author URL.
		/// </summary>
		public string AuthorUrl {
			get { return authorUrl; }
		}

		/// <summary>
		/// Gets the provider update status.
		/// </summary>
		public string UpdateStatus {
			get { return updateStatus; }
		}

		/// <summary>
		/// Gets the additional CSS class.
		/// </summary>
		public string AdditionalClass {
			get { return additionalClass; }
		}

	}

}
