namespace System.Collections.Generic
{
    // Summary:
    //     Defines a method that a type implements to compare two objects.
    //
    // Type parameters:
    //   T:
    //     The type of objects to compare.This type parameter is contravariant. That
    //     is, you can use either the type you specified or any type that is less derived.
    //     For more information about covariance and contravariance, see Covariance
    //     and Contravariance in Generics.
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
