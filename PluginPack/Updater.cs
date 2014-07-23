
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using System.Net;
using System.IO;

namespace ScrewTurn.Wiki.Plugins.PluginPack {

	/// <summary>
	/// Plugin with the sole purpose of removing PluginPack.dll in favor of separate DLLs.
	/// </summary>
	public class Updater : IFormatterProviderV30 {

		private static bool AlreadyRun = false;

		private static readonly ComponentInformation _info = new ComponentInformation("Updater Plugin", "Threeplicate Srl", "3.0.2.538", "http://www.screwturn.eu", null);

		/// <summary>
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public void Init(IHostV30 host, string config) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			if(AlreadyRun) return;

			// 1. Delete PluginPack.dll
			// 2. Download all other DLLs

			string root = "http://www.screwturn.eu/Version/PluginPack/";

			string[] dllNames = new string[] {
				"DownloadCounterPlugin.dll",
				"FootnotesPlugin.dll",
				"MultilanguageContentPlugin.dll",
				"RssFeedDisplayPlugin.dll",
				"UnfuddleTicketsPlugin.dll"
			};

			string[] providerNames = new string[] {
				"ScrewTurn.Wiki.Plugins.PluginPack.DownloadCounter",
				"ScrewTurn.Wiki.Plugins.PluginPack.Footnotes",
				"ScrewTurn.Wiki.Plugins.PluginPack.MultilanguageContentPlugin",
				"ScrewTurn.Wiki.Plugins.PluginPack.RssFeedDisplay",
				"ScrewTurn.Wiki.Plugins.PluginPack.UnfuddleTickets",
			};

			Dictionary<string, byte[]> assemblies = new Dictionary<string, byte[]>(dllNames.Length);

			try {
				foreach(string dll in dllNames) {
					host.LogEntry("Downloading " + dll, LogEntryType.General, null, this);

					HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(root + dll);
					req.AllowAutoRedirect = true;
					HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

					if(resp.StatusCode == HttpStatusCode.OK) {
						BinaryReader reader = new BinaryReader(resp.GetResponseStream());
						byte[] content = reader.ReadBytes((int)resp.ContentLength);
						reader.Close();

						assemblies.Add(dll, content);
					}
					else {
						throw new InvalidOperationException("Response status code for " + dll + ":" + resp.StatusCode.ToString());
					}
				}

				foreach(string dll in dllNames) {
					host.GetSettingsStorageProvider().StorePluginAssembly(dll, assemblies[dll]);
				}

				foreach(string dll in dllNames) {
					LoadProvider(dll);
				}

				host.GetSettingsStorageProvider().DeletePluginAssembly("PluginPack.dll");

				AlreadyRun = true;
			}
			catch(Exception ex) {
				host.LogEntry("Error occurred during automatic DLL updating with Updater Plugin\n" + ex.ToString(), LogEntryType.Error, null, this);
			}
		}

		private void LoadProvider(string dll) {
			Type loader = Type.GetType("ScrewTurn.Wiki.ProviderLoader, ScrewTurn.Wiki.Core");
			var method = loader.GetMethod("LoadFromAuto");
			method.Invoke(null, new[] { dll });
		}

		/// <summary>
		/// Method invoked on shutdown.
		/// </summary>
		/// <remarks>This method might not be invoked in some cases.</remarks>
		public void Shutdown() {
		}

		/// <summary>
		/// Specifies whether or not to execute Phase 1.
		/// </summary>
		public bool PerformPhase1 {
			get { return false; }
		}

		/// <summary>
		/// Specifies whether or not to execute Phase 2.
		/// </summary>
		public bool PerformPhase2 {
			get { return false; }
		}

		/// <summary>
		/// Specifies whether or not to execute Phase 3.
		/// </summary>
		public bool PerformPhase3 {
			get { return false; }
		}

		/// <summary>
		/// Gets the execution priority of the provider (0 lowest, 100 highest).
		/// </summary>
		public int ExecutionPriority {
			get { return 50; }
		}

		/// <summary>
		/// Performs a Formatting phase.
		/// </summary>
		/// <param name="raw">The raw content to Format.</param>
		/// <param name="context">The Context information.</param>
		/// <param name="phase">The Phase.</param>
		/// <returns>The Formatted content.</returns>
		public string Format(string raw, ContextInformation context, FormattingPhase phase) {
			return raw;
		}

		/// <summary>
		/// Prepares the title of an item for display (always during phase 3).
		/// </summary>
		/// <param name="title">The input title.</param>
		/// <param name="context">The context information.</param>
		/// <returns>The prepared title (no markup allowed).</returns>
		public string PrepareTitle(string title, ContextInformation context) {
			return title;
		}

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public ComponentInformation Information {
			get { return _info; }
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public string ConfigHelpHtml {
			get { return null; }
		}

	}

}
