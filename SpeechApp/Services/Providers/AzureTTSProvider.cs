using System.Net.Http.Json;
using System.Text;
using System.Xml.Linq;
using SpeechApp.Models;
using SpeechApp.Models.Providers;
using SpeechApp.Services.Interfaces;

namespace SpeechApp.Services.Providers;

public class AzureTTSProvider : ITTSProvider
{
    private readonly HttpClient _httpClient;
    private readonly IStorageService _storageService;
    private string? _apiKey;
    private string? _region;
    private List<Voice>? _cachedVoices;
    private DateTime? _cacheExpiry;

    private const string PROVIDER_ID = "azure";
    private const int MAX_CHARACTERS = 3000;
    private const decimal COST_PER_CHAR = 0.000016m; // Neural voices
    private const int CACHE_DURATION_DAYS = 14;

    public AzureTTSProvider(HttpClient httpClient, IStorageService storageService)
    {
        _httpClient = httpClient;
        _storageService = storageService;
        _region = "eastus"; // Default region
    }

    public ProviderInfo GetProviderInfo()
    {
        return new ProviderInfo
        {
            Id = PROVIDER_ID,
            Name = "Azure",
            DisplayName = "Azure Cognitive Services TTS",
            MaxCharacterLimit = MAX_CHARACTERS,
            RequiresApiKey = true,
            SupportsSSML = true,
            SetupGuideUrl = "/help#azure",
            Health = ProviderHealth.Unknown
        };
    }

    public async Task<SynthesisResult> SynthesizeSpeechAsync(string text, VoiceConfig config, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new SynthesisResult
            {
                Success = false,
                ErrorMessage = "Text cannot be empty"
            };
        }

        if (text.Length > MAX_CHARACTERS)
        {
            return new SynthesisResult
            {
                Success = false,
                ErrorMessage = $"Text exceeds maximum length of {MAX_CHARACTERS} characters"
            };
        }

        if (string.IsNullOrEmpty(_apiKey))
        {
            _apiKey = await _storageService.GetApiKeyAsync(PROVIDER_ID);
            if (string.IsNullOrEmpty(_apiKey))
            {
                return new SynthesisResult
                {
                    Success = false,
                    ErrorMessage = "API key not configured. Please add your Azure API key in settings."
                };
            }
        }

        var startTime = DateTime.UtcNow;

        try
        {
            // Build SSML
            var ssml = BuildSSML(text, config.VoiceId, config.Speed, config.Pitch);

            var url = $"https://{_region}.tts.speech.microsoft.com/cognitiveservices/v1";

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
            httpRequest.Headers.Add("X-Microsoft-OutputFormat", "audio-16khz-128kbitrate-mono-mp3");
            httpRequest.Content = new StringContent(ssml, Encoding.UTF8, "application/ssml+xml");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return new SynthesisResult
                {
                    Success = false,
                    ErrorMessage = $"Azure TTS API error: {response.StatusCode} - {errorContent}"
                };
            }

            var audioData = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            return new SynthesisResult
            {
                Success = true,
                AudioData = audioData,
                CharactersProcessed = text.Length,
                Cost = CalculateCost(text.Length, config),
                Duration = duration
            };
        }
        catch (HttpRequestException ex)
        {
            return new SynthesisResult
            {
                Success = false,
                ErrorMessage = $"Network error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new SynthesisResult
            {
                Success = false,
                ErrorMessage = $"Synthesis failed: {ex.Message}"
            };
        }
    }

    public async Task<List<Voice>> GetVoicesAsync(bool bypassCache = false, CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (!bypassCache && _cachedVoices != null && _cacheExpiry.HasValue && DateTime.UtcNow < _cacheExpiry.Value)
        {
            return _cachedVoices;
        }

        if (string.IsNullOrEmpty(_apiKey))
        {
            _apiKey = await _storageService.GetApiKeyAsync(PROVIDER_ID);
            if (string.IsNullOrEmpty(_apiKey))
            {
                return new List<Voice>();
            }
        }

        try
        {
            var url = $"https://{_region}.tts.speech.microsoft.com/cognitiveservices/voices/list";

            var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
            httpRequest.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new List<Voice>();
            }

            var azureVoices = await response.Content.ReadFromJsonAsync<List<AzureVoice>>(cancellationToken: cancellationToken);

            if (azureVoices == null)
            {
                return new List<Voice>();
            }

            var voices = azureVoices.Select(av => new Voice
            {
                Id = av.ShortName,
                Name = av.DisplayName,
                Language = GetLanguageName(av.Locale),
                LanguageCode = av.Locale,
                Gender = av.Gender?.ToUpper(),
                Quality = av.VoiceType ?? "Neural",
                ProviderId = PROVIDER_ID,
                Metadata = new Dictionary<string, object>
                {
                    ["Locale"] = av.Locale,
                    ["LocalName"] = av.LocalName,
                    ["Styles"] = av.StyleList ?? new List<string>()
                }
            }).ToList();

            // Cache the results
            _cachedVoices = voices;
            _cacheExpiry = DateTime.UtcNow.AddDays(CACHE_DURATION_DAYS);

            return voices;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching Azure voices: {ex.Message}");
            return new List<Voice>();
        }
    }

    public async Task<bool> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        try
        {
            var url = $"https://{_region}.tts.speech.microsoft.com/cognitiveservices/voices/list";

            var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
            httpRequest.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public int GetMaxCharacterLimit()
    {
        return MAX_CHARACTERS;
    }

    public decimal CalculateCost(int characterCount, VoiceConfig? config = null)
    {
        // Azure Neural voices cost
        return characterCount * COST_PER_CHAR;
    }

    public void SetApiKey(string apiKey)
    {
        _apiKey = apiKey;
    }

    public void SetRegion(string region)
    {
        _region = region;
    }

    private string BuildSSML(string text, string voiceName, double speed, double pitch)
    {
        var prosodyRate = speed.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        var prosodyPitch = pitch >= 0
            ? $"+{pitch}Hz"
            : $"{pitch}Hz";

        var ssml = $@"<speak version='1.0' xml:lang='en-US'>
    <voice name='{voiceName}'>
        <prosody rate='{prosodyRate}' pitch='{prosodyPitch}'>
            {System.Security.SecurityElement.Escape(text)}
        </prosody>
    </voice>
</speak>";

        return ssml;
    }

    private string GetLanguageName(string locale)
    {
        var languageNames = new Dictionary<string, string>
        {
            // English variants
            ["en-US"] = "English (US)",
            ["en-GB"] = "English (UK)",
            ["en-AU"] = "English (Australia)",
            ["en-CA"] = "English (Canada)",
            ["en-IN"] = "English (India)",
            ["en-IE"] = "English (Ireland)",
            ["en-NZ"] = "English (New Zealand)",
            ["en-ZA"] = "English (South Africa)",
            ["en-SG"] = "English (Singapore)",
            ["en-HK"] = "English (Hong Kong)",
            ["en-PH"] = "English (Philippines)",

            // Spanish variants
            ["es-ES"] = "Spanish (Spain)",
            ["es-MX"] = "Spanish (Mexico)",
            ["es-AR"] = "Spanish (Argentina)",
            ["es-CO"] = "Spanish (Colombia)",
            ["es-CL"] = "Spanish (Chile)",
            ["es-VE"] = "Spanish (Venezuela)",
            ["es-PE"] = "Spanish (Peru)",
            ["es-CR"] = "Spanish (Costa Rica)",
            ["es-CU"] = "Spanish (Cuba)",
            ["es-DO"] = "Spanish (Dominican Republic)",
            ["es-EC"] = "Spanish (Ecuador)",
            ["es-GT"] = "Spanish (Guatemala)",
            ["es-HN"] = "Spanish (Honduras)",
            ["es-NI"] = "Spanish (Nicaragua)",
            ["es-PA"] = "Spanish (Panama)",
            ["es-PR"] = "Spanish (Puerto Rico)",
            ["es-PY"] = "Spanish (Paraguay)",
            ["es-SV"] = "Spanish (El Salvador)",
            ["es-UY"] = "Spanish (Uruguay)",
            ["es-US"] = "Spanish (US)",
            ["es-BO"] = "Spanish (Bolivia)",
            ["es-GQ"] = "Spanish (Equatorial Guinea)",

            // French variants
            ["fr-FR"] = "French (France)",
            ["fr-CA"] = "French (Canada)",
            ["fr-BE"] = "French (Belgium)",
            ["fr-CH"] = "French (Switzerland)",

            // German variants
            ["de-DE"] = "German (Germany)",
            ["de-AT"] = "German (Austria)",
            ["de-CH"] = "German (Switzerland)",

            // Portuguese variants
            ["pt-BR"] = "Portuguese (Brazil)",
            ["pt-PT"] = "Portuguese (Portugal)",

            // Italian
            ["it-IT"] = "Italian (Italy)",

            // Chinese variants
            ["zh-CN"] = "Chinese (Simplified, China)",
            ["zh-TW"] = "Chinese (Traditional, Taiwan)",
            ["zh-HK"] = "Chinese (Traditional, Hong Kong)",

            // Japanese
            ["ja-JP"] = "Japanese (Japan)",

            // Korean
            ["ko-KR"] = "Korean (Korea)",

            // Arabic variants
            ["ar-SA"] = "Arabic (Saudi Arabia)",
            ["ar-EG"] = "Arabic (Egypt)",
            ["ar-AE"] = "Arabic (UAE)",
            ["ar-BH"] = "Arabic (Bahrain)",
            ["ar-DZ"] = "Arabic (Algeria)",
            ["ar-IQ"] = "Arabic (Iraq)",
            ["ar-JO"] = "Arabic (Jordan)",
            ["ar-KW"] = "Arabic (Kuwait)",
            ["ar-LB"] = "Arabic (Lebanon)",
            ["ar-LY"] = "Arabic (Libya)",
            ["ar-MA"] = "Arabic (Morocco)",
            ["ar-OM"] = "Arabic (Oman)",
            ["ar-QA"] = "Arabic (Qatar)",
            ["ar-SY"] = "Arabic (Syria)",
            ["ar-TN"] = "Arabic (Tunisia)",
            ["ar-YE"] = "Arabic (Yemen)",

            // Hindi
            ["hi-IN"] = "Hindi (India)",

            // Russian
            ["ru-RU"] = "Russian (Russia)",

            // Turkish
            ["tr-TR"] = "Turkish (Turkey)",

            // Dutch variants
            ["nl-NL"] = "Dutch (Netherlands)",
            ["nl-BE"] = "Dutch (Belgium)",

            // Polish
            ["pl-PL"] = "Polish (Poland)",

            // Swedish
            ["sv-SE"] = "Swedish (Sweden)",

            // Norwegian
            ["nb-NO"] = "Norwegian (Norway)",

            // Danish
            ["da-DK"] = "Danish (Denmark)",

            // Finnish
            ["fi-FI"] = "Finnish (Finland)",

            // Greek
            ["el-GR"] = "Greek (Greece)",

            // Hebrew
            ["he-IL"] = "Hebrew (Israel)",

            // Thai
            ["th-TH"] = "Thai (Thailand)",

            // Vietnamese
            ["vi-VN"] = "Vietnamese (Vietnam)",

            // Indonesian
            ["id-ID"] = "Indonesian (Indonesia)",

            // Malay
            ["ms-MY"] = "Malay (Malaysia)",

            // Czech
            ["cs-CZ"] = "Czech (Czechia)",

            // Hungarian
            ["hu-HU"] = "Hungarian (Hungary)",

            // Romanian
            ["ro-RO"] = "Romanian (Romania)",

            // Slovak
            ["sk-SK"] = "Slovak (Slovakia)",

            // Bulgarian
            ["bg-BG"] = "Bulgarian (Bulgaria)",

            // Croatian
            ["hr-HR"] = "Croatian (Croatia)",

            // Serbian
            ["sr-RS"] = "Serbian (Serbia)",

            // Slovenian
            ["sl-SI"] = "Slovenian (Slovenia)",

            // Ukrainian
            ["uk-UA"] = "Ukrainian (Ukraine)",

            // Catalan
            ["ca-ES"] = "Catalan (Spain)",

            // Galician
            ["gl-ES"] = "Galician (Spain)",

            // Basque
            ["eu-ES"] = "Basque (Spain)",

            // Afrikaans
            ["af-ZA"] = "Afrikaans (South Africa)",

            // Amharic
            ["am-ET"] = "Amharic (Ethiopia)",

            // Bengali
            ["bn-IN"] = "Bengali (India)",
            ["bn-BD"] = "Bengali (Bangladesh)",

            // Filipino
            ["fil-PH"] = "Filipino (Philippines)",

            // Gujarati
            ["gu-IN"] = "Gujarati (India)",

            // Icelandic
            ["is-IS"] = "Icelandic (Iceland)",

            // Kannada
            ["kn-IN"] = "Kannada (India)",

            // Khmer
            ["km-KH"] = "Khmer (Cambodia)",

            // Lao
            ["lo-LA"] = "Lao (Laos)",

            // Latvian
            ["lv-LV"] = "Latvian (Latvia)",

            // Lithuanian
            ["lt-LT"] = "Lithuanian (Lithuania)",

            // Malayalam
            ["ml-IN"] = "Malayalam (India)",

            // Marathi
            ["mr-IN"] = "Marathi (India)",

            // Nepali
            ["ne-NP"] = "Nepali (Nepal)",

            // Persian
            ["fa-IR"] = "Persian (Iran)",

            // Sinhala
            ["si-LK"] = "Sinhala (Sri Lanka)",

            // Swahili
            ["sw-KE"] = "Swahili (Kenya)",
            ["sw-TZ"] = "Swahili (Tanzania)",

            // Tamil
            ["ta-IN"] = "Tamil (India)",
            ["ta-LK"] = "Tamil (Sri Lanka)",
            ["ta-SG"] = "Tamil (Singapore)",
            ["ta-MY"] = "Tamil (Malaysia)",

            // Telugu
            ["te-IN"] = "Telugu (India)",

            // Urdu
            ["ur-PK"] = "Urdu (Pakistan)",
            ["ur-IN"] = "Urdu (India)",

            // Uzbek
            ["uz-UZ"] = "Uzbek (Uzbekistan)",

            // Welsh
            ["cy-GB"] = "Welsh (UK)",

            // Irish
            ["ga-IE"] = "Irish (Ireland)",

            // Maltese
            ["mt-MT"] = "Maltese (Malta)",

            // Macedonian
            ["mk-MK"] = "Macedonian (North Macedonia)",

            // Albanian
            ["sq-AL"] = "Albanian (Albania)",

            // Bosnian
            ["bs-BA"] = "Bosnian (Bosnia)",

            // Estonian
            ["et-EE"] = "Estonian (Estonia)",

            // Kazakh
            ["kk-KZ"] = "Kazakh (Kazakhstan)",

            // Mongolian
            ["mn-MN"] = "Mongolian (Mongolia)",

            // Pashto
            ["ps-AF"] = "Pashto (Afghanistan)",

            // Somali
            ["so-SO"] = "Somali (Somalia)",

            // Zulu
            ["zu-ZA"] = "Zulu (South Africa)",

            // Azerbaijani
            ["az-AZ"] = "Azerbaijani (Azerbaijan)",

            // Georgian
            ["ka-GE"] = "Georgian (Georgia)",

            // Javanese
            ["jv-ID"] = "Javanese (Indonesia)",

            // Sundanese
            ["su-ID"] = "Sundanese (Indonesia)"
        };

        return languageNames.TryGetValue(locale, out var name) ? name : locale;
    }
}
