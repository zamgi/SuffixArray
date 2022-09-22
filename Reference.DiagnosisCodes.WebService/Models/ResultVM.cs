using System;
using System.Collections.Generic;

using JP = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Reference.DiagnosisCodes.WebService
{
    /// <summary>
    /// 
    /// </summary>
    internal readonly struct ResultVM
    {
        /// <summary>
        /// 
        /// </summary>
        public readonly struct value_t
        {
            [JP("id")]        public int    Id          { get; init; }
            [JP("name")]      public string Name        { get; init; }
            [JP("suffixIdx")] public int    SuffixIndex { get; init; }
        }

        public ResultVM( in InitParamsVM m, Exception ex ) : this() => (InitParams, ExceptionMessage) = (m, ex.ToString());
        public ResultVM( in InitParamsVM m, in SuffixArrayProcessor.Result p ) : this()
        {
            InitParams     = m;
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

        [JP("ip")]             public InitParamsVM InitParams    { get; }
        [JP("err")]            public string    ExceptionMessage { get; }
        [JP("suffix")]         public string    Suffix           { get; }
        [JP("maxCount")]       public int       MaxCount         { get; }
        [JP("findTotalCount")] public int       FindTotalCount   { get; }
        [JP("values")]         public value_t[] Values           { get; }
    }
}
