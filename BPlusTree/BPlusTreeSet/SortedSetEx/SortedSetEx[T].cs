﻿using System.Diagnostics;

namespace System.Collections.Generic
{
    //
    // A binary search tree is a red-black tree if it satisfies the following red-black properties:
    // 1. Every node is either red or black
    // 2. Every leaf (nil node) is black
    // 3. If a node is red, the both its children are black
    // 4. Every simple path from a node to a descendant leaf contains the same number of black nodes
    // 
    // The basic idea of red-black tree is to represent 2-3-4 trees as standard BSTs but to add one extra bit of information  
    // per node to encode 3-nodes and 4-nodes. 
    // 4-nodes will be represented as:          B
    //                                                              R            R
    // 3 -node will be represented as:           B             or         B     
    //                                                              R          B               B       R
    // 
    // For a detailed description of the algorithm, take a look at "Algorithm" by Rebert Sedgewick.
    //

    [DebuggerTypeProxy( typeof(ICollectionDebugView<>) )]
    [DebuggerDisplay("Count = {Count}")]
    internal class SortedSetEx< T > : ISet< T >, ICollection< T >, ICollection/*, IReadOnlyCollection< T >*/
    {
        #region [.local variables/constants.]
        internal const int STACK_ALLOC_THRESHOLD = 100;

        protected Node _Root;
        private IComparer< T > _Comparer;
        private int _Count;
        private object _SyncRoot;
        #endregion

        #region [.ctor.]
        public SortedSetEx() => _Comparer = Comparer< T >.Default;
        public SortedSetEx( IComparer< T > comparer ) => _Comparer = comparer ?? Comparer< T >.Default; 
        public SortedSetEx( IEnumerable< T > collection ) : this( collection, Comparer< T >.Default )  { }
        public SortedSetEx( IEnumerable< T > collection, IComparer< T > comparer ) : this( comparer )
        {
            if ( collection == null ) throw (new ArgumentNullException( nameof(collection) ));

            // these are explicit type checks in the mould of HashSet. It would have worked better
            // with something like an ISorted< T > (we could make this work for SortedList.Keys etc)
            SortedSetEx< T > baseSortedSet = collection as SortedSetEx< T >;
            SortedSetEx< T > baseTreeSubSet = collection as TreeSubSet;
            if ( baseSortedSet != null && baseTreeSubSet == null && AreComparersEqual( this, baseSortedSet ) )
            {
                //breadth first traversal to recreate nodes
                if ( baseSortedSet.Count == 0 )
                {
                    return;
                }

                //pre order way to replicate nodes
                var capacity   = 2 * log2( baseSortedSet.Count ) + 2;
                var theirStack = new Stack< Node >( capacity );
                var myStack    = new Stack< Node >( capacity );
                Node theirCurrent = baseSortedSet._Root;
                Node myCurrent = (theirCurrent != null ? new Node( theirCurrent.Item, theirCurrent.IsRed ) : null);
                _Root = myCurrent;
                while ( theirCurrent != null )
                {
                    theirStack.Push( theirCurrent );
                    myStack.Push( myCurrent );
                    myCurrent.Left = (theirCurrent.Left != null ? new Node( theirCurrent.Left.Item, theirCurrent.Left.IsRed ) : null);
                    theirCurrent = theirCurrent.Left;
                    myCurrent = myCurrent.Left;
                }
                while ( theirStack.Count != 0 )
                {
                    theirCurrent = theirStack.Pop();
                    myCurrent = myStack.Pop();
                    Node theirRight = theirCurrent.Right;
                    Node myRight = null;
                    if ( theirRight != null )
                    {
                        myRight = new Node( theirRight.Item, theirRight.IsRed );
                    }
                    myCurrent.Right = myRight;

                    while ( theirRight != null )
                    {
                        theirStack.Push( theirRight );
                        myStack.Push( myRight );
                        myRight.Left = (theirRight.Left != null ? new Node( theirRight.Left.Item, theirRight.Left.IsRed ) : null);
                        theirRight = theirRight.Left;
                        myRight = myRight.Left;
                    }
                }
                _Count = baseSortedSet._Count;
            }
            else
            {
                int count;
                T[] els = EnumerableHelpers.ToArray( collection, out count );
                if ( count > 0 )
                {
                    comparer = _Comparer; // If comparer is null, sets it to Comparer< T >.Default
                    Array.Sort( els, 0, count, comparer );
                    int index = 1;
                    for ( int i = 1; i < count; i++ )
                    {
                        if ( comparer.Compare( els[ i ], els[ i - 1 ] ) != 0 )
                        {
                            els[ index++ ] = els[ i ];
                        }
                    }
                    count = index;

                    _Root = ConstructRootFromSortedArray( els, 0, count - 1, null );
                    _Count = count;
                }
            }
        }
        #endregion

        #region [.Bulk Operation Helpers.]
        private void AddAllElements( IEnumerable< T > collection )
        {
            foreach ( T item in collection )
            {
                if ( !Contains( item ) )
                {
                    Add( item );
                }
            }
        }

        private void RemoveAllElements( IEnumerable< T > collection )
        {
            T min = Min;
            T max = Max;
            foreach ( T item in collection )
            {
                if ( !(_Comparer.Compare( item, min ) < 0 || _Comparer.Compare( item, max ) > 0) && Contains( item ) )
                {
                    Remove( item );
                }
            }
        }

        private bool ContainsAllElements( IEnumerable< T > collection )
        {
            foreach ( T item in collection )
            {
                if ( !Contains( item ) )
                {
                    return (false);
                }
            }
            return (true);
        }

        // Do a in order walk on tree and calls the delegate for each node.
        // If the action delegate returns false, stop the walk.
        // 
        // Return true if the entire tree has been walked. 
        // Otherwise returns false.
        private bool InOrderTreeWalk( TreeWalkPredicate< T > action )
        {
            return (InOrderTreeWalk( action, false ));
        }

        // Allows for the change in traversal direction. Reverse visits nodes in descending order 
        internal virtual bool InOrderTreeWalk( TreeWalkPredicate< T > action, bool reverse )
        {
            if ( _Root == null )
            {
                return (true);
            }

            // The maximum height of a red-black tree is 2*lg(n+1).
            // See page 264 of "Introduction to algorithms" by Thomas H. Cormen
            // note: this should be logbase2, but since the stack grows itself, we 
            // don't want the extra cost
            var stack = new Stack< Node >( 2 * (int) (log2( Count + 1 )) );
            Node current = _Root;
            while ( current != null )
            {
                stack.Push( current );
                current = (reverse ? current.Right : current.Left);
            }
            while ( stack.Count != 0 )
            {
                current = stack.Pop();
                if ( !action( current ) )
                {
                    return (false);
                }

                Node node = (reverse ? current.Left : current.Right);
                while ( node != null )
                {
                    stack.Push( node );
                    node = (reverse ? node.Right : node.Left);
                }
            }
            return (true);
        }

        // Do a left to right breadth first walk on tree and 
        // calls the delegate for each node.
        // If the action delegate returns false, stop the walk.
        // 
        // Return true if the entire tree has been walked. 
        // Otherwise returns false.
        internal virtual bool BreadthFirstTreeWalk( TreeWalkPredicate< T > action )
        {
            if ( _Root == null )
            {
                return (true);
            }

            var processQueue = new Queue< Node >();
            processQueue.Enqueue( _Root );
            Node current;

            while ( processQueue.Count != 0 )
            {
                current = processQueue.Dequeue();
                if ( !action( current ) )
                {
                    return (false);
                }
                if ( current.Left != null )
                {
                    processQueue.Enqueue( current.Left );
                }
                if ( current.Right != null )
                {
                    processQueue.Enqueue( current.Right );
                }
            }
            return (true);
        }
        #endregion

        #region [.Properties.]
        public int Count => _Count;
        public IComparer< T > Comparer => _Comparer;
        bool ICollection< T >.IsReadOnly => false;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot
        {
            get
            {
                if ( _SyncRoot == null )
                {
                    Threading.Interlocked.CompareExchange( ref _SyncRoot, new object(), null );
                }
                return (_SyncRoot);
            }
        }
        #endregion

        #region [.Subclass helpers.]
        //virtual function for subclass that needs to do range checks
        internal virtual bool IsWithinRange( T item ) { return (true); }
        #endregion

        #region [.ICollection< T > Members.]
        /// <summary>
        /// Add the value ITEM to the tree, returns true if added, false if duplicate 
        /// </summary>
        /// <param name="item">item to be added</param> 
        public bool Add( T item )
        {
            return (AddIfNotPresent( item ));
        }
        void ICollection< T >.Add( T item )
        {
            AddIfNotPresent( item );
        }

        /// <summary>
        /// Adds ITEM to the tree if not already present. Returns TRUE if value was successfully added         
        /// or FALSE if it is a duplicate
        /// </summary>        
        internal virtual bool AddIfNotPresent( T item )
        {
            if ( _Root == null )
            {   // empty tree
                _Root = new Node( item, false );
                _Count = 1;
                /*_version++;*/
                return (true);
            }

            // Search for a node at bottom to insert the new node. 
            // If we can guarantee the node we found is not a 4-node, it would be easy to do insertion.
            // We split 4-nodes along the search path.
            Node current = _Root;
            Node parent = null;
            Node grandParent = null;
            Node greatGrandParent = null;

            //even if we don't actually add to the set, we may be altering its structure (by doing rotations
            //and such). so update version to disable any enumerators/subsets working on it
            /*_version++;*/

            int order = 0;
            while ( current != null )
            {
                order = _Comparer.Compare( item, current.Item );
                if ( order == 0 )
                {
                    // We could have changed root node to red during the search process.
                    // We need to set it to black before we return.
                    _Root.IsRed = false;
                    return (false);
                }

                // split a 4-node into two 2-nodes                
                if ( Is4Node( current ) )
                {
                    Split4Node( current );
                    // We could have introduced two consecutive red nodes after split. Fix that by rotation.
                    if ( IsRed( parent ) )
                    {
                        InsertionBalance( current, ref parent, grandParent, greatGrandParent );
                    }
                }
                greatGrandParent = grandParent;
                grandParent = parent;
                parent = current;
                current = (order < 0) ? current.Left : current.Right;
            }

            Debug.Assert( parent != null, "Parent node cannot be null here!" );
            // ready to insert the new node
            Node node = new Node( item );
            if ( order > 0 )
            {
                parent.Right = node;
            }
            else
            {
                parent.Left = node;
            }

            // the new node will be red, so we will need to adjust the colors if parent node is also red
            if ( parent.IsRed )
            {
                InsertionBalance( node, ref parent, grandParent, greatGrandParent );
            }

            // Root node is always black
            _Root.IsRed = false;
            ++_Count;
            return (true);
        }

        /// <summary>
        /// Remove the T ITEM from this SortedSet. Returns true if successfully removed.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove( T item )
        {
            return (DoRemove( item )); // hack so it can be made non-virtual
        }
        internal virtual bool DoRemove( T item )
        {
            if ( _Root == null )
            {
                return (false);
            }

            // Search for a node and then find its successor. 
            // Then copy the item from the successor to the matching node and delete the successor. 
            // If a node doesn't have a successor, we can replace it with its left child (if not empty.) 
            // or delete the matching node.
            // 
            // In top-down implementation, it is important to make sure the node to be deleted is not a 2-node.
            // Following code will make sure the node on the path is not a 2 Node. 

            //even if we don't actually remove from the set, we may be altering its structure (by doing rotations
            //and such). so update version to disable any enumerators/subsets working on it
            /*_version++;*/

            Node current       = _Root;
            Node parent        = null;
            Node grandParent   = null;
            Node match         = null;
            Node parentOfMatch = null;
            bool foundMatch    = false;
            while ( current != null )
            {
                if ( Is2Node( current ) )
                { // fix up 2-Node
                    if ( parent == null )
                    {   // current is root. Mark it as red
                        current.IsRed = true;
                    }
                    else
                    {
                        Node sibling = GetSibling( current, parent );
                        if ( sibling.IsRed )
                        {
                            // If parent is a 3-node, flip the orientation of the red link. 
                            // We can achieve this by a single rotation        
                            // This case is converted to one of other cased below.
                            Debug.Assert( !parent.IsRed, "parent must be a black node!" );
                            if ( parent.Right == sibling )
                            {
                                RotateLeft( parent );
                            }
                            else
                            {
                                RotateRight( parent );
                            }

                            parent.IsRed = true;
                            sibling.IsRed = false;    // parent's color
                            // sibling becomes child of grandParent or root after rotation. Update link from grandParent or root
                            ReplaceChildOfNodeOrRoot( grandParent, parent, sibling );
                            // sibling will become grandParent of current node 
                            grandParent = sibling;
                            if ( parent == match )
                            {
                                parentOfMatch = sibling;
                            }

                            // update sibling, this is necessary for following processing
                            sibling = (parent.Left == current) ? parent.Right : parent.Left;
                        }
                        Debug.Assert( sibling != null && sibling.IsRed == false, "sibling must not be null and it must be black!" );

                        if ( Is2Node( sibling ) )
                        {
                            Merge2Nodes( parent, current, sibling );
                        }
                        else
                        {
                            // current is a 2-node and sibling is either a 3-node or a 4-node.
                            // We can change the color of current to red by some rotation.
                            TreeRotation rotation = RotationNeeded( parent, current, sibling );
                            Node newGrandParent = null;
                            switch ( rotation )
                            {
                                case TreeRotation.RightRotation:
                                    Debug.Assert( parent.Left == sibling, "sibling must be left child of parent!" );
                                    Debug.Assert( sibling.Left.IsRed, "Left child of sibling must be red!" );
                                    sibling.Left.IsRed = false;
                                    newGrandParent = RotateRight( parent );
                                break;
                                case TreeRotation.LeftRotation:
                                    Debug.Assert( parent.Right == sibling, "sibling must be left child of parent!" );
                                    Debug.Assert( sibling.Right.IsRed, "Right child of sibling must be red!" );
                                    sibling.Right.IsRed = false;
                                    newGrandParent = RotateLeft( parent );
                                break;

                                case TreeRotation.RightLeftRotation:
                                    Debug.Assert( parent.Right == sibling, "sibling must be left child of parent!" );
                                    Debug.Assert( sibling.Left.IsRed, "Left child of sibling must be red!" );
                                    newGrandParent = RotateRightLeft( parent );
                                break;

                                case TreeRotation.LeftRightRotation:
                                    Debug.Assert( parent.Left == sibling, "sibling must be left child of parent!" );
                                    Debug.Assert( sibling.Right.IsRed, "Right child of sibling must be red!" );
                                    newGrandParent = RotateLeftRight( parent );
                                break;
                            }

                            newGrandParent.IsRed = parent.IsRed;
                            parent.IsRed = false;
                            current.IsRed = true;
                            ReplaceChildOfNodeOrRoot( grandParent, parent, newGrandParent );
                            if ( parent == match )
                            {
                                parentOfMatch = newGrandParent;
                            }
                            grandParent = newGrandParent;
                        }
                    }
                }

                // we don't need to compare any more once we found the match
                int order = foundMatch ? -1 : _Comparer.Compare( item, current.Item );
                if ( order == 0 )
                {
                    // save the matching node
                    foundMatch = true;
                    match = current;
                    parentOfMatch = parent;
                }

                grandParent = parent;
                parent = current;

                if ( order < 0 )
                {
                    current = current.Left;
                }
                else
                {
                    current = current.Right;       // continue the search in  right sub tree after we find a match
                }
            }

            // move successor to the matching node position and replace links
            if ( match != null )
            {
                ReplaceNode( match, parentOfMatch, parent, grandParent );
                --_Count;
            }

            if ( _Root != null )
            {
                _Root.IsRed = false;
            }
            return (foundMatch);
        }

        public virtual void Clear()
        {
            _Root = null;
            _Count = 0;
            /*_version++;*/
        }
        public virtual bool Contains( T item )
        {
            return (FindNode( item ) != null);
        }

        public void CopyTo( T[] array )
        {
            CopyTo( array, 0, Count );
        }
        public void CopyTo( T[] array, int index )
        {
            CopyTo( array, index, Count );
        }
        public void CopyTo( T[] array, int index, int count )
        {
            if ( array == null )
            {
                throw (new ArgumentNullException( nameof(array) ));
            }

            if ( index < 0 )
            {
                throw (new ArgumentOutOfRangeException( nameof(index), index, "SR.ArgumentOutOfRange_NeedNonNegNum" ));
            }

            if ( count < 0 )
            {
                throw (new ArgumentOutOfRangeException( nameof(count), "SR.ArgumentOutOfRange_NeedNonNegNum" ));
            }

            // will array, starting at arrayIndex, be able to hold elements? Note: not
            // checking arrayIndex >= array.Length (consistency with list of allowing
            // count of 0; subsequent check takes care of the rest)
            if ( index > array.Length || count > array.Length - index )
            {
                throw (new ArgumentException( "SR.Arg_ArrayPlusOffTooSmall" ));
            }
            //upper bound
            count += index;

            InOrderTreeWalk( delegate( Node node )
            {
                if ( index >= count )
                {
                    return (false);
                }
                else
                {
                    array[ index++ ] = node.Item;
                    return (true);
                }
            } );
        }
        void ICollection.CopyTo( Array array, int index )
        {
            if ( array == null )
            {
                throw (new ArgumentNullException( nameof(array) ));
            }

            if ( array.Rank != 1 )
            {
                throw (new ArgumentException( "SR.Arg_RankMultiDimNotSupported", nameof(array) ));
            }

            if ( array.GetLowerBound( 0 ) != 0 )
            {
                throw (new ArgumentException( "SR.Arg_NonZeroLowerBound", nameof(array) ));
            }

            if ( index < 0 )
            {
                throw (new ArgumentOutOfRangeException( nameof(index), index, "SR.ArgumentOutOfRange_NeedNonNegNum" ));
            }

            if ( array.Length - index < Count )
            {
                throw (new ArgumentException( "SR.Arg_ArrayPlusOffTooSmall" ));
            }

            var tarray = array as T[];
            if ( tarray != null )
            {
                CopyTo( tarray, index );
            }
            else
            {
                var objects = array as object[];
                if ( objects == null )
                {
                    throw (new ArgumentException( "SR.Argument_InvalidArrayType", nameof(array) ));
                }

                try
                {
                    InOrderTreeWalk( delegate( Node node )
                    { 
                        objects[ index++ ] = node.Item; 
                        return (true); 
                    } );
                }
                catch ( ArrayTypeMismatchException )
                {
                    throw (new ArgumentException( "SR.Argument_InvalidArrayType", nameof(array) ));
                }
            }
        }
        #endregion

        #region [.IEnumerable< T > members.]
        public Enumerator GetEnumerator()
        {
            return (new Enumerator( this ));
        }
        IEnumerator< T > IEnumerable< T >.GetEnumerator()
        {
            return (new Enumerator( this ));
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (new Enumerator( this ));
        }
        #endregion

        #region [.Tree Specific Operations.]
        private static Node GetSibling( Node node, Node parent )
        {
            if ( parent.Left == node )
            {
                return (parent.Right);
            }
            return (parent.Left);
        }

        // After calling InsertionBalance, we need to make sure current and parent up-to-date.
        // It doesn't matter if we keep grandParent and greatGrantParent up-to-date 
        // because we won't need to split again in the next node.
        // By the time we need to split again, everything will be correctly set.
        private void InsertionBalance( Node current, ref Node parent, Node grandParent, Node greatGrandParent )
        {
            Debug.Assert( grandParent != null, "Grand parent cannot be null here!" );
            bool parentIsOnRight = (grandParent.Right == parent);
            bool currentIsOnRight = (parent.Right == current);

            Node newChildOfGreatGrandParent;
            if ( parentIsOnRight == currentIsOnRight )
            { // same orientation, single rotation
                newChildOfGreatGrandParent = currentIsOnRight ? RotateLeft( grandParent ) : RotateRight( grandParent );
            }
            else
            {  // different orientation, double rotation
                newChildOfGreatGrandParent = currentIsOnRight ? RotateLeftRight( grandParent ) : RotateRightLeft( grandParent );
                // current node now becomes the child of greatgrandparent 
                parent = greatGrandParent;
            }
            // grand parent will become a child of either parent of current.
            grandParent.IsRed = true;
            newChildOfGreatGrandParent.IsRed = false;

            ReplaceChildOfNodeOrRoot( greatGrandParent, grandParent, newChildOfGreatGrandParent );
        }

        private static bool Is2Node( Node node )
        {
            Debug.Assert( node != null, "node cannot be null!" );
            return (IsBlack( node ) && IsNullOrBlack( node.Left ) && IsNullOrBlack( node.Right ));
        }
        private static bool Is4Node( Node node )
        {
            return (IsRed( node.Left ) && IsRed( node.Right ));
        }
        private static bool IsBlack( Node node )
        {
            return (node != null && !node.IsRed);
        }
        private static bool IsNullOrBlack( Node node )
        {
            return (node == null || !node.IsRed);
        }
        private static bool IsRed( Node node )
        {
            return (node != null && node.IsRed);
        }
        private static void Merge2Nodes( Node parent, Node child1, Node child2 )
        {
            Debug.Assert( IsRed( parent ), "parent must be red" );
            // combing two 2-nodes into a 4-node
            parent.IsRed = false;
            child1.IsRed = true;
            child2.IsRed = true;
        }

        // Replace the child of a parent node. 
        // If the parent node is null, replace the root.        
        private void ReplaceChildOfNodeOrRoot( Node parent, Node child, Node newChild )
        {
            if ( parent != null )
            {
                if ( parent.Left == child )
                {
                    parent.Left = newChild;
                }
                else
                {
                    parent.Right = newChild;
                }
            }
            else
            {
                _Root = newChild;
            }
        }

        // Replace the matching node with its successor.
        private void ReplaceNode( Node match, Node parentOfMatch, Node successor, Node parentOfsuccessor )
        {
            if ( successor == match )
            {  // this node has no successor, should only happen if right child of matching node is null.
                Debug.Assert( match.Right == null, "Right child must be null!" );
                successor = match.Left;
            }
            else
            {
                Debug.Assert( parentOfsuccessor != null, "parent of successor cannot be null!" );
                Debug.Assert( successor.Left == null, "Left child of successor must be null!" );
                Debug.Assert( (successor.Right == null && successor.IsRed) || (successor.Right.IsRed && !successor.IsRed), "Successor must be in valid state" );
                if ( successor.Right != null )
                {
                    successor.Right.IsRed = false;
                }

                if ( parentOfsuccessor != match )
                {   // detach successor from its parent and set its right child
                    parentOfsuccessor.Left = successor.Right;
                    successor.Right = match.Right;
                }

                successor.Left = match.Left;
            }

            if ( successor != null )
            {
                successor.IsRed = match.IsRed;
            }

            ReplaceChildOfNodeOrRoot( parentOfMatch, match, successor );
        }

        internal virtual Node FindNode( T item )
        {
            Node current = _Root;
            while ( current != null )
            {
                int order = _Comparer.Compare( item, current.Item );
                if ( order == 0 )
                {
                    return (current);
                }
                else
                {
                    current = (order < 0) ? current.Left : current.Right;
                }
            }
            return (null);
        }

        //used for bithelpers. Note that this implementation is completely different 
        //from the Subset's. The two should not be mixed. This indexes as if the tree were an array.
        //http://en.wikipedia.org/wiki/Binary_Tree#Methods_for_storing_binary_trees
        internal virtual int InternalIndexOf( T item )
        {
            int count = 0;
            Node current = _Root;
            while ( current != null )
            {
                int order = _Comparer.Compare( item, current.Item );
                if ( order == 0 )
                {
                    return (count);
                }
                else
                {
                    count = (2 * count + 1);
                    if ( order < 0 )
                    {
                        current = current.Left;                        
                    }
                    else
                    {
                        current = current.Right;
                        count++;
                    }
                }
            }
            return (-1);
        }

        internal Node FindRange( T from, T to )
        {
            return (FindRange( from, to, true, true ));
        }
        internal Node FindRange( T from, T to, bool lowerBoundActive, bool upperBoundActive )
        {
            Node current = _Root;
            while ( current != null )
            {
                if ( lowerBoundActive && _Comparer.Compare( from, current.Item ) > 0 )
                {
                    current = current.Right;
                }
                else
                {
                    if ( upperBoundActive && _Comparer.Compare( to, current.Item ) < 0 )
                    {
                        current = current.Left;
                    }
                    else
                    {
                        return (current);
                    }
                }
            }

            return (null);
        }

        /*internal void UpdateVersion()
        {
            _version++;
        }*/

        private static Node RotateLeft( Node node )
        {
            Node x = node.Right;
            node.Right = x.Left;
            x.Left = node;
            return (x);
        }
        private static Node RotateLeftRight( Node node )
        {
            Node child      = node.Left;
            Node grandChild = child.Right;

            node.Left = grandChild.Right;
            grandChild.Right = node;
            child.Right = grandChild.Left;
            grandChild.Left = child;
            return (grandChild);
        }
        private static Node RotateRight( Node node )
        {
            Node x = node.Left;
            node.Left = x.Right;
            x.Right = node;
            return (x);
        }
        private static Node RotateRightLeft( Node node )
        {
            Node child      = node.Right;
            Node grandChild = child.Left;

            node.Right = grandChild.Left;
            grandChild.Left = node;
            child.Left = grandChild.Right;
            grandChild.Right = child;
            return (grandChild);
        }

        /// <summary>
        /// Testing counter that can track rotations
        /// </summary>
        private static TreeRotation RotationNeeded( Node parent, Node current, Node sibling )
        {
            Debug.Assert( IsRed( sibling.Left ) || IsRed( sibling.Right ), "sibling must have at least one red child" );
            if ( IsRed( sibling.Left ) )
            {
                if ( parent.Left == current )
                {
                    return (TreeRotation.RightLeftRotation);
                }
                return (TreeRotation.RightRotation);
            }
            else
            {
                if ( parent.Left == current )
                {
                    return (TreeRotation.LeftRotation);
                }
                return (TreeRotation.LeftRightRotation);
            }
        }

        /// <summary>
        /// Used for deep equality of SortedSet testing
        /// </summary>
        /// <returns></returns>
        public static IEqualityComparer< SortedSetEx< T > > CreateSetComparer()
        {
            return (new SortedSetEqualityComparer< T >());
        }

        /// <summary>
        /// Create a new set comparer for this set, where this set's members' equality is defined by the
        /// memberEqualityComparer. Note that this equality comparer's definition of equality must be the
        /// same as this set's Comparer's definition of equality
        /// </summary>                
        public static IEqualityComparer< SortedSetEx< T > > CreateSetComparer( IEqualityComparer< T > memberEqualityComparer )
        {
            return (new SortedSetEqualityComparer< T >( memberEqualityComparer ));
        }

        /// <summary>
        /// Decides whether these sets are the same, given the comparer. If the EC's are the same, we can
        /// just use SetEquals, but if they aren't then we have to manually check with the given comparer
        /// </summary>        
        internal static bool SortedSetEquals( SortedSetEx< T > set1, SortedSetEx< T > set2, IComparer< T > comparer )
        {
            // handle null cases first
            if ( set1 == null )
            {
                return (set2 == null);
            }
            else if ( set2 == null )
            {
                // set1 != null
                return (false);
            }

            if ( AreComparersEqual( set1, set2 ) )
            {
                if ( set1.Count != set2.Count )
                    return (false);

                return (set1.SetEquals( set2 ));
            }
            else
            {
                bool found = false;
                foreach ( T item1 in set1 )
                {
                    found = false;
                    foreach ( T item2 in set2 )
                    {
                        if ( comparer.Compare( item1, item2 ) == 0 )
                        {
                            found = true;
                            break;
                        }
                    }
                    if ( !found )
                        return (false);
                }
                return (true);
            }

        }

        //This is a little frustrating because we can't support more sorted structures
        private static bool AreComparersEqual( SortedSetEx< T > set1, SortedSetEx< T > set2 )
        {
            return (set1.Comparer.Equals( set2.Comparer ));
        }

        private static void Split4Node( Node node )
        {
            node.IsRed       = true;
            node.Left.IsRed  = false;
            node.Right.IsRed = false;
        }
        #endregion

        #region [.ISet Members.]
        /// <summary>
        /// Transform this set into its union with the IEnumerable OTHER            
        ///Attempts to insert each element and rejects it if it exists.          
        /// NOTE: The caller object is important as UnionWith uses the Comparator 
        ///associated with THIS to check equality                                
        /// Throws ArgumentNullException if OTHER is null                         
        /// </summary>
        public void UnionWith( IEnumerable< T > other )
        {
            if ( other == null )
            {
                throw (new ArgumentNullException( nameof(other) ));
            }

            var s = other as SortedSetEx< T >;
            var t = this as TreeSubSet;

            if ( s != null && t == null && _Count == 0 )
            {
                var dummy = new SortedSetEx< T >( s, _Comparer );
                _Root  = dummy._Root;
                _Count = dummy._Count;
                /*_version++;*/
                return;
            }

            if ( s != null && t == null && AreComparersEqual( this, s ) && (s.Count > (this.Count >> 1)) )
            { //this actually hurts if N is much greater than M the /2 is arbitrary
                //first do a merge sort to an array.
                var merged = new T[ s.Count + this.Count ];
                int c = 0;
                Enumerator mine   = this.GetEnumerator();
                Enumerator theirs = s.GetEnumerator();
                bool mineEnded   = !mine.MoveNext(), 
                     theirsEnded = !theirs.MoveNext();
                while ( !mineEnded && !theirsEnded )
                {
                    int comp = Comparer.Compare( mine.Current, theirs.Current );
                    if ( comp < 0 )
                    {
                        merged[ c++ ] = mine.Current;
                        mineEnded = !mine.MoveNext();
                    }
                    else if ( comp == 0 )
                    {
                        merged[ c++ ] = theirs.Current;
                        mineEnded = !mine.MoveNext();
                        theirsEnded = !theirs.MoveNext();
                    }
                    else
                    {
                        merged[ c++ ] = theirs.Current;
                        theirsEnded = !theirs.MoveNext();
                    }
                }

                if ( !mineEnded || !theirsEnded )
                {
                    Enumerator remaining = (mineEnded ? theirs : mine);
                    do
                    {
                        merged[ c++ ] = remaining.Current;
                    } 
                    while ( remaining.MoveNext() );
                }

                //now merged has all c elements

                //safe to gc the root, we have all the elements
                _Root = null;

                _Root  = ConstructRootFromSortedArray( merged, 0, c - 1, null );
                _Count = c;
                /*_version++;*/
            }
            else
            {
                AddAllElements( other );
            }
        }

        private static Node ConstructRootFromSortedArray( T[] arr, int startIndex, int endIndex, Node redNode )
        {
            //what does this do?
            //you're given a sorted array... say 1 2 3 4 5 6 
            //2 cases:
            //    If there are odd # of elements, pick the middle element (in this case 4), and compute
            //    its left and right branches
            //    If there are even # of elements, pick the left middle element, save the right middle element
            //    and call the function on the rest
            //    1 2 3 4 5 6 -> pick 3, save 4 and call the fn on 1,2 and 5,6
            //    now add 4 as a red node to the lowest element on the right branch
            //             3                       3
            //         1       5       ->     1        5
            //           2       6             2     4   6            
            //    As we're adding to the leftmost of the right branch, nesting will not hurt the red-black properties
            //    Leaf nodes are red if they have no sibling (if there are 2 nodes or if a node trickles
            //    down to the bottom

            //the iterative way to do this ends up wasting more space than it saves in stack frames (at
            //least in what i tried)
            //so we're doing this recursively
            //base cases are described below
            int size = endIndex - startIndex + 1;
            if ( size == 0 )
            {
                return (null);
            }
            Node root = null;
            if ( size == 1 )
            {
                root = new Node( arr[ startIndex ], false );
                if ( redNode != null )
                {
                    root.Left = redNode;
                }
            }
            else if ( size == 2 )
            {
                root = new Node( arr[ startIndex ], false )
                {
                    Right = new Node( arr[ endIndex ] ),
                };
                if ( redNode != null )
                {
                    root.Left = redNode;
                }
            }
            else if ( size == 3 )
            {
                root = new Node( arr[ startIndex + 1 ], false )
                {
                    Left  = new Node( arr[ startIndex ], false ),
                    Right = new Node( arr[ endIndex   ], false ),
                };
                if ( redNode != null )
                {
                    root.Left.Left = redNode;
                }
            }
            else
            {
                int midpt = ((startIndex + endIndex) >> 1);
                root = new Node( arr[ midpt ], false )
                {
                    Left = ConstructRootFromSortedArray( arr, startIndex, midpt - 1, redNode ),
                };
                if ( size % 2 == 0 )
                {
                    root.Right = ConstructRootFromSortedArray( arr, midpt + 2, endIndex, new Node( arr[ midpt + 1 ], true ) );
                }
                else
                {
                    root.Right = ConstructRootFromSortedArray( arr, midpt + 1, endIndex, null );
                }
            }
            return (root);
        }

        /// <summary>
        /// Transform this set into its intersection with the IEnumerable OTHER     
        /// NOTE: The caller object is important as IntersectionWith uses the        
        /// comparator associated with THIS to check equality                        
        /// Throws ArgumentNullException if OTHER is null                         
        /// </summary>
        /// <param name="other"></param>   
        public virtual void IntersectWith( IEnumerable< T > other )
        {
            if ( other == null )
            {
                throw (new ArgumentNullException( nameof(other) ));
            }

            if ( Count == 0 )
                return;

            //HashSet< T > optimizations can't be done until equality comparers and comparers are related

            //Technically, this would work as well with an ISorted< T >
            var s = other as SortedSetEx< T >;
            var t = this as TreeSubSet;
            //only let this happen if i am also a SortedSet, not a SubSet
            if ( s != null && t == null && AreComparersEqual( this, s ) )
            {
                //first do a merge sort to an array.
                var merged = new T[ this.Count ];
                int c = 0;
                Enumerator mine = this.GetEnumerator();
                Enumerator theirs = s.GetEnumerator();
                bool mineEnded = !mine.MoveNext(), theirsEnded = !theirs.MoveNext();
                T max = Max;
                T min = Min;

                while ( !mineEnded && !theirsEnded && Comparer.Compare( theirs.Current, max ) <= 0 )
                {
                    int comp = Comparer.Compare( mine.Current, theirs.Current );
                    if ( comp < 0 )
                    {
                        mineEnded = !mine.MoveNext();
                    }
                    else if ( comp == 0 )
                    {
                        merged[ c++ ] = theirs.Current;
                        mineEnded = !mine.MoveNext();
                        theirsEnded = !theirs.MoveNext();
                    }
                    else
                    {
                        theirsEnded = !theirs.MoveNext();
                    }
                }

                //now merged has all c elements

                //safe to gc the root, we  have all the elements
                _Root = null;

                _Root = ConstructRootFromSortedArray( merged, 0, c - 1, null );
                _Count = c;
                /*_version++;*/
            }
            else
            {
                IntersectWithEnumerable( other );
            }
        }
        internal virtual void IntersectWithEnumerable( IEnumerable< T > other )
        {
            //TODO: Perhaps a more space-conservative way to do this
            var toSave = new List< T >( Count );
            foreach ( T item in other )
            {
                if ( Contains( item ) )
                {
                    toSave.Add( item );
                }
            }

            if ( toSave.Count < Count )
            {
                Clear();
                AddAllElements( toSave );
            }
        }

        /// <summary>
        ///  Transform this set into its complement with the IEnumerable OTHER       
        ///  NOTE: The caller object is important as ExceptWith uses the        
        ///  comparator associated with THIS to check equality                        
        ///  Throws ArgumentNullException if OTHER is null                           
        /// </summary>
        /// <param name="other"></param>
        public void ExceptWith( IEnumerable< T > other )
        {
            if ( other == null )
            {
                throw (new ArgumentNullException( nameof(other) ));
            }

            if ( _Count == 0 )
                return;

            if ( other == this )
            {
                Clear();
                return;
            }

            var asSorted = other as SortedSetEx< T >;
            if ( asSorted != null && AreComparersEqual( this, asSorted ) )
            {
                //outside range, no point doing anything               
                if ( !(_Comparer.Compare( asSorted.Max, Min ) < 0 || _Comparer.Compare( asSorted.Min, Max ) > 0) )
                {
                    T min = Min;
                    T max = Max;
                    foreach ( T item in other )
                    {
                        if ( _Comparer.Compare( item, min ) < 0 )
                            continue;
                        if ( _Comparer.Compare( item, max ) > 0 )
                            break;
                        Remove( item );
                    }
                }
            }
            else
            {
                RemoveAllElements( other );
            }
        }

        /// <summary>
        ///  Transform this set so it contains elements in THIS or OTHER but not both
        ///  NOTE: The caller object is important as SymmetricExceptWith uses the        
        ///  comparator associated with THIS to check equality                        
        ///  Throws ArgumentNullException if OTHER is null                           
        /// </summary>
        /// <param name="other"></param>
        public void SymmetricExceptWith( IEnumerable< T > other )
        {
            if ( other == null )
            {
                throw (new ArgumentNullException( nameof(other) ));
            }

            if ( Count == 0 )
            {
                UnionWith( other );
                return;
            }

            if ( other == this )
            {
                Clear();
                return;
            }

            var asSorted = other as SortedSetEx< T >;
            if ( asSorted != null && AreComparersEqual( this, asSorted ) )
            {
                SymmetricExceptWithSameEC( asSorted );
            }
            else
            {
                int length;
                T[] elements = EnumerableHelpers.ToArray( other, out length );
                Array.Sort( elements, 0, length, Comparer );
                SymmetricExceptWithSameEC( elements, length );
            }
        }
        private void SymmetricExceptWithSameEC( SortedSetEx< T > other )
        {
            Debug.Assert( other != null );
            Debug.Assert( AreComparersEqual( this, other ) );

            foreach ( T item in other )
            {
                //yes, it is classier to say
                //if (!this.Remove(item))this.Add(item);
                //but this ends up saving on rotations                    
                if ( Contains( item ) )
                {
                    Remove( item );
                }
                else
                {
                    Add( item );
                }
            }
        }
        //OTHER must be a sorted array
        private void SymmetricExceptWithSameEC( T[] other, int count )
        {
            Debug.Assert( other != null );
            Debug.Assert( count >= 0 && count <= other.Length );

            if ( count == 0 )
            {
                return;
            }
            T last = other[ 0 ];
            for ( int i = 0; i < count; i++ )
            {
                while ( i < count && i != 0 && _Comparer.Compare( other[ i ], last ) == 0 )
                {
                    i++;
                }
                if ( i >= count )
                {
                    break;
                }
                if ( Contains( other[ i ] ) )
                {
                    Remove( other[ i ] );
                }
                else
                {
                    Add( other[ i ] );
                }
                last = other[ i ];
            }
        }

        /// <summary>
        /// Checks whether this Tree is a subset of the IEnumerable other
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsSubsetOf( IEnumerable< T > other )
        {
            if ( other == null )
            {
                throw (new ArgumentNullException( nameof(other) ));
            }

            if ( Count == 0 )
                return (true);


            var asSorted = other as SortedSetEx< T >;
            if ( asSorted != null && AreComparersEqual( this, asSorted ) )
            {
                if ( Count > asSorted.Count )
                    return (false);
                return IsSubsetOfSortedSetWithSameEC( asSorted );
            }
            else
            {
                //worst case: mark every element in my set and see if I've counted all
                //O(MlogN)

                ElementCount result = CheckUniqueAndUnfoundElements( other, false );
                return (result.uniqueCount == Count && result.unfoundCount >= 0);
            }
        }
        private bool IsSubsetOfSortedSetWithSameEC( SortedSetEx< T > asSorted )
        {
            SortedSetEx< T > prunedOther = asSorted.GetViewBetween( Min, Max );
            foreach ( T item in this )
            {
                if ( !prunedOther.Contains( item ) )
                {
                    return (false);
                }
            }
            return (true);
        }

        /// <summary>
        /// Checks whether this Tree is a proper subset of the IEnumerable other
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsProperSubsetOf( IEnumerable< T > other )
        {
            if ( other == null )
            {
                throw (new ArgumentNullException( nameof(other) ));
            }

            var coll = other as ICollection;
            if ( coll != null )
            {
                if ( Count == 0 )
                    return (coll.Count > 0);
            }

            //another for sorted sets with the same comparer
            var asSorted = other as SortedSetEx< T >;
            if ( asSorted != null && AreComparersEqual( this, asSorted ) )
            {
                if ( Count >= asSorted.Count )
                    return (false);
                return IsSubsetOfSortedSetWithSameEC( asSorted );
            }

            //worst case: mark every element in my set and see if I've counted all
            //O(MlogN).
            ElementCount result = CheckUniqueAndUnfoundElements( other, false );
            return (result.uniqueCount == Count && result.unfoundCount > 0);
        }

        /// <summary>
        /// Checks whether this Tree is a super set of the IEnumerable other
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsSupersetOf( IEnumerable< T > other )
        {
            if ( other == null )
            {
                throw (new ArgumentNullException( nameof(other) ));
            }

            var coll = other as ICollection;
            if ( coll != null && coll.Count == 0 )
            {
                return (true);
            }

            //do it one way for HashSets
            //another for sorted sets with the same comparer
            var asSorted = other as SortedSetEx< T >;
            if ( asSorted != null && AreComparersEqual( this, asSorted ) )
            {
                if ( Count < asSorted.Count )
                {
                    return (false);
                }
                SortedSetEx< T > pruned = GetViewBetween( asSorted.Min, asSorted.Max );
                foreach ( T item in asSorted )
                {
                    if ( !pruned.Contains( item ) )
                    {
                        return (false);
                    }
                }
                return (true);
            }
            //and a third for everything else
            return ContainsAllElements( other );
        }

        /// <summary>
        /// Checks whether this Tree is a proper super set of the IEnumerable other
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsProperSupersetOf( IEnumerable< T > other )
        {
            if ( other == null )
            {
                throw (new ArgumentNullException( nameof(other) ));
            }

            if ( Count == 0 )
                return (false);

            var coll = other as ICollection;
            if ( coll != null && coll.Count == 0 )
            {
                return (true);
            }

            //another way for sorted sets
            var asSorted = other as SortedSetEx< T >;
            if ( asSorted != null && AreComparersEqual( asSorted, this ) )
            {
                if ( asSorted.Count >= Count )
                {
                    return (false);
                }
                SortedSetEx< T > pruned = GetViewBetween( asSorted.Min, asSorted.Max );
                foreach ( T item in asSorted )
                {
                    if ( !pruned.Contains( item ) )
                    {
                        return (false);
                    }
                }
                return (true);
            }

            //worst case: mark every element in my set and see if I've counted all
            //O(MlogN)
            //slight optimization, put it into a HashSet and then check can do it in O(N+M)
            //but slower in better cases + wastes space
            ElementCount result = CheckUniqueAndUnfoundElements( other, true );
            return (result.uniqueCount < Count && result.unfoundCount == 0);
        }

        /// <summary>
        /// Checks whether this Tree has all elements in common with IEnumerable other
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool SetEquals( IEnumerable< T > other )
        {
            if ( other == null )
            {
                throw (new ArgumentNullException( nameof(other) ));
            }

            var asSorted = other as SortedSetEx< T >;
            if ( asSorted != null && AreComparersEqual( this, asSorted ) )
            {
                Enumerator mine = GetEnumerator();
                Enumerator theirs = asSorted.GetEnumerator();
                bool mineEnded = !mine.MoveNext();
                bool theirsEnded = !theirs.MoveNext();
                while ( !mineEnded && !theirsEnded )
                {
                    if ( Comparer.Compare( mine.Current, theirs.Current ) != 0 )
                    {
                        return (false);
                    }
                    mineEnded   = !mine.MoveNext();
                    theirsEnded = !theirs.MoveNext();
                }
                return (mineEnded && theirsEnded);
            }

            //worst case: mark every element in my set and see if I've counted all
            //O(N) by size of other            
            ElementCount result = CheckUniqueAndUnfoundElements( other, true );
            return (result.uniqueCount == Count && result.unfoundCount == 0);
        }

        /// <summary>
        /// Checks whether this Tree has any elements in common with IEnumerable other
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Overlaps( IEnumerable< T > other )
        {
            if ( other == null )
            {
                throw (new ArgumentNullException( nameof(other) ));
            }

            if ( Count == 0 )
                return (false);

            var coll = other as ICollection< T >;
            if ( (coll != null) && coll.Count == 0 )
            {
                return (false);
            }

            var asSorted = other as SortedSetEx< T >;
            if ( asSorted != null && AreComparersEqual( this, asSorted ) && (_Comparer.Compare( Min, asSorted.Max ) > 0 || _Comparer.Compare( Max, asSorted.Min ) < 0) )
            {
                return (false);
            }
            foreach ( T item in other )
            {
                if ( Contains( item ) )
                {
                    return (true);
                }
            }
            return (false);
        }

        /// <summary>
        /// This works similar to HashSet's CheckUniqueAndUnfound (description below), except that the bit
        /// array maps differently than in the HashSet. We can only use this for the bulk boolean checks.
        /// 
        /// Determines counts that can be used to determine equality, subset, and superset. This
        /// is only used when other is an IEnumerable and not a HashSet. If other is a HashSet
        /// these properties can be checked faster without use of marking because we can assume 
        /// other has no duplicates.
        /// 
        /// The following count checks are performed by callers:
        /// 1. Equals: checks if unfoundCount = 0 and uniqueFoundCount = Count; i.e. everything 
        /// in other is in this and everything in this is in other
        /// 2. Subset: checks if unfoundCount >= 0 and uniqueFoundCount = Count; i.e. other may
        /// have elements not in this and everything in this is in other
        /// 3. Proper subset: checks if unfoundCount > 0 and uniqueFoundCount = Count; i.e
        /// other must have at least one element not in this and everything in this is in other
        /// 4. Proper superset: checks if unfound count = 0 and uniqueFoundCount strictly less
        /// than Count; i.e. everything in other was in this and this had at least one element
        /// not contained in other.
        /// 
        /// An earlier implementation used delegates to perform these checks rather than returning
        /// an ElementCount struct; however this was changed due to the perf overhead of delegates.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="returnIfUnfound">Allows us to finish faster for equals and proper superset
        /// because unfoundCount must be 0.</param>
        /// <returns></returns>
        // <SecurityKernel Critical="True" Ring="0">
        // <UsesUnsafeCode Name="Local bitArrayPtr of type: Int32*" />
        // <ReferencesCritical Name="Method: BitHelper..ctor(System.Int32*,System.Int32)" Ring="1" />
        // <ReferencesCritical Name="Method: BitHelper.IsMarked(System.Int32):System.Boolean" Ring="1" />
        // <ReferencesCritical Name="Method: BitHelper.MarkBit(System.Int32):System.Void" Ring="1" />
        // </SecurityKernel>
        private unsafe ElementCount CheckUniqueAndUnfoundElements( IEnumerable< T > other, bool returnIfUnfound )
        {
            ElementCount result;

            // need special case in case this has no elements. 
            if ( Count == 0 )
            {
                int numElementsInOther = 0;
                foreach ( T item in other )
                {
                    numElementsInOther++;
                    // break right away, all we want to know is whether other has 0 or 1 elements
                    break;
                }
                result.uniqueCount = 0;
                result.unfoundCount = numElementsInOther;
                return (result);
            }

            int intArrayLength = BitHelper.ToIntArrayLength( this.Count );
            BitHelper bitHelper;
            if ( intArrayLength <= STACK_ALLOC_THRESHOLD )
            {
                int* bitArrayPtr = stackalloc int[ intArrayLength ];
                bitHelper = new BitHelper( bitArrayPtr, intArrayLength );
            }
            else
            {
                int[] bitArray = new int[ intArrayLength ];
                bitHelper = new BitHelper( bitArray, intArrayLength );
            }

            // count of items in other not found in this
            int unfoundCount = 0;
            // count of unique items in other found in this
            int uniqueFoundCount = 0;

            foreach ( T item in other )
            {
                int index = InternalIndexOf( item );
                if ( index >= 0 )
                {
                    if ( !bitHelper.IsMarked( index ) )
                    {
                        // item hasn't been seen yet
                        bitHelper.MarkBit( index );
                        uniqueFoundCount++;
                    }
                }
                else
                {
                    unfoundCount++;
                    if ( returnIfUnfound )
                    {
                        break;
                    }
                }
            }

            result.uniqueCount  = uniqueFoundCount;
            result.unfoundCount = unfoundCount;
            return (result);
        }
        public int RemoveWhere( Predicate< T > match )
        {
            if ( match == null )
            {
                throw (new ArgumentNullException( nameof(match) ));
            }

            var matches = new List< T >( this.Count );
            BreadthFirstTreeWalk( (n) =>
            {
                if ( match( n.Item ) )
                {
                    matches.Add( n.Item );
                }
                return (true);
            } );
            // reverse breadth first to (try to) incur low cost
            int actuallyRemoved = 0;
            for ( int i = matches.Count - 1; i >= 0; i-- )
            {
                if ( Remove( matches[ i ] ) )
                {
                    actuallyRemoved++;
                }
            }

            return (actuallyRemoved);
        }
        #endregion

        #region [.ISorted Members.]
        public T Min
        {
            get
            {
                if ( _Root == null )
                {
                    return (default(T));
                }

                Node current = _Root;
                while ( current.Left != null )
                {
                    current = current.Left;
                }

                return (current.Item);
            }
        }
        public T Max
        {
            get
            {
                if ( _Root == null )
                {
                    return (default(T));
                }

                Node current = _Root;
                while ( current.Right != null )
                {
                    current = current.Right;
                }

                return current.Item;
            }
        }

        public IEnumerable< T > Reverse()
        {
            var e = new Enumerator( this, true );
            while ( e.MoveNext() )
            {
                yield return e.Current;
            }
        }

        /// <summary>
        /// Returns a subset of this tree ranging from values lBound to uBound
        /// Any changes made to the subset reflect in the actual tree
        /// </summary>
        /// <param name="lowVestalue">Lowest Value allowed in the subset</param>
        /// <param name="highestValue">Highest Value allowed in the subset</param>        
        public virtual SortedSetEx< T > GetViewBetween( T lowerValue, T upperValue )
        {
            if ( 0 < Comparer.Compare( lowerValue, upperValue ) ) throw (new ArgumentException( "SR.SortedSet_LowerValueGreaterThanUpperValue", "lowerValue" ));
            
            return (new TreeSubSet( this, lowerValue, upperValue, true, true ));
        }
        /// <summary>
        /// This class represents a subset view into the tree. Any changes to this view
        /// are reflected in the actual tree. Uses the Comparator of the underlying tree.
        /// </summary>
        private sealed class TreeSubSet : SortedSetEx< T >
        {
            private SortedSetEx< T > _underlying;
            private T _min, _max;
            //these exist for unbounded collections
            //for instance, you could allow this subset to be defined for i>10. The set will throw if
            //anything <=10 is added, but there is no upperbound. These features Head(), Tail(), were punted
            //in the spec, and are not available, but the framework is there to make them available at some point.
            private bool _lBoundActive, _uBoundActive;
            //used to see if the count is out of date

            public TreeSubSet( SortedSetEx< T > Underlying, T Min, T Max, bool lowerBoundActive, bool upperBoundActive )
                : base( Underlying.Comparer )
            {
                _underlying = Underlying;
                _min = Min;
                _max = Max;
                _lBoundActive = lowerBoundActive;
                _uBoundActive = upperBoundActive;
                _Root = _underlying.FindRange( _min, _max, _lBoundActive, _uBoundActive ); // root is first element within range                                
                _Count = 0;
            }

            /// <summary>
            /// Additions to this tree need to be added to the underlying tree as well
            /// </summary>           
            internal override bool AddIfNotPresent( T item )
            {
                if ( !IsWithinRange( item ) )
                {
                    throw (new ArgumentOutOfRangeException( nameof(item) ));
                }

                bool ret = _underlying.AddIfNotPresent( item );
                return ret;
            }

            internal override bool DoRemove( T item )
            { // todo: uppercase this and others
                if ( !IsWithinRange( item ) )
                {
                    return (false);
                }
                return _underlying.Remove( item );
            }

            public override void Clear()
            {
                if ( _Count == 0 )
                {
                    return;
                }

                var toRemove = new List< T >();
                BreadthFirstTreeWalk( (n) =>
                {
                    toRemove.Add( n.Item );
                    return (true);
                } );
                while ( toRemove.Count != 0 )
                {
                    _underlying.Remove( toRemove[ toRemove.Count - 1 ] );
                    toRemove.RemoveAt( toRemove.Count - 1 );
                }
                _Root = null;
                _Count = 0;
            }

            internal override bool IsWithinRange( T item )
            {
                int comp = (_lBoundActive ? Comparer.Compare( _min, item ) : -1);
                if ( comp > 0 )
                {
                    return (false);
                }
                comp = (_uBoundActive ? Comparer.Compare( _max, item ) : 1);
                if ( comp < 0 )
                {
                    return (false);
                }
                return (true);
            }

            internal override bool InOrderTreeWalk( TreeWalkPredicate< T > action, bool reverse )
            {
                if ( _Root == null )
                {
                    return (true);
                }

                // The maximum height of a red-black tree is 2*lg(n+1).
                // See page 264 of "Introduction to algorithms" by Thomas H. Cormen
                var stack = new Stack< Node >( 2 * (int) log2( _Count + 1 ) ); //this is not exactly right if count is out of date, but the stack can grow
                Node current = _Root;
                while ( current != null )
                {
                    if ( IsWithinRange( current.Item ) )
                    {
                        stack.Push( current );
                        current = (reverse ? current.Right : current.Left);
                    }
                    else if ( _lBoundActive && Comparer.Compare( _min, current.Item ) > 0 )
                    {
                        current = current.Right;
                    }
                    else
                    {
                        current = current.Left;
                    }
                }

                while ( stack.Count != 0 )
                {
                    current = stack.Pop();
                    if ( !action( current ) )
                    {
                        return (false);
                    }

                    Node node = (reverse ? current.Left : current.Right);
                    while ( node != null )
                    {
                        if ( IsWithinRange( node.Item ) )
                        {
                            stack.Push( node );
                            node = (reverse ? node.Right : node.Left);
                        }
                        else if ( _lBoundActive && Comparer.Compare( _min, node.Item ) > 0 )
                        {
                            node = node.Right;
                        }
                        else
                        {
                            node = node.Left;
                        }
                    }
                }
                return (true);
            }

            internal override bool BreadthFirstTreeWalk( TreeWalkPredicate< T > action )
            {
                if ( _Root == null )
                {
                    return (true);
                }

                var processQueue = new Queue< Node >();
                processQueue.Enqueue( _Root );
                Node current;

                while ( processQueue.Count != 0 )
                {
                    current = processQueue.Dequeue();
                    if ( IsWithinRange( current.Item ) && !action( current ) )
                    {
                        return (false);
                    }
                    if ( current.Left != null && (!_lBoundActive || Comparer.Compare( _min, current.Item ) < 0) )
                    {
                        processQueue.Enqueue( current.Left );
                    }
                    if ( current.Right != null && (!_uBoundActive || Comparer.Compare( _max, current.Item ) > 0) )
                    {
                        processQueue.Enqueue( current.Right );
                    }
                }
                return (true);
            }

            internal override Node FindNode( T item )
            {
                if ( !IsWithinRange( item ) )
                {
                    return (null);
                }
                return base.FindNode( item );
            }

            //this does indexing in an inefficient way compared to the actual sortedset, but it saves a lot of space
            internal override int InternalIndexOf( T item )
            {
                int count = -1;
                foreach ( T i in this )
                {
                    count++;
                    if ( Comparer.Compare( item, i ) == 0 )
                    {
                        return (count);
                    }
                }
                return (-1);
            }


            //This passes functionality down to the underlying tree, clipping edges if necessary
            //There's nothing gained by having a nested subset. May as well draw it from the base
            //Cannot increase the bounds of the subset, can only decrease it
            public override SortedSetEx< T > GetViewBetween( T lowerValue, T upperValue )
            {
                if ( _lBoundActive && Comparer.Compare( _min, lowerValue ) > 0 )
                {
                    //lBound = min;
                    throw (new ArgumentOutOfRangeException( nameof(lowerValue) ));
                }
                if ( _uBoundActive && Comparer.Compare( _max, upperValue ) < 0 )
                {
                    //uBound = max;
                    throw (new ArgumentOutOfRangeException( nameof(upperValue) ));
                }
                var ret = (TreeSubSet) _underlying.GetViewBetween( lowerValue, upperValue );
                return ret;
            }
#if DEBUG
            internal override void IntersectWithEnumerable( IEnumerable< T > other )
            {
                base.IntersectWithEnumerable( other );
                Debug.Assert( _Root == _underlying.FindRange( _min, _max ) );
            }
#endif
        }
        #endregion

        #region [.Helper Classes.]
        /// <summary>
        /// 
        /// </summary>
        internal delegate bool TreeWalkPredicate< X >( Node node );

        /// <summary>
        /// 
        /// </summary>
        private enum TreeRotation
        {
            LeftRotation = 1,
            RightRotation = 2,
            RightLeftRotation = 3,
            LeftRightRotation = 4,
        }

        /// <summary>
        /// 
        /// </summary>
        public sealed class Node
        {
            public Node Left;
            public Node Right;            
            public T    Item;
            public bool IsRed;

            public Node( T item )
            {
                // The default color will be red, we never need to create a black node directly.                
                Item  = item;
                IsRed = true;
            }
            public Node( T item, bool isRed )
            {
                // The default color will be red, we never need to create a black node directly.                
                Item  = item;
                IsRed = isRed;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public struct Enumerator : IEnumerator< T >, IEnumerator
        {
            private SortedSetEx< T > _tree;
            private Stack< Node >  _stack;
            private Node _current;
            private bool _reverse;

            internal Enumerator( SortedSetEx< T > set )
            {
                _tree = set;

                // 2lg(n + 1) is the maximum height
                _stack = new Stack< Node >( 2 * (int) log2( set.Count + 1 ) );
                _current = null;
                _reverse = false;

                Intialize();
            }
            internal Enumerator( SortedSetEx< T > set, bool reverse )
            {
                _tree = set;

                // 2lg(n + 1) is the maximum height
                _stack = new Stack< Node >( 2 * (int) log2( set.Count + 1 ) );
                _current = null;
                _reverse = reverse;

                Intialize();
            }

            private void Intialize()
            {
                _current = null;
                Node node = _tree._Root;
                Node next = null, other = null;
                while ( node != null )
                {
                    next = (_reverse ? node.Right : node.Left);
                    other = (_reverse ? node.Left : node.Right);
                    if ( _tree.IsWithinRange( node.Item ) )
                    {
                        _stack.Push( node );
                        node = next;
                    }
                    else if ( next == null || !_tree.IsWithinRange( next.Item ) )
                    {
                        node = other;
                    }
                    else
                    {
                        node = next;
                    }
                }
            }

            public bool MoveNext()
            {
                if ( _stack.Count == 0 )
                {
                    _current = null;
                    return (false);
                }

                _current = _stack.Pop();
                Node node = (_reverse ? _current.Left : _current.Right);
                Node next = null, other = null;
                while ( node != null )
                {
                    next = (_reverse ? node.Right : node.Left);
                    other = (_reverse ? node.Left : node.Right);
                    if ( _tree.IsWithinRange( node.Item ) )
                    {
                        _stack.Push( node );
                        node = next;
                    }
                    else if ( other == null || !_tree.IsWithinRange( other.Item ) )
                    {
                        node = next;
                    }
                    else
                    {
                        node = other;
                    }
                }
                return (true);
            }

            public void Dispose()
            {
            }

            public T Current
            {
                get
                {
                    if ( _current != null )
                    {
                        return _current.Item;
                    }
                    return (default(T));
                }
            }
            object IEnumerator.Current
            {
                get
                {
                    if ( _current == null )
                    {
                        throw (new InvalidOperationException( "SR.InvalidOperation_EnumOpCantHappen" ));
                    }

                    return _current.Item;
                }
            }

            internal bool NotStartedOrEnded
            {
                get
                {
                    return (_current == null);
                }
            }

            internal void Reset()
            {
                /*if ( _version != _tree._version )
                {
                    throw (new InvalidOperationException( "SR.InvalidOperation_EnumFailedVersion" ));
                }*/

                _stack.Clear();
                Intialize();
            }
            void IEnumerator.Reset()
            {
                Reset();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private struct ElementCount
        {
            internal int uniqueCount;
            internal int unfoundCount;
        }
        #endregion

        #region [.misc.]
        // used for set checking operations (using enumerables) that rely on counting
        private static int log2( int value )
        {
            int c = 0;
            while ( 0 < value )
            {
                c++;
                value >>= 1;
            }
            return (c);
        }
        #endregion
    }

    /// <summary>
    /// A class that generates an IEqualityComparer for this SortedSet. Requires that the definition of
    /// equality defined by the IComparer for this SortedSet be consistent with the default IEqualityComparer
    /// for the type T. If not, such an IEqualityComparer should be provided through the constructor.
    /// </summary>    
    internal sealed class SortedSetEqualityComparer< T > : IEqualityComparer< SortedSetEx< T > >
    {
        private readonly IComparer< T > _Comparer;
        private readonly IEqualityComparer< T > _MemberEqualityComparer;

        public SortedSetEqualityComparer() : this( null, null ) { }
        public SortedSetEqualityComparer( IEqualityComparer< T > memberEqualityComparer ) : this( null, memberEqualityComparer ) { }
        /// <summary>
        /// Create a new SetEqualityComparer, given a comparer for member order and another for member equality (these
        /// must be consistent in their definition of equality)
        /// </summary>        
        private SortedSetEqualityComparer( IComparer< T > comparer, IEqualityComparer< T > memberEqualityComparer )
        {
            _Comparer               = comparer               ?? Comparer< T >.Default;
            _MemberEqualityComparer = memberEqualityComparer ?? EqualityComparer< T >.Default;
        }

        // using comparer to keep equals properties in tact; don't want to choose one of the comparers
        public bool Equals( SortedSetEx< T > x, SortedSetEx< T > y ) => SortedSetEx< T >.SortedSetEquals( x, y, _Comparer );

        //IMPORTANT: this part uses the fact that GetHashCode() is consistent with the notion of equality in the set
        public int GetHashCode( SortedSetEx< T > obj )
        {
            int hashCode = 0;
            if ( obj != null )
            {
                foreach ( T t in obj )
                {
                    hashCode = hashCode ^ (_MemberEqualityComparer.GetHashCode( t ) & 0x7FFFFFFF);
                }
            } // else returns hashcode of 0 for null HashSets
            return (hashCode);
        }

        // Equals method for the comparer itself. 
        public override bool Equals( object obj ) => (obj is SortedSetEqualityComparer< T > comparer) && (_Comparer == comparer._Comparer);
        public override int GetHashCode() => (_Comparer.GetHashCode() ^ _MemberEqualityComparer.GetHashCode());
    }
}