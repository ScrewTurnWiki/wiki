using System.Collections.Generic;

namespace ScrewTurn.Wiki
{
	using System.Linq;

	/// <summary>
	/// Implements a generic Provider Collector.
	/// </summary>
	/// <typeparam name="T">The type of the Collector.</typeparam>
	public class ProviderCollector<T>
	{

		private readonly List<T> _list;

		/// <summary>
		/// Initializes a new instance of the class.
		/// </summary>
		public ProviderCollector( )
		{
			_list = new List<T>( 3 );
		}

		/// <summary>
		/// Adds a Provider to the Collector.
		/// </summary>
		/// <param name="provider">The Provider to add.</param>
		public void AddProvider( T provider )
		{
			lock ( this )
			{
				_list.Add( provider );
			}
		}

		/// <summary>
		/// Removes a Provider from the Collector.
		/// </summary>
		/// <param name="provider">The Provider to remove.</param>
		public void RemoveProvider( T provider )
		{
			lock ( this )
			{
				_list.Remove( provider );
			}
		}

		/// <summary>
		/// Gets all the Providers (copied array).
		/// </summary>
		public T[ ] AllProviders
		{
			get
			{
				lock ( this )
				{
					return _list.ToArray( );
				}
			}
		}

		/// <summary>
		/// Gets a Provider, searching for its Type Name.
		/// </summary>
		/// <param name="typeName">The Type Name.</param>
		/// <returns>The Provider, or null if the Provider was not found.</returns>
		public T GetProvider( string typeName )
		{
			lock ( this )
			{
				return _list.FirstOrDefault( t => t.GetType( ).FullName.Equals( typeName ) );
			}
		}

	}

}
