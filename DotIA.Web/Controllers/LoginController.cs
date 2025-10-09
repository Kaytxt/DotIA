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

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string email, string senha)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(senha))
            {
                ViewBag.Erro = "Preencha todos os campos!";
                return View();
            }

            var resultado = await _apiClient.LoginAsync(email, senha);

            if (resultado != null && resultado.Sucesso)
            {
                HttpContext.Session.SetInt32("UsuarioId", resultado.UsuarioId);
                HttpContext.Session.SetString("Nome", resultado.Nome);
                HttpContext.Session.SetString("TipoUsuario", resultado.TipoUsuario);

                if (resultado.TipoUsuario == "Solicitante")
                {
                    return RedirectToAction("Index", "Chat");
                }
                else
                {
                    return RedirectToAction("Index", "Tecnico");
                }
            }

            ViewBag.Erro = resultado?.Mensagem ?? "Email ou senha inválidos";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}