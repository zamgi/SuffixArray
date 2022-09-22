
namespace System.Collections.Generic
{
    /// <summary>
    /// 
    /// </summary>
    public interface IStringValueGetter< T >
    {
        string GetStringValue( in T obj );
    }
}
