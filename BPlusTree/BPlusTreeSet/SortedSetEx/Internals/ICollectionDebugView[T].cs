using System.Diagnostics;

namespace System.Collections.Generic
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class ICollectionDebugView< T >
    {
        private readonly ICollection< T > _Collection;
        public ICollectionDebugView( ICollection< T > collection ) => _Collection = collection ?? throw (new ArgumentNullException( nameof(collection) ));

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                var items = new T[ _Collection.Count ];
                _Collection.CopyTo( items, 0 );
                return (items);
            }
        }
    }
}