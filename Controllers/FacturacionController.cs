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
        private readonly IConfiguration _configuration; // Para leer el Access Token

        public FacturacionController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;

            // 2. CONFIGURAR EL ACCESS TOKEN (MODO SANDBOX)
            // Debes añadir tu "TEST-AccessToken" de Sandbox en appsettings.json
            MercadoPagoConfig.AccessToken = _configuration["MercadoPago:AccessToken"];
        }

        // ... Tu método Index() y Detalles() se quedan igual ...
        public async Task<IActionResult> Index()
        {
            var facturas = await _context.Facturas
                .Include(f => f.OrdenDeTrabajo)
                .OrderByDescending(f => f.FechaEmision)
                .ToListAsync();

            return View(facturas);
        }

        public async Task<IActionResult> Detalles(int? id)
        {
            if (id == null) return NotFound();

            var factura = await _context.Facturas
                .Include(f => f.OrdenDeTrabajo)
                    .ThenInclude(ot => ot.Costos) // Incluimos los costos de la OT
                .FirstOrDefaultAsync(f => f.Id == id);

            if (factura == null) return NotFound();
            
            return View(factura);
        }

        // POST: /Facturacion/GenerarFactura
        // Esta es la acción clave. Se llamará desde un botón en la vista de Orden de Trabajo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarFactura(int ordenTrabajoId)
        {
            // 1. Validar la Orden de Trabajo
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
                return RedirectToAction("Detalles", "OrdenesTrabajo", new { id = ordenTrabajoId });
            }

            // 2. Crear la Factura
            var montoTotal = ot.Costos.Sum(c => c.Monto);
            var nuevaFactura = new Factura
            {
                OrdenTrabajoId = ot.Id,
                FechaEmision = DateTime.Now,
                MontoTotal = montoTotal,
                Estado = "Pendiente de Pago"
            };

            // 3. Guardar
            _context.Facturas.Add(nuevaFactura);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Factura generada exitosamente.";
            return RedirectToAction("Detalles", new { id = nuevaFactura.Id });
        }

        // 3. ¡NUEVA ACCIÓN! ESTA ES LA LÓGICA DE LA API
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearPreferenciaDePago(int facturaId)
        {
            MercadoPagoConfig.AccessToken = "APP_USR-5142632128560601-102513-8cf91f8a575b62eeff018bc74bb24c91-2946107394";
            var factura = await _context.Facturas
                .Include(f => f.OrdenDeTrabajo)
                .FirstOrDefaultAsync(f => f.Id == facturaId);

            if (factura == null) return NotFound();

            try
            {
                // 4. Crear la lista de ítems para Mercado Pago
                var items = new List<PreferenceItemRequest>
                {
                    new PreferenceItemRequest
                    {
                        Title = $"Servicio de Mantenimiento OT-{@factura.OrdenTrabajoId}",
                        Description = "Reparación y costos asociados a la Orden de Trabajo.",
                        Quantity = 1,
                        CurrencyId = "PEN", // O la moneda de tu país
                        UnitPrice = factura.MontoTotal
                    }
                };

                // 5. Crear la preferencia de pago
                string scheme = "https";
                string successUrl = Url.Action("PagoExitoso", "Facturacion", null, Request.Scheme);
                string failureUrl = Url.Action("PagoFallido", "Facturacion", null, Request.Scheme);
                string pendingUrl = Url.Action("PagoPendiente", "Facturacion", null, Request.Scheme);
                var request = new PreferenceRequest
                {
                    Items = items,
                    ExternalReference = factura.Id.ToString(), // ID de nuestra factura
                    BackUrls = new PreferenceBackUrlsRequest
                    {
                        // Usamos las URLs seguras que acabamos de crear
                        Success = successUrl,
                        Failure = failureUrl,
                        Pending = pendingUrl
                    },
                    //AutoReturn = "approved" // Redirigir automáticamente si es aprobado
                };

                // 6. Enviar la solicitud a la API de Mercado Pago
                var client = new PreferenceClient();
                Preference preference = await client.CreateAsync(request);

                // 7. Redirigir al usuario al link de pago (InitPoint)
                return Redirect(preference.InitPoint);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al contactar Mercado Pago: {ex.Message}";
                return RedirectToAction("Detalles", new { id = facturaId });
            }
        }

        // 7. ACCIONES DE RETORNO (Webhook Simplificado)
        // Mercado Pago redirigirá aquí después del pago

        public async Task<IActionResult> PagoExitoso(string external_reference, string payment_id, string status)
        {
            if (string.IsNullOrEmpty(external_reference))
            {
                return BadRequest("Falta la referencia externa.");
            }

            // external_reference es el ID de nuestra factura
            int facturaId = int.Parse(external_reference); 
            var factura = await _context.Facturas.FindAsync(facturaId);

            if (factura != null && factura.Estado != "Pagada")
            {
                factura.Estado = "Pagada";
                factura.MetodoPago = "Mercado Pago";
                factura.PagoIdExterno = payment_id; // ID de la transacción de MP
                
                _context.Update(factura);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "¡Pago procesado exitosamente!";
            }
            
            return RedirectToAction("Detalles", new { id = facturaId });
        }

        public IActionResult PagoFallido(string external_reference)
        {
            TempData["ErrorMessage"] = "El pago falló o fue cancelado.";
            return RedirectToAction("Detalles", new { id = int.Parse(external_reference) });
        }
        
        public IActionResult PagoPendiente(string external_reference)
        {
            TempData["InfoMessage"] = "El pago está pendiente de procesamiento.";
            return RedirectToAction("Detalles", new { id = int.Parse(external_reference) });
        }
    }
}