
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.AclEngine {

	/// <summary>
	/// Implements tools for evaluating permissions.
	/// </summary>
	public static class AclEvaluator {

		/// <summary>
		/// Decides whether a user, member of some groups, is authorized to perform an action on a resource.
		/// </summary>
		/// <param name="resource">The resource.</param>
		/// <param name="action">The action on the resource.</param>
		/// <param name="user">The user, in the form 'U.Name'.</param>
		/// <param name="groups">The groups the user is member of, in the form 'G.Name'.</param>
		/// <param name="entries">The available ACL entries for the resource.</param>
		/// <returns>The positive, negative, or indeterminate result.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="resource"/>, <paramref name="action"/>, <paramref name="user"/>, <paramref name="groups"/> or <paramref name="entries"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="resource"/>, <paramref name="action"/>, <paramref name="user"/> are empty, or if <paramref name="action"/> equals <see cref="AclEntry.FullControlAction"/>.</exception>
		public static Authorization AuthorizeAction(string resource, string action, string user, string[] groups, AclEntry[] entries) {
			if(resource == null) throw new ArgumentNullException("resource");
			if(resource.Length == 0) throw new ArgumentException("Resource cannot be empty", "resource");
			if(action == null) throw new ArgumentNullException("action");
			if(action.Length == 0) throw new ArgumentException("Action cannot be empty", "action");
			if(action == AclEntry.FullControlAction) throw new ArgumentException("Action cannot be the FullControl flag", "action");
			if(user == null) throw new ArgumentNullException("user");
			if(user.Length == 0) throw new ArgumentException("User cannot be empty", "user");
			if(groups == null) throw new ArgumentNullException("groups");
			if(entries == null) throw new ArgumentNullException("entries");

			// Simple ACL model
			// Sort entries so that FullControl ones are at the bottom
			// First look for an entry specific for the user
			// If not found, look for a group that denies the permission

			AclEntry[] sortedEntries = new AclEntry[entries.Length];
			Array.Copy(entries, sortedEntries, entries.Length);

			Array.Sort(sortedEntries, delegate(AclEntry x, AclEntry y) {
				return x.Action.CompareTo(y.Action);
			});
			Array.Reverse(sortedEntries);

			foreach(AclEntry entry in sortedEntries) {
				if(entry.Resource == resource && (entry.Action == action || entry.Action == AclEntry.FullControlAction) && entry.Subject == user) {
					if(entry.Value == Value.Grant) return Authorization.Granted;
					else if(entry.Value == Value.Deny) return Authorization.Denied;
					else throw new NotSupportedException("Entry value not supported");
				}
			}

			// For each group, a decision is made
			Dictionary<string, bool> groupFullControlGrant = new Dictionary<string, bool>();
			Dictionary<string, bool> groupExplicitGrant = new Dictionary<string, bool>();
			Dictionary<string, bool> groupFullControlDeny = new Dictionary<string, bool>();

			foreach(string group in groups) {
				foreach(AclEntry entry in entries) {

					if(entry.Resource == resource && entry.Subject == group) {
						if(!groupFullControlGrant.ContainsKey(group)) {
							groupFullControlGrant.Add(group, false);
							groupExplicitGrant.Add(group, false);
							groupFullControlDeny.Add(group, false);
						}

						if(entry.Action == action) {
							// Explicit action
							if(entry.Value == Value.Grant) {
								// An explicit grant only wins if there are no other explicit deny
								groupExplicitGrant[group] = true;
							}
							else if(entry.Value == Value.Deny) {
								// An explicit deny wins over all other entries
								return Authorization.Denied;
							}
						}
						else if(entry.Action == AclEntry.FullControlAction) {
							// Full control, lower priority
							if(entry.Value == Value.Deny) {
								groupFullControlDeny[group] = true;
							}
							else if(entry.Value == Value.Grant) {
								groupFullControlGrant[group] = true;
							}
						}
					}
				}
			}

			// Any explicit grant found at this step wins, because all explicit deny have been processed previously
			bool tentativeGrant = false;
			bool tentativeDeny = false;
			foreach(string group in groupFullControlGrant.Keys) {
				if(groupExplicitGrant[group]) return Authorization.Granted;
				
				if(groupFullControlGrant[group] && !groupFullControlDeny[group]) tentativeGrant = true;
				if(!groupFullControlGrant[group] && groupFullControlDeny[group]) tentativeDeny = true;
			}
			if(tentativeGrant && !tentativeDeny) return Authorization.Granted;
			else if(tentativeDeny) return Authorization.Denied;
			else return Authorization.Unknown;
		}

	}

	/// <summary>
	/// Lists legal authorization values.
	/// </summary>
	public enum Authorization {
		/// <summary>
		/// Authorization granted.
		/// </summary>
		Granted,
		/// <summary>
		/// Authorization denied.
		/// </summary>
		Denied,
		/// <summary>
		/// No information available.
		/// </summary>
		Unknown
	}

}
