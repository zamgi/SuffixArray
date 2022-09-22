using System.Diagnostics;
using System.Linq;

namespace System.Collections.Generic
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class SortedBlockSet< T > : SortedSetEx< BPlusTreeBlock< T > >
    {
        public SortedBlockSet() : base( BPlusTreeBlock< T >.BlockComparer.Inst ) { }

        public BPlusTreeBlock< T > FindNearestBlock( T value )
        {
            Node currNode = base._Root;
            if ( currNode == null )
            {
                return (null);
            }
            for ( ; ; )
            {
                int order = currNode.Item.CompareWith4_SortedBlockSet( value );
                if ( order == 0 )
                {
                    return (currNode.Item);
                }
                else
                {
                    var t = (order < 0) ? currNode.Right : currNode.Left;
                    if ( t == null )
                    {
                        return (currNode.Item);
                    }
                    currNode = t;
                }
            }
        }
#if DEBUG
        public override string ToString()
        {
            if ( this.Count == 0 )
            {
                return ("EMPTY");
            }
            return ($"count: {Count}, min: '{this.Min.Min}', max: '{this.Max.Max}'");
        }
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class BPlusTreeSet< T > : IBPlusTree< T >
    {
        private const int DEFAULT_BLOCK_CAPACITY = 0x100;

        private SortedBlockSet< T > _SortedBlockSet;
        private int                 _BlockCapacity;
        private IComparer< T >      _Comparer;

        public BPlusTreeSet( IComparer< T > comparer ) : this( comparer, DEFAULT_BLOCK_CAPACITY ) { }
        public BPlusTreeSet( IComparer< T > comparer, int blockCapacity )
        {
            _Comparer       = comparer;
            _SortedBlockSet = new SortedBlockSet< T >();
            _BlockCapacity  = blockCapacity;
        }

        public bool Add( T t )
        {
            var block = _SortedBlockSet.FindNearestBlock( t );
            if ( block == null ) //not-exists
            {
                block = new BPlusTreeBlock< T >( _Comparer, _BlockCapacity, t );
                _SortedBlockSet.Add( block );
                return (true);
            }

            if ( block.IsFull )
            {
                //Debugger.Break();

                //---var result = block.TryAdd( t );
                var newBlock = block.SplitInTwo();
                _SortedBlockSet.Add( newBlock );
                
                var index = block.IndexOfKeyCore( t );
                if ( index < 0 ) //not-exists
                {
                    index = ~index;
                    if ( index != block.Count )
                    {
                        var result = block.TryAdd( t );
                        Debug.Assert( result, "!result" );
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
            var block = _SortedBlockSet.FindNearestBlock( t );
            if ( block == null )
            {
                exists = default(T);
                return (false);
            }

            var success = block.TryGetValue( t, out exists );
            return (success);
        }
        public T GetValue( T t, T defaultValue = default(T) )
        {
            var block = _SortedBlockSet.FindNearestBlock( t );
            if ( block == null )
            {
                return (defaultValue);
            }

            var v = block.GetValue( t, defaultValue );
            return (v);
        }
        public bool Contains( T t )
        {
            var block = _SortedBlockSet.FindNearestBlock( t );
            if ( block == null )
            {
                return (false);
            }

            var index = block.IndexOfKeyCore( t );
            return (0 <= index);
        }
        public bool Remove( T t )
        {
            var block = _SortedBlockSet.FindNearestBlock( t );
            if ( block == null )
            {
                return (false);
            }

            if ( block.Count == 1 )
            {
                var index = block.IndexOfKey( t );
                if ( 0 <= index )
                {
                    var success_2 = _SortedBlockSet.Remove( block );
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
            var block = _SortedBlockSet.FindNearestBlock( t );
            if ( block == null ) //not-exists
            {
                return (false);
            }
            #endregion

            #region [.-2- getting block and search in him.]
            var index = block.IndexOfKeyCore( t );
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

        public int GetCount() => _SortedBlockSet.Sum( block => block.Count );
        public int Count => GetCount();

        public void Trim()
        {
            foreach ( var block in _SortedBlockSet )
            {
                block.Trim();
            }
        }

        public IEnumerator< T > GetEnumerator()
        {
            foreach ( var block in _SortedBlockSet )
            {
                foreach ( var t in block )
                {
                    yield return (t);
                }
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
#if DEBUG
        public override string ToString() => _SortedBlockSet.ToString();
#endif
    }
}
