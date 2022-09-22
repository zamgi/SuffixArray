namespace System.Collections.Generic
{
    /// <summary>
    /// 
    /// </summary>
    internal unsafe sealed class BitHelper
    {   // should not be serialized
        private const byte MARKED_BIT_FLAG = 1;
        private const byte INT_SIZE = 32;

        // m_length of underlying int array (not logical bit array)
        private readonly int _Length;

        // ptr to stack alloc'd array of ints
        private readonly int* _ArrayPtr;

        // array of ints
        private readonly int[] _Array;

        // whether to operate on stack alloc'd or heap alloc'd array 
        private readonly bool _UseStackAlloc;

        /// <summary>
        /// Instantiates a BitHelper with a heap alloc'd array of ints
        /// </summary>
        /// <param name="bitArray">int array to hold bits</param>
        /// <param name="length">length of int array</param>
        internal BitHelper( int* bitArrayPtr, int length )
        {
            _ArrayPtr      = bitArrayPtr;
            _Length        = length;
            _UseStackAlloc = true;
        }

        /// <summary>
        /// Instantiates a BitHelper with a heap alloc'd array of ints
        /// </summary>
        /// <param name="bitArray">int array to hold bits</param>
        /// <param name="length">length of int array</param>
        internal BitHelper( int[] bitArray, int length )
        {
            _Array  = bitArray;
            _Length = length;
        }

        /// <summary>
        /// Mark bit at specified position
        /// </summary>
        internal void MarkBit( int bitPosition )
        {
            int bitArrayIndex = bitPosition / INT_SIZE;
            if ( bitArrayIndex < _Length && bitArrayIndex >= 0 )
            {
                int flag = (MARKED_BIT_FLAG << (bitPosition % INT_SIZE));
                if ( _UseStackAlloc )
                {
                    _ArrayPtr[ bitArrayIndex ] |= flag;
                }
                else
                {
                    _Array[ bitArrayIndex ] |= flag;
                }
            }
        }

        /// <summary>
        /// Is bit at specified position marked?
        /// </summary>
        internal bool IsMarked( int bitPosition )
        {
            int bitArrayIndex = bitPosition / INT_SIZE;
            if ( bitArrayIndex < _Length && bitArrayIndex >= 0 )
            {
                int flag = (MARKED_BIT_FLAG << (bitPosition % INT_SIZE));
                if ( _UseStackAlloc )
                {
                    return ((_ArrayPtr[ bitArrayIndex ] & flag) != 0);
                }
                else
                {
                    return ((_Array[ bitArrayIndex ] & flag) != 0);
                }
            }
            return (false);
        }

        /// <summary>
        /// How many ints must be allocated to represent n bits. Returns (n+31)/32, but avoids overflow
        /// </summary>
        internal static int ToIntArrayLength( int n ) => ((n > 0) ? ((n - 1) / INT_SIZE + 1) : 0);
    }
}