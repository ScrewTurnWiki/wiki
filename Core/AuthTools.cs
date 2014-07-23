
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Implements tools supporting athorization management.
	/// </summary>
	public static class AuthTools {

		/// <summary>
		/// Determines whether an action is valid.
		/// </summary>
		/// <param name="action">The action to validate.</param>
		/// <param name="validActions">The list of valid actions.</param>
		/// <returns><c>true</c> if the action is valid, <c>false</c> otherwise.</returns>
		public static bool IsValidAction(string action, string[] validActions) {
			return Array.Find(validActions, delegate(string s) { return s == action; }) != null;
		}

		/// <summary>
		/// Determines whether a subject is a group.
		/// </summary>
		/// <param name="subject">The subject to test.</param>
		/// <returns><c>true</c> if the subject is a group, <c>false</c> if it is a user.</returns>
		public static bool IsGroup(string subject) {
			if(subject == null) throw new ArgumentNullException("subject");
			if(subject.Length < 2) throw new ArgumentException("Subject must contain at least 2 characters", "subject");

			return subject.ToUpperInvariant().StartsWith("G.");
		}

		/// <summary>
		/// Prepends the proper string to a username.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <returns>The resulting username.</returns>
		public static string PrepareUsername(string username) {
			if(username == null) throw new ArgumentNullException("username");
			if(username.Length == 0) throw new ArgumentException("Username cannot be empty", "username");

			return "U." + username;
		}

		/// <summary>
		/// Prepends the proper string to each group name in an array.
		/// </summary>
		/// <param name="groups">The group array.</param>
		/// <returns>The resulting group array.</returns>
		public static string[] PrepareGroups(string[] groups) {
			if(groups == null) throw new ArgumentNullException("groups");

			if(groups.Length == 0) return groups;

			string[] result = new string[groups.Length];

			for(int i = 0; i < groups.Length; i++) {
				if(groups[i] == null) throw new ArgumentNullException("groups");
				if(groups[i].Length == 0) throw new ArgumentException("Groups cannot contain empty elements", "groups");

				result[i] = PrepareGroup(groups[i]);
			}

			return result;
		}

		/// <summary>
		/// Prepends the proper string to the group name.
		/// </summary>
		/// <param name="group">The group name.</param>
		/// <returns>The result string.</returns>
		public static string PrepareGroup(string group) {
			return "G." + group;
		}

		/// <summary>
		/// Gets the proper full name for a directory.
		/// </summary>
		/// <param name="prov">The provider.</param>
		/// <param name="name">The directory name.</param>
		/// <returns>The full name (<b>not</b> prepended with <see cref="Actions.ForDirectories.ResourceMasterPrefix" />.</returns>
		public static string GetDirectoryName(IFilesStorageProviderV30 prov, string name) {
			if(prov == null) throw new ArgumentNullException("prov");
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");

			return "(" + prov.GetType().FullName + ")" + name;
		}

	}

}
