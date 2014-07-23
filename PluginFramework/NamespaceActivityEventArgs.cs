
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// Contains arguments for Namespace activity events.
	/// </summary>
	public class NamespaceActivityEventArgs : EventArgs {

		private NamespaceInfo nspace;
		private string nspaceOldName;
		private NamespaceActivity activity;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:NamespaceActivityEventArgs" /> class.
		/// </summary>
		/// <param name="nspace">The namespace the activity refers to.</param>
		/// <param name="nspaceOldName">The old name of the renamed namespace, or <c>null</c>.</param>
		/// <param name="activity">The activity.</param>
		public NamespaceActivityEventArgs(NamespaceInfo nspace, string nspaceOldName, NamespaceActivity activity) {
			this.nspace = nspace;
			this.nspaceOldName = nspaceOldName;
			this.activity = activity;
		}

		/// <summary>
		/// Gets the namespace the activity refers to.
		/// </summary>
		public NamespaceInfo NamespaceInfo {
			get { return nspace; }
		}

		/// <summary>
		/// Gets the old name of the renamed namespace, or <c>null</c>.
		/// </summary>
		public string NamespaceOldName {
			get { return nspaceOldName; }
		}

		/// <summary>
		/// Gets the activity.
		/// </summary>
		public NamespaceActivity Activity {
			get { return activity; }
		}

	}

	/// <summary>
	/// Lists legal namespace activity types.
	/// </summary>
	public enum NamespaceActivity {
		/// <summary>
		/// A namespace has been added.
		/// </summary>
		NamespaceAdded,
		/// <summary>
		/// A namespace has been renamed.
		/// </summary>
		NamespaceRenamed,
		/// <summary>
		/// A namespace has been modified.
		/// </summary>
		NamespaceModified,
		/// <summary>
		/// A namespace has been removed.
		/// </summary>
		NamespaceRemoved
	}

}
