using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DotIA.API.Data;
using DotIA.API.Models;  
using TabelasDoBanco;

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
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Senha))
                {
                    return BadRequest(new LoginResponse
                    {
                        Sucesso = false,
                        Mensagem = "Email e senha são obrigatórios"
                    });
                }

                var solicitante = await _context.Solicitantes
                    .FirstOrDefaultAsync(s => s.Email == request.Email && s.Senha == request.Senha);

                if (solicitante != null)
                {
                    return Ok(new LoginResponse
                    {
                        Sucesso = true,
                        TipoUsuario = "Solicitante",
                        UsuarioId = solicitante.Id,
                        Nome = solicitante.Nome,
                        Mensagem = "Login realizado com sucesso!"
                    });
                }

                var tecnico = await _context.Tecnicos
                    .FirstOrDefaultAsync(t => t.Email == request.Email && t.Senha == request.Senha);

                if (tecnico != null)
                {
                    return Ok(new LoginResponse
                    {
                        Sucesso = true,
                        TipoUsuario = "Tecnico",
                        UsuarioId = tecnico.Id,
                        Nome = tecnico.Nome,
                        Mensagem = "Login realizado com sucesso!"
                    });
                }

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
                    Mensagem = $"Erro ao fazer login: {ex.Message}"
                });
            }
        }
    }
}