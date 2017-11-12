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
        [Serializable]
        private sealed class tupleIStringValueGetter : IStringValueGetter< tuple >
        {
            public string GetStringValue( tuple t )
            {
                return (t.Text);
            }                
        }

        [Serializable]
        private struct tuple
        {
            public int    Id   { get; private set; }
            public string Text { get; private set; }

            public tuple( int id, string text ) : this()
            {
                Id   = id;
                Text = text;
            }

            public static tuple Create( string s )
            {
                var index = s.IndexOf( ';' );
                if ( index == -1 )
                    throw (new InvalidDataException());

                return (new tuple() { Id   = int.Parse( s.Substring( 0, index ) ), 
                                      Text = s.Substring( index + 1 ) 
                                    });
            }
        }

        private static void Main( string[] args )
        {
            var sa = CreateSuffixArray();

            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
            GC.WaitForPendingFinalizers();
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );

            SuffixArray__test( sa ); //--- SuffixArray__test_Threads( sa ); //--- 

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine( Environment.NewLine + "[.....finita.....]" );
            Console.ReadLine();
        }

        private static SuffixArrayBase< tuple > CreateSuffixArray()
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
            var tuples = new List< tuple >( 110000 );
            using ( var sr = new StreamReader( Config.INPUT_CSV_FILE ) )
            {
                for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
                {
                    var t = tuple.Create( line );
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
            var sa = new SuffixArray_v2< tuple >( tuples, new tupleIStringValueGetter() ); //---new SuffixArray< tuple >( tuples, new tupleIStringValueGetter() ); //--- 
            sw.Stop();

            Console.WriteLine( "end, elapsed: " + sw.Elapsed );
            Console.WriteLine( "base of suffix: " + sa.GetAllSuffixesCount( SuffixArray< tuple >.EnumerableModeEnum.BaseOfSuffix ).ToString("0,0") );
            Console.WriteLine( "all sub-suffix: " + sa.GetAllSuffixesCount( SuffixArray< tuple >.EnumerableModeEnum.AllSubSuffix ).ToString("0,0") );
            Console.WriteLine( "------------------------------------" + Environment.NewLine );

            return (sa);
        }
        private static void SuffixArray__test( SuffixArrayBase< tuple > sa )
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

        private static void SuffixArray__test_Threads( SuffixArrayBase< tuple > sa )
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
        private static void SuffixArray__test_ThreadRoutine( SuffixArrayBase< tuple > sa, Barrier br )
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
