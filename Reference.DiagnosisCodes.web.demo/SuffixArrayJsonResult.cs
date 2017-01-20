using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using find_result_t = System.Collections.Generic.SuffixArray< Reference.DiagnosisCodes.web.demo.tuple >.find_result_t;

namespace Reference.DiagnosisCodes.web.demo
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class SuffixArrayJsonResultParams
    {
        public string          suffix;
        public int             maxCount;
        public int             findTotalCount;
        public find_result_t[] frs;
        public IList< tuple >  tuples;
    }

    /// <summary>
    /// 
    /// </summary>
    internal sealed class SuffixArrayJsonResult
    {
        /// <summary>
        /// 
        /// </summary>
        public struct value_t
        {
            [JsonProperty(PropertyName="id")]        public int    Id
            {
                get;
                set;
            }
            [JsonProperty(PropertyName="name")]      public string Name
            {
                get;
                set;
            }
            [JsonProperty(PropertyName="suffixIdx")] public int    SuffixIndex
            {
                get;
                set;
            }
        }

        public SuffixArrayJsonResult( Exception ex )
        {
            ExceptionMessage = ex.ToString();
        }
        public SuffixArrayJsonResult( SuffixArrayJsonResultParams p )
        {
            Suffix         = p.suffix;
            MaxCount       = p.maxCount;
            FindTotalCount = p.findTotalCount;
            Values         = new value_t[ p.frs.Length ];
            for ( int i = 0, len = p.frs.Length; i < len; i++ )
            {
                var fr = p.frs[ i ];
                Values[ i ] = new value_t() 
                { 
                    Id          = p.tuples[ fr.ObjIndex ].Id,
                    Name        = fr.Word,
                    SuffixIndex = fr.SuffixIndex,
                };
            }
        }

        [JsonProperty(PropertyName = "err")]            public string ExceptionMessage
        {
            get;
            private set;
        }
        [JsonProperty(PropertyName = "suffix")]         public string Suffix
        {
            get;
            private set;
        }
        [JsonProperty(PropertyName = "maxCount")]       public int MaxCount
        {
            get;
            private set;
        }
        [JsonProperty(PropertyName = "findTotalCount")] public int FindTotalCount
        {
            get;
            private set;
        }        
        [JsonProperty(PropertyName = "values")]         public value_t[] Values
        {
            get;
            private set;
        }
    }
}