using Microsoft.Extensions.Logging;

namespace CRM
{
    /// <summary>
    /// Ana sayfa - Blazor WebView container
    /// </summary>
    public partial class MainPage : ContentPage
    {
        private readonly ILogger<MainPage> _logger;

        public MainPage()
        {
            InitializeComponent();

            // Dependency injection through Handler
            _logger = Handler?.MauiContext?.Services?.GetService<ILogger<MainPage>>() ??
                     Microsoft.Extensions.Logging.Abstractions.NullLogger<MainPage>.Instance;

            _logger.LogInformation("📄 MainPage initialized");

            // **BLAZOR WEBVIEW EVENT HANDLERS**
            blazorWebView.BlazorWebViewInitialized += OnBlazorWebViewInitialized;
            blazorWebView.UrlLoading += OnUrlLoading;
        }

        /// <summary>
        /// Blazor WebView başlatıldığında çalışır
        /// </summary>
        private void OnBlazorWebViewInitialized(object? sender, Microsoft.AspNetCore.Components.WebView.BlazorWebViewInitializedEventArgs e)
        {
            _logger.LogInformation("🌐 Blazor WebView initialized successfully");

#if DEBUG
            // Development ortamında web developer tools'u aç
            if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                e.WebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                e.WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            }
#endif
        }

        /// <summary>
        /// URL yüklenirken çalışır
        /// </summary>
        private void OnUrlLoading(object? sender, Microsoft.AspNetCore.Components.WebView.UrlLoadingEventArgs e)
        {
            _logger.LogDebug("🔗 Loading URL: {Url}", e.Url);

            // **EXTERNAL URL HANDLING**
            if (e.Url.Scheme != "https" && e.Url.Scheme != "http")
            {
                e.UrlLoadingStrategy = Microsoft.AspNetCore.Components.WebView.UrlLoadingStrategy.OpenInWebView;
            }
        }

        /// <summary>
        /// Sayfa görünür hale geldiğinde çalışır
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            _logger.LogDebug("MainPage appearing");
        }

        /// <summary>
        /// Sayfa gizlendiğinde çalışır
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _logger.LogDebug("MainPage disappearing");
        }
    }
}
