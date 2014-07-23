
using System;
using System.Collections.Generic;
using System.Text;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// Represents a Page Discussion Message.
	/// </summary>
	public class Message {

		/// <summary>
		/// The Message ID.
		/// </summary>
		protected int id;
		/// <summary>
		/// The Username.
		/// </summary>
		protected string username;
		/// <summary>
		/// The Subject.
		/// </summary>
		protected string subject;
		/// <summary>
		/// The Date/Time.
		/// </summary>
		protected DateTime dateTime;
		/// <summary>
		/// The Body.
		/// </summary>
		protected string body;
		/// <summary>
		/// The Replies.
		/// </summary>
		protected Message[] replies = new Message[0];

		/// <summary>
		/// Initializes a new instance of the <b>Message</b> class.
		/// </summary>
		/// <param name="id">The ID of the Message.</param>
		/// <param name="username">The Username of the User.</param>
		/// <param name="subject">The Subject of the Message.</param>
		/// <param name="dateTime">The Date/Time of the Message.</param>
		/// <param name="body">The body of the Message.</param>
		public Message(int id, string username, string subject, DateTime dateTime, string body) {
			this.id = id;
			this.username = username;
			this.subject = subject;
			this.dateTime = dateTime;
			this.body = body;
		}

		/// <summary>
		/// Gets or sets the Message ID.
		/// </summary>
		public int ID {
			get { return id; }
			set { id = value; }
		}

		/// <summary>
		/// Gets or sets the Username.
		/// </summary>
		public string Username {
			get { return username; }
			set { username = value; }
		}

		/// <summary>
		/// Gets or sets the Subject.
		/// </summary>
		public string Subject {
			get { return subject; }
			set { subject = value; }
		}

		/// <summary>
		/// Gets or sets the Date/Time.
		/// </summary>
		public DateTime DateTime {
			get { return dateTime; }
			set { dateTime = value; }
		}

		/// <summary>
		/// Gets or sets the Body.
		/// </summary>
		public string Body {
			get { return body; }
			set { body = value; }
		}

		/// <summary>
		/// Gets or sets the Replies.
		/// </summary>
		public Message[] Replies {
			get { return replies; }
			set { replies = value; }
		}

	}

	/// <summary>
	/// Compares two Message object using their Date/Time as parameter.
	/// </summary>
	public class MessageDateTimeComparer : IComparer<Message> {

		bool reverse = false;

		/// <summary>
		/// Initializes a new instance of the <b>MessageDateTimeComparer</b> class.
		/// </summary>
		/// <param name="reverse">True to compare in reverse order (bigger to smaller).</param>
		public MessageDateTimeComparer(bool reverse) {
			this.reverse = reverse;
		}

		/// <summary>
		/// Compares two Message objects.
		/// </summary>
		/// <param name="x">The first object.</param>
		/// <param name="y">The second object.</param>
		/// <returns>The result of the comparison (1, 0 or -1).</returns>
		public int Compare(Message x, Message y) {
			if(!reverse) return x.DateTime.CompareTo(y.DateTime);
			else return y.DateTime.CompareTo(x.DateTime);
		}
	}

}
