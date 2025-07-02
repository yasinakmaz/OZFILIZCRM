namespace CRM
{
    /// <summary>
/// Ana sayfa sınıfı
/// Blazor WebView'ı barındırır ve uygulama başlatma işlemlerini yönetir
/// </summary>
public partial class MainPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly LoggingService _loggingService;

    public MainPage(AuthService authService, LoggingService loggingService)
    {
        InitializeComponent();
        
        _authService = authService;
        _loggingService = loggingService;
        
        // Blazor WebView loading event'ları
        blazorWebView.BlazorWebViewInitialized += OnBlazorWebViewInitialized;
        blazorWebView.UrlLoading += OnUrlLoading;
    }

    /// <summary>
    /// Sayfa görünür hale geldiğinde çalışır
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        try
        {
            // Loading overlay'ı göster
            loadingOverlay.IsVisible = true;
            
            // Kısa bir loading delay (smooth UX için)
            await Task.Delay(1000);
            
            // Loading overlay'ı gizle
            loadingOverlay.IsVisible = false;
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync(ex, "MAIN_PAGE_LOAD", "MainPage");
            loadingOverlay.IsVisible = false;
        }
    }

    /// <summary>
    /// Blazor WebView başlatıldığında çalışır
    /// </summary>
    private async void OnBlazorWebViewInitialized(object? sender, Microsoft.AspNetCore.Components.WebView.BlazorWebViewInitializedEventArgs e)
    {
        try
        {
            await _loggingService.LogAsync(
                "BLAZOR_WEBVIEW_INITIALIZED",
                "MainPage",
                description: "Blazor WebView başlatıldı",
                userId: _authService.CurrentUser?.Id);
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync(ex, "BLAZOR_WEBVIEW_INIT", "MainPage");
        }
    }

    /// <summary>
    /// URL yüklenirken çalışır
    /// </summary>
    private void OnUrlLoading(object? sender, Microsoft.AspNetCore.Components.WebView.UrlLoadingEventArgs e)
    {
        // External URL'leri engelle (güvenlik için)
        if (!e.Url.Host.Equals("0.0.0.0", StringComparison.OrdinalIgnoreCase) && 
            !e.Url.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            e.UrlLoadingStrategy = Microsoft.AspNetCore.Components.WebView.UrlLoadingStrategy.CancelLoad;
        }
    }
}

}
