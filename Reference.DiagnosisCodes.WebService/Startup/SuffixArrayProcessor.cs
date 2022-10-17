using System;
using System.Collections.Generic;
using System.IO;

using find_result_t = System.Collections.Generic.SuffixArray< Reference.DiagnosisCodes.WebService.tuple >.find_result_t;

namespace Reference.DiagnosisCodes.WebService
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class SuffixArrayProcessor
    {
        /// <summary>
        /// 
        /// </summary>
        internal readonly struct Result
        {
            public string          suffix         { get; init; }
            public int             maxCount       { get; init; }
            public int             findTotalCount { get; init; }
            public find_result_t[] frs            { get; init; }
            public IList< tuple >  tuples         { get; init; }
        }

        private SuffixArray< tuple > _SuffixArray;
        private IList< tuple > _Tuples;

        internal SuffixArrayProcessor( IList< tuple > tuples, SuffixArray< tuple > suffixArray )
        {
            _Tuples      = tuples;
            _SuffixArray = suffixArray;
        }
        internal Result Find( string suffix, int maxCount )
        {
            #region [.find suffix in tuple-data.]
            var frs = _SuffixArray.Find( suffix, maxCount, out var findTotalCount );
            #endregion

            #region comm.
            /*
            #region [.sort result by CityType & StreetType.]
            Array.Sort< find_result_t >( frs, (fr1, fr2) =>
            {
                var street1 = tuples.GetStreet( fr1.ObjIndex );
                var street2 = tuples.GetStreet( fr2.ObjIndex );

                var city1 = tuples.GetCityByIndex( street1.City );
                var city2 = tuples.GetCityByIndex( street2.City );
                if ( city1.CityType == CityTypeEnum.City )
                {
                    if ( city2.CityType == CityTypeEnum.City )
                    {
                        #region [.MOSCOW & ST_PETERBURG to up.]
                        var sct1 = GetSpecialCityType( city1.City );
                        var sct2 = GetSpecialCityType( city2.City );

                        if ( sct1 == SpecialCityType.Moscow )
                        {
                            if ( sct2 != SpecialCityType.Any )
                                return (fr1.SuffixIndex - fr2.SuffixIndex);
                            return (-1);
                        }
                        if ( sct2 == SpecialCityType.Moscow )
                        {
                            if ( sct1 != SpecialCityType.Any )
                                return (fr1.SuffixIndex - fr2.SuffixIndex);
                            return (1);
                        }
                        if ( sct1 == SpecialCityType.StPeteburg )
                        {
                            return (-1);
                        }
                        if ( sct2 == SpecialCityType.StPeteburg )
                        {
                            return (1);
                        }
                        #endregion

                        var d = string.Compare( city1.City, city2.City, true );
                        if ( d == 0 )
                            return (fr1.SuffixIndex - fr2.SuffixIndex);
                        return (d);
                    }
                    return (-1);
                }
                else
                if ( city2.CityType == CityTypeEnum.City )
                {
                    return (1);
                }

                if ( street1.StreetType == (int) CityTypeEnum.Street )
                {
                    if ( street2.StreetType == (int) CityTypeEnum.Street )
                    {
                        var d = string.Compare( street1.Street, street2.Street, true );
                        if ( d == 0 )
                            return (fr1.SuffixIndex - fr2.SuffixIndex);
                        return (d);
                    }
                    return (-1);
                }
                else
                if ( street2.StreetType == (int) CityTypeEnum.Street )
                {
                    return (1);
                }
                return (0);
            });
            #endregion
            */
            
            #endregion

            #region [.result.]
            var p = new Result()
            {
                suffix         = suffix,
                maxCount       = maxCount,
                findTotalCount = findTotalCount,
                frs            = frs,
                tuples         = _Tuples,
            };
            return (p);
            #endregion
        }

        private static IList< tuple > CreateTupleData( string INPUT_CSV_FILE, int capacity = 110_000 )
        {
            var tuples = new List< tuple >( capacity );
            using ( var sr = new StreamReader( INPUT_CSV_FILE ) )
            {
                for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
                {
                    tuples.Add( tuple.Create( line ) );
                }
            }
            tuples.Capacity = tuples.Count;
            return (tuples);
        }
        public static SuffixArrayProcessor Create( string INPUT_CSV_FILE )
        {
            var tuples      = CreateTupleData( INPUT_CSV_FILE );
            var suffixArray = new SuffixArray< tuple >( tuples, new tupleStringValueGetter() );

            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
            GC.WaitForPendingFinalizers();
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );

            return (new SuffixArrayProcessor( tuples, suffixArray ));
        }
    }
}