using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    /// <summary>
    /// 
    /// </summary>
	internal sealed class Set< T >
        where T: struct
	{
        /// <summary>
        /// 
        /// </summary>
		internal struct Slot
		{
			internal int hashCode;
			internal T   value;
			internal int next;
		}

        private const int DEFAULT_CAPACITY = 7;

		private int[]                  _Buckets;
		private Slot[]                 _Slots;
		private int                    _Count;
		private int                    _FreeList;
		private IEqualityComparer< T > _Comparer;

        internal Slot[] Slots
        {
            get { return (_Slots); }
        }
        public int Count
        {
            get { return (_Count); }
        }

		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		public Set() : this( DEFAULT_CAPACITY, null )
		{
		}
		public Set( IEqualityComparer< T > comparer ) : this( DEFAULT_CAPACITY, comparer )
		{
		}
		public Set( int capacity ) : this( capacity, null )
		{
		}
        public Set( int capacity, IEqualityComparer< T > comparer )
        {
            _Comparer = comparer ?? EqualityComparer< T >.Default;
			_Buckets  = new int[ capacity ];
			_Slots    = new Slot[ capacity ];
			_FreeList = -1;
        }

        public bool Add( T value )
		{
            #region [.exists.]
            int hash = (_Comparer.GetHashCode( value ) & 0x7FFFFFFF); //--- InternalGetHashCode( ref value );
            for ( int i = _Buckets[ hash % _Buckets.Length ] - 1; 0 <= i; /*i = _Slots[ i ].next*/ )
            {
                var slot = _Slots[ i ];
                if ( (slot.hashCode == hash) && _Comparer.Equals( slot.value, value ) )
                {
                    return (false);
                }
                i = slot.next;
            }
            #endregion

            #region [.add.]
            int index;
            if ( 0 <= _FreeList )
            {
                index = _FreeList;
                _FreeList = _Slots[ index ].next;
            }
            else
            {
                if ( _Count == _Slots.Length )
                {
                    Resize();
                }
                index = _Count;
                _Count++;
            }
            int bucket = hash % _Buckets.Length;
            _Slots[ index ] = new Slot() 
            {
                hashCode = hash,
                value    = value,
                next     = _Buckets[ bucket ] - 1,
            };
            _Buckets[ bucket ] = index + 1;

            return (true);
            #endregion
        }

        public void Clear()
        {
            Array.Clear( _Buckets, 0, _Buckets.Length );
            Array.Clear( _Slots  , 0, _Slots  .Length );
            _Count    = 0;
            _FreeList = -1;
        }

		private void Resize()
		{
            int newSize = checked( _Count * 2 + 1 );
            int[]  newBuckets = new int[ newSize ];
            Slot[] newSlots   = new Slot[ newSize ];
            Array.Copy( _Slots, 0, newSlots, 0, _Count );
            for ( int i = 0; i < _Count; i++ )
            {
                int bucket = newSlots[ i ].hashCode % newSize;
                newSlots[ i ].next = newBuckets[ bucket ] - 1;
                newBuckets[ bucket ] = i + 1;
            }
            _Buckets = newBuckets;
            _Slots   = newSlots;
		}

        //[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        //public bool Contains( T value )
        //{
        //    return (Find( value, false ));
        //}
        //public bool Remove( T value )
        //{
        //  int hash = InternalGetHashCode( value );
        //	int bucket = hash % _Buckets.Length;
        //	int last = -1;
        //  for ( int i = _Buckets[ bucket ] - 1; 0 <= i; /*i = _Slots[ i ].next*/ )
        //	{
        //      var slot = _Slots[ i ];
        //      if ( (slot.hashCode == hash) && _Comparer.Equals( slot.value, value ) )
        //		{
        //          if ( last < 0 )
        //			{
        //                _Buckets[ bucket ] = _Slots[ i ].next + 1;
        //			}
        //			else
        //			{
        //                _Slots[ last ].next = _Slots[ i ].next;
        //			}
        //          _Slots[ i ] = new Slot()
        //          {
        //              hashCode = -1,
        //              value    = default(T),
        //              next     = _FreeList,
        //          };
        //			_FreeList = i;
        //			return (true);
        //		}
        //		last = i;
        //      i = slot.next;
        //	}
        //	return (false);
        //}
        //private bool Find( T value, bool add )
        //{
        //    int hash = InternalGetHashCode( value );
        //    for ( int i = _Buckets[ hash % _Buckets.Length ] - 1; 0 <= i; /*i = _Slots[ i ].next*/ )
        //    {
        //        var slot = _Slots[ i ];
        //        if ( (slot.hashCode == hash) && _Comparer.Equals( slot.value, value ) )
        //        {
        //            return (true);
        //        }
        //        i = slot.next;
        //    }

        //    if ( add )
        //    {
        //        int index;
        //        if ( 0 <= _FreeList )
        //        {
        //            index = _FreeList;
        //            _FreeList = _Slots[ index ].next;
        //        }
        //        else
        //        {
        //            if ( _Count == _Slots.Length )
        //            {
        //                Resize();
        //            }
        //            index = _Count;
        //            _Count++;
        //        }
        //        int bucket = hash % _Buckets.Length;
        //        _Slots[ index ] = new Slot() 
        //        {
        //            hashCode = hash,
        //            value    = value,
        //            next     = _Buckets[ bucket ] - 1,
        //        };
        //        _Buckets[ bucket ] = index + 1;
        //    }

        //	return (false);
        //}
        //private int InternalGetHashCode( ref T value )
        //{
        //    return (_Comparer.GetHashCode( value ) & 0x7FFFFFFF);
        //
        //  /*
        //  if ( value != null )
        //	{
        //		return (_Comparer.GetHashCode( value ) & 0x7FFFFFFF);
        //	}
        //	return (0);
        //  */
        //}
	}
}
