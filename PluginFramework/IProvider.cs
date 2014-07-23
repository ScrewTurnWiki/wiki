
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// The base interface that all the Providers must implement. All the Provider Type-specific interfaces inherit from this one or from a one, either directly or from a derived interface.
	/// </summary>
	/// <remarks>This interface should not be implemented directly by a class.</remarks>
	public interface IProviderV30 {

		/// <summary>
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		void Init(IHostV30 host, string config);

		/// <summary>
		/// Method invoked on shutdown.
		/// </summary>
		/// <remarks>This method might not be invoked in some cases.</remarks>
		void Shutdown();

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		ComponentInformation Information { get; }

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		string ConfigHelpHtml { get; }

	}

	/// <summary>
	/// Represents errors that occur while decoding the Provider's Configuration String.
	/// </summary>
	public class InvalidConfigurationException : Exception {

		/// <summary>
		/// Initializes a new instance of the <b>InvalidConfigurationException</b> class.
		/// </summary>
		public InvalidConfigurationException() : base() { }

		/// <summary>
		/// Initializes a new instance of the <b>InvalidConfigurationException</b> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		public InvalidConfigurationException(string message) : base(message) { }

		/// <summary>
		/// Initializes a new instance of the <b>InvalidConfigurationException</b> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="innerException">The inner Exception.</param>
		public InvalidConfigurationException(string message, Exception innerException) : base(message, innerException) { }

		/// <summary>
		/// Initializes a new instance of the <b>InvalidConfigurationException</b> class.
		/// </summary>
		/// <param name="info">The serialization info.</param>
		/// <param name="context">The streaming context.</param>
		public InvalidConfigurationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

	}

}
