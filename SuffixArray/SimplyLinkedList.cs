using System;
using System.Runtime;

namespace System.Collections.Generic
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
	internal sealed class SimplyLinkedListNode< T >
	{
		internal SimplyLinkedListNode< T > _Next;
		internal T _Item;

		/// <summary>Gets the next node in the <see cref="T:System.Collections.Generic.LinkedList`1" />.</summary>
		/// <returns>A reference to the next node in the <see cref="T:System.Collections.Generic.LinkedList`1" />, or null if the current node is the last element (<see cref="P:System.Collections.Generic.LinkedList`1.Last" />) of the <see cref="T:System.Collections.Generic.LinkedList`1" />.</returns>
		public SimplyLinkedListNode< T > Next
		{
			get { return _Next; }
		}
		/// <summary>Gets the value contained in the node.</summary>
		/// <returns>The value contained in the node.</returns>
		public T Value
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get
			{
				return _Item;
			}
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			set
			{
				_Item = value;
			}
		}
		/// <summary>Initializes a new instance of the <see cref="T:System.Collections.Generic.linked_list_node`1" /> class, containing the specified value.</summary>
		/// <param name="value">The value to contain in the <see cref="T:System.Collections.Generic.linked_list_node`1" />.</param>
		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		public SimplyLinkedListNode( T value )
		{
			_Item = value;
		}

		internal void Invalidate()
		{
			_Next = null;
		}
	}

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    internal sealed class SimplyLinkedList< T > : ICollection< T >, IEnumerable< T >, ICollection, IEnumerable
	{
        /// <summary>
        /// Enumerates the elements of a <see cref="T:System.Collections.Generic.linked_list`1" />.
        /// </summary>
		public struct Enumerator : IEnumerator< T >
		{
			private SimplyLinkedListNode< T > _Node;
			private T _Current;

			/// <summary>Gets the element at the current position of the enumerator.</summary>
			/// <returns>The element in the <see cref="T:System.Collections.Generic.LinkedList`1" /> at the current position of the enumerator.</returns>			
			public T Current
			{
				[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
				get { return _Current; }
			}
			/// <summary>Gets the element at the current position of the enumerator.</summary>
			/// <returns>The element in the collection at the current position of the enumerator.</returns>
			/// <exception cref="T:System.InvalidOperationException">The enumerator is positioned before the first element of the collection or after the last element. </exception>
			object IEnumerator.Current
			{
				get { return _Current; }
			}
			internal Enumerator( SimplyLinkedList< T > list )
			{
				_Node    = list._Head;
				_Current = default(T);
			}

			/// <summary>Advances the enumerator to the next element of the <see cref="T:System.Collections.Generic.LinkedList`1" />.</summary>
			/// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
			/// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
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
			/// <summary>Sets the enumerator to its initial position, which is before the first element in the collection. This class cannot be inherited.</summary>
			/// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
			void IEnumerator.Reset()
			{
                throw (new NotSupportedException());

				//_Current = default(T);
				//_Node    = _List._Head;
			}
			/// <summary>Releases all resources used by the <see cref="T:System.Collections.Generic.LinkedList`1.Enumerator" />.</summary>
			public void Dispose()
			{
			}
		}

		internal SimplyLinkedListNode< T > _Head;
		internal int _Count;

		/// <summary>Gets the number of nodes actually contained in the <see cref="T:System.Collections.Generic.LinkedList`1" />.</summary>
		/// <returns>The number of nodes actually contained in the <see cref="T:System.Collections.Generic.LinkedList`1" />.</returns>
		public int Count
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get { return _Count; }
		}
		/// <summary>Gets the first node of the <see cref="T:System.Collections.Generic.LinkedList`1" />.</summary>
		/// <returns>The first <see cref="T:System.Collections.Generic.linked_list_node`1" /> of the <see cref="T:System.Collections.Generic.LinkedList`1" />.</returns>
		public SimplyLinkedListNode< T > Head
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get { return _Head; }
		}
		bool ICollection< T >.IsReadOnly
		{
			get { return false; }
		}
		/// <summary>Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection" /> is synchronized (thread safe).</summary>
		/// <returns>true if access to the <see cref="T:System.Collections.ICollection" /> is synchronized (thread safe); otherwise, false.  In the default implementation of <see cref="T:System.Collections.Generic.LinkedList`1" />, this property always returns false.</returns>
		bool ICollection.IsSynchronized
		{
            get { return false; }
		}
		/// <summary>Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.</summary>
		/// <returns>An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.  In the default implementation of <see cref="T:System.Collections.Generic.LinkedList`1" />, this property always returns the current instance.</returns>
		object ICollection.SyncRoot
		{			
			get { throw (new NotImplementedException()); }
		}

		/// <summary>Adds a new node containing the specified value at the end of the <see cref="T:System.Collections.Generic.LinkedList`1" />.</summary>
		/// <returns>The new <see cref="T:System.Collections.Generic.linked_list_node`1" /> containing <paramref name="value" />.</returns>
		/// <param name="value">The value to add at the end of the <see cref="T:System.Collections.Generic.LinkedList`1" />.</param>
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
		/// <summary>Removes all nodes from the <see cref="T:System.Collections.Generic.LinkedList`1" />.</summary>
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
		/// <summary>Determines whether a value is in the <see cref="T:System.Collections.Generic.LinkedList`1" />.</summary>
		/// <returns>true if <paramref name="value" /> is found in the <see cref="T:System.Collections.Generic.LinkedList`1" />; otherwise, false.</returns>
		/// <param name="value">The value to locate in the <see cref="T:System.Collections.Generic.LinkedList`1" />. The value can be null for reference types.</param>
		public bool Contains( T value )
		{
            return (Find( value ) != null);
		}
		/// <summary>Copies the entire <see cref="T:System.Collections.Generic.LinkedList`1" /> to a compatible one-dimensional <see cref="T:System.Array" />, starting at the specified index of the target array.</summary>
		/// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.LinkedList`1" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
		/// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins.</param>
		/// <exception cref="T:System.ArgumentNullException">
		///   <paramref name="array" /> is null.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">
		///   <paramref name="index" /> is less than zero.</exception>
		/// <exception cref="T:System.ArgumentException">The number of elements in the source <see cref="T:System.Collections.Generic.LinkedList`1" /> is greater than the available space from <paramref name="index" /> to the end of the destination <paramref name="array" />.</exception>
		public void CopyTo( T[] array, int index )
		{
            if ( array == null )
            {
                throw new ArgumentNullException( nameof(array) );
            }
            if ( index < 0 || index > array.Length )
            {
                throw new ArgumentOutOfRangeException( nameof(index), "IndexOutOfRange" );
            }
            if ( array.Length - index < Count )
            {
                throw new ArgumentException( "Arg_InsufficientSpace" );
            }
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
		/// <summary>Finds the first node that contains the specified value.</summary>
		/// <returns>The first <see cref="T:System.Collections.Generic.linked_list_node`1" /> that contains the specified value, if found; otherwise, null.</returns>
		/// <param name="value">The value to locate in the <see cref="T:System.Collections.Generic.LinkedList`1" />.</param>
		public SimplyLinkedListNode< T > Find( T value )
		{
			var next = _Head;
            if ( next != null )
            {
                var @default = EqualityComparer< T >.Default;
                if ( value != null )
                {
                    while ( !@default.Equals( next._Item, value ) )
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
		/// <summary>Returns an enumerator that iterates through the <see cref="T:System.Collections.Generic.LinkedList`1" />.</summary>
		/// <returns>An <see cref="T:System.Collections.Generic.LinkedList`1.Enumerator" /> for the <see cref="T:System.Collections.Generic.LinkedList`1" />.</returns>
		public SimplyLinkedList< T >.Enumerator GetEnumerator()
		{
			return (new Enumerator( this ));
		}
		
		IEnumerator< T > IEnumerable< T >.GetEnumerator()
		{
            return (new Enumerator( this ));
		}
		/// <summary>Removes the first occurrence of the specified value from the <see cref="T:System.Collections.Generic.LinkedList`1" />.</summary>
		/// <returns>true if the element containing <paramref name="value" /> is successfully removed; otherwise, false.  This method also returns false if <paramref name="value" /> was not found in the original <see cref="T:System.Collections.Generic.LinkedList`1" />.</returns>
		/// <param name="value">The value to remove from the <see cref="T:System.Collections.Generic.LinkedList`1" />.</param>
		public bool Remove( T value )
		{
            throw (new NotImplementedException());
		}
		/// <summary>Removes the specified node from the <see cref="T:System.Collections.Generic.LinkedList`1" />.</summary>
		/// <param name="node">The <see cref="T:System.Collections.Generic.linked_list_node`1" /> to remove from the <see cref="T:System.Collections.Generic.LinkedList`1" />.</param>
		/// <exception cref="T:System.ArgumentNullException">
		///   <paramref name="node" /> is null.</exception>
		/// <exception cref="T:System.InvalidOperationException">
		///   <paramref name="node" /> is not in the current <see cref="T:System.Collections.Generic.LinkedList`1" />.</exception>
		public void Remove( SimplyLinkedListNode< T > node )
		{
            throw (new NotImplementedException());
		}

		/// <summary>Copies the elements of the <see cref="T:System.Collections.ICollection" /> to an <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.</summary>
		/// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.ICollection" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
		/// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins.</param>
		/// <exception cref="T:System.ArgumentNullException">
		///   <paramref name="array" /> is null.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">
		///   <paramref name="index" /> is less than zero.</exception>
		/// <exception cref="T:System.ArgumentException">
		///   <paramref name="array" /> is multidimensional.-or-<paramref name="array" /> does not have zero-based indexing.-or-The number of elements in the source <see cref="T:System.Collections.ICollection" /> is greater than the available space from <paramref name="index" /> to the end of the destination <paramref name="array" />.-or-The type of the source <see cref="T:System.Collections.ICollection" /> cannot be cast automatically to the type of the destination <paramref name="array" />.</exception>
		void ICollection.CopyTo( Array array, int index )
		{
            if ( array == null )
            {
                throw new ArgumentNullException( nameof(array) );
            }
            if ( array.Rank != 1 )
            {
                throw (new ArgumentException( "Arg_MultiRank" ));
            }
            if ( array.GetLowerBound( 0 ) != 0 )
            {
                throw (new ArgumentException( "Arg_NonZeroLowerBound" ));
            }
            if ( index < 0 )
            {
                throw (new ArgumentOutOfRangeException( nameof(index), "IndexOutOfRange" ));
            }
            if ( array.Length - index < _Count )
            {
                throw (new ArgumentException( "Arg_InsufficientSpace" ));
            }
            T[] array2 = array as T[];
			if (array2 != null)
			{
				CopyTo(array2, index);
				return;
			}
			Type elementType = array.GetType().GetElementType();
			Type typeFromHandle = typeof(T);
            if ( !elementType.IsAssignableFrom( typeFromHandle ) && !typeFromHandle.IsAssignableFrom( elementType ) )
            {
                throw (new ArgumentException( "Invalid_Array_Type" ));
            }
			var array3 = array as object[];
            if ( array3 == null )
            {
                throw (new ArgumentException( "Invalid_Array_Type" ));
            }
			SimplyLinkedListNode< T > next = _Head;
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
		/// <summary>Returns an enumerator that iterates through the linked list as a collection.</summary>
		/// <returns>An <see cref="T:System.Collections.IEnumerator" /> that can be used to iterate through the linked list as a collection.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
            return (new Enumerator( this ));
		}
	}
}
