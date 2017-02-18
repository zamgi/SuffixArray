using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Web;

namespace Reference.DiagnosisCodes.web.demo
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Config
    {
        public static readonly string INPUT_CSV_FILE = ConfigurationManager.AppSettings[ "INPUT_CSV_FILE" ];
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class SuffixArrayDataHttpContext
    {
        private static readonly object _Lock = new object();

        public static SuffixArrayJsonResultParams Find( string suffix, int maxCount )
        {
            #region [.load tuple-data.]
            var tuples = _TupleData;
            if ( tuples == null )
            {
                lock ( _Lock )
                {
                    tuples = _TupleData;
                    if ( tuples == null )
                    {
                        {
                            tuples = CreateTupleData();
                            _TupleData = tuples;
                        }
                        {
                            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
                            GC.WaitForPendingFinalizers();
                            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
                        }
                    }
                }
            }
            #endregion

            #region [.load suffix-array by tuple-data.]
            var sa = _SuffixArray;
            if ( sa == null )
            {
                lock ( _Lock )
                {
                    sa = _SuffixArray;
                    if ( sa == null )
                    {
                        {
                            sa = new SuffixArray< tuple >( tuples, new tupleIStringValueGetter() );
                            _SuffixArray = sa;
                        }
                        {
                            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
                            GC.WaitForPendingFinalizers();
                            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
                        }
                    }
                }
            }
            #endregion

            #region [.find suffix in tuple-data.]
            var findTotalCount = default(int);
            var frs = sa.Find( suffix, maxCount, out findTotalCount );
            #endregion

            #region commented
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
            var p = new SuffixArrayJsonResultParams()
            {
                suffix         = suffix,
                maxCount       = maxCount,
                findTotalCount = findTotalCount,
                frs            = frs,
                tuples         = tuples,
            };
            return (p);
            #endregion
        }

        private static IList< tuple > CreateTupleData()
        {
            var appRootPath = HttpContext.Current.Server.MapPath( "~/" );
            var tuples = new List< tuple >( 110000 );
            using ( var sr = new StreamReader( Path.Combine( appRootPath, Config.INPUT_CSV_FILE ) ) )
            {
                for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
                {
                    tuples.Add( tuple.Create( line ) );
                }
            }
            tuples.Capacity = tuples.Count;
            return (tuples);
        }

        private static SuffixArray< tuple > _SuffixArray;
        private static IList< tuple > _TupleData;
    }
}