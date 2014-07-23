
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Describes the subject of an ACL entry.
	/// </summary>
	public class SubjectInfo {

		private string name;
		private SubjectType type;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SubjectInfo" /> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="type">The type.</param>
		public SubjectInfo(string name, SubjectType type) {
			this.name = name;
			this.type = type;
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		public string Name {
			get { return name; }
		}

		/// <summary>
		/// Gets the type.
		/// </summary>
		public SubjectType Type {
			get { return type; }
		}

	}

	/// <summary>
	/// Lists legal values for the type of a subject.
	/// </summary>
	public enum SubjectType {
		/// <summary>
		/// A user.
		/// </summary>
		User,
		/// <summary>
		/// A group.
		/// </summary>
		Group
	}

}
