using System;

namespace System.Collections.Generic
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class SuffixArrayBase< T > : IEnumerable< SuffixArrayBase< T >.find_result_t >
    {
        /// <summary>
        /// 
        /// </summary>
        public enum FindModeEnum
        {
            IgnoreCase,
            UseCase,
        }

        /// <summary>
        /// 
        /// </summary>
        public enum EnumerableModeEnum
        {
            BaseOfSuffix,
            AllSubSuffix,
        }

        /// <summary>
        /// 
        /// </summary>
        public struct find_result_t
        {
            internal static readonly find_result_t[] EMPTY = new find_result_t[ 0 ];

            internal static find_result_t Create( int objIndex, string word, int suffixIndex, int suffixLength )
            {
                var fr = new find_result_t() 
                {
                    ObjIndex     = objIndex, 
                    Word         = word, 
                    SuffixIndex  = suffixIndex, 
                    SuffixLength = suffixLength 
                };
                return (fr);
            }
            /*internal static find_result_t Create( ref data_t data, string word, int suffixLength )
            {
                var fr = new find_result_t() 
                {
                    ObjIndex     = data.WordIndex, 
                    Word         = word, 
                    SuffixIndex  = data.SuffixIndex, 
                    SuffixLength = suffixLength 
                };
                return (fr);
            }*/

            public int    ObjIndex;
            public string Word;
            public int    SuffixIndex;
            public int    SuffixLength;

            public string GetBeforeSuffix()
            {
                return (Word.Substring( 0, SuffixIndex ));
            }
            public string GetSuffix()
            {
                return (Word.Substring( SuffixIndex, SuffixLength ));
            }
            public string GetAfterSuffix()
            {
                return (Word.Substring( SuffixIndex + SuffixLength ));
            }
            public string GetHighlightSuffix( string left, string right )
            {
                return (string.Concat( GetBeforeSuffix(), left, GetSuffix(), right, GetAfterSuffix() ));
            }
#if DEBUG
            public override string ToString()
            {
                return ('\'' + GetBeforeSuffix() + '[' + GetSuffix() + ']' + GetAfterSuffix() + '\'');
            }
#endif
        }

        public abstract bool ContainsKey( string suffix, FindModeEnum findMode = FindModeEnum.IgnoreCase );
        public abstract IEnumerable< find_result_t > Find( string suffix, FindModeEnum findMode = FindModeEnum.IgnoreCase );
        public abstract find_result_t[] Find( string suffix, int maxCount, out int findTotalCount, FindModeEnum findMode = FindModeEnum.IgnoreCase );
        public abstract int FindCount( string suffix, FindModeEnum findMode = FindModeEnum.IgnoreCase );
        public abstract IEnumerable< string > GetAllSuffixes( EnumerableModeEnum enumerableMode );
        public abstract int GetAllSuffixesCount( EnumerableModeEnum enumerableMode );
        public abstract IEnumerator< find_result_t > GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (GetEnumerator());
        }
    }
}
