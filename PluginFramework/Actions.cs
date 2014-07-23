
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.AclEngine;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Contains actions for resources.
	/// </summary>
	public static class Actions {

		/// <summary>
		/// The full control action.
		/// </summary>
		public const string FullControl = AclEntry.FullControlAction;

		/// <summary>
		/// Contains actions for global resources.
		/// </summary>
		public static class ForGlobals {

			/// <summary>
			/// The master prefix for global resources ('G').
			/// </summary>
			public const string ResourceMasterPrefix = "G";

			/// <summary>
			/// Manage user accounts.
			/// </summary>
			public const string ManageAccounts = "Man_Acc";
			/// <summary>
			/// Manage user groups.
			/// </summary>
			public const string ManageGroups = "Man_Grp";
			/// <summary>
			/// Manage pages and categories.
			/// </summary>
			public const string ManagePagesAndCategories = "Man_PgCat";
			/// <summary>
			/// Manage page discussions.
			/// </summary>
			public const string ManageDiscussions = "Man_Disc";
			/// <summary>
			/// Manage namespaces.
			/// </summary>
			public const string ManageNamespaces = "Man_Ns";
			/// <summary>
			/// Manage configuration.
			/// </summary>
			public const string ManageConfiguration = "Man_Conf";
			/// <summary>
			/// Manage providers.
			/// </summary>
			public const string ManageProviders = "Man_Prov";
			/// <summary>
			/// Manage files.
			/// </summary>
			public const string ManageFiles = "Man_Files";
			/// <summary>
			/// Manage snippets and templates.
			/// </summary>
			public const string ManageSnippetsAndTemplates = "Man_Snips_Temps";
			/// <summary>
			/// Manage navigation paths.
			/// </summary>
			public const string ManageNavigationPaths = "Man_NavPath";
			/// <summary>
			/// Manage meta-files.
			/// </summary>
			public const string ManageMetaFiles = "Man_MetaFiles";
			/// <summary>
			/// Manage permissions.
			/// </summary>
			public const string ManagePermissions = "Man_Perms";

			/// <summary>
			/// Gets an array containing all actions.
			/// </summary>
			public static readonly string[] All = new string[] {
				ManageAccounts,
				ManageGroups,
				ManagePagesAndCategories,
				ManageDiscussions,
				ManageNamespaces,
				ManageConfiguration,
				ManageProviders,
				ManageFiles,
				ManageSnippetsAndTemplates,
				ManageNavigationPaths,
				ManageMetaFiles,
				ManagePermissions
			};

			/// <summary>
			/// Gets the full name of an action.
			/// </summary>
			/// <param name="name">The internal name.</param>
			/// <returns>The full name.</returns>
			public static string GetFullName(string name) {
				if(name == FullControl) return Exchanger.ResourceExchanger.GetResource("Action_FullControl");
				else return Exchanger.ResourceExchanger.GetResource("Action_" + name);
			}

		}

		/// <summary>
		/// Contains actions for namespaces.
		/// </summary>
		public static class ForNamespaces {

			/// <summary>
			/// The master prefix for namespaces ('N.').
			/// </summary>
			public const string ResourceMasterPrefix = "N.";

			/// <summary>
			/// The local escalation policies.
			/// </summary>
			public static readonly Dictionary<string, string[]> LocalEscalators = new Dictionary<string, string[]>() {
				{ ReadPages, new string[] { CreatePages, ModifyPages, DeletePages, ManagePages } },
				{ ModifyPages, new string[] { ManagePages } },
				{ DeletePages, new string[] { ManagePages } },
				{ CreatePages, new string[] { ManagePages } },
				{ ReadDiscussion, new string[] { PostDiscussion, ManageDiscussion } },
				{ PostDiscussion, new string[] { ManageDiscussion } },
				{ ManageDiscussion, new string[] { ManagePages } },
				{ DownloadAttachments, new string[] { UploadAttachments, DeleteAttachments } },
				{ UploadAttachments, new string[] { DeleteAttachments } }
			};

			/// <summary>
			/// The global escalation policies.
			/// </summary>
			public static readonly Dictionary<string, string[]> GlobalEscalators = new Dictionary<string, string[]>() {
				{ ReadPages, new string[] { Actions.ForGlobals.ManagePagesAndCategories, Actions.ForGlobals.ManageNamespaces } },
				{ CreatePages, new string[] { Actions.ForGlobals.ManagePagesAndCategories, Actions.ForGlobals.ManageNamespaces } },
				{ ModifyPages, new string[] { Actions.ForGlobals.ManagePagesAndCategories, Actions.ForGlobals.ManageNamespaces } },
				{ ReadDiscussion, new string[] { Actions.ForGlobals.ManageDiscussions } },
				{ PostDiscussion, new string[] { Actions.ForGlobals.ManageDiscussions } },
				{ ManageDiscussion, new string[] { Actions.ForGlobals.ManageDiscussions } },
				{ DeletePages, new string[] { Actions.ForGlobals.ManagePagesAndCategories, Actions.ForGlobals.ManageNamespaces } },
				{ ManagePages, new string[] { Actions.ForGlobals.ManagePagesAndCategories, Actions.ForGlobals.ManageNamespaces } },
				{ ManageCategories, new string[] { Actions.ForGlobals.ManagePagesAndCategories } },
				{ DownloadAttachments, new string[] { Actions.ForGlobals.ManageFiles } },
				{ UploadAttachments, new string[] { Actions.ForGlobals.ManageFiles } },
				{ DeleteAttachments, new string[] { Actions.ForGlobals.ManageFiles } }
			};

			/// <summary>
			/// Read pages.
			/// </summary>
			public const string ReadPages = "Rd_Pg";
			/// <summary>
			/// Create pages.
			/// </summary>
			public const string CreatePages = "Crt_Pg";
			/// <summary>
			/// Modify pages.
			/// </summary>
			public const string ModifyPages = "Mod_Pg";
			/// <summary>
			/// Delete pages.
			/// </summary>
			public const string DeletePages = "Del_Pg";
			/// <summary>
			/// Manage pages.
			/// </summary>
			public const string ManagePages = "Man_Pg";
			/// <summary>
			/// Read page discussions.
			/// </summary>
			public const string ReadDiscussion = "Rd_Disc";
			/// <summary>
			/// Post messages in page discussions.
			/// </summary>
			public const string PostDiscussion = "Pst_Disc";
			/// <summary>
			/// Manage messages in page discussions.
			/// </summary>
			public const string ManageDiscussion = "Man_Disc";
			/// <summary>
			/// Manage categories.
			/// </summary>
			public const string ManageCategories = "Man_Cat";
			/// <summary>
			/// Download attachments.
			/// </summary>
			public const string DownloadAttachments = "Down_Attn";
			/// <summary>
			/// Upload attachments.
			/// </summary>
			public const string UploadAttachments = "Up_Attn";
			/// <summary>
			/// Delete attachments.
			/// </summary>
			public const string DeleteAttachments = "Del_Attn";

			/// <summary>
			/// Gets an array containing all actions.
			/// </summary>
			public static readonly string[] All = new string[] {
				ReadPages,
				CreatePages,
				ModifyPages,
				DeletePages,
				ManagePages,
				ReadDiscussion,
				PostDiscussion,
				ManageDiscussion,
				ManageCategories,
				DownloadAttachments,
				UploadAttachments,
				DeleteAttachments
			};

			/// <summary>
			/// Gets the full name of an action.
			/// </summary>
			/// <param name="name">The internal name.</param>
			/// <returns>The full name.</returns>
			public static string GetFullName(string name) {
				if(name == FullControl) return Exchanger.ResourceExchanger.GetResource("Action_FullControl");
				else return Exchanger.ResourceExchanger.GetResource("Action_" + name);
			}

		}

		/// <summary>
		/// Contains actions for pages.
		/// </summary>
		public static class ForPages {

			/// <summary>
			/// The master prefix for pages ('P.').
			/// </summary>
			public const string ResourceMasterPrefix = "P.";

			/// <summary>
			/// The local escalation policies.
			/// </summary>
			public static readonly Dictionary<string, string[]> LocalEscalators = new Dictionary<string, string[]>() {
				{ ReadPage, new string[] { ModifyPage, ManagePage } },
				{ ModifyPage, new string[] { ManagePage } },
				{ ReadDiscussion, new string[] { PostDiscussion, ManageDiscussion } },
				{ PostDiscussion, new string[] { ManageDiscussion } },
				{ ManageDiscussion, new string[] { ManagePage } },
				{ DownloadAttachments, new string[] { UploadAttachments, DeleteAttachments } },
				{ UploadAttachments, new string[] { DeleteAttachments } }
			};

			/// <summary>
			/// The namespace escalation policies.
			/// </summary>
			public static readonly Dictionary<string, string[]> NamespaceEscalators = new Dictionary<string, string[]>() {
				{ ReadPage, new string[] { Actions.ForNamespaces.ReadPages, Actions.ForNamespaces.ModifyPages, Actions.ForNamespaces.ManagePages, Actions.ForNamespaces.CreatePages, Actions.ForNamespaces.DeletePages } },
				{ ModifyPage, new string[] { Actions.ForNamespaces.CreatePages, Actions.ForNamespaces.ModifyPages, Actions.ForNamespaces.ManagePages, Actions.ForNamespaces.CreatePages, Actions.ForNamespaces.DeletePages } },
				{ ManagePage, new string[] { Actions.ForNamespaces.ManagePages } },
				{ ReadDiscussion, new string[] { Actions.ForNamespaces.ReadDiscussion, Actions.ForNamespaces.PostDiscussion, Actions.ForNamespaces.ManageDiscussion } },
				{ PostDiscussion, new string[] { Actions.ForNamespaces.PostDiscussion, Actions.ForNamespaces.ManageDiscussion } },
				{ ManageDiscussion, new string[] { Actions.ForNamespaces.ManageDiscussion } },
				{ DownloadAttachments, new string[] { Actions.ForNamespaces.DownloadAttachments, Actions.ForNamespaces.UploadAttachments, Actions.ForNamespaces.DeleteAttachments } },
				{ ManageCategories, new string[] { Actions.ForNamespaces.ManageCategories } },
				{ UploadAttachments, new string[] { Actions.ForNamespaces.UploadAttachments } },
				{ DeleteAttachments, new string[] { Actions.ForNamespaces.DeleteAttachments } }
			};

			/// <summary>
			/// The global escalation policies.
			/// </summary>
			public static readonly Dictionary<string, string[]> GlobalEscalators = new Dictionary<string, string[]>() {
				{ ReadPage, new string[] { Actions.ForGlobals.ManagePagesAndCategories, Actions.ForGlobals.ManageNamespaces } },
				{ ModifyPage, new string[] { Actions.ForGlobals.ManagePagesAndCategories, Actions.ForGlobals.ManageNamespaces } },
				{ ManagePage, new string[] { Actions.ForGlobals.ManagePagesAndCategories, Actions.ForGlobals.ManageNamespaces } },
				{ ReadDiscussion, new string[] { Actions.ForGlobals.ManageDiscussions } },
				{ PostDiscussion, new string[] { Actions.ForGlobals.ManageDiscussions } },
				{ ManageDiscussion, new string[] { Actions.ForGlobals.ManageDiscussions } },
				{ ManageCategories, new string[] { Actions.ForGlobals.ManagePagesAndCategories } },
				{ DownloadAttachments, new string[] { Actions.ForGlobals.ManageFiles } },
				{ UploadAttachments, new string[] { Actions.ForGlobals.ManageFiles } },
				{ DeleteAttachments, new string[] { Actions.ForGlobals.ManageFiles } }
			};

			/// <summary>
			/// Read the page.
			/// </summary>
			public const string ReadPage = "Rd_1Pg";
			/// <summary>
			/// Modify the page.
			/// </summary>
			public const string ModifyPage = "Mod_1Pg";
			/// <summary>
			/// Manage the page.
			/// </summary>
			public const string ManagePage = "Man_1Pg";
			/// <summary>
			/// Read page discussion.
			/// </summary>
			public const string ReadDiscussion = "Rd_1Disc";
			/// <summary>
			/// Post messages in page discussion.
			/// </summary>
			public const string PostDiscussion = "Pst_1Disc";
			/// <summary>
			/// Manage page discussion.
			/// </summary>
			public const string ManageDiscussion = "Man_1Disc";
			/// <summary>
			/// Manage the categories of the page.
			/// </summary>
			public const string ManageCategories = "Man_1Cat";
			/// <summary>
			/// Download attachments.
			/// </summary>
			public const string DownloadAttachments = "Down_1Attn";
			/// <summary>
			/// Upload attachments.
			/// </summary>
			public const string UploadAttachments = "Up_1Attn";
			/// <summary>
			/// Delete attachments.
			/// </summary>
			public const string DeleteAttachments = "Del_1Attn";

			/// <summary>
			/// Gets an array containing all actions.
			/// </summary>
			public static readonly string[] All = new string[] {
				ReadPage,
				ModifyPage,
				ManagePage,
				ReadDiscussion,
				PostDiscussion,
				ManageDiscussion,
				ManageCategories,
				DownloadAttachments,
				UploadAttachments,
				DeleteAttachments
			};

			/// <summary>
			/// Gets the full name of an action.
			/// </summary>
			/// <param name="name">The internal name.</param>
			/// <returns>The full name.</returns>
			public static string GetFullName(string name) {
				if(name == FullControl) return Exchanger.ResourceExchanger.GetResource("Action_FullControl");
				else return Exchanger.ResourceExchanger.GetResource("Action_" + name);
			}

		}

		/// <summary>
		/// Contains actions for file directories.
		/// </summary>
		public static class ForDirectories {

			/// <summary>
			/// The master prefix for directories ('D.').
			/// </summary>
			public const string ResourceMasterPrefix = "D.";

			/// <summary>
			/// The local escalation policies.
			/// </summary>
			public static readonly Dictionary<string, string[]> LocalEscalators = new Dictionary<string, string[]>() {
				{ List, new string[] { DownloadFiles, UploadFiles, DeleteFiles, CreateDirectories, DeleteDirectories } },
				{ DownloadFiles, new string[] { UploadFiles, DeleteFiles, CreateDirectories, DeleteDirectories } },
				{ UploadFiles, new string[] { DeleteFiles } },
				{ CreateDirectories, new string[] { DeleteDirectories } }
			};

			/// <summary>
			/// The global escalation policies.
			/// </summary>
			public static readonly Dictionary<string, string[]> GlobalEscalators = new Dictionary<string, string[]>() {
				{ List, new string[] { Actions.ForGlobals.ManageFiles } },
				{ DownloadFiles, new string[] { Actions.ForGlobals.ManageFiles } },
				{ UploadFiles, new string[] { Actions.ForGlobals.ManageFiles } },
				{ DeleteFiles, new string[] { Actions.ForGlobals.ManageFiles } },
				{ CreateDirectories, new string[] { Actions.ForGlobals.ManageFiles } },
				{ DeleteDirectories, new string[] { Actions.ForGlobals.ManageFiles } }
			};

			/// <summary>
			/// List files and directories.
			/// </summary>
			public const string List = "List";
			/// <summary>
			/// Download files.
			/// </summary>
			public const string DownloadFiles = "Down_Files";
			/// <summary>
			/// Upload files.
			/// </summary>
			public const string UploadFiles = "Up_Files";
			/// <summary>
			/// Delete files.
			/// </summary>
			public const string DeleteFiles = "Del_Files";
			/// <summary>
			/// Create directories.
			/// </summary>
			public const string CreateDirectories = "Crt_Dirs";
			/// <summary>
			/// Delete directories.
			/// </summary>
			public const string DeleteDirectories = "Del_Dirs";

			/// <summary>
			/// Gets an array containing all actions.
			/// </summary>
			public static readonly string[] All = new string[] {
				List,
				DownloadFiles,
				UploadFiles,
				DeleteFiles,
				CreateDirectories,
				DeleteDirectories
			};

			/// <summary>
			/// Gets the full name of an action.
			/// </summary>
			/// <param name="name">The internal name.</param>
			/// <returns>The full name.</returns>
			public static string GetFullName(string name) {
				if(name == FullControl) return Exchanger.ResourceExchanger.GetResource("Action_FullControl");
				else return Exchanger.ResourceExchanger.GetResource("Action_" + name);
			}

		}

	}

}
