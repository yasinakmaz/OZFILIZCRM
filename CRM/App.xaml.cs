namespace CRM
{
     /// <summary>
    /// MAUI Application ana sınıfı
    /// Uygulama lifecycle management ve global exception handling
    /// </summary>
    public partial class App : Application
    {
        private readonly ILogger<App> _logger;
        private readonly IGlobalErrorHandler _errorHandler;

        public App(ILogger<App> logger, IGlobalErrorHandler errorHandler)
        {
            InitializeComponent();
            
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));

            // **GLOBAL EXCEPTION HANDLING**
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // **SET MAIN PAGE**
            MainPage = new MainPage();

            _logger.LogInformation("🚀 Teknik Servis CRM Uygulaması başlatıldı");
        }

        /// <summary>
        /// Uygulama uyku modundan çıktığında çalışır
        /// </summary>
        protected override void OnResume()
        {
            base.OnResume();
            _logger.LogInformation("📱 Uygulama aktif hale geldi");
        }

        /// <summary>
        /// Uygulama uyku moduna girdiğinde çalışır
        /// </summary>
        protected override void OnSleep()
        {
            base.OnSleep();
            _logger.LogInformation("😴 Uygulama uyku moduna girdi");
        }

        /// <summary>
        /// Unhandled exception handler
        /// </summary>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                _logger.LogCritical(exception, "💥 CRITICAL: Unhandled exception occurred");
                _ = Task.Run(() => _errorHandler.HandleErrorAsync(exception, "UnhandledException"));
            }
        }

        /// <summary>
        /// Unobserved task exception handler
        /// </summary>
        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            _logger.LogError(e.Exception, "⚠️ Unobserved task exception occurred");
            _ = Task.Run(() => _errorHandler.HandleErrorAsync(e.Exception, "UnobservedTaskException"));
            e.SetObserved(); // Mark as observed to prevent app termination
        }
    }
}
