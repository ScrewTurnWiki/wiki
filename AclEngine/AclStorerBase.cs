
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.AclEngine {

	/// <summary>
	/// Implements a base class for an ACL Storer.
	/// </summary>
	public abstract class AclStorerBase : IDisposable {

		/// <summary>
		/// Indicates whether the object was disposed.
		/// </summary>
		protected bool disposed = false;

		/// <summary>
		/// The instance of the ACL Manager to handle.
		/// </summary>
		protected IAclManager aclManager;

		/// <summary>
		/// The event handler for the <see cref="IAclManager.AclChanged" /> event.
		/// </summary>
		protected EventHandler<AclChangedEventArgs> aclChangedHandler;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:AclStorerBase" /> abstract class.
		/// </summary>
		/// <param name="aclManager">The instance of the ACL Manager to handle.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="aclManager"/> is <c>null</c>.</exception>
		public AclStorerBase(IAclManager aclManager) {
			if(aclManager == null) throw new ArgumentNullException("aclManager");

			this.aclManager = aclManager;

			aclChangedHandler = new EventHandler<AclChangedEventArgs>(aclManager_AclChanged);

			this.aclManager.AclChanged += aclChangedHandler;
		}

		/// <summary>
		/// Handles the <see cref="IAclManager.AclChanged" /> event.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event arguments.</param>
		private void aclManager_AclChanged(object sender, AclChangedEventArgs e) {
			if(e.Change == Change.EntryDeleted) DeleteEntries(e.Entries);
			else if(e.Change == Change.EntryStored) StoreEntries(e.Entries);
			else throw new NotSupportedException("Change type not supported");
		}

		/// <summary>
		/// Loads data from storage.
		/// </summary>
		/// <returns>The loaded ACL entries.</returns>
		protected abstract AclEntry[] LoadDataInternal();

		/// <summary>
		/// Deletes some entries.
		/// </summary>
		/// <param name="entries">The entries to delete.</param>
		protected abstract void DeleteEntries(AclEntry[] entries);

		/// <summary>
		/// Stores some entries.
		/// </summary>
		/// <param name="entries">The entries to store.</param>
		protected abstract void StoreEntries(AclEntry[] entries);

		/// <summary>
		/// Loads the data and injects it in the instance of <see cref="T:IAclManager" />.
		/// </summary>
		public void LoadData() {
			lock(this) {
				AclEntry[] entries = LoadDataInternal();
				aclManager.InitializeData(entries);
			}
		}

		/// <summary>
		/// Gets the instance of the ACL Manager.
		/// </summary>
		public IAclManager AclManager {
			get {
				lock(this) {
					return aclManager;
				}
			}
		}

		/// <summary>
		/// Disposes the current object.
		/// </summary>
		public void Dispose() {
			lock(this) {
				if(!disposed) {
					disposed = true;
					aclManager.AclChanged -= aclChangedHandler;
				}
			}
		}

	}

}
