using DotIA_Mobile.Views;

namespace DotIA_Mobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Começar com a página de Login
            MainPage = new NavigationPage(new LoginPage())
            {
                BarBackgroundColor = Color.FromArgb("#2563eb"),
                BarTextColor = Colors.White
            };
        }
    }
}
