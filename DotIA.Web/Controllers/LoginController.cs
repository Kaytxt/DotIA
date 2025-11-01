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
                var resultado = await _apiClient.LoginAsync(email, senha);

                if (resultado.Sucesso)
                {
                    HttpContext.Session.SetInt32("UsuarioId", resultado.UsuarioId);
                    HttpContext.Session.SetString("Nome", resultado.Nome);
                    HttpContext.Session.SetString("TipoUsuario", resultado.TipoUsuario);

                    if (resultado.TipoUsuario == "Tecnico")
                    {
                        return RedirectToAction("Index", "Tecnico");
                    }
                    else if (resultado.TipoUsuario == "Gerente")
                    {
                        return RedirectToAction("Index", "Gerente");
                    }
                    else
                    {
                        return RedirectToAction("Index", "Chat");
                    }
                }
                else
                {
                    ViewBag.Erro = resultado.Mensagem;
                    return View("Index");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Erro = "Erro ao conectar com o servidor: " + ex.Message;
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