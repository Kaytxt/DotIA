using DotIA.Mobile.Views;

namespace DotIA.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Registrar rotas para navegação
        Routing.RegisterRoute("LoginPage", typeof(LoginPage));
        Routing.RegisterRoute("RegistroPage", typeof(RegistroPage));
        Routing.RegisterRoute("ChatPage", typeof(ChatPage));
        Routing.RegisterRoute("TecnicoPage", typeof(TecnicoPage));
        Routing.RegisterRoute("GerentePage", typeof(GerentePage));
    }
}
