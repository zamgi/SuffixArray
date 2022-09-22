using System.Linq;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    /// <summary>
    /// 
    /// </summary>
	public sealed class SuffixArray_v2< T > : SuffixArrayBase< T >, IEnumerable< SuffixArrayBase< T >.find_result_t >
	{
        /// <summary>
        /// 
        /// </summary>
        private struct data_t
        {
            public data_t( int suffixIndex, int objIndex )
            {
                SuffixIndex = suffixIndex;
                ObjIndex    = objIndex;
            }

            public int SuffixIndex;
            public int ObjIndex;
#if DEBUG
            public override string ToString() => (SuffixIndex + " (w: " + ObjIndex + ")");
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        private struct tuple_t : IComparer< tuple_t >
        {
            public string Suffix;
            public SimplyLinkedList< data_t > Data;
#if DEBUG
            public override string ToString() => (Suffix + " - " + Data.Count);
#endif
            public int CompareTo( tuple_t other )
            {
                //if ( other.Suffix.Length <= this.Suffix.Length )
                //{
                //    return (string.Compare( this.Suffix, 0, other.Suffix, 0, other.Suffix.Length, false ));
                //}
                //return (string.Compare( this.Suffix, other.Suffix, false ));

                return (other.Suffix.CompareTo( this.Suffix )); //---return (this.Suffix.CompareTo( other.Suffix ));
            }

            public int Compare( tuple_t x, tuple_t y ) => string.CompareOrdinal( x.Suffix, y.Suffix );
        }

        /// <summary>
        /// 
        /// </summary>
        private struct StartsWithStringComparer : IBPlusTreeComparer< tuple_t >
        {
            public static readonly StartsWithStringComparer Inst = new StartsWithStringComparer();

            public int Compare( tuple_t existsInTreeValue, tuple_t searchingValue )
            {
                if ( searchingValue.Suffix.Length <= existsInTreeValue.Suffix.Length )
                {
                    return (string.Compare( existsInTreeValue.Suffix, 0, searchingValue.Suffix, 0, searchingValue.Suffix.Length, false ));
                }
                return (string.Compare( existsInTreeValue.Suffix, searchingValue.Suffix, false ));

                //return (string.Compare( value, 0, key, 0, Math.Min( value.Length, key.Length ), true));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static class SuffixArrayBuilder
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
                public override string ToString() => (Suffix + " - " + SuffixIndex + " (w: " + WordIndex + ")");
#endif
                /*public override int GetHashCode() => (SuffixIndex.GetHashCode() ^ WordIndex.GetHashCode());*/
            }
            /// <summary>
            /// 
            /// </summary>
            private sealed class suffix_t_IEqualityComparer : IEqualityComparer< suffix_t >
            {
                public bool Equals( suffix_t x, suffix_t y ) => (/*x.WordIndex == y.WordIndex &&*/ x.SuffixIndex == y.SuffixIndex);
                public int GetHashCode( suffix_t obj ) => obj.SuffixIndex.GetHashCode();
            }

            private static bool[] IS_LETTER_OR_DIGIT_MAP;
            private static char[] UPPER_INVARIANT_MAP;
            private static void CreateMapArrays()
            {
                IS_LETTER_OR_DIGIT_MAP = new bool[ char.MaxValue ];
                UPPER_INVARIANT_MAP    = new char[ char.MaxValue ];
                for ( var ch = char.MinValue; ; )
                {
                    IS_LETTER_OR_DIGIT_MAP[ ch ] = char.IsLetter( ch ) || char.IsDigit( ch );
                    UPPER_INVARIANT_MAP   [ ch ] = char.ToUpperInvariant( ch );
                    if ( ++ch == char.MaxValue )
                    {
                        break;
                    }
                }
            }
            private static void DestroyMapArrays()
            {
                IS_LETTER_OR_DIGIT_MAP = null;
                UPPER_INVARIANT_MAP    = null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static string ClearString( string word, ref int startIndex )
            {
                int len = word.Length;
                for ( ; startIndex < len; startIndex++ )
                {
                    if ( IS_LETTER_OR_DIGIT_MAP[ word[ startIndex ] ] )
                        break;
                }
                if ( startIndex < len )
                {
                    var endIndex = len - 1;
                    for ( ; 0 <= endIndex; endIndex-- )
                    {
                        if ( IS_LETTER_OR_DIGIT_MAP[ word[ endIndex ] ] )
                            break;
                    }
                    if ( startIndex <= endIndex )
                    {
                        return (word.Substring( startIndex, endIndex - startIndex + 1 ));
                    }
                }   
             
                return (null);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            unsafe private static void ToUpperInvariantInPlace( string value )
            {
                fixed ( char* value_ptr = value )
                {
                    ToUpperInvariantInPlace( value_ptr );
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            unsafe private static void ToUpperInvariantInPlace( char* word )
            {
                for ( ; ; word++ )
                {
                    var ch = *word;
                    if ( ch == '\0' )
                        return;
                    *word = UPPER_INVARIANT_MAP[ ch ]; //--- *(UPPER_INVARIANT_MAP + ch);
                }
            }

            private static int GetSuffixCount( string value ) => value.Length;
            private static IEnumerable< suffix_t > GetSuffixes( int wordIndex, string word )
            {
                int startIndex = 0;
                word = ClearString( word, ref startIndex );
                if ( word != null )
                {
                    ToUpperInvariantInPlace( word ); //---word = word.ToUpperInvariant();
                    yield return (new suffix_t( word, startIndex, wordIndex ));

                    for ( int beginIndex = startIndex, len = word.Length; startIndex < len; startIndex++ )
                    {
                        var suffix = ClearString( word, ref startIndex );
                        if ( suffix != null )
                        {
                            yield return (new suffix_t( suffix, beginIndex + startIndex, wordIndex ));
                        }
                    }
                }
            }

            public static BPlusTreeList< tuple_t > Build( IList< T > objs, int index, int length, IStringValueGetter< T > stringValueGetter )
            {
                CreateMapArrays();

                var totalSuffixCount = (from value in objs.Skip( index ).Take( length )
                                            select GetSuffixCount( stringValueGetter.GetStringValue( value ) )
                                       ).Sum();

                /*
                var capacity = (int) Math.Sqrt( totalSuffixCount ); //(int) (Math.Sqrt( length - index ) + 1);
                var bpt = new BPlusTreeList< tuple_t >( default(tuple_t), capacity, capacity );
                */
                int BLOCK_CAPACITY_4_LST = 512;
                var bpt = new BPlusTreeList< tuple_t >( default(tuple_t), ((int) (totalSuffixCount / BLOCK_CAPACITY_4_LST * 1.0 + 0.5) + 25), BLOCK_CAPACITY_4_LST );

                var set = new Set< suffix_t >( new suffix_t_IEqualityComparer() );

                for ( int i = index, end = index + length; i < end; i++ )
                {
                    var str = stringValueGetter.GetStringValue( objs[ i ] );
                    #region test.commented.
                    /*
                    if ( str == "м.бабий" )
                    System.Diagnostics.Debugger.Break();
                    var __ = GetSuffix( i, str ).Distinct().ToArray();
                    */
                    #endregion

                    var tuple = new tuple_t() { Data = new SimplyLinkedList< data_t >() };
                    var tupleExists = default(tuple_t);

                    #region test.commented.
                    /*
                    str = "м.бабий";
                    var x1 = GetSuffixes( i, str ).ToArray();
                    var x2 = GetSuffixes( i, str ).Distinct().ToArray();
                    if ( x1.Length != x2.Length )
                    {
                        foreach ( var suff_t in GetSuffixes( i, str ) )
                        {
                            set.Add( suff_t );
                        }
                        System.Diagnostics.Debug.Assert( set.Count == x2.Length );
                    }
                    */
                    #endregion

                    foreach ( var suff_t in GetSuffixes( i, str )/*.Distinct()*/ )
                    {
                        if ( !set.Add( suff_t ) )
                        {
                            continue;
                        }

                        var data = new data_t( suff_t.SuffixIndex, suff_t.WordIndex );
                        tuple.Suffix = suff_t.Suffix;
                        if ( bpt.AddOrGetExistsValue( tuple, out tupleExists ) )
                        {
                            tuple.Data.Add( data );
                            tuple = new tuple_t() { Data = new SimplyLinkedList< data_t >() };
                        }
                        else
                        {
                            tupleExists.Data.Add( data );
                        }
                    }
                    set.Clear();
                }

                DestroyMapArrays();

                var bpt_out = new BPlusTreeList< tuple_t >( default(tuple_t), bpt.Count / bpt.BlockCount, bpt.BlockCount );
                using ( var e = bpt.GetEnumerator() )
                {
                    if ( e.MoveNext() )
                    {
                        var root_tuple = e.Current;
                        bpt_out.Add( root_tuple );
                        for ( ; e.MoveNext(); )
                        {
                            var tuple = e.Current;
                            if ( root_tuple.Suffix.StartsWith( tuple.Suffix ) )
                            {
                                foreach ( var data in tuple.Data )
                                {
                                    root_tuple.Data.Add( data );
                                }
                            }
                            else
                            {
                                root_tuple = tuple;
                                bpt_out.Add( root_tuple );
                            }
                        }
                    }
                }

                return (bpt_out);
            }
        }

#if DEBUG
        public override string ToString() => GetAllSuffixesCount( EnumerableModeEnum.BaseOfSuffix ).ToString();
#endif        
        private IList< T >               _Objects;
        private IStringValueGetter< T >  _StringValueGetter;
        private BPlusTreeList< tuple_t > _BPT;

		/// <summary>
        /// Initializes a new instance of the <see cref="T:System.Collections.Generic.sorted_list_key_char`2" /> class that is empty, has the default initial capacity, and uses the default <see cref="T:System.Collections.Generic.IComparer`1" />.
        /// </summary>
        public SuffixArray_v2( IList< T > objs, IStringValueGetter< T > stringValueGetter ) : this( objs, 0, objs.Count, stringValueGetter ) { }
        public SuffixArray_v2( IList< T > objs, int index, int length, IStringValueGetter< T > stringValueGetter )
        {
            if ( objs == null )                                                throw (new ArgumentNullException( nameof(objs) ));
            if ( (length <= 0) || (length <= index) || (objs.Count < length) ) throw (new ArgumentException( "index-or-length" ));
            if ( stringValueGetter == null )                                   throw (new ArgumentNullException( nameof(stringValueGetter) ));
            //if ( values.Any( s => string.IsNullOrEmpty( s ) ) ) throw (new ArgumentNullException("values.Any()"));

            _Objects           = objs;
            _StringValueGetter = stringValueGetter;
            _BPT               = SuffixArrayBuilder.Build( objs, index, length, stringValueGetter );
        }

        public override IEnumerable< find_result_t > Find( string suffix, FindModeEnum findMode = FindModeEnum.IgnoreCase )
        {
            suffix = CorrectFindSuffix( suffix, findMode );

            var tuple = new tuple_t() { Suffix = suffix };
            var bpt_tuples = _BPT.GetValues( tuple, StartsWithStringComparer.Inst );

            foreach ( var bpt_tuple in bpt_tuples )
            {
                foreach ( var data in bpt_tuple.Data )
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

        public override int FindCount( string suffix, FindModeEnum findMode = FindModeEnum.IgnoreCase )
        {
            suffix = CorrectFindSuffix( suffix, findMode );

            var findCount = 0;

            var tuple = new tuple_t() { Suffix = suffix };
            var bpt_tuples = _BPT.GetValues( tuple, StartsWithStringComparer.Inst );

            foreach ( var bpt_tuple in bpt_tuples )
            {
                foreach ( var data in bpt_tuple.Data )
                {
                    var word = _StringValueGetter.GetStringValue( _Objects[ data.ObjIndex ] );
                    var endIndex = data.SuffixIndex + suffix.Length;
                    if ( endIndex <= word.Length )
                    {
                        findCount++;
                    }
                }
            }
       
            return (findCount);
        }

        public override find_result_t[] Find( string suffix, int maxCount, out int findTotalCount, FindModeEnum findMode = FindModeEnum.IgnoreCase )
        {
            findTotalCount = 0;

            suffix = CorrectFindSuffix( suffix, findMode );            

            var tuple = new tuple_t() { Suffix = suffix };
            var bpt_tuples = _BPT.GetValues( tuple, StartsWithStringComparer.Inst );

            var frs = new LinkedList< find_result_t >();

            foreach ( var bpt_tuple in bpt_tuples )
            {
                foreach ( var data in bpt_tuple.Data )
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
                      
            return (frs.Any() ? frs.ToArray() : find_result_t.EMPTY);
        }

		/// <summary>Determines whether the <see cref="T:System.Collections.Generic.sorted_list_key_char`2" /> contains a specific key.</summary>
		/// <returns>true if the <see cref="T:System.Collections.Generic.sorted_list_key_char`2" /> contains an element with the specified key; otherwise, false.</returns>
		/// <param name="suffix">The key to locate in the <see cref="T:System.Collections.Generic.sorted_list_key_char`2" />.</param>
		/// <exception cref="T:System.ArgumentNullException">
		///   <paramref name="suffix" /> is null.</exception>
        public override bool ContainsKey( string suffix, FindModeEnum findMode = FindModeEnum.IgnoreCase )
		{
            suffix = CorrectFindSuffix( suffix, findMode );

            var tuple = new tuple_t() { Suffix = suffix };
            var bpt_tuples = _BPT.GetValues( tuple, StartsWithStringComparer.Inst );

            return (bpt_tuples.Any());
		}

        public override IEnumerable< string > GetAllSuffixes( EnumerableModeEnum enumerableMode )
        {
            foreach ( var t in _BPT )
            {
                yield return (t.Suffix);

                if ( enumerableMode == EnumerableModeEnum.AllSubSuffix )
                {
                    for ( int len = t.Suffix.Length - 1; 1 <= len; len-- )
                    {
                        yield return (t.Suffix.Substring( 0, len ));
                    }
                }
            }
        }
        public override int GetAllSuffixesCount( EnumerableModeEnum enumerableMode )
        {
            switch ( enumerableMode )
            {
                case EnumerableModeEnum.BaseOfSuffix:
                    return (_BPT.GetCount());

                case EnumerableModeEnum.AllSubSuffix:
                    var suffixCount = 0;
                    foreach ( var t in _BPT )
                    {
                        suffixCount += t.Data.Count;
                    }
                    return (suffixCount);

                default:
                    throw (new ArgumentException( enumerableMode.ToString() ));
            }
        }

        #region [.IEnumerable< tuple_t >.]
        public override IEnumerator< find_result_t > GetEnumerator()
        {
            throw new NotImplementedException();

            //for ( int i = 0, arrayLength = _BPT.Length; i < arrayLength; i++ )
            //{
            //    var t = _BPT[ i ];
            //    foreach ( var data in t.Data )
            //    {
            //        var word = _StringValueGetter.GetStringValue( _Objects[ data.ObjIndex ] );
            //        var fr = find_result_t.Create( data.ObjIndex, word, data.SuffixIndex, -1 );
            //        for ( int j = 1, len = word.Length - data.SuffixIndex; j <= len; j++ )
            //        {
            //            fr.SuffixLength = j;
            //            yield return (fr);
            //        }
            //    }
            //}
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        private static string CorrectFindSuffix( string suffix, FindModeEnum findMode )
        {
            suffix = suffix.Replace( 'ё', 'е' ).Replace( 'Ё', 'Е' );

            if ( findMode == FindModeEnum.IgnoreCase )
            {
                suffix = suffix.ToUpperInvariant();
            }

            return (suffix);
        }
    }
}
