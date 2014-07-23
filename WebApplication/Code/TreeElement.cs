
using System;
using System.Collections.Generic;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Defines an item or element in an tree structure.
	/// </summary>
	public class TreeElement {

		private string name, text, onClientClick;
		private List<TreeElement> subItems;

		/// <summary>
		/// Initializes a new instance of the <b>TreeElement</b> class.
		/// </summary>
		/// <param name="name">The name of the item.</param>
		/// <param name="text">The text of the item.</param>
		/// <param name="onClientClick">The JavaScript to execute on client click.</param>
		/// <param name="subItems">The sub-items.</param>
		public TreeElement(string name, string text, string onClientClick, List<TreeElement> subItems) {
			this.name = name;
			this.text = text;
			this.onClientClick = onClientClick;
			this.subItems = subItems;
		}

		/// <summary>
		/// Initializes a new instance of the <b>TreeElement</b> class.
		/// </summary>
		/// <param name="name">The name of the item.</param>
		/// <param name="text">The text of the item.</param>
		/// <param name="onClientClick">The JavaScript to execute on client click.</param>
		public TreeElement(string name, string text, string onClientClick)
			: this(name, text, onClientClick, new List<TreeElement>()) { }

		/// <summary>
		/// Initializes a new instance of the <b>TreeElement</b> class.
		/// </summary>
		/// <param name="name">The name of the item.</param>
		/// <param name="text">The text of the item.</param>
		/// <param name="subItems">The sub-items.</param>
		public TreeElement(string name, string text, List<TreeElement> subItems)
			: this(name, text, "", subItems) { }

		/// <summary>
		/// Gets or sets the name of the item.
		/// </summary>
		public string Name {
			get { return name; }
			set { this.name = value; }
		}

		/// <summary>
		/// Gets or sets the text of the item.
		/// </summary>
		public string Text {
			get { return text; }
			set { text = value; }
		}

		/// <summary>
		/// Gets or sets the JavaScript to execute on client click.
		/// </summary>
		public string OnClientClick {
			get { return onClientClick; }
			set { onClientClick = value; }
		}

		/// <summary>
		/// Gets or sets the SubItems.
		/// </summary>
		public List<TreeElement> SubItems {
			get { return subItems; }
			set { subItems = value; }
		}

	}

	/// <summary>
	/// Contains the event arguments for the Populate event.
	/// </summary>
	public class PopulateEventArgs : EventArgs {

	}

}
