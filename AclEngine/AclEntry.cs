
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.AclEngine {

	/// <summary>
	/// Represents an ACL Entry.
	/// </summary>
	public class AclEntry {

		/// <summary>
		/// The full control action.
		/// </summary>
		public const string FullControlAction = "*";

		/// <summary>
		/// The controlled resource.
		/// </summary>
		private string resource;
		/// <summary>
		/// The controlled action on the resource.
		/// </summary>
		private string action;
		/// <summary>
		/// The subject whose access to the resource/action is controlled.
		/// </summary>
		private string subject;
		/// <summary>
		/// The entry value.
		/// </summary>
		private Value value;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:AclEntry" /> class.
		/// </summary>
		/// <param name="resource">The controlled resource.</param>
		/// <param name="action">The controlled action on the resource.</param>
		/// <param name="subject">The subject whose access to the resource/action is controlled.</param>
		/// <param name="value">The entry value.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="resource"/>, <paramref name="action"/> or <paramref name="subject"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="resource"/>, <paramref name="action"/> or <paramref name="subject"/> are empty.</exception>
		public AclEntry(string resource, string action, string subject, Value value) {
			if(resource == null) throw new ArgumentNullException("resource");
			if(resource.Length == 0) throw new ArgumentException("Resource cannot be empty", "resource");
			if(action == null) throw new ArgumentNullException("action");
			if(action.Length == 0) throw new ArgumentException("Action cannot be empty", "action");
			if(subject == null) throw new ArgumentNullException("subject");
			if(subject.Length == 0) throw new ArgumentException("Subject cannot be empty", "subject");

			this.resource = resource;
			this.action = action;
			this.subject = subject;
			this.value = value;
		}

		/// <summary>
		/// Gets the controlled resource.
		/// </summary>
		public string Resource {
			get { return resource; }
		}

		/// <summary>
		/// Gets the controlled action on the resource.
		/// </summary>
		public string Action {
			get { return action; }
		}

		/// <summary>
		/// Gets the subject of the entry.
		/// </summary>
		public string Subject {
			get { return subject; }
		}

		/// <summary>
		/// Gets the value of the entry.
		/// </summary>
		public Value Value {
			get { return value; }
		}

		/// <summary>
		/// Gets a string representation of the current object.
		/// </summary>
		/// <returns>The string representation.</returns>
		public override string ToString() {
			return resource + "->" + action + ": " + subject + " (" + value.ToString() + ")";
		}

		/// <summary>
		/// Gets a hash code for the current object.
		/// </summary>
		/// <returns>The hash code.</returns>
		public override int GetHashCode() {
			return base.GetHashCode();
		}

		/// <summary>
		/// Determines whether this object equals another (by value).
		/// </summary>
		/// <param name="obj">The other object.</param>
		/// <returns><c>true</c> if this object equals <b>obj</b>, <c>false</c> otherwise.</returns>
		public override bool Equals(object obj) {
			AclEntry other = obj as AclEntry;
			if(other != null) return Equals(other);
			else return false;
		}

		/// <summary>
		/// Determines whether this instance equals another (by value).
		/// </summary>
		/// <param name="other">The other instance.</param>
		/// <returns><c>true</c> if this instance equals <b>other</b>, <c>false</c> otherwise.</returns>
		public bool Equals(AclEntry other) {
			if(object.ReferenceEquals(other, null)) return false;
			else return resource == other.Resource &&
				action == other.Action && subject == other.Subject;
		}

		/// <summary>
		/// Determines whether two instances of <see cref="T:AclEntry" /> are equal (by value).
		/// </summary>
		/// <param name="x">The first instance.</param>
		/// <param name="y">The second instance.</param>
		/// <returns><c>true</c> if <b>x</b> equals <b>y</b>, <c>false</c> otherwise.</returns>
		public static bool Equals(AclEntry x, AclEntry y) {
			if(object.ReferenceEquals(x, null) && !object.ReferenceEquals(x, null)) return false;
			if(!object.ReferenceEquals(x, null) && object.ReferenceEquals(x, null)) return false;
			if(object.ReferenceEquals(x, null) && object.ReferenceEquals(x, null)) return true;
			return x.Equals(y);
		}

	}

	/// <summary>
	/// Lists legal ACL Entry values.
	/// </summary>
	public enum Value {
		/// <summary>
		/// Deny the action.
		/// </summary>
		Deny = 0,
		/// <summary>
		/// Grant the action.
		/// </summary>
		Grant = 1
	}

}
