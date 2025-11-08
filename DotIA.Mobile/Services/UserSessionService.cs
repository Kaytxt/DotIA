namespace DotIA.Mobile.Services
{
    public class UserSessionService
    {
        public int? UsuarioId { get; set; }
        public string? Nome { get; set; }
        public string? Email { get; set; }
        public string? TipoUsuario { get; set; } // "Solicitante", "Tecnico", "Gerente"

        public bool IsLoggedIn => UsuarioId.HasValue;

        public bool IsSolicitante => TipoUsuario == "Solicitante";
        public bool IsTecnico => TipoUsuario == "Tecnico";
        public bool IsGerente => TipoUsuario == "Gerente";

        public void SetUserSession(int usuarioId, string nome, string email, string tipoUsuario)
        {
            UsuarioId = usuarioId;
            Nome = nome;
            Email = email;
            TipoUsuario = tipoUsuario;
        }

        public void ClearSession()
        {
            UsuarioId = null;
            Nome = null;
            Email = null;
            TipoUsuario = null;
        }
    }
}
