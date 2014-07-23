
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.AclEngine;

namespace ScrewTurn.Wiki.Tests {

	/// <summary>
	/// Implements a dummy Settings Storage Provider to use for testing.
	/// </summary>
	public class TestSettingsStorageProvider : ISettingsStorageProviderV30 {

		public string GetSetting(string name) {
			throw new NotImplementedException();
		}

		public bool SetSetting(string name, string value) {
			throw new NotImplementedException();
		}

		public void BeginBulkUpdate() {
			throw new NotImplementedException();
		}

		public void EndBulkUpdate() {
			throw new NotImplementedException();
		}

		public void LogEntry(string message, EntryType entryType, string user) {
			throw new NotImplementedException();
		}

		public LogEntry[] GetLogEntries() {
			throw new NotImplementedException();
		}

		public void ClearLog() {
			throw new NotImplementedException();
		}

		public void CutLog(int size) {
			throw new NotImplementedException();
		}

		public int LogSize {
			get { throw new NotImplementedException(); }
		}

		public string GetMetaDataItem(MetaDataItem item, string tag) {
			throw new NotImplementedException();
		}

		public bool SetMetaDataItem(MetaDataItem item, string tag, string content) {
			throw new NotImplementedException();
		}

		public RecentChange[] GetRecentChanges() {
			throw new NotImplementedException();
		}

		public bool AddRecentChange(string page, string title, string messageSubject, DateTime dateTime, string user, ScrewTurn.Wiki.PluginFramework.Change change, string descr) {
			throw new NotImplementedException();
		}

		public void Init(IHostV30 host, string config) {
			throw new NotImplementedException();
		}

		public void Shutdown() {
			throw new NotImplementedException();
		}

		public ComponentInformation Information {
			get { throw new NotImplementedException(); }
		}

		public string ConfigHelpHtml {
			get { throw new NotImplementedException(); }
		}

		public string[] ListPluginAssemblies() {
			throw new NotImplementedException();
		}

		public bool StorePluginAssembly(string filename, byte[] assembly) {
			throw new NotImplementedException();
		}

		public byte[] RetrievePluginAssembly(string filename) {
			throw new NotImplementedException();
		}

		public bool DeletePluginAssembly(string filename) {
			throw new NotImplementedException();
		}

		public bool SetPluginStatus(string typeName, bool enabled) {
			throw new NotImplementedException();
		}

		public bool GetPluginStatus(string typeName) {
			throw new NotImplementedException();
		}

		public bool SetPluginConfiguration(string typeName, string config) {
			throw new NotImplementedException();
		}

		public string GetPluginConfiguration(string typeName) {
			throw new NotImplementedException();
		}

		public IAclManager AclManager {
			get {
				throw new NotImplementedException();
			}
		}

		public bool StoreOutgoingLinks(string page, string[] outgoingLinks) {
			throw new NotImplementedException();
		}

		public string[] GetOutgoingLinks(string page) {
			throw new NotImplementedException();
		}

		public IDictionary<string, string[]> GetAllOutgoingLinks() {
			throw new NotImplementedException();
		}

		public bool DeleteOutgoingLinks(string page) {
			throw new NotImplementedException();
		}

		public bool UpdateOutgoingLinksForRename(string oldName, string newName) {
			throw new NotImplementedException();
		}

		public IDictionary<string, string> GetAllSettings() {
			throw new NotImplementedException();
		}

		public bool IsFirstApplicationStart() {
			throw new NotImplementedException();
		}

	}

}
