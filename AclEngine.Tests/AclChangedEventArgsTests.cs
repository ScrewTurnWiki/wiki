﻿using System;

namespace AclEngine.Tests
{
	using NUnit.Framework;
	using ScrewTurn.Wiki.AclEngine;

	[TestFixture]
	public class AclChangedEventArgsTests
	{
		[Test]
		public void Constructor( )
		{
			AclEntry entry = new AclEntry( "Res", "Action", "U.User", Value.Grant );

			AclChangedEventArgs args = new AclChangedEventArgs( new[ ] { entry }, Change.EntryStored );

			Assert.AreEqual( 1, args.Entries.Length, "Wrong entry count" );
			Assert.AreSame( entry, args.Entries[ 0 ], "Wrong Entry instance" );
			Assert.AreEqual( Change.EntryStored, args.Change, "Wrong change" );

			args = new AclChangedEventArgs( new[ ] { entry }, Change.EntryDeleted );

			Assert.AreEqual( 1, args.Entries.Length, "Wrong entry count" );
			Assert.AreSame( entry, args.Entries[ 0 ], "Wrong Entry instance" );
			Assert.AreEqual( Change.EntryDeleted, args.Change, "Wrong change" );
		}

		[Test]
		[ExpectedException( typeof( ArgumentNullException ) )]
		public void Constructor_NullEntries( )
		{
			AclChangedEventArgs args = new AclChangedEventArgs( null, Change.EntryDeleted );
		}

		[Test]
		[ExpectedException( typeof( ArgumentException ) )]
		public void Constructor_EmptyEntries( )
		{
			AclChangedEventArgs args = new AclChangedEventArgs( new AclEntry[ 0 ], Change.EntryDeleted );
		}
	}
}
