using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Blazored.LocalStorage;
using SpeechApp;
using SpeechApp.Services;
using SpeechApp.Services.Interfaces;
using SpeechApp.Services.Providers;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// MudBlazor
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
});

// Local Storage
builder.Services.AddBlazoredLocalStorage();

// Core Services
builder.Services.AddSingleton<AppStateService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddScoped<IUsageHistoryService, UsageHistoryService>();

// Error Handling & Resilience Services
builder.Services.AddScoped<IRateLimitService, RateLimitService>();
builder.Services.AddScoped<IRetryService, RetryService>();
builder.Services.AddScoped<IErrorLoggingService, ErrorLoggingService>();

// TTS Providers (Cloud)
builder.Services.AddScoped<GoogleCloudTTSProvider>();
builder.Services.AddScoped<ElevenLabsProvider>();
builder.Services.AddScoped<DeepgramProvider>();
builder.Services.AddScoped<AzureTTSProvider>();
builder.Services.AddScoped<AmazonPollyProvider>();

// TTS Providers (Offline)
builder.Services.AddScoped<SpeechApp.Services.Offline.PiperTTSService>();
builder.Services.AddScoped<SpeechApp.Services.Offline.ESpeakNGService>();

// Provider Manager
builder.Services.AddScoped<ITTSProviderManager, TTSProviderManager>();

// Text and Audio Services
builder.Services.AddScoped<ITextChunkingService, TextChunkingService>();
builder.Services.AddScoped<IAudioService, AudioMergingService>();

// File Processing Services
builder.Services.AddScoped<PdfProcessingService>();
builder.Services.AddScoped<TextFileProcessingService>();
builder.Services.AddScoped<IFileProcessingService, FileProcessingService>();

await builder.Build().RunAsync();
