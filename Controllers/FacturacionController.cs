using SistemaGestionActivos.Data;
using SistemaGestionActivos.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

// 1. AÑADIR USINGS DE MERCADO PAGO
using MercadoPago.Config;
using MercadoPago.Client.Preference;
using MercadoPago.Resource.Preference;
using System;
using System.Collections.Generic; // Para la lista de Items

namespace SistemaGestionActivos.Controllers
{
    [Authorize(Roles = "Administrador,Gestor de Activos")]
    public class FacturacionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        // ¡MODIFICADO! He quitado la configuración del AccessToken de aquí.
        // La pondremos directamente en la acción de pago para asegurar que funcione
        // durante las pruebas.
        public FacturacionController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: /Facturacion
        public async Task<IActionResult> Index()
        {
            var facturas = await _context.Facturas
                .Include(f => f.OrdenDeTrabajo)
                .OrderByDescending(f => f.FechaEmision)
                .ToListAsync();

            return View(facturas);
        }

        // GET: /Facturacion/Detalles/5
        public async Task<IActionResult> Detalles(int? id)
        {
            if (id == null) return NotFound();

            var factura = await _context.Facturas
                .Include(f => f.OrdenDeTrabajo)
                    .ThenInclude(ot => ot.Costos)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (factura == null) return NotFound();
            
            return View(factura);
        }

        // POST: /Facturacion/GenerarFactura
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarFactura(int ordenTrabajoId)
        {
            var ot = await _context.OrdenesDeTrabajo
                .Include(o => o.Costos)
                .Include(o => o.Factura)
                .FirstOrDefaultAsync(o => o.Id == ordenTrabajoId);

            if (ot == null) return NotFound("OT no encontrada.");
            
            // --- VALIDACIONES DE NEGOCIO ---
            if (ot.Estado != EstadoOT.Resuelta)
            {
                TempData["ErrorMessage"] = "Solo se pueden facturar OTs 'Resueltas'.";
                return RedirectToAction("Detalles", "OrdenesDeTrabajo", new { id = ordenTrabajoId });
            }
            if (ot.Factura != null)
            {
                TempData["ErrorMessage"] = "Esta OT ya tiene una factura generada.";
                return RedirectToAction("Detalles", "Facturacion", new { id = ot.Factura.Id });
            }
            if (!ot.Costos.Any())
            {
                TempData["ErrorMessage"] = "Esta OT no tiene costos registrados.";
                return RedirectToAction("Detalles", "OrdenesDeTrabajo", new { id = ordenTrabajoId });
            }

            var montoTotal = ot.Costos.Sum(c => c.Monto);
            var nuevaFactura = new Factura
            {
                OrdenTrabajoId = ot.Id,
                FechaEmision = DateTime.Now,
                MontoTotal = montoTotal,
                Estado = "Pendiente de Pago"
            };

            _context.Facturas.Add(nuevaFactura);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Factura generada exitosamente.";
            return RedirectToAction("Detalles", new { id = nuevaFactura.Id });
        }

        // POST: /Facturacion/CrearPreferenciaDePago
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearPreferenciaDePago(int facturaId)
        {
            // ¡IMPORTANTE! Pega tu Access Token de PRUEBA aquí
            MercadoPagoConfig.AccessToken = "APP_USR-5142632128560601-102513-8cf91f8a575b62eeff018bc74bb24c91-2946107394";
            
            var factura = await _context.Facturas
                .Include(f => f.OrdenDeTrabajo)
                .FirstOrDefaultAsync(f => f.Id == facturaId);

            if (factura == null) return NotFound();

            try
            {
                var items = new List<PreferenceItemRequest>
                {
                    new PreferenceItemRequest
                    {
                        Title = $"Servicio de Mantenimiento OT-{factura.OrdenTrabajoId}",
                        Description = "Reparación y costos asociados a la Orden de Trabajo.",
                        Quantity = 1,
                        CurrencyId = "PEN", // ¡Asegúrate que tu cuenta de prueba sea de Perú!
                        UnitPrice = factura.MontoTotal
                    }
                };

                // ===== ¡ESTA ES LA CORRECCIÓN MÁS IMPORTANTE! =====
                // Debemos usar la URL pública de ngrok que creaste.
                // ¡RECUERDA PEGAR TU URL DE NGROK AQUÍ! (La que empieza con https://)
                string publicNgrokUrl = "https://palest-alta-untaught.ngrok-free.dev"; // <-- Pega tu URL de ngrok aquí

                var request = new PreferenceRequest
                {
                    Items = items,
                    ExternalReference = factura.Id.ToString(),
                    BackUrls = new PreferenceBackUrlsRequest
                    {
                        // Usamos la URL pública de ngrok
                        Success = $"{publicNgrokUrl}/Facturacion/PagoExitoso",
                        Failure = $"{publicNgrokUrl}/Facturacion/PagoFallido",
                        Pending = $"{publicNgrokUrl}/Facturacion/PagoPendiente"
                    },
                    // Ahora que las URLs son HTTPS válidas, AutoReturn SÍ funcionará.
                    AutoReturn = "approved" 
                };
                // ===== FIN DE LA CORRECCIÓN =====

                var client = new PreferenceClient();
                Preference preference = await client.CreateAsync(request);

                return Redirect(preference.InitPoint);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al contactar Mercado Pago: {ex.Message}";
                return RedirectToAction("Detalles", new { id = facturaId });
            }
        }

        // GET: /Facturacion/PagoExitoso
        public async Task<IActionResult> PagoExitoso(string external_reference, string payment_id, string status)
        {
            if (string.IsNullOrEmpty(external_reference))
            {
                return BadRequest("Falta la referencia externa.");
            }

            int facturaId = int.Parse(external_reference); 
            var factura = await _context.Facturas.FindAsync(facturaId);

            if (factura != null && factura.Estado != "Pagada")
            {
                factura.Estado = "Pagada";
                factura.MetodoPago = "Mercado Pago";
                factura.PagoIdExterno = payment_id; 
                
                _context.Update(factura);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "¡Pago procesado exitosamente!";
            }
            
            return RedirectToAction("Detalles", new { id = facturaId });
        }

        // GET: /Facturacion/PagoFallido
        public IActionResult PagoFallido(string external_reference)
        {
            TempData["ErrorMessage"] = "El pago falló o fue cancelado.";
            return RedirectToAction("Detalles", new { id = int.Parse(external_reference) });
        }
        
        // GET: /Facturacion/PagoPendiente
        public IActionResult PagoPendiente(string external_reference)
        {
            TempData["InfoMessage"] = "El pago está pendiente de procesamiento.";
            return RedirectToAction("Detalles", new { id = int.Parse(external_reference) });
        }
    }
}