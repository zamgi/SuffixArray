namespace System.Collections.Generic
{
    /// <summary>
    /// 
    /// </summary>
	internal sealed class SimplyLinkedListNode< T >
	{
		internal SimplyLinkedListNode< T > _Next;
		internal T _Item;

		public SimplyLinkedListNode( T value ) => _Item = value;

		public SimplyLinkedListNode< T > Next => _Next;
        public T Value
		{
			get => _Item;
			set => _Item = value;
        }
		internal void Invalidate() => _Next = null;
	}

    /// <summary>
    /// 
    /// </summary>
    internal sealed class SimplyLinkedList< T > : ICollection< T >, IEnumerable< T >, ICollection, IEnumerable
	{
        /// <summary>
        ///
        /// </summary>
		public struct Enumerator : IEnumerator< T >
		{
			private SimplyLinkedListNode< T > _Node;
			private T _Current;

			public T Current => _Current;
            object IEnumerator.Current => _Current;
            internal Enumerator( SimplyLinkedList< T > list )
			{
				_Node    = list._Head;
				_Current = default(T);
			}
            public void Dispose() { }

            public bool MoveNext()
			{
                if ( _Node == null )
                {
                    return (false);
                }
				_Current = _Node._Item;
				_Node    = _Node._Next;
				return (true);
			}
			void IEnumerator.Reset()
			{
                throw (new NotSupportedException());

				//_Current = default(T);
				//_Node    = _List._Head;
			}			
        }

		internal SimplyLinkedListNode< T > _Head;
		internal int _Count;

		public int Count => _Count;
        public SimplyLinkedListNode< T > Head => _Head;
		bool ICollection< T >.IsReadOnly => false;
		bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => throw (new NotImplementedException());

        public void Add( T value )
		{
            var node = new SimplyLinkedListNode< T >( value );
            if ( _Head == null )
            {
                _Head = node;
            }
            else
            {
                var tmp = _Head;
                _Head = node;
                node._Next = tmp;
            }
            _Count++;
		}
		public void Clear()
		{
            var next = _Head;
            while ( next != null )
            {
                var linkedListNode = next;
                next = next.Next;
                linkedListNode.Invalidate();
            }
            _Head = null;
            _Count = 0;
		}
		public bool Contains( T value ) => (Find( value ) != null);
		public void CopyTo( T[] array, int index )
		{
            if ( array == null )                     throw (new ArgumentNullException( nameof(array) ));
            if ( index < 0 || index > array.Length ) throw (new ArgumentOutOfRangeException( nameof(index), "IndexOutOfRange" ));
            if ( array.Length - index < Count )      throw (new ArgumentException( "Arg_InsufficientSpace" ));
			
            var next = _Head;
            if ( next != null )
			{
				do
				{
                    array[ index++ ] = next._Item;
                    next = next._Next;
				}
                while ( next != _Head );
			}
		}
		public SimplyLinkedListNode< T > Find( T value )
		{
			var next = _Head;
            if ( next != null )
            {
                var comp = EqualityComparer< T >.Default;
                if ( value != null )
                {
                    while ( !comp.Equals( next._Item, value ) )
                    {
                        next = next._Next;
                        if ( next == _Head )
                        {
                            return (null); //---goto EXIT;
                        }
                    }
                    return (next);
                }
                while ( next._Item != null )
                {
                    next = next._Next;
                    if ( next == _Head )
                    {
                        return (null); //---goto EXIT;
                    }
                }
                return (next);
            }
        //---EXIT:
			return (null);
		}
        public bool Remove( T value ) => throw (new NotImplementedException());
		public void Remove( SimplyLinkedListNode< T > node ) => throw (new NotImplementedException());

        void ICollection.CopyTo( Array array, int index )
		{
            if ( array == null )                 throw (new ArgumentNullException( nameof(array) ));
            if ( array.Rank != 1 )               throw (new ArgumentException( "Arg_MultiRank" ));
            if ( array.GetLowerBound( 0 ) != 0 ) throw (new ArgumentException( "Arg_NonZeroLowerBound" ));
            if ( index < 0 )                     throw (new ArgumentOutOfRangeException( nameof(index), "IndexOutOfRange" ));
            if ( array.Length - index < _Count ) throw (new ArgumentException( "Arg_InsufficientSpace" ));
            
			if ( array is T[] array2)
			{
				CopyTo(array2, index);
				return;
			}

			Type elementType    = array.GetType().GetElementType();
			Type typeFromHandle = typeof(T);
            if ( !elementType.IsAssignableFrom( typeFromHandle ) && !typeFromHandle.IsAssignableFrom( elementType ) ) throw (new ArgumentException( "Invalid_Array_Type" ));
            if ( !(array is object[] array3) ) throw (new ArgumentException( "Invalid_Array_Type" ));
			
            var next = _Head;
			try
			{
                if ( next != null )
                {
					do
					{
                        array3[ index++ ] = next._Item;
                        next = next._Next;
					}
                    while ( next != _Head );
                }
			}
			catch (ArrayTypeMismatchException)
			{
                throw (new ArgumentException( "Invalid_Array_Type" ));
            }
		}
		
		public Enumerator GetEnumerator() => new Enumerator( this );
		IEnumerator< T > IEnumerable< T >.GetEnumerator() => new Enumerator( this );
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator( this );
	}
}
