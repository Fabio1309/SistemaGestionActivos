using Microsoft.AspNetCore.Mvc;
using SistemaGestionActivos.Services;

namespace SistemaGestionActivos.Controllers
{
    [ApiController]
    [Route("debug")]
    public class DebugController : ControllerBase
    {
        private readonly ICategoryPredictionService _predictor;

        public DebugController(ICategoryPredictionService predictor)
        {
            _predictor = predictor;
        }

        // GET /debug/prediction-info
        [HttpGet("prediction-info")]
        public IActionResult PredictionInfo()
        {
            try
            {
                var diag = _predictor.GetDiagnostics();
                // Try a sample prediction to show raw result
                string sampleText = "el archivo esta corrupto y no abre";
                var sample = _predictor.PredictCategory(sampleText);
                return Ok(new { ok = true, diagnostics = diag, sampleText, samplePrediction = sample });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { ok = false, error = ex.Message });
            }
        }
    }
}
