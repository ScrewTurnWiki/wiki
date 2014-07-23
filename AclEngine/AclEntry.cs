
using System;

namespace ScrewTurn.Wiki.AclEngine {

	/// <summary>
	/// Represents an ACL Entry.
	/// </summary>
	public class AclEntry : IEquatable<AclEntry>
	{
		/// <summary>
		/// Gets a hash code for this AclEntry.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode( )
		{
			unchecked
			{
				int hashCode = ( Resource != null ? Resource.GetHashCode( ) : 0 );
				hashCode = ( hashCode * 397 ) ^ ( Action != null ? Action.GetHashCode( ) : 0 );
				hashCode = ( hashCode * 397 ) ^ ( Subject != null ? Subject.GetHashCode( ) : 0 );
				hashCode = ( hashCode * 397 ) ^ (int) Value;
				return hashCode;
			}
		}


		/// <summary>
		/// Determines if two AclEntry objects are equal, by value.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==( AclEntry left, AclEntry right )
		{
			return Equals( left, right );
		}

		/// <summary>
		/// Determines if two AclEntry objects are unequal, by value.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=( AclEntry left, AclEntry right )
		{
			return !Equals( left, right );
		}

		/// <summary>
		/// The full control action.
		/// </summary>
		public const string FullControlAction = "*";

		/// <summary>
		/// Initializes a new instance of the <see cref="T:AclEntry" /> class.
		/// </summary>
		/// <param name="resource">The controlled resource.</param>
		/// <param name="action">The controlled action on the resource.</param>
		/// <param name="subject">The subject whose access to the resource/action is controlled.</param>
		/// <param name="value">The entry value.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="resource"/>, <paramref name="action"/> or <paramref name="subject"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="resource"/>, <paramref name="action"/> or <paramref name="subject"/> are empty.</exception>
		public AclEntry(string resource, string action, string subject, Value value)
		{
			if ( resource == null )
				throw new ArgumentNullException( "resource" );
			if ( resource.Length == 0 )
				throw new ArgumentException( "Resource cannot be empty", "resource" );
			if ( action == null )
				throw new ArgumentNullException( "action" );
			if ( action.Length == 0 )
				throw new ArgumentException( "Action cannot be empty", "action" );
			if ( subject == null )
				throw new ArgumentNullException( "subject" );
			if ( subject.Length == 0 )
				throw new ArgumentException( "Subject cannot be empty", "subject" );

			Resource = resource;
			Action = action;
			Subject = subject;
			Value = value;
		}

		/// <summary>
		/// Gets the controlled resource.
		/// </summary>
		public string Resource { get; private set; }

		/// <summary>
		/// Gets the controlled action on the resource.
		/// </summary>
		public string Action { get; private set; }

		/// <summary>
		/// Gets the subject of the entry.
		/// </summary>
		public string Subject { get; private set; }

		/// <summary>
		/// Gets the value of the entry.
		/// </summary>
		public Value Value { get; private set; }

		/// <summary>
		/// Gets a string representation of the current object.
		/// </summary>
		/// <returns>The string representation.</returns>
		public override string ToString() {
			return Resource + "->" + Action + ": " + Subject + " (" + Value + ")";
		}

		/// <summary>
		/// Determines whether this object equals another (by value).
		/// </summary>
		/// <param name="obj">The other object.</param>
		/// <returns><c>true</c> if this object equals <b>obj</b>, <c>false</c> otherwise.</returns>
		public override bool Equals( object obj )
		{
			if ( ReferenceEquals( null, obj ) )
			{
				return false;
			}
			if ( ReferenceEquals( this, obj ) )
			{
				return true;
			}
			if ( obj.GetType( ) != GetType( ) )
			{
				return false;
			}
			return Equals( (AclEntry) obj );
		}

		/// <summary>
		/// Determines whether this instance equals another (by value).
		/// </summary>
		/// <param name="other">The other instance.</param>
		/// <returns><c>true</c> if this instance equals <b>other</b>, <c>false</c> otherwise.</returns>
		public bool Equals( AclEntry other )
		{
			if ( ReferenceEquals( null, other ) )
			{
				return false;
			}
			if ( ReferenceEquals( this, other ) )
			{
				return true;
			}
			return string.Equals( Resource, other.Resource )
				&& string.Equals( Action, other.Action )
				&& string.Equals( Subject, other.Subject )
				&& Value == other.Value;
		}

		/// <summary>
		/// Determines whether two instances of <see cref="T:AclEntry" /> are equal (by value).
		/// </summary>
		/// <param name="x">The first instance.</param>
		/// <param name="y">The second instance.</param>
		/// <returns><c>true</c> if <b>x</b> equals <b>y</b>, <c>false</c> otherwise.</returns>
		public static bool Equals( AclEntry x, AclEntry y )
		{
			if ( ReferenceEquals( x, null ) && !ReferenceEquals( y, null ) )
				return false;
			if ( ReferenceEquals( x, null ) )
				return true;
			if ( ReferenceEquals( y, null ) )
				return false;
			return x.Equals( y );
		}

	}

	/// <summary>
	/// Lists legal ACL Entry values.
	/// </summary>
	public enum Value {
		/// <summary>
		/// Deny the action.
		/// </summary>
		Deny = 0,
		/// <summary>
		/// Grant the action.
		/// </summary>
		Grant = 1
	}

}
