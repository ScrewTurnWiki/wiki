namespace ScrewTurn.Wiki.Acl {

	/// <summary>
	/// Describes the subject of an ACL entry.
	/// </summary>
	public class SubjectInfo {
		/// <summary>
		/// Initializes a new instance of the <see cref="T:SubjectInfo" /> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="type">The type.</param>
		public SubjectInfo(string name, SubjectType type) {
			Name = name;
			Type = type;
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the type.
		/// </summary>
		public SubjectType Type { get; private set; }
	}
}
