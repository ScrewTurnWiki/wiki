
using System;

namespace ScrewTurn.Wiki.ImportWiki {

	/// <summary>
	/// Exposes an interface for building import tools.
	/// </summary>
    public interface ITranslator {

		/// <summary>
		/// Executes the translation.
		/// </summary>
		/// <param name="input">The input content.</param>
		/// <returns>The WikiMarkup.</returns>
        string Translate(string input);

    }

}
