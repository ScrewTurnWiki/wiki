
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Implements a generic Provider Collector.
	/// </summary>
	/// <typeparam name="T">The type of the Collector.</typeparam>
	public class ProviderCollector<T> {

		private List<T> list;

		/// <summary>
		/// Initializes a new instance of the class.
		/// </summary>
		public ProviderCollector() {
			list = new List<T>(3);
		}

		/// <summary>
		/// Adds a Provider to the Collector.
		/// </summary>
		/// <param name="provider">The Provider to add.</param>
		public void AddProvider(T provider) {
			lock(this) {
				list.Add(provider);
			}
		}

		/// <summary>
		/// Removes a Provider from the Collector.
		/// </summary>
		/// <param name="provider">The Provider to remove.</param>
		public void RemoveProvider(T provider) {
			lock(this) {
				list.Remove(provider);
			}
		}

		/// <summary>
		/// Gets all the Providers (copied array).
		/// </summary>
		public T[] AllProviders {
			get {
				lock(this) {
					return list.ToArray();
				}
			}
		}

		/// <summary>
		/// Gets a Provider, searching for its Type Name.
		/// </summary>
		/// <param name="typeName">The Type Name.</param>
		/// <returns>The Provider, or null if the Provider was not found.</returns>
		public T GetProvider(string typeName) {
			lock(this) {
				for(int i = 0; i < list.Count; i++) {
					if(list[i].GetType().FullName.Equals(typeName)) return list[i];
				}
				return default(T);
			}
		}

	}

}
