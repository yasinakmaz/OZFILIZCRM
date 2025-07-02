namespace CRM
{
    public partial class App : Application
    {
        private readonly AuthService _authService;
        private readonly LoggingService _loggingService;

        public App(AuthService authService, LoggingService loggingService)
        {
            InitializeComponent();

            _authService = authService;
            _loggingService = loggingService;
        }

        protected override Window CreateWindow(IActivationState? activationState) 
        { 
            return new Window(new MainPage()) { Title = "CRM" }; 
        }

        /// <summary>
        /// Uygulama başlatıldığında çalışır
        /// Session restore ve initial setup işlemleri
        /// </summary>
        protected override async void OnStart()
        {
            try
            {
                // Önceki oturumu geri yüklemeye çalış
                await _authService.RestoreSessionAsync();

                // Uygulama başlatma log'u
                await _loggingService.LogAsync(
                    "APP_START",
                    "Application",
                    description: "Uygulama başlatıldı",
                    userId: _authService.CurrentUser?.Id);
            }
            catch (Exception ex)
            {
                // Başlatma hatalarını logla
                await _loggingService.LogErrorAsync(ex, "APP_START", "Application");
            }
        }

        /// <summary>
        /// Uygulama sleep moduna geçtiğinde çalışır
        /// </summary>
        protected override async void OnSleep()
        {
            try
            {
                await _loggingService.LogAsync(
                    "APP_SLEEP",
                    "Application",
                    description: "Uygulama sleep moduna geçti",
                    userId: _authService.CurrentUser?.Id);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "APP_SLEEP", "Application");
            }
        }

        /// <summary>
        /// Uygulama sleep modundan çıktığında çalışır
        /// </summary>
        protected override async void OnResume()
        {
            try
            {
                await _loggingService.LogAsync(
                    "APP_RESUME",
                    "Application",
                    description: "Uygulama sleep modundan çıktı",
                    userId: _authService.CurrentUser?.Id);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "APP_RESUME", "Application");
            }
        }
    }
}
