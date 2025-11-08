using DotIA.Mobile.Views;

namespace DotIA.Mobile;

public partial class App : Application
{
    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        // Criar AppShell e configurar página inicial
        var shell = new AppShell();

        // Obter LoginPage via DI e configurar como página inicial
        var loginPage = serviceProvider.GetRequiredService<LoginPage>();
        shell.CurrentItem = new ShellContent { Content = loginPage };

        MainPage = shell;
    }
}
