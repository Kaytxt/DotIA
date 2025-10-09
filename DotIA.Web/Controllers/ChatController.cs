using Microsoft.AspNetCore.Mvc;
using DotIA.Web.Services;

namespace DotIA.Web.Controllers
{
    public class ChatController : Controller
    {
        private readonly ApiClient _apiClient;

        public ChatController(HttpClient httpClient, IConfiguration configuration)
        {
            _apiClient = new ApiClient(httpClient, configuration);
        }

        public IActionResult Index()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
                return RedirectToAction("Index", "Login");

            ViewBag.Nome = HttpContext.Session.GetString("Nome");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> EnviarPergunta([FromBody] PerguntaRequest request)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
                return Unauthorized();

            var resposta = await _apiClient.EnviarPerguntaAsync(usuarioId.Value, request.Pergunta);
            return Json(resposta);
        }

        [HttpGet]
        public async Task<IActionResult> ObterHistorico()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
                return Unauthorized();

            var historico = await _apiClient.ObterHistoricoAsync(usuarioId.Value);
            return Json(historico);
        }

        [HttpPost]
        public async Task<IActionResult> AvaliarResposta([FromBody] AvaliacaoRequest request)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
                return Unauthorized();

            var sucesso = await _apiClient.AvaliarRespostaAsync(
                usuarioId.Value,
                request.Pergunta,
                request.Resposta,
                request.FoiUtil
            );

            return Json(new { sucesso });
        }
    }

    public class PerguntaRequest
    {
        public string Pergunta { get; set; }
    }

    public class AvaliacaoRequest
    {
        public string Pergunta { get; set; }
        public string Resposta { get; set; }
        public bool FoiUtil { get; set; }
    }
}