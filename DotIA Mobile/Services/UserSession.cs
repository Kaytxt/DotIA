namespace DotIA_Mobile.Services
{
    public static class UserSession
    {
        public static int? UsuarioId { get; set; }
        public static string? Nome { get; set; }
        public static string? TipoUsuario { get; set; }
        public static string? Email { get; set; }

        public static bool IsLoggedIn => UsuarioId.HasValue;

        public static void Login(int usuarioId, string nome, string tipoUsuario, string email)
        {
            UsuarioId = usuarioId;
            Nome = nome;
            TipoUsuario = tipoUsuario;
            Email = email;
        }

        public static void Logout()
        {
            UsuarioId = null;
            Nome = null;
            TipoUsuario = null;
            Email = null;
        }
    }
}
