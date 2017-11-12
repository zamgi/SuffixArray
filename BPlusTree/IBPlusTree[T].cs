namespace System.Collections.Generic
{
    /// <summary>
    /// 
    /// </summary>
    public interface IBPlusTree< T > : IEnumerable< T >
    {
        bool Add( T t );
        bool TryGetValue( T t, out T exists );
        T GetValue( T t, T defaultValue = default(T) );
        bool Contains( T t );
        bool Remove( T t );
        int GetCount();
        int Count { get; }
        IEnumerable< T > GetValues( T t );
        IEnumerable< T > GetValues( T t, IBPlusTreeComparer< T > comparer );
        IEnumerable< T > GetValuesBetween( T min, T max );
        IEnumerable< T > GetValuesBetween( T min, T max, IBPlusTreeComparer< T > comparer );
        void Trim();
    }
}
