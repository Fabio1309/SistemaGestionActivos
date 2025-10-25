using Microsoft.AspNetCore.Mvc;
using QRCoder; // Librería para QR
using IronPdf; // Librería para PDF
using System.Drawing;
using System.IO;
using SistemaGestionActivos.Data;
using SistemaGestionActivos.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SistemaGestionActivos.Controllers
{
    public class QrGeneratorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QrGeneratorController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /QrGenerator/GenerateQrImage/5
        // Este método genera y devuelve la imagen PNG del código QR.
        public IActionResult GenerateQrImage(int id)
        {
            // 1. Construir la URL que contendrá el QR.
            // Esta URL apunta al perfil detallado del activo.
            var url = Url.Action("Detalles", "Activos", new { id = id }, Request.Scheme);

            if (url == null)
            {
                return BadRequest("No se pudo generar la URL para el QR.");
            }

            // 2. Generar el código QR
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeImage = qrCode.GetGraphic(20); // El número es el tamaño de los píxeles

            // 3. Devolver la imagen como un archivo
            return File(qrCodeImage, "image/png");
        }

        // GET: /QrGenerator/GeneratePdfLabel/5
        // Este método genera un PDF simple que contiene el QR y datos del activo.
        public async Task<IActionResult> GeneratePdfLabel(int id)
        {
            var activo = await _context.Activos.FindAsync(id);
            if (activo == null)
            {
                return NotFound();
            }

            // URL para la imagen del QR que insertaremos en el PDF
            var qrImageUrl = Url.Action("GenerateQrImage", "QrGenerator", new { id = id }, Request.Scheme);
            
            // Instalar Chrome Renderer si es la primera vez (IronPDF lo requiere)
            IronPdf.Installation.Initialize();
            
            var renderer = new ChromePdfRenderer();

            // 2. Crear el contenido HTML para el PDF
            var htmlContent = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: sans-serif; text-align: center; border: 2px solid black; padding: 20px; width: 300px; }}
                        h3 {{ margin: 0; }}
                        p {{ margin: 5px 0; }}
                        img {{ width: 200px; height: 200px; margin-top: 10px; }}
                    </style>
                </head>
                <body>
                    <h3>{activo.nom_act}</h3>
                    <p><strong>Código:</strong> {activo.cod_act}</p>
                    <p><strong>N/S:</strong> {activo.num_serie ?? "N/A"}</p>
                    <img src='{qrImageUrl}' alt='Código QR' />
                </body>
                </html>
            ";

            // 3. Renderizar el HTML a PDF
            var pdf = renderer.RenderHtmlAsPdf(htmlContent);

            // 4. Devolver el PDF como un archivo descargable
            return File(pdf.BinaryData, "application/pdf", $"Etiqueta-Activo-{activo.cod_act}.pdf");
        }
    }
}