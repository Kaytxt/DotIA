using Microsoft.AspNetCore.Mvc;
using DotIA.Web.Services;

namespace DotIA.Web.Controllers
{
    public class LoginController : Controller
    {
        private readonly ApiClient _apiClient;

        public LoginController(HttpClient httpClient, IConfiguration configuration)
        {
            _apiClient = new ApiClient(httpClient, configuration);
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Entrar(string email, string senha)
        {
            try
            {
                var resposta = await _apiClient.LoginAsync(email, senha);

                if (resposta.Sucesso)
                {
                    // Salvar dados na sessão
                    HttpContext.Session.SetInt32("UsuarioId", resposta.UsuarioId);
                    HttpContext.Session.SetString("Nome", resposta.Nome);
                    HttpContext.Session.SetString("TipoUsuario", resposta.TipoUsuario);

                    // Redirecionar baseado no tipo de usuário
                    if (resposta.TipoUsuario == "Solicitante")
                    {
                        return RedirectToAction("Index", "Chat");
                    }
                    else if (resposta.TipoUsuario == "Tecnico")
                    {
                        return RedirectToAction("Index", "Tecnico");
                    }
                }

                ViewBag.Erro = resposta.Mensagem;
                return View("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Erro = $"Erro ao conectar com a API: {ex.Message}";
                return View("Index");
            }
        }

        public IActionResult Sair()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}