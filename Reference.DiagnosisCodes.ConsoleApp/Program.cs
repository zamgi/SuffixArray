using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Reference.DiagnosisCodes.ConsoleApp
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Config
    {
        public static readonly string INPUT_CSV_FILE = ConfigurationManager.AppSettings[ "INPUT_CSV_FILE" ];
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// 
        /// </summary>
        private sealed class tupleIStringValueGetter : IStringValueGetter< Tuple >
        {
            public string GetStringValue( in Tuple t ) => t.Text;
        }

        /// <summary>
        /// 
        /// </summary>
        private readonly struct Tuple
        {
            public int    Id   { get; init; }
            public string Text { get; init; }

            public Tuple( int id, string text ) : this()
            {
                Id   = id;
                Text = text;
            }

            public static Tuple Create( string s )
            {
                var idx = s.IndexOf( ';' );
                if ( idx == -1 )
                    throw (new InvalidDataException());

                return (new Tuple() { Id   = int.Parse( s.Substring( 0, idx ) ), 
                                      Text = s.Substring( idx + 1 ) 
                                    });
            }
        }

        private static void Main( string[] args )
        {
            try
            {
                var sa = CreateSuffixArray();

                GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
                GC.WaitForPendingFinalizers();
                GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );

                SuffixArray__test( sa ); //--- SuffixArray__test_Threads( sa ); //--- 
            }
            catch ( Exception ex )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( ex );
            }
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine( Environment.NewLine + "[.....finita.....]" );
            Console.ReadLine();
        }

        private static SuffixArrayBase< Tuple > CreateSuffixArray()
        {
            #region [.initialize input-data.]
            var sw = Stopwatch.StartNew();
            /*
            var tuples = new[] 
            { 
                new tuple( 1, "abcabc" ),
                new tuple( 2, "papa" ),
                new tuple( 3, "papyrus" ),
                new tuple( 4, "russian" ),
                new tuple( 5, "banana" ),
                new tuple( 6, "ananas" ),
                new tuple( 7, "pineapple" ),
                new tuple( 7, "apple" ),
            };
            //*/

            //*
            Console.WriteLine( "INPUT_CSV_FILE: '" + Config.INPUT_CSV_FILE + "'..." );
            
            var text_length_total = 0L;
            var tuples = new List< Tuple >( 110000 );
            using ( var sr = new StreamReader( Config.INPUT_CSV_FILE ) )
            {
                for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
                {
                    var t = Tuple.Create( line );
                    text_length_total += t.Text.Length;
                    tuples.Add( t );
                }
            }
            tuples.Capacity = tuples.Count;
            sw.Stop();

            Console.WriteLine( "end, elapsed: " + sw.Elapsed );
            Console.WriteLine( "total-text-length: " + text_length_total.ToString("0,0") );
            Console.WriteLine( "speed: " + (text_length_total / sw.Elapsed.TotalSeconds).ToString("0,0") + " (bytes-per-second)" + Environment.NewLine );
            Console.WriteLine( "------------------------------------" + Environment.NewLine );            
            //*/
            #endregion

            Console.WriteLine( "Start build suffix-array..." );

            sw.Restart();
            var sa = new SuffixArray_v2< Tuple >( tuples, new tupleIStringValueGetter() ); //---new SuffixArray< tuple >( tuples, new tupleIStringValueGetter() ); //--- 
            sw.Stop();

            Console.WriteLine( "end, elapsed: " + sw.Elapsed );
            Console.WriteLine( "base of suffix: " + sa.GetAllSuffixesCount( SuffixArray< Tuple >.EnumerableModeEnum.BaseOfSuffix ).ToString("0,0") );
            Console.WriteLine( "all sub-suffix: " + sa.GetAllSuffixesCount( SuffixArray< Tuple >.EnumerableModeEnum.AllSubSuffix ).ToString("0,0") );
            Console.WriteLine( "------------------------------------" + Environment.NewLine );

            return (sa);
        }
        private static void SuffixArray__test( SuffixArrayBase< Tuple > sa )
        {            
            Action< string > find = (suffix) =>
            {
                const int MAX = 51;
                var findCount = 0;
                var frs = sa.Find( suffix );
                foreach ( var fr in frs )
                {
                    findCount++;
                    if ( findCount < MAX )
                    {
                        if ( findCount == 1 )
                        {
                            Console.WriteLine( $"\r\n suffix: '[{suffix}]'\r\n\t  =>" );
                        }
                        Console.WriteLine( $"\t '{fr.GetHighlightSuffix( "[", "]" ).ToLowerInvariant()}'" );
                    }
                }
                //result
                if ( 0 < findCount )
                {
                    if ( MAX < findCount )
                    {
                        Console.WriteLine( $"\t ...else {(findCount - MAX + 1)}..." );
                    }
                    Console.WriteLine( $"\t  => ({findCount})" );
                }
                else
                {
                    Console.WriteLine( $"\r\n suffix: '{suffix}' => not found" );
                }
            };

            find( "ru" );
            find( "al" );
            find( "exe" );
        }

        private static void SuffixArray__test_Threads( SuffixArrayBase< Tuple > sa )
        {
            const int THREAD_COUNT = 4;

            using ( var br = new Barrier( THREAD_COUNT + 1 ) )
            {
                for ( var i = 0; i < THREAD_COUNT; i++ )
                {
                    SuffixArray__test_ThreadRoutine( sa, br );
                }

                br.SignalAndWait();
            }
        }
        private static void SuffixArray__test_ThreadRoutine( SuffixArrayBase< Tuple > sa, Barrier br )
        {
            var th = new Thread( _ /*state*/ =>
            {
                br.SignalAndWait();

                const int MAX = 51;

                var sb = new StringBuilder();
                for ( var rnd = new Random( Thread.CurrentThread.ManagedThreadId ); ; sb.Clear() )
                {
                    var len = rnd.Next( 1, 10 );
                    for ( ; 0 <= len; len-- )
                    {
                        sb.Append( (char) rnd.Next( 'a', 'z' + 1 ) );
                    }

                    var frs = sa.Find( sb.ToString() );
                    var sum = frs.Take( MAX ).Sum( fr => fr.GetSuffix().Length );
                    //foreach ( var fr in frs.Take( MAX ) )
                    //{
                    //    x += fr.GetSuffix().Length;
                    //}
                    Console.WriteLine( sum );
                }
            } ) { IsBackground = true };
            th.Start();
        }
    }
}
