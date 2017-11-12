using System.Runtime;

namespace System.Collections.Generic
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class BPlusTreeBlock< T > : SortedList< T >
    {
        /// <summary>
        /// 
        /// </summary>
        internal sealed class BlockComparer : IComparer< BPlusTreeBlock< T > >
        {
            public static BlockComparer Inst = new BlockComparer();

            [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
            public int Compare( BPlusTreeBlock< T > x, BPlusTreeBlock< T > y )
            {
                return (x.CompareOtherWith4_BPlusTreeBlock( y ));
            }
        }


        private BPlusTreeBlock< T > _Next;

        private BPlusTreeBlock()
        {
        }
        public BPlusTreeBlock( IComparer< T > comparer, int capacity, T t ) : base( comparer, capacity, t )
        {
        }

        public BPlusTreeBlock< T > SplitInTwo()
        {
            _Next = new BPlusTreeBlock< T >() { _Next = _Next };
            base.SplitInTwo< BPlusTreeBlock< T > >( _Next );
            return (_Next);
        }

        public BPlusTreeBlock< T > Next
        {
            get { return (_Next); }    
        }
        //public bool IsFull
        //{
        //    get { return (this.Capacity == this.Count); }
        //}
    }
}
