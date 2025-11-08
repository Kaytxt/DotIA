using Microsoft.Extensions.Logging;
using DotIA.Mobile.Services;
using DotIA.Mobile.ViewModels;
using DotIA.Mobile.Views;

namespace DotIA.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>();
            // Fontes removidas temporariamente - usar fontes padrão do sistema
            //.ConfigureFonts(fonts =>
            //{
            //    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            //    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            //});

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Registrar Services
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<UserSessionService>();

        // Registrar ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegistroViewModel>();
        builder.Services.AddTransient<ChatViewModel>();
        builder.Services.AddTransient<TecnicoViewModel>();
        builder.Services.AddTransient<GerenteViewModel>();
        builder.Services.AddTransient<AbrirTicketViewModel>();

        // Registrar Views
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegistroPage>();
        builder.Services.AddTransient<ChatPage>();
        builder.Services.AddTransient<TecnicoPage>();
        builder.Services.AddTransient<GerentePage>();
        builder.Services.AddTransient<AbrirTicketPage>();

        return builder.Build();
    }
}
