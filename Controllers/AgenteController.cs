using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.SemanticKernel;
using SistemaGestionActivos.Plugins;
using System.Security.Claims;

namespace SistemaGestionActivos.Controllers
{
    [Authorize]
    public class AgenteController : Controller
    {
        private readonly Kernel _kernel;
        private readonly OrdenDeTrabajoPlugin? _otPlugin;

        public AgenteController(Kernel kernel, OrdenDeTrabajoPlugin otPlugin)
        {
            _kernel = kernel;
            _otPlugin = otPlugin;
            // "Enseñamos" a la IA nuestra herramienta de C#
            _kernel.ImportPluginFromObject(otPlugin, "OrdenDeTrabajoPlugin");
        }

        // GET: /Agente/Index
        // Esta será tu vista de Chat
        public IActionResult Index()
        {
            return View();
        }

        // POST: /Agente/EnviarMensaje
        [HttpPost]
        public async Task<IActionResult> EnviarMensaje([FromBody] ChatRequest request)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email); // Email del usuario logueado

            // 1. Damos contexto al Agente (quién está hablando)
            var displayName = User?.Identity?.Name ?? "usuario";
            string prompt = $@"
                Un usuario llamado {displayName} (email: {userEmail}) necesita ayuda.
                Mensaje del usuario: '{request.Prompt}'

                Tu trabajo es ayudarlo. Si reporta un problema, USA la herramienta 'CrearOrdenDeTrabajo'.
                Pídele el código del activo si no te lo da.
                Responde amablemente en español.";

            // 2. Ejecutar el Agente
            try
            {
                var result = await _kernel.InvokePromptAsync(prompt);
                // 3. Devolver la respuesta de la IA
                return Json(new { ok = true, respuesta = result.ToString() });
            }
            catch (Microsoft.SemanticKernel.HttpOperationException ex)
            {
                // Error retornado por el connector (por ejemplo modelo no disponible, credenciales incorrectas o cuota)
                var logger = HttpContext.RequestServices.GetService(typeof(ILogger<AgenteController>)) as ILogger;
                logger?.LogError(ex, "Error invoking kernel prompt: {Message}", ex.Message);

                // Intentar fallback heurístico local
                var fallback = await HeuristicResponseAsync(request.Prompt);
                if (!string.IsNullOrEmpty(fallback))
                {
                    return Json(new { ok = true, respuesta = fallback, fallback = true });
                }

                return Json(new { ok = false, error = "Error al comunicarse con el servicio de IA: " + ex.Message });
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetService(typeof(ILogger<AgenteController>)) as ILogger;
                logger?.LogError(ex, "Unexpected error invoking kernel prompt");

                // Último recurso: intentar heurística local
                var fallback = await HeuristicResponseAsync(request.Prompt);
                if (!string.IsNullOrEmpty(fallback))
                {
                    return Json(new { ok = true, respuesta = fallback, fallback = true });
                }

                return Json(new { ok = false, error = "Ocurrió un error interno al procesar la solicitud." });
            }

            // Heurística simple para responder sin llamar a la API externa
            async Task<string?> HeuristicResponseLocal(string? prm)
            {
                if (string.IsNullOrWhiteSpace(prm)) return null;
                var p = prm.ToLowerInvariant();

                // 1) Detectar si el usuario incluye un código de activo (ej. LP-001)
                var m = System.Text.RegularExpressions.Regex.Match(prm ?? string.Empty, "\\b[A-Z]{2}-\\d{3}\\b");
                string? userEmailLocal = null;
                try
                {
                    userEmailLocal = User?.FindFirstValue(System.Security.Claims.ClaimTypes.Email);
                }
                catch { }

                if (m.Success && _otPlugin != null && !string.IsNullOrEmpty(userEmailLocal))
                {
                    try
                    {
                        var codigo = m.Value;
                        var creation = await _otPlugin.CrearOrdenDeTrabajo(codigo, prm ?? "", userEmailLocal);
                        return $"He creado una Orden de Trabajo automáticamente: {creation}";
                    }
                    catch (Exception ex)
                    {
                        var logger = HttpContext.RequestServices.GetService(typeof(ILogger<AgenteController>)) as ILogger;
                        logger?.LogWarning(ex, "No se pudo crear OT desde la heurística");
                    }
                }

                // 2) Mapear palabras clave a respuestas útiles
                if (p.Contains("impresora") && p.Contains("atasc"))
                {
                    return "Sugerencia: apaga y enciende la impresora, revisa la bandeja por atascos y asegúrate de que el papel esté alineado. Si persiste, crea una Orden de Trabajo.";
                }
                if (p.Contains("impresora") && p.Contains("toner"))
                {
                    return "Sugerencia: revisa el nivel de tóner en el panel de la impresora o en la app del fabricante. Si está bajo, se necesita reemplazar el cartucho.";
                }
                if (p.Contains("no enciende") || p.Contains("pantalla negra") || p.Contains("no arranca") || p.Contains("no prende"))
                {
                    return "Sugerencia: comprueba el cargador y las conexiones, intenta un arranque en frío (mantén pulsado el botón de encendido 10s) y prueba con otro cargador si es posible. Si el equipo tiene código de activo, inclúyelo para crear una OT.";
                }
                if (p.Contains("teclado") && (p.Contains("no funciona") || p.Contains("no responde")))
                {
                    return "Sugerencia: verifica la conexión (USB/Bluetooth), reinicia el equipo y prueba otro teclado. Si el problema es persistente, crea una Orden de Trabajo.";
                }
                if (p.Contains("wifi") || p.Contains("internet") || p.Contains("red"))
                {
                    return "Sugerencia: reinicia el router, verifica si otros equipos tienen conexión y si el problema es general contacta con redes. Para una incidencia formal, crea una Orden de Trabajo.";
                }
                if (p.Contains("contraseña") || p.Contains("login") || p.Contains("acceso") || p.Contains("no puedo entrar"))
                {
                    return "Sugerencia: intenta restablecer tu contraseña mediante la opción de 'Olvidé mi contraseña' o contacta con el administrador para resetear tu cuenta.";
                }
                if (p.Contains("software") || p.Contains("instalar") || p.Contains("actualizar"))
                {
                    return "Sugerencia: Indica qué software necesitas y la versión. Para instalaciones administrativas solicita al equipo de TI crear una orden con los permisos necesarios.";
                }

                // Si no hay una heurística específica, ofrecer ayuda genérica
                return "No pude contactar con el servicio de IA. Puedo sugerir pasos básicos: describe más el problema (modelo/código del activo, qué intentaste ya) o crea una Orden de Trabajo para que el equipo de soporte revise el activo.";
            }

            // Método auxiliar: invocar heurística desde los catch
            async Task<string?> HeuristicResponseAsync(string? prm) => await HeuristicResponseLocal(prm);

            // Fin del método EnviarMensaje
        }
    }

    public class ChatRequest { public string? Prompt { get; set; } }
}