namespace System.Collections.Generic
{
    /// <summary>
    /// 
    /// </summary>
    public interface IBPlusTreeComparer< in T >
    {
        // Summary:
        //     Compares two objects and returns a value indicating whether one is less than,
        //     equal to, or greater than the other.
        //
        // Parameters:
        //   x:
        //     The first object to compare.
        //
        //   y:
        //     The second object to compare.
        //
        // Returns:
        //     A signed integer that indicates the relative values of x and y, as shown
        //     in the following table.Value Meaning Less than zerox is less than y.Zerox
        //     equals y.Greater than zerox is greater than y.
        int Compare( T existsInTreeValue, T searchingValue );
    }
}
