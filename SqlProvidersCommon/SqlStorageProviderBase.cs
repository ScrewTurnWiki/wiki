
using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Plugins.SqlCommon {

	/// <summary>
	/// Implements a base class for a SQL storage provider.
	/// </summary>
	public abstract class SqlStorageProviderBase : SqlClassBase {

		/// <summary>
		/// The connection string.
		/// </summary>
		protected string connString;

		/// <summary>
		/// The host.
		/// </summary>
		protected IHostV30 host;

		/// <summary>
		/// Gets a new command builder object.
		/// </summary>
		/// <returns>The command builder.</returns>
		protected abstract ICommandBuilder GetCommandBuilder();

		/// <summary>
		/// Logs an exception.
		/// </summary>
		/// <param name="ex">The exception.</param>
		protected override void LogException(Exception ex) {
			try {
				host.LogEntry(ex.ToString(), LogEntryType.Error, null, this);
			}
			catch { }
		}

		#region IProvider Members

		/// <summary>
		/// Validates a connection string.
		/// </summary>
		/// <param name="connString">The connection string to validate.</param>
		/// <remarks>If the connection string is invalid, the method throws <see cref="T:InvalidConfigurationException" />.</remarks>
		protected abstract void ValidateConnectionString(string connString);

		/// <summary>
		/// Creates or updates the database schema if necessary.
		/// </summary>
		protected abstract void CreateOrUpdateDatabaseIfNecessary();

		/// <summary>
		/// Tries to load the configuration from a corresponding v2 provider.
		/// </summary>
		/// <returns>The configuration, or an empty string.</returns>
		protected abstract string TryLoadV2Configuration();

		/// <summary>
		/// Tries to load the configuration of the corresponding settings storage provider.
		/// </summary>
		/// <returns>The configuration, or an empty string.</returns>
		protected abstract string TryLoadSettingsStorageProviderConfiguration();

		/// <summary>
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <b>host</b> or <b>config</b> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <b>config</b> is not valid or is incorrect.</exception>
		public void Init(IHostV30 host, string config) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			this.host = host;

			if(config.Length == 0) {
				// Try to load v2 provider configuration
				config = TryLoadV2Configuration();
			}
			
			if(config == null || config.Length == 0) {
				// Try to load Settings Storage Provider configuration
				config = TryLoadSettingsStorageProviderConfiguration();
			}

			if(config == null) config = "";

			ValidateConnectionString(config);

			connString = config;

			CreateOrUpdateDatabaseIfNecessary();
		}

		/// <summary>
		/// Method invoked on shutdown.
		/// </summary>
		/// <remarks>This method might not be invoked in some cases.</remarks>
		public void Shutdown() {
		}

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public abstract ComponentInformation Information {
			get;
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public abstract string ConfigHelpHtml {
			get;
		}

		#endregion

	}

}
