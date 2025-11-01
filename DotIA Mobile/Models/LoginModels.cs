namespace DotIA_Mobile.Models
{
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public string? TipoUsuario { get; set; }
        public int? UsuarioId { get; set; }
        public string? Nome { get; set; }
    }

    public class RegistroRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public string ConfirmacaoSenha { get; set; } = string.Empty;
        public int IdDepartamento { get; set; }
    }

    public class RegistroResponse
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public int UsuarioId { get; set; }
    }

    public class DepartamentoDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
    }
}
