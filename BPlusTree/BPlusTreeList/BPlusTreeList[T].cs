using System.Diagnostics;
using System.Linq;

namespace System.Collections.Generic
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class SortedBlockList< T > : SortedList< BPlusTreeBlock< T > >
    {
        public SortedBlockList( int capacity ) : base( BPlusTreeBlock< T >.BlockComparer.Inst, capacity ) { }

        public int BinarySearch( T value )
        {
            int i = 0;
            for ( var endIndex = this.Count - 1; i <= endIndex; )
            {
                int middleIndex = i + ((endIndex - i) >> 1);
                int d = this[ middleIndex ].CompareWith4_SortedBlockList( value );
                if ( d == 0 )
                {
                    return (middleIndex);
                }

                if ( d < 0 )
                {
                    i = middleIndex + 1;
                }
                else
                {
                    endIndex = middleIndex - 1;
                }
            }
            return (~i);
        }
#if DEBUG
        public override string ToString()
        {
            if ( Count == 0 )
            {
                return ("EMPTY");
            }
            return ($"count: {Count}, min: '{this[ 0 ].Min}', max: '{this[ this.Count - 1 ].Max}'");
        }
#endif
    }


    /// <summary>
    /// 
    /// </summary>
    public sealed class BPlusTreeList< T > : IBPlusTree< T >
    {
        private const int DEFAULT_BLOCKLIST_CAPACITY = 7;
        private const int DEFAULT_BLOCK_CAPACITY     = 100;

        private SortedBlockList< T > _SortedBlockList;
        private int                  _BlockCapacity;
        private IComparer< T >       _Comparer;

        public BPlusTreeList( IComparer< T > comparer ) : this( comparer, DEFAULT_BLOCKLIST_CAPACITY, DEFAULT_BLOCK_CAPACITY ) { }
        public BPlusTreeList( IComparer< T > comparer, int blockCapacity ) : this( comparer, DEFAULT_BLOCKLIST_CAPACITY, blockCapacity ) { }
        public BPlusTreeList( IComparer< T > comparer, int blockListCapacity, int blockCapacity )
        {
            _Comparer        = comparer;
            _SortedBlockList = new SortedBlockList< T >( blockListCapacity );
            _BlockCapacity   = blockCapacity;
        }

        public bool Add( T t )
        {
            var index = _SortedBlockList.BinarySearch( t );
            if ( index < 0 ) //not-exists
            {
                var count = _SortedBlockList.Count;
                if ( count == 0 )
                {
                    var blockForAdd = new BPlusTreeBlock< T >( _Comparer, _BlockCapacity, t );
                    _SortedBlockList.Add( blockForAdd );
                    return (true);
                }

                index = ~index;
                if ( index == count )
                {
                    index--;
                }
            }

            var block = _SortedBlockList[ index ];
            if ( block.IsFull )
            {
                //Debugger.Break();

                var newBlock = block.SplitInTwo();
                _SortedBlockList.Add( newBlock );
                
                index = block.IndexOfKeyCore( t );
                if ( index < 0 ) //not-exists
                {
                    index = ~index;
                    if ( index != block.Count )
                    {
                        var result = block.TryAdd( t );
                        Debug.Assert( result );
                        return (result);
                    }
                    else
                    {
                        var result = newBlock.TryAdd( t );
                        return (result);
                    }
                }
                return (false);
            }
            else
            {
                var result = block.TryAdd( t );
                return (result);
            }            
        }
        public bool TryGetValue( T t, out T exists )
        {
            var index = _SortedBlockList.BinarySearch( t );
            if ( index < 0 )
            {
                var count = _SortedBlockList.Count;
                if ( count == 0 )
                {
                    exists = default(T);
                    return (false);
                }

                index = ~index;
                if ( index == count )
                {
                    index--;
                }
            }

            var block = _SortedBlockList[ index ];
            var success = block.TryGetValue( t, out exists );
            return (success);
        }
        public bool AddOrGetExistsValue( T t, out T exists )
        {
            var index = _SortedBlockList.BinarySearch( t );
            if ( index < 0 ) //not-exists => Add
            {
                var count = _SortedBlockList.Count;
                if ( count == 0 ) // empty => Add
                {
                    var blockForAdd = new BPlusTreeBlock< T >( _Comparer, _BlockCapacity, t );
                    _SortedBlockList.Add( blockForAdd );
                    exists = default(T);
                    return (true);
                }

                index = ~index;
                if ( index == count )
                {
                    index--;
                }
            }

            var block = _SortedBlockList[ index ];
            if ( block.IsFull )
            {
                var newBlock = block.SplitInTwo();
                _SortedBlockList.Add( newBlock );
                
                index = block.IndexOfKeyCore( t );
                if ( index < 0 ) //not-exists => Add
                {
                    index = ~index;
                    if ( index != block.Count )
                    {
                        var result = block.TryAddOrGetExistsValue( t, out exists ); //---block.Add( t );
                        Debug.Assert( result, "!result" );
                        return (result);
                    }
                    else
                    {
                        var result = newBlock.TryAddOrGetExistsValue( t, out exists ); //---newBlock.TryAdd( t );
                        return (result);
                    }
                }
                else //exists => Get
                {
                    exists = block[ index ];
                }
                return (false);
            }
            else
            {
                var result = block.TryAddOrGetExistsValue( t, out exists ); //---block.Add( t );
                return (result);
            }
        }
        public T GetValue( T t, T defaultValue = default(T) )
        {
            var index = _SortedBlockList.BinarySearch( t );
            if ( index < 0 )
            {
                var count = _SortedBlockList.Count;
                if ( count == 0 )
                {
                    return (defaultValue);
                }

                index = ~index;
                if ( index == count )
                {
                    index--;
                }
            }

            var block = _SortedBlockList[ index ];
            var v = block.GetValue( t, defaultValue );
            return (v);
        }
        public bool Contains( T t )
        {
            var index = _SortedBlockList.BinarySearch( t );
            if ( index < 0 )
            {
                var count = _SortedBlockList.Count;
                if ( count == 0 )
                {
                    return (false);
                }

                index = ~index;
                if ( index == count )
                {
                    index--;
                }
            }

            var block = _SortedBlockList[ index ];
            index = block.IndexOfKeyCore( t );
            return (0 <= index);
        }
        public bool Remove( T t )
        {
            var index = _SortedBlockList.BinarySearch( t );
            if ( index < 0 )
            {
                var count = _SortedBlockList.Count;
                if ( count == 0 )
                {
                    return (false);
                }

                index = ~index;
                if ( index == count )
                {
                    index--;
                }
            }

            var block = _SortedBlockList[ index ];
            if ( block.Count == 1 )
            {
                index = block.IndexOfKey( t );
                if ( 0 <= index )
                {
                    var success_2 = _SortedBlockList.Remove( block );
                    Debug.Assert( success_2 );

                    block.RemoveAt( index ); //unnecessary
                    Debug.Assert( block.Count == 0 );

                    return (true);
                }
                return (false);
            }
            else
            {
                var success = block.Remove( t );
                return (success);
            }
        }
        public IEnumerable< T > GetValuesBetween( T min, T max )
        {
            #region [.check input params.]
            if ( 0 < _Comparer.Compare( min, max ) )
                throw (new ArgumentException( "max < min" )); 
            #endregion

            #region commented
            /*
            #region [.-1- search in fnb.Block-list.]
            var count = default( int );
            var fnb.Index = _SortedBlockList.BinarySearch( min );
            if ( fnb.Index < 0 ) //not-exists
            {
                count = _SortedBlockList.Count;
                if ( count == 0 )
                {
                    yield break;
                }

                fnb.Index = ~fnb.Index;
                if ( fnb.Index == count )
                {
                    fnb.Index--;
                }
            } 
            #endregion

            #region [.-2- getting fnb.Block and search in him.]
            var fnb.Block = _SortedBlockList[ fnb.Index ];
            count = fnb.Block.Count;
            fnb.Index = fnb.Block.IndexOfKeyCore( min );
            if ( fnb.Index < 0 ) //not-exists
            {
                if ( count == 0 )
                {
                    yield break;
                }

                fnb.Index = ~fnb.Index;
                //if ( fnb.Index == count )
                //{
                //    fnb.Index--;
                //}
                if ( fnb.Index != 0 )
                {
                    fnb.Index--;
                }
            } 
            #endregion
            */
            #endregion

            #region [.-1,2- find nearest block.]
            var r = default(FindNearestBlockResult);            
            if ( !FindNearestBlock( min, ref r ) )
            {
                yield break;
            }
            #endregion

            #region [.-3.1- first block.]
            for ( var count = r.Block.Count; r.Index < count; r.Index++ )
            {
                var v = r.Block[ r.Index ];
                if ( 0 <= _Comparer.Compare( v, min ) ) //min <= v
                {
                    if ( 0 < _Comparer.Compare( v, max ) ) //max < v
                    {
                        yield break;
                    }
                    yield return (v);

                    for ( r.Index++; r.Index < count; r.Index++ )
                    {
                        v = r.Block[ r.Index ];
                        if ( 0 < _Comparer.Compare( v, max ) ) //max < v
                        {
                            yield break;
                        }
                        yield return (v);
                    }
                }
            } 
            #endregion

            #region [.-3.2- next block's.]
            for ( var block = r.Block.Next; block != null; block = block.Next )
            {
                foreach ( var v in block )
                {
                    //if ( 0 <= v.CompareTo( min ) ) //min <= v
                    //{
                    if ( 0 < _Comparer.Compare( v, max ) ) //max < v
                    {
                        yield break;
                    }
                    yield return (v);
                    //}
                }
            } 
            #endregion 
        }
        public IEnumerable< T > GetValues( T t )
        {
            #region [.-1,2- find nearest block.]
            var r = default(FindNearestBlockResult);
            if ( !FindNearestBlock( t, ref r ) )
            {
                yield break;
            }
            #endregion

            #region [.-3.1- first block.]
            for ( var count = r.Block.Count; r.Index < count; r.Index++ )
            {
                var v = r.Block[ r.Index ];
                if ( _Comparer.Compare( v, t ) == 0 )
                {
                    yield return (v);

                    for ( r.Index++; r.Index < count; r.Index++ )
                    {
                        v = r.Block[ r.Index ];
                        if ( _Comparer.Compare( v, t ) != 0 )
                        {
                            yield break;
                        }
                        yield return (v);
                    }
                }
            } 
            #endregion

            #region [.-3.2- next block's.]
            for ( var block = r.Block.Next; block != null; block = block.Next )
            {
                foreach ( var v in block )
                {
                    if ( _Comparer.Compare( v, t ) != 0 )
                    {
                        yield break;
                    }
                    yield return (v);
                }
            } 
            #endregion                            
        }
        public IEnumerable< T > GetValuesBetween( T min, T max, IBPlusTreeComparer< T > comparer )
        {
            #region [.check input params.]
            if ( 0 < comparer.Compare( min, max ) )
                throw (new ArgumentException( "max < min" )); 
            #endregion

            #region [.-1,2- find nearest block.]
            var r = default(FindNearestBlockResult);
            if ( !FindNearestBlock( min, ref r ) )
            {
                yield break;
            }
            #endregion

            #region [.-3.1- first block.]
            for ( var count = r.Block.Count; r.Index < count; r.Index++ )
            {
                var v = r.Block[ r.Index ];
                if ( 0 <= comparer.Compare( v, min ) ) //min <= t
                {
                    if ( 0 < comparer.Compare( v, max ) ) //max < v
                    {
                        yield break;
                    }
                    yield return (v);

                    for ( r.Index++; r.Index < count; r.Index++ )
                    {
                        v = r.Block[ r.Index ];
                        if ( 0 < comparer.Compare( v, max ) ) //max < v
                        {
                            yield break;
                        }
                        yield return (v);
                    }
                }
            } 
            #endregion

            #region [.-3.2- next block's.]
            for ( var block = r.Block.Next; block != null; block = block.Next )
            {
                foreach ( var v in block )
                {
                    //if ( 0 <= comparer.Compare( v, min ) ) //min <= v
                    //{
                    if ( 0 < comparer.Compare( v, max ) ) //max < v
                    {
                        yield break;
                    }
                    yield return (v);
                    //}
                }
            } 
            #endregion            
        }
        public IEnumerable< T > GetValues( T t, IBPlusTreeComparer< T > comparer )
        {
            #region [.-1,2- find nearest block.]
            var r = default(FindNearestBlockResult);
            if ( !FindNearestBlock( t, ref r ) )
            {
                yield break;
            }
            #endregion

            #region [.-3.1- first block.]
            for ( var count = r.Block.Count; r.Index < count; r.Index++ )
            {
                var v = r.Block[ r.Index ];
                if ( comparer.Compare( v, t ) == 0 )
                {
                    yield return (v);

                    for ( r.Index++; r.Index < count; r.Index++ )
                    {
                        v = r.Block[ r.Index ];
                        if ( comparer.Compare( v, t ) != 0 )
                        {
                            yield break;
                        }
                        yield return (v);
                    }
                }
            } 
            #endregion

            #region [.-3.2- next block's.]
            for ( var block = r.Block.Next; block != null; block = block.Next )
            {
                foreach ( var v in block )
                {
                    if ( comparer.Compare( v, t ) != 0 )
                    {
                        yield break;
                    }
                    yield return (v);
                }
            } 
            #endregion            
        }

        /// <summary>
        /// 
        /// </summary>
        private struct FindNearestBlockResult
        {
            public int Index;
            public BPlusTreeBlock< T > Block;
        }
        private bool FindNearestBlock( T t, ref FindNearestBlockResult fnb )
        {
            #region [.-1- search in block-list.]
            var index = _SortedBlockList.BinarySearch( t );
            if ( index < 0 ) //not-exists
            {
                var count = _SortedBlockList.Count;
                if ( count == 0 )
                {
                    return (false);
                }

                index = ~index;
                if ( index == count )
                {
                    index--;
                }
            }
            #endregion

            #region [.-2- getting block and search in him.]
            var block = _SortedBlockList[ index ];            
            index = block.IndexOfKeyCore( t );
            if ( index < 0 ) //not-exists
            {
                if ( block.Count == 0 )
                {
                    return (false);
                }

                index = ~index;
                //if ( index == count )
                //{
                //    index--;
                //}
                if ( index != 0 )
                {
                    index--;
                }
            }
            fnb.Index = index;
            fnb.Block = block;
            return (true);

            #endregion
        }

        public int GetCount() => _SortedBlockList.Sum( block => block.Count );
        public int Count => GetCount();
        public int BlockCount => _SortedBlockList.Count;

        public void Trim()
        {
            _SortedBlockList.Trim();
            foreach ( var block in _SortedBlockList )
            {
                block.Trim();
            }
        }

        public IEnumerator< T > GetEnumerator()
        {
            foreach ( var block in _SortedBlockList )
            {
                foreach ( var t in block )
                {
                    yield return (t);
                }
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
#if DEBUG
        public override string ToString() => _SortedBlockList.ToString();
#endif
    }
}