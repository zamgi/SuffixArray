using System;

using Microsoft.AspNetCore.Mvc;
#if DEBUG
using Microsoft.Extensions.Logging;
#endif

namespace Reference.DiagnosisCodes.WebService.Controllers
{
    [ApiController, Route("[controller]")]
    public sealed class ProcessController : ControllerBase
    {
        #region [.ctor().]
        private readonly SuffixArrayProcessor _SuffixArrayProcessor;        
#if DEBUG
        private readonly ILogger< ProcessController > _Logger;
#endif
#if DEBUG
        public ProcessController( SuffixArrayProcessor suffixArrayProcessor, ILogger< ProcessController > logger )
        {
            _SuffixArrayProcessor = suffixArrayProcessor;
            _Logger               = logger;
        }
#else
        public ProcessController( SuffixArrayProcessor suffixArrayProcessor ) => _SuffixArrayProcessor = suffixArrayProcessor;
#endif
        #endregion

        [HttpPost, Route("Run")] public IActionResult Run( [FromBody] InitParamsVM m )
        {
            try
            {
#if DEBUG
                _Logger.LogInformation( $"start process: '{m.Suffix}'..." );
#endif
                var p = _SuffixArrayProcessor.Find( m.Suffix, m.MaxCount.GetValueOrDefault( 25 ) );
                var result = new ResultVM( m, p );
#if DEBUG
                _Logger.LogInformation( $"end process: '{m.Suffix}'." );
#endif
                return Ok( result );
            }
            catch ( Exception ex )
            {
#if DEBUG
                _Logger.LogError( $"Error while process: '{m.Suffix}' => {ex}" );
#endif
                return Ok( new ResultVM( m, ex ) ); //---return StatusCode( 500, new SuffixArrayJsonResult( m, ex ) ); //Internal Server Error
            }
        }
    }
}
