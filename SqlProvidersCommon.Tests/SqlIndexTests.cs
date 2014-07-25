namespace SqlProvidersCommon.Tests
{
	using System;
	using NUnit.Framework;
	using Rhino.Mocks;
	using ScrewTurn.Wiki.Plugins.SqlCommon;
	using ScrewTurn.Wiki.SearchEngine;
	using Is = Rhino.Mocks.Constraints.Is;
	using Tools = ScrewTurn.Wiki.SearchEngine.Tools;

	[TestFixture]
	public class SqlIndexTests
	{

		[Test]
		public void Constructor_NullConnector( )
		{
			Assert.Throws<ArgumentNullException>( ( ) =>
			{
				var x = new SqlIndex( null );
			} );
		}

		[Test]
		public void TotalDocuments_TotalWords_TotalOccurrences( )
		{
			MockRepository mocks = new MockRepository( );
			IIndexConnector conn = mocks.StrictMock<IIndexConnector>( );

			Expect.Call( conn.GetCount( IndexElementType.Documents ) ).Return( 12 );
			Expect.Call( conn.GetCount( IndexElementType.Words ) ).Return( 567 );
			Expect.Call( conn.GetCount( IndexElementType.Occurrences ) ).Return( 3456 );

			mocks.ReplayAll( );

			SqlIndex index = new SqlIndex( conn );

			Assert.AreEqual( 12, index.TotalDocuments, "Wrong document count" );
			Assert.AreEqual( 567, index.TotalWords, "Wrong word count" );
			Assert.AreEqual( 3456, index.TotalOccurrences, "Wrong occurence count" );

			mocks.VerifyAll( );
		}

		[Test]
		public void Clear( )
		{
			MockRepository mocks = new MockRepository( );
			IIndexConnector conn = mocks.StrictMock<IIndexConnector>( );

			const string dummyState = "state";

			conn.ClearIndex( dummyState );
			LastCall.On( conn );

			mocks.ReplayAll( );

			SqlIndex index = new SqlIndex( conn );

			index.Clear( dummyState );

			mocks.VerifyAll( );
		}

		[Test]
		public void StoreDocument( )
		{
			MockRepository mocks = new MockRepository( );
			IIndexConnector conn = mocks.StrictMock<IIndexConnector>( );
			IDocument doc = mocks.StrictMock<IDocument>( );

			const string dummyState = "state";

			const string content = "This is some test content.";
			const string title = "My Document";

			Expect.Call( doc.Title ).Return( title ).Repeat.AtLeastOnce( );
			Expect.Call( doc.Tokenize( content ) ).Return( Tools.Tokenize( content, WordLocation.Content ) );
			Expect.Call( doc.Tokenize( title ) ).Return( Tools.Tokenize( title, WordLocation.Title ) );

			Predicate<WordInfo[ ]> contentPredicate = array => array.Length == 5 &&
			                                                   array[ 0 ].Text == "this" &&
			                                                   array[ 1 ].Text == "is" &&
			                                                   array[ 2 ].Text == "some" &&
			                                                   array[ 3 ].Text == "test" &&
			                                                   array[ 4 ].Text == "content";
			Predicate<WordInfo[ ]> titlePredicate = array => array.Length == 2 &&
			                                                 array[ 0 ].Text == "my" &&
			                                                 array[ 1 ].Text == "document";
			Predicate<WordInfo[ ]> keywordsPredicate = array => array.Length == 1 &&
			                                                    array[ 0 ].Text == "test";

			conn.DeleteDataForDocument( doc, dummyState );
			LastCall.On( conn );
			Expect.Call( conn.SaveDataForDocument( null, null, null, null, null ) ).IgnoreArguments( )
				.Constraints( Is.Same( doc ), Is.Matching( contentPredicate ), Is.Matching( titlePredicate ), Is.Matching( keywordsPredicate ), Is.Same( dummyState ) )
				.Return( 8 );

			mocks.ReplayAll( );

			SqlIndex index = new SqlIndex( conn );

			Assert.AreEqual( 8, index.StoreDocument( doc, new[ ] { "test" }, content, dummyState ), "Wrong occurrence count" );

			mocks.VerifyAll( );
		}

		[Test]
		public void Search( )
		{
			// Basic integration test: search algorithms are already extensively tested with InMemoryIndexBase

			MockRepository mocks = new MockRepository( );

			IIndexConnector conn = mocks.StrictMock<IIndexConnector>( );
			IWordFetcher fetcher = mocks.StrictMock<IWordFetcher>( );

			Word dummy;
			Expect.Call( fetcher.TryGetWord( "test", out dummy ) ).Return( false );
			Expect.Call( fetcher.TryGetWord( "query", out dummy ) ).Return( false );
			fetcher.Dispose( );
			LastCall.On( fetcher );

			Expect.Call( conn.GetWordFetcher( ) ).Return( fetcher );

			mocks.ReplayAll( );

			SqlIndex index = new SqlIndex( conn );

			Assert.AreEqual( 0, index.Search( new SearchParameters( "test query" ) ).Count, "Wrong search result count" );

			mocks.VerifyAll( );
		}

	}

}
