
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScrewTurn.Wiki.AclEngine;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Implements a file-based ACL Storer.
	/// </summary>
	public class AclStorer : AclStorerBase {

		private string file;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:AclStorer" /> class.
		/// </summary>
		/// <param name="aclManager">The instance of the ACL Manager to handle.</param>
		/// <param name="file">The storage file.</param>
		public AclStorer(IAclManager aclManager, string file)
			: base(aclManager) {

			if(file == null) throw new ArgumentNullException("file");
			if(file.Length == 0) throw new ArgumentException("File cannot be empty", "file");

			this.file = file;
		}

		/// <summary>
		/// Loads data from storage.
		/// </summary>
		/// <returns>The loaded ACL entries.</returns>
		protected override AclEntry[] LoadDataInternal() {
			lock(this) {
				if(!File.Exists(file)) {
					File.Create(file).Close();
					return new AclEntry[0];
				}

				// Format
				// Resource|Action|Subject|(1|0)

				string[] lines = File.ReadAllLines(file);

				AclEntry[] result = new AclEntry[lines.Length];

				string[] fields;
				for(int i = 0; i < lines.Length; i++) {
					fields = lines[i].Split('|');

					result[i] = new AclEntry(fields[0], fields[1], fields[2], (fields[3] == "1" ? Value.Grant : Value.Deny));
				}

				return result;
			}
		}

		/// <summary>
		/// Dumps a <see cref="T:AclEntry" /> into a string.
		/// </summary>
		/// <param name="entry">The entry to dump.</param>
		/// <returns>The resulting string.</returns>
		private static string DumpAclEntry(AclEntry entry) {
			return string.Format("{0}|{1}|{2}|{3}", entry.Resource, entry.Action, entry.Subject, (entry.Value == Value.Grant ? "1" : "0"));
		}

		/// <summary>
		/// Deletes some entries.
		/// </summary>
		/// <param name="entries">The entries to delete.</param>
		protected override void DeleteEntries(AclEntry[] entries) {
			lock(this) {
				AclEntry[] allEntries = LoadDataInternal();

				StringBuilder sb = new StringBuilder(10000);
				foreach(AclEntry originalEntry in allEntries) {
					// If the current entry is not contained in the entries array, then preserve it
					bool delete = false;
					foreach(AclEntry entryToDelete in entries) {
						if(AclEntry.Equals(originalEntry, entryToDelete)) {
							delete = true;
							break;
						}
					}

					if(!delete) {
						sb.Append(DumpAclEntry(originalEntry));
						sb.Append("\r\n");
					}
				}

				File.WriteAllText(file, sb.ToString());
			}
		}

		/// <summary>
		/// Stores some entries.
		/// </summary>
		/// <param name="entries">The entries to store.</param>
		protected override void StoreEntries(AclEntry[] entries) {
			lock(this) {
				StringBuilder sb = new StringBuilder(100);
				foreach(AclEntry entry in entries) {
					sb.Append(DumpAclEntry(entry));
					sb.Append("\r\n");
				}

				File.AppendAllText(file, sb.ToString());
			}
		}

	}

}
