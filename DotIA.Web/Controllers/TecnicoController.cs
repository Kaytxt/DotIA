using Microsoft.AspNetCore.Mvc;
using DotIA.Web.Services;

namespace DotIA.Web.Controllers
{
    public class TecnicoController : Controller
    {
        private readonly ApiClient _apiClient;

        public TecnicoController(HttpClient httpClient, IConfiguration configuration)
        {
            _apiClient = new ApiClient(httpClient, configuration);
        }

        public IActionResult Index()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
                return RedirectToAction("Index", "Login");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ObterTickets()
        {
            var tickets = await _apiClient.ObterTicketsPendentesAsync();
            return Json(tickets);
        }

        [HttpPost]
        public async Task<IActionResult> ResolverTicket([FromBody] ResolverRequest request)
        {
            var sucesso = await _apiClient.ResolverTicketAsync(
                request.TicketId,
                request.Solucao,
                request.MarcarComoResolvido // ✅ ADICIONADO
            );
            return Json(new { sucesso });
        }
    }

    public class ResolverRequest
    {
        public int TicketId { get; set; }
        public string Solucao { get; set; } = string.Empty;
        public bool MarcarComoResolvido { get; set; } // ✅ ADICIONADO
    }
}