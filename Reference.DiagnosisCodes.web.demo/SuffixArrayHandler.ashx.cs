using System;
using System.Diagnostics;
using System.Web;

using Newtonsoft.Json;

namespace Reference.DiagnosisCodes.web.demo
{
    /// <summary>
    ///
    /// </summary>
    public sealed class SuffixArrayHandler : IHttpHandler
    {        
        public bool IsReusable
        {
            get { return (true); }
        }

        public void ProcessRequest( HttpContext context )
        {
            context.Response.ContentTypeJson();
            context.Response.TryAccessControlAllowOrigin();
            try
            {
                var suffix   = context.Request[ "suffix" ];
                var maxCount = context.Request[ "maxCount" ].Try2Int( 25 );

                var p = SuffixArrayDataHttpContext.Find( suffix, maxCount );

                context.Response.ToJson( p );
            }
            catch ( Exception ex )
            {
                context.Response.ToJson( ex );
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Extensions
    {
        public static int Try2Int( this string value, int defaultValue )
        {
            if ( value != null )
            {
                var result = default(int);
                if ( int.TryParse( value, out result ) )
                    return (result);
            }
            return (defaultValue);
        }

        public static void ToJson( this HttpResponse response, SuffixArrayJsonResultParams p )
        {
            response.ToJson( new SuffixArrayJsonResult( p ) );
        }
        public static void ToJson( this HttpResponse response, SuffixArrayJsonResult result )
        {
            var json = JsonConvert.SerializeObject( result );
            response.Write( json );
        }
        public static void ToJson( this HttpResponse response, Exception ex )
        {
            response.ToJson( new SuffixArrayJsonResult( ex ) );
        }

        public static void ContentTypeJson( this HttpResponse response )
        {
            response.ContentType = "application/json";
        }
        public static void TryAccessControlAllowOrigin( this HttpResponse response )
        {
            try
            {
                response.Headers.Add( "Access-Control-Allow-Origin", "*" );
            }
            catch ( Exception ex )
            {
                Debug.WriteLine( ex );
            }
        }
    }
}