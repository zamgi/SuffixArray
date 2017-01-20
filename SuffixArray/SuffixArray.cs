using System;
using System.Linq;

namespace System.Collections.Generic
{
    /// <summary>
    /// 
    /// </summary>
    public interface IStringValueGetter< T >
    {
        string GetStringValue( T obj );
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class SuffixArrayBuilder< T >
    {
        /// <summary>
        /// 
        /// </summary>        
        private struct suffix_t
        {
            public suffix_t( string suffix, int suffixIndex, int wordIndex )
            {
                Suffix      = suffix;
                SuffixIndex = suffixIndex;
                WordIndex   = wordIndex;
            }

            public string Suffix;
            public int    SuffixIndex;
            public int    WordIndex;
#if DEBUG
            public override string ToString()
            {
                return (Suffix + " - " + SuffixIndex + " (w: " + WordIndex + ")");
            }
#endif
        }

        private static bool[] IS_LETTER_OR_DIGIT;
        private static void CreateIsLetterOrDigitArray()
        {
            IS_LETTER_OR_DIGIT = new bool[ char.MaxValue ];
            for ( var ch = char.MinValue; ; )
            {
                IS_LETTER_OR_DIGIT[ ch ] = char.IsLetter( ch ) || char.IsDigit( ch );
                if ( ++ch == char.MaxValue )
                {
                    break;
                }
            }
        }
        private static void DestroyIsLetterOrDigitArray()
        {
            IS_LETTER_OR_DIGIT = null;
        }

        private static string ClearString( string word, out int startIndex )
        {
            startIndex = 0;
            int len = word.Length;
            for ( ; startIndex < len; startIndex++ )
            {
                if ( IS_LETTER_OR_DIGIT[ word[ startIndex ] ] )
                    break;
                //var ch = word[ startIndex ];
                //if ( char.IsLetter( ch ) || char.IsDigit( ch ) )
                //    break;
            }
            if ( startIndex < len )
            {
                var endIndex = len - 1;
                for ( ; 0 <= endIndex; endIndex-- )
                {
                    if ( IS_LETTER_OR_DIGIT[ word[ endIndex ] ] )
                        break;
                    //var ch = word[ endIndex ];
                    //if ( char.IsLetter( ch ) || char.IsDigit( ch ) )
                    //    break;
                }
                if ( startIndex <= endIndex )
                {
                    return (word.Substring( startIndex, endIndex - startIndex + 1 ));
                }
            }   
             
            return (null);
        }

        private static int GetSuffixCount( string value )
        {
            return (value.Length);
        }
        private static IEnumerable< suffix_t > GetSuffix( int wordIndex, string word )
        {
            int index_base;
            word = ClearString( word, out index_base );
            if ( word != null )
            {
                word = word.ToUpperInvariant();
                yield return (new suffix_t( word, index_base, wordIndex ));

                int index;
                for ( int i = 1, len = word.Length; i < len; i++ )
                {                    
                    var suffix = ClearString( word.Substring( i ), out index );
                    if ( suffix != null )
                    {
                        suffix = suffix.ToUpperInvariant();
                        yield return (new suffix_t( suffix, i + index + index_base, wordIndex ));
                    }
                }
            }
        }

        /*private static IEnumerable< TSource > Distinct< TSource >( IEnumerable< TSource > source )
        {
	        var set = new Set< TSource >();
	        foreach ( TSource current in source )
	        {
                if ( set.Add( current ) )
		        {
			        yield return (current);
		        }
	        }
	        yield break;
        }*/

        private static int suffix_t_Comparison( suffix_t x, suffix_t y )
        {
            return (string.CompareOrdinal( y.Suffix, x.Suffix ));
        }

        internal static SuffixArray< T >.tuple_t[] Build( 
            IList< T > objs, int index, int length, IStringValueGetter< T > stringValueGetter )
        {
            CreateIsLetterOrDigitArray();

            var totalSuffixCount = (from value in objs.Skip( index ).Take( length )
                                    select GetSuffixCount( stringValueGetter.GetStringValue( value ) )
                                   ).Sum();
            var arrayIndex = 0;
            var array = new suffix_t[ totalSuffixCount ];
            //---for ( int i = 0, len = objs.Count; i < len; i++ )
            for ( int i = index; i < length; i++ )
            {
                var str = stringValueGetter.GetStringValue( objs[ i ] );
                //if ( str == "μ.αΰαθι" )
                //System.Diagnostics.Debugger.Break();
                //var __ = GetSuffix( i, str ).Distinct().ToArray();
                foreach ( var suffix in GetSuffix( i, str ).Distinct() )
                {
                    array[ arrayIndex++ ] = suffix;
                }
            }
            Array.Resize< suffix_t >( ref array, arrayIndex );
            Array.Sort< suffix_t >( array, suffix_t_Comparison );


            var tuples = new SuffixArray< T >.tuple_t[ array.Length ];
            arrayIndex = 0;
            var s = array[ arrayIndex ];
            var suffixCurrent = s.Suffix;
            var llCurrent     = new SimplyLinkedList< SuffixArray< T >.data_t >();
            tuples[ arrayIndex++ ] = new SuffixArray< T >.tuple_t() { Suffix = suffixCurrent, Data = llCurrent };
            llCurrent.Add( new SuffixArray< T >.data_t( s.SuffixIndex, s.WordIndex ) );
            for ( int i = 1, len = array.Length; i < len; i++ )
            {
                s = array[ i ];
                if ( !suffixCurrent.StartsWith( s.Suffix ) )
                {
                    suffixCurrent = s.Suffix;
                    llCurrent     = new SimplyLinkedList< SuffixArray< T >.data_t >();
                    tuples[ arrayIndex++ ] = new SuffixArray< T >.tuple_t() { Suffix = suffixCurrent, Data = llCurrent };
                }
                llCurrent.Add( new SuffixArray< T >.data_t( s.SuffixIndex, s.WordIndex ) );
            }
            array = null;
            Array.Resize< SuffixArray< T >.tuple_t >( ref tuples, arrayIndex );
            Array.Reverse( tuples );

            DestroyIsLetterOrDigitArray();

            return (tuples);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
	public sealed class SuffixArray< T > : IEnumerable< SuffixArray< T >.find_result_t >
	{
        /// <summary>
        /// 
        /// </summary>
        [Serializable]
        internal struct data_t
        {
            public data_t( int suffixIndex, int objIndex )
            {
                SuffixIndex = suffixIndex;
                ObjIndex    = objIndex;
            }

            public int SuffixIndex;
            public int ObjIndex;
#if DEBUG
            public override string ToString()
            {
                return (SuffixIndex + " (w: " + ObjIndex + ")");
            }
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable]
        internal struct tuple_t
        {
            public string Suffix;
            public SimplyLinkedList< data_t > Data;
#if DEBUG
            public override string ToString()
            {
                return (Suffix + " - " + Data.Count);
            }
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        public struct find_result_t
        {
            internal static readonly find_result_t[] EMPTY = new find_result_t[ 0 ];

            internal static find_result_t Create( int objIndex, string word, int suffixIndex, int suffixLength )
            {
                var fr = new find_result_t() 
                {
                    ObjIndex     = objIndex, 
                    Word         = word, 
                    SuffixIndex  = suffixIndex, 
                    SuffixLength = suffixLength 
                };
                return (fr);
            }
            /*internal static find_result_t Create( ref data_t data, string word, int suffixLength )
            {
                var fr = new find_result_t() 
                {
                    ObjIndex     = data.WordIndex, 
                    Word         = word, 
                    SuffixIndex  = data.SuffixIndex, 
                    SuffixLength = suffixLength 
                };
                return (fr);
            }*/

            public int     ObjIndex;
            public string  Word;
            public int     SuffixIndex;
            public int     SuffixLength;

            public string GetBeforeSuffix()
            {
                return (Word.Substring( 0, SuffixIndex ));
            }
            public string GetSuffix()
            {
                return (Word.Substring( SuffixIndex, SuffixLength ));
            }
            public string GetAfterSuffix()
            {
                return (Word.Substring( SuffixIndex + SuffixLength ));
            }
            public string GetHighlightSuffix( string left, string right )
            {
                return (string.Concat( GetBeforeSuffix(), left, GetSuffix(), right, GetAfterSuffix() ));
            }
#if DEBUG
            public override string ToString()
            {
                return ('\'' + GetBeforeSuffix() + '[' + GetSuffix() + ']' + GetAfterSuffix() + '\'');
            }
#endif
        }

#if DEBUG
        public override string ToString()
        {
            return (GetAllSuffixesCount( EnumerableModeEnum.BaseOfSuffix ).ToString());
        }
#endif        
        private IList< T >              _Objects;
        private IStringValueGetter< T > _StringValueGetter;
        private tuple_t[]               _Array;

		/// <summary>
        /// Initializes a new instance of the <see cref="T:System.Collections.Generic.sorted_list_key_char`2" /> class that is empty, has the default initial capacity, and uses the default <see cref="T:System.Collections.Generic.IComparer`1" />.
        /// </summary>
        public SuffixArray( IList< T > objs, IStringValueGetter< T > stringValueGetter )
            : this( objs, 0, objs.Count, stringValueGetter )
        {
        }
        public SuffixArray( IList< T > objs, int index, int length, IStringValueGetter< T > stringValueGetter )
        {
            if ( objs == null ) 
                throw (new ArgumentNullException("objs"));
            if ( (length <= 0) || (length <= index) || (objs.Count < length) )
                throw (new ArgumentException( "index-or-length" ));
            if ( stringValueGetter == null )
                throw (new ArgumentNullException("stringValueGetter"));
            //if ( values.Any( s => string.IsNullOrEmpty( s ) ) ) throw (new ArgumentNullException("values.Any()"));

            _Objects           = objs;
            _StringValueGetter = stringValueGetter;
            _Array             = SuffixArrayBuilder< T >.Build( objs, index, length, stringValueGetter );
        }

        /// <summary>
        /// 
        /// </summary>
        public enum FindModeEnum
        {
            IgnoreCase,
            UseCase,
        }

        public IEnumerable< find_result_t > 
            Find( string suffix, FindModeEnum findMode = FindModeEnum.IgnoreCase )
        {
            suffix = CorrectFindSuffix( suffix, findMode );

            var index = InternalBinarySearch( suffix ); 
            if ( 0 <= index )
            {                
                //up
                for ( int i = index; 0 <= i; i-- )
                {
                    var t = _Array[ i ];
                    if ( !t.Suffix.StartsWith( suffix ) )
                        break;
                    foreach ( var data in t.Data )
                    {
                        var word = _StringValueGetter.GetStringValue( _Objects[ data.ObjIndex ] );
                        var endIndex = data.SuffixIndex + suffix.Length;
                        if ( endIndex <= word.Length )
                        {
                            yield return (find_result_t.Create( data.ObjIndex, word, data.SuffixIndex, suffix.Length ));
                        }
                    }
                }
                //down
                for ( int i = index + 1, arrayLength = _Array.Length; i < arrayLength; i++ )
                {
                    var t = _Array[ i ];
                    if ( !t.Suffix.StartsWith( suffix ) )
                        break;
                    foreach ( var data in t.Data )
                    {
                        var word = _StringValueGetter.GetStringValue( _Objects[ data.ObjIndex ] );
                        var endIndex = data.SuffixIndex + suffix.Length;
                        if ( endIndex <= word.Length )
                        {
                            yield return (find_result_t.Create( data.ObjIndex, word, data.SuffixIndex, suffix.Length ));
                        }
                    }
                }
            }
        }

        public int 
            FindCount( string suffix, FindModeEnum findMode = FindModeEnum.IgnoreCase )
        {
            suffix = CorrectFindSuffix( suffix, findMode );

            var index = InternalBinarySearch( suffix );
            var findCount = 0;
            if ( 0 <= index )
            {
                //up
                for ( var i = index; 0 <= i; i-- )
                {
                    var t = _Array[ i ];
                    if ( !t.Suffix.StartsWith( suffix ) )
                        break;
                    foreach ( var data in t.Data )
                    {
                        var word = _StringValueGetter.GetStringValue( _Objects[ data.ObjIndex ] );
                        var endIndex = data.SuffixIndex + suffix.Length;
                        if ( endIndex <= word.Length )
                        {
                            findCount++;
                        }
                    }
                }
                //down
                for ( int i = index + 1, arrayLength = _Array.Length; i < arrayLength; i++ )
                {
                    var t = _Array[ i ];
                    if ( !t.Suffix.StartsWith( suffix ) )
                        break;
                    foreach ( var data in t.Data )
                    {
                        var word = _StringValueGetter.GetStringValue( _Objects[ data.ObjIndex ] );
                        var endIndex = data.SuffixIndex + suffix.Length;
                        if ( endIndex <= word.Length )
                        {
                            findCount++;
                        }
                    }
                }
            }
            return (findCount);
        }

        public find_result_t[] 
            Find( string suffix, int maxCount, out int findTotalCount, FindModeEnum findMode = FindModeEnum.IgnoreCase )
        {
            suffix = CorrectFindSuffix( suffix, findMode );

            findTotalCount = 0;
            
            var index = InternalBinarySearch( suffix ); 
            if ( 0 <= index )
            {
                var frs = new LinkedList< find_result_t >();

                //up
                for ( int i = index; 0 <= i; i-- )
                {
                    var t = _Array[ i ];
                    if ( !t.Suffix.StartsWith( suffix ) )
                        break;
                    foreach ( var data in t.Data )
                    {
                        var word = _StringValueGetter.GetStringValue( _Objects[ data.ObjIndex ] );
                        var endIndex = data.SuffixIndex + suffix.Length;
                        if ( endIndex <= word.Length )
                        {
                            if ( ++findTotalCount <= maxCount )
                            {
                                frs.AddFirst( find_result_t.Create( data.ObjIndex, word, data.SuffixIndex, suffix.Length ) );
                            }
                        }
                    }
                }
                //down
                for ( int i = index + 1, arrayLength = _Array.Length; i < arrayLength; i++ )
                {
                    var t = _Array[ i ];
                    if ( !t.Suffix.StartsWith( suffix ) )
                        break;
                    foreach ( var data in t.Data )
                    {
                        var word = _StringValueGetter.GetStringValue( _Objects[ data.ObjIndex ] );
                        var endIndex = data.SuffixIndex + suffix.Length;
                        if ( endIndex <= word.Length )
                        {
                            if ( ++findTotalCount <= maxCount )
                            {
                                frs.AddLast( find_result_t.Create( data.ObjIndex, word, data.SuffixIndex, suffix.Length ) );
                            }
                        }
                    }
                }

                return (frs.ToArray());
            }

            return (find_result_t.EMPTY);
        }

		/// <summary>Determines whether the <see cref="T:System.Collections.Generic.sorted_list_key_char`2" /> contains a specific key.</summary>
		/// <returns>true if the <see cref="T:System.Collections.Generic.sorted_list_key_char`2" /> contains an element with the specified key; otherwise, false.</returns>
		/// <param name="suffix">The key to locate in the <see cref="T:System.Collections.Generic.sorted_list_key_char`2" />.</param>
		/// <exception cref="T:System.ArgumentNullException">
		///   <paramref name="suffix" /> is null.</exception>
        public bool 
            ContainsKey( string suffix, FindModeEnum findMode = FindModeEnum.IgnoreCase )
		{
            suffix = CorrectFindSuffix( suffix, findMode );

            int n = InternalBinarySearch( suffix );
            return (n >= 0);
		}

        #region [.IEnumerable< tuple_t >.]
        public IEnumerator< find_result_t > GetEnumerator()
        {
            for ( int i = 0, arrayLength = _Array.Length; i < arrayLength; i++ )
            {
                var t = _Array[ i ];
                foreach ( var data in t.Data )
                {
                    var word = _StringValueGetter.GetStringValue( _Objects[ data.ObjIndex ] );
                    var fr = find_result_t.Create( data.ObjIndex, word, data.SuffixIndex, -1 );
                    for ( int j = 1, len = word.Length - data.SuffixIndex; j <= len; j++ )
                    {
                        fr.SuffixLength = j;
                        yield return (fr);
                    }
                }
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (GetEnumerator());
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        public enum EnumerableModeEnum
        {
            BaseOfSuffix,
            AllSubSuffix,
        }

        public IEnumerable< string > GetAllSuffixes( EnumerableModeEnum enumerableMode )
        {
            for ( int i = 0, arrayLength = _Array.Length; i < arrayLength; i++ )
            {
                var suffix = _Array[ i ].Suffix;

                yield return (suffix);

                if ( enumerableMode == EnumerableModeEnum.AllSubSuffix )
                {
                    for ( int len = suffix.Length - 1; 1 <= len; len-- )
                    {
                        yield return (suffix.Substring( 0, len ));
                    }
                }
            }
        }
        public int GetAllSuffixesCount( EnumerableModeEnum enumerableMode )
        {
            var suffixCount = _Array.Length;
            if ( enumerableMode == EnumerableModeEnum.AllSubSuffix )
            {
                for ( int i = 0, arrayLength = _Array.Length; i < arrayLength; i++ )
                {
                    suffixCount += _Array[ i ].Suffix.Length;
                }
            }
            return (suffixCount);
        }

        private int InternalBinarySearch( string suffix4Find )
        {
            int i  = 0;
            int n1 = _Array.Length - 1;
            while ( i <= n1 )
            {
                int n2 = i + (n1 - i >> 1);
                var suffix = _Array[ n2 ].Suffix;
                int n3;
                if ( suffix4Find.Length <= suffix.Length )
                    n3 = string.CompareOrdinal( suffix, 0, suffix4Find, 0, suffix4Find.Length );
                else
                    n3 = string.CompareOrdinal( suffix, suffix4Find );
                if ( n3 == 0 )
                {
                    return n2;
                }

                if ( n3 < 0 )
                {
                    i = n2 + 1;
                }
                else
                {
                    n1 = n2 - 1;
                }
            }
            return (~i);
        }

        private static string CorrectFindSuffix( string suffix, FindModeEnum findMode )
        {
            suffix = suffix.Replace( 'Έ', 'ε' ).Replace( '¨', 'Ε' );

            if ( findMode == FindModeEnum.IgnoreCase )
            {
                suffix = suffix.ToUpperInvariant();
            }

            return (suffix);
        }
    }
}
