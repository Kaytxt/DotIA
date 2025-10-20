using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DotIA.API.Data;
using DotIA.API.Models;

namespace DotIA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Verifica se � um solicitante
                var solicitante = await _context.Solicitantes
                    .FirstOrDefaultAsync(s => s.Email == request.Email && s.Senha == request.Senha);

                if (solicitante != null)
                {
                    return Ok(new LoginResponse
                    {
                        Sucesso = true,
                        TipoUsuario = "solicitante",
                        UsuarioId = solicitante.Id,
                        Nome = solicitante.Nome,
                        Mensagem = "Login realizado com sucesso!"
                    });
                }

                // Verifica se � um t�cnico
                var tecnico = await _context.Tecnicos
                    .FirstOrDefaultAsync(t => t.Email == request.Email && t.Senha == request.Senha);

                if (tecnico != null)
                {
                    return Ok(new LoginResponse
                    {
                        Sucesso = true,
                        TipoUsuario = "tecnico",
                        UsuarioId = tecnico.Id,
                        Nome = tecnico.Nome,
                        Mensagem = "Login realizado com sucesso!"
                    });
                }

                // Credenciais inv�lidas
                return Unauthorized(new LoginResponse
                {
                    Sucesso = false,
                    Mensagem = "Email ou senha incorretos"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new LoginResponse
                {
                    Sucesso = false,
                    Mensagem = $"Erro no servidor: {ex.Message}"
                });
            }
        }
    }
}