using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

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
    internal class Program
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
            SuffixArray__test( sa );

            Console.WriteLine( Environment.NewLine + "[.....finish.....]" );
            Console.ReadLine();
        }

        private static SuffixArray< tuple > CreateSuffixArray()
        {
            Console.WriteLine( "INPUT_CSV_FILE: '" + Config.INPUT_CSV_FILE + "'..." );

            var sw = Stopwatch.StartNew();
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
            sw.Stop();

            Console.WriteLine( "end, elapsed: " + sw.Elapsed );
            Console.WriteLine( "total-text-length: " + text_length_total.ToString( "0,0" ) );
            Console.WriteLine( "speed: " + (text_length_total / sw.Elapsed.TotalSeconds).ToString( "0,0" ) + " (bytes-per-second)" + Environment.NewLine );
            Console.WriteLine( "------------------------------------" + Environment.NewLine );
            

            Console.WriteLine( "Start build suffix-array..." );

            sw = Stopwatch.StartNew();
            var sa = new SuffixArray< tuple >( tuples, new tupleIStringValueGetter() );
            sw.Stop();

            Console.WriteLine( "end, elapsed: " + sw.Elapsed );
            Console.WriteLine( "base of suffix: " + sa.GetAllSuffixesCount( SuffixArray<tuple>.EnumerableModeEnum.BaseOfSuffix ) );
            Console.WriteLine( "all sub-suffix: " + sa.GetAllSuffixesCount( SuffixArray<tuple>.EnumerableModeEnum.AllSubSuffix ) );
            Console.WriteLine( "------------------------------------" + Environment.NewLine );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            return (sa);
        }
        private static void SuffixArray__test( SuffixArray< tuple > sa )
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
                            Console.WriteLine( "\r\n suffix: '[" + suffix + "]'" );
                        Console.WriteLine( "\t '" + fr.GetHighlightSuffix( "[", "]" ).ToLowerInvariant() + '\'' );
                    }
                }
                //result
                if ( 0 < findCount )
                {
                    if ( MAX < findCount )
                        Console.WriteLine( "\t ...else " + (findCount - MAX + 1) + "..." );
                    Console.WriteLine( "\t => (" + findCount + ")" );
                }
                else
                {
                    Console.WriteLine( "\r\n suffix: '" + suffix + "' => not found" );
                }
            };
            find( "al" );
            find( "exe" );
        }
    }
}
