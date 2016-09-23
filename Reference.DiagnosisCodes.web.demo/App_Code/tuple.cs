using System.Collections;
using System.IO;

namespace Reference.DiagnosisCodes.web.demo
{
    /// <summary>
    /// 
    /// </summary>
    internal struct tuple : IStringValueGetter< tuple >
    {
        public int    Id   { get; private set; }
        public string Text { get; private set; }

        public string GetStringValue( tuple t )
        {
            return (t.Text);
        }

        public static tuple Create( string s )
        {
            var index = s.IndexOf( ';' );
            if ( index == -1 )
                throw (new InvalidDataException());

            return (new tuple() { Id   = int.Parse( s.Substring( 0, index ) ), 
                                    Text = s.Substring( index + 1 ) 
                                });
        }
    }
}