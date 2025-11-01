namespace DotIA_Mobile.Services
{
    public static class ApiConfig
    {
        // IMPORTANTE: Para emulador Android, use 10.0.2.2 ao invés de localhost
        // Para dispositivo físico, use o IP da sua máquina na rede local
        public const string BaseUrl = "http://189.46.91.125:5100/api";
        
        // Alternativas:
        // Dispositivo físico: "http://SEU_IP_LOCAL:5100/api" (ex: "http://192.168.1.10:5100/api")
        // iOS Simulator: "http://localhost:5100/api"
        
        public static TimeSpan Timeout => TimeSpan.FromSeconds(30);
    }
}
