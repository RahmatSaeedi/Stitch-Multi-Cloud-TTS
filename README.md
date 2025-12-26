# Stitch Multi-Cloud Text-to-Speech

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor WebAssembly](https://img.shields.io/badge/Blazor-WASM-512BD4)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![MudBlazor](https://img.shields.io/badge/MudBlazor-8.0-594AE2)](https://mudblazor.com/)
[![PWA](https://img.shields.io/badge/PWA-Enabled-5A0FC8)](https://web.dev/progressive-web-apps/)

A privacy-focused, client-side text-to-speech application supporting 5 major cloud TTS providers. Built with Blazor WebAssembly and designed as a Progressive Web App for offline capability.

## ✨ Features

### 🔐 Privacy-First Design
- **100% Client-Side**: All processing, encryption, and API calls happen in your browser
- **AES-GCM Encryption**: API keys encrypted with master password using Web Crypto API
- **No Backend**: Static hosting, no server, no data collection
- **Offline Capable**: Works offline after initial load (PWA)

### 🌐 Multi-Cloud TTS Support (All Implemented!)
- ✅ **Google Cloud TTS**: Neural2, Chirp 3, WaveNet (100+ languages, 1000+ voices)
- ✅ **ElevenLabs**: Premium AI voices with emotional control (70+ languages)
- ✅ **Azure Cognitive Services**: Neural voices (100+ languages)
- ✅ **Amazon Polly**: Standard, Neural, Long-form, Generative (40+ languages)
- ✅ **Deepgram**: Ultra-low latency TTS (<200ms, 9 languages)

### 📄 File Processing
- ✅ **PDF Support**: Extract text from PDFs using PDF.js (up to 100 MB)
- ✅ **Text Files**: .txt, .md, .log, .csv, .json, .xml, .html support
- ✅ **Chapter Detection**: Auto-detect chapters in markdown/PDF files
- ✅ **Smart Chunking**: Sentence-boundary-aware text splitting
- ✅ **Edit Before Synthesis**: Review and edit extracted text

### 🎙️ Advanced Features
- ✅ **Voice Filtering**: Filter 1000+ voices by language, gender, quality
- ✅ **Auto-Selection**: Selected voice updates when filters change
- ✅ **Cost Estimation**: Real-time cost calculation before synthesis
- ✅ **Usage Tracking**: Monitor character usage and costs per provider
- ✅ **Audio Download**: Download synthesized speech as MP3
- ✅ **Multi-Chunk**: Automatically split and merge large texts

### 📱 Progressive Web App
- ✅ **Installable**: Add to home screen on mobile/desktop
- ✅ **Offline Support**: Service worker caches assets
- ✅ **Responsive**: Optimized for mobile, tablet, desktop
- ✅ **Dark/Light Themes**: System-aware theme switching
- ✅ **Shortcuts**: PWA shortcuts for quick actions

## 🚀 Quick Start

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- Modern web browser (Chrome 90+, Firefox 88+, Safari 14+, Edge 79+)
- API key from at least one TTS provider (see [Getting API Keys](#-getting-api-keys))

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd Speech
   ```

2. **Restore dependencies**
   ```bash
   cd SpeechApp
   dotnet restore
   ```

3. **Run the application**
   ```bash
   dotnet watch run
   ```

4. **Open in browser**
   Navigate to `https://localhost:5001`

### Production Build

```bash
cd SpeechApp
dotnet publish -c Release
```

Output: `bin/Release/net10.0/publish/wwwroot/` (ready for static hosting)

## 📖 User Guide

### First-Time Setup

1. **Set Master Password**: Create a strong master password to encrypt your API keys (minimum 8 characters)
   - This password is **never** sent to any server
   - **Cannot be recovered** if forgotten (choose wisely!)
   - Required on every session for security

2. **Add API Keys**: Navigate to "API Keys" page and add at least one provider
   - Keys are encrypted with AES-GCM before storage
   - Use "Test Connection" to verify each key
   - All keys stored locally in your browser

3. **Start Using**: Go to "Home" page to synthesize your first speech!

### Synthesizing Text

1. Enter or paste text in the text area
2. Select a **TTS Provider** from dropdown
3. **Filter Voices** (optional):
   - Language: Narrow by region/language
   - Gender: Male, Female, or Neutral
   - Quality: Standard, Neural, Premium, etc.
4. Select a **Voice** from filtered results
5. Review **estimated cost**
6. Click **"Synthesize Speech"**
7. **Play** audio inline or **Download** as MP3

### Uploading Files

1. Navigate to **"File Upload"** page
2. Drag-and-drop or click to select file (PDF, TXT, MD, etc.)
3. Wait for text extraction (shows progress)
4. **Review and edit** extracted text if needed
5. Choose TTS provider and voice
6. **Optional**: Enable "Split by chapters" for multi-file output
7. Continue to synthesis or download

### Managing Settings

Navigate to **"Settings"** page:
- **Theme**: Light, Dark, or System Default
- **Default Provider**: Auto-select on startup
- **Default Voices**: Set favorite per provider
- **Storage**: Clear cache or reset all settings

## 🔑 Getting API Keys

### Google Cloud TTS
1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create project → Enable "Cloud Text-to-Speech API"
3. Navigate to "Credentials" → Create API Key
4. Copy the key (starts with `AIzaSy...`)

**Pricing**: $0.000016/char (Neural2), $0.000004/char (Standard)
**Free Tier**: 1M characters/month (Standard), 100K characters/month (Neural2)

### ElevenLabs
1. Sign up at [ElevenLabs](https://elevenlabs.io/)
2. Go to Profile → API Keys → Create
3. Copy the key (starts with `sk_...` or similar)

**Pricing**: Varies by plan (~$0.00003/char)
**Free Tier**: 10,000 characters/month

### Azure Cognitive Services
1. Create account at [Azure Portal](https://portal.azure.com/)
2. Create "Cognitive Services" resource → "Speech Services"
3. Go to "Keys and Endpoint" → Copy "Key 1"

**Pricing**: $0.000016/char (Neural)
**Free Tier**: 500,000 characters/month

### Amazon Polly
1. Sign up for [AWS Account](https://aws.amazon.com/)
2. Go to IAM → Users → Create User → Attach `AmazonPollyFullAccess`
3. Create Access Key → Copy both AccessKeyId and SecretAccessKey
4. **Format**: `{AccessKeyId}:{SecretAccessKey}`
   Example: `AKIAIOSFFDNN7EXAMPLE:wJrlrXUtnFEMI/K8MDENG/bPxRfi6YEXAMPLEKEY`

**Pricing**: $0.000016/char (Neural), $0.000004/char (Standard)
**Free Tier**: 5M characters/month (first 12 months)

### Deepgram
1. Sign up at [Deepgram](https://deepgram.com/)
2. Go to API Keys → Create new key
3. Copy the key

**Pricing**: $0.000015/char
**Free Trial**: Available

## 🏗️ Architecture

### Technology Stack
- **Frontend**: Blazor WebAssembly (.NET 10)
- **UI Framework**: MudBlazor 8.0 (Material Design 3)
- **Encryption**: Web Crypto API (AES-GCM, PBKDF2)
- **Storage**: IndexedDB (voice cache) + localStorage (settings)
- **PDF Processing**: PDF.js 3.11
- **PWA**: Service Worker with intelligent caching

### Project Structure

```
SpeechApp/
├── Components/             # Reusable UI components
│   ├── DragDropZone.razor
│   ├── FilePreview.razor
│   └── ErrorLogViewer.razor
├── Models/                 # Data models
│   ├── Voice.cs
│   ├── VoiceConfig.cs
│   ├── SynthesisResult.cs
│   └── Providers/          # Provider-specific models
│       ├── GoogleVoice.cs
│       ├── ElevenLabsVoice.cs
│       ├── AzureVoice.cs
│       ├── PollyVoice.cs
│       └── DeepgramVoice.cs
├── Services/               # Business logic
│   ├── Interfaces/
│   │   ├── ITTSProvider.cs
│   │   ├── IEncryptionService.cs
│   │   ├── IStorageService.cs
│   │   ├── IFileProcessingService.cs
│   │   └── IAudioService.cs
│   ├── EncryptionService.cs
│   ├── StorageService.cs
│   ├── TextChunkingService.cs
│   ├── AudioMergingService.cs
│   ├── FileProcessingService.cs
│   ├── PdfProcessingService.cs
│   ├── TextFileProcessingService.cs
│   ├── AwsSignatureV4.cs
│   └── Providers/
│       ├── GoogleCloudTTSProvider.cs
│       ├── ElevenLabsProvider.cs
│       ├── AzureTTSProvider.cs
│       ├── AmazonPollyProvider.cs
│       └── DeepgramProvider.cs
├── Pages/                  # Razor pages
│   ├── Index.razor
│   ├── FileUpload.razor
│   ├── ApiKeys.razor
│   ├── Settings.razor
│   ├── Usage.razor
│   └── Help.razor
├── Shared/                 # Layout components
│   ├── MainLayout.razor
│   └── NavMenu.razor
└── wwwroot/
    ├── js/
    │   ├── crypto.js       # Web Crypto API
    │   ├── pdf-helper.js   # PDF.js wrapper
    │   └── audio-merger.js # Web Audio API
    ├── icon-192.png
    ├── icon-512.png
    ├── index.html
    ├── manifest.webmanifest
    └── service-worker.published.js
```

### Offline TTS Architecture

1. **Piper TTS (High Quality)**:
   - WebAssembly-compiled neural TTS
   - 50-120 MB per voice model
   - Stored in IndexedDB
   - Natural prosody and expressiveness

2. **eSpeak-NG (Lightweight)**:
   - ~5 MB total footprint
   - 127+ language support
   - Pre-bundled, no downloads
   - Instant availability

### Security Architecture

1. **Master Password Flow:**
   - User creates master password
   - PBKDF2 (100,000 iterations) derives encryption key
   - SHA-256 hash stored for validation

2. **API Key Encryption:**
   - AES-GCM 256-bit encryption
   - Random salt per encryption session
   - Unique IV per encrypted value
   - Base64-encoded storage in LocalStorage

3. **No Backend:**
   - 100% client-side processing
   - API keys never leave the browser
   - Direct calls to TTS provider APIs

## 🚢 Deployment

### GitHub Pages (Production)
**Live URL**: https://rahmatsaeedi.github.io/stitch-multi-cloud-TTS/

Automatic deployment via GitHub Actions on every push to `main` branch.

### Manual Deployment

**Build for production:**
```bash
cd SpeechApp
dotnet publish -c Release -o ../publish
```

**Deploy to static host:**
```bash
# GitHub Pages
cp -r publish/wwwroot/* docs/

# Netlify
netlify deploy --prod --dir=publish/wwwroot

# Azure Static Web Apps
az staticwebapp create --name stitch-tts --resource-group my-rg --source publish/wwwroot

# Vercel
vercel --prod
```

### Configuration Notes

1. **Base Path**: Set to `/stitch-multi-cloud-TTS/` for GitHub Pages subdirectory hosting
2. **HTTPS Required**: Service workers and Web Crypto API require HTTPS
3. **CORS**: Not needed (100% client-side, direct API calls)
4. **Headers**: Consider adding security headers:
   ```
   X-Frame-Options: DENY
   X-Content-Type-Options: nosniff
   Referrer-Policy: no-referrer
   Permissions-Policy: geolocation=(), microphone=(), camera=()
   ```

## 🌐 Browser Support

| Browser | Version | Status | Notes |
|---------|---------|--------|-------|
| Chrome  | 90+     | ✅ Fully Supported | Recommended for best performance |
| Firefox | 88+     | ✅ Fully Supported | Excellent PWA support |
| Safari  | 14+     | ✅ Supported | Limited PWA features on iOS |
| Edge    | 79+     | ✅ Fully Supported | Chromium-based, same as Chrome |

**Required Browser Features:**
- WebAssembly (for offline TTS)
- Web Crypto API (for encryption)
- IndexedDB (for storage)
- Service Workers (for offline support)
- ES2020+ JavaScript

**Known Limitations:**
- Private/incognito mode has restricted storage
- iOS Safari has 50 MB IndexedDB limit (desktop: ~1 GB)
- Older browsers (<2021) not supported

## 🗺️ Roadmap & Future Features

### Completed ✅
- ✅ 5 cloud TTS providers (Google, ElevenLabs, Azure, Polly, Deepgram)
- ✅ Offline TTS (Piper + eSpeak-NG infrastructure)
- ✅ File upload (PDF, text files)
- ✅ Encrypted API key storage
- ✅ PWA with offline support
- ✅ Voice filtering and search
- ✅ Cost estimation and tracking
- ✅ Responsive Material Design UI
- ✅ GitHub Actions CI/CD

### Planned 🔮
- [ ] SSML editor with live preview
- [ ] Voice preview samples
- [ ] Batch file processing
- [ ] Audio format conversion (WAV, OGG)
- [ ] Browser extension version
- [ ] Mobile app (iOS/Android via PWA)
- [ ] Voice cloning (ElevenLabs)
- [ ] Real-time streaming synthesis
- [ ] Pronunciation dictionary
- [ ] Multi-language UI (i18n)

## 🛠️ Technologies Used

- **Framework:** .NET 10 Blazor WebAssembly
- **UI:** MudBlazor 8.0 (Material Design 3)
- **Storage:** Blazored.LocalStorage
- **Encryption:** Web Crypto API (AES-GCM, PBKDF2)
- **Build:** AOT compilation, IL trimming
- **PWA:** Service Workers, Web App Manifest

## 💡 Key Features

- **Security First:** Master password + AES-GCM encryption
- **Privacy:** 100% client-side, no backend
- **Multi-Provider:** Support for 5+ cloud TTS services
- **Offline Ready:** PWA with service worker caching
- **Cost Aware:** Real-time cost estimation
- **Responsive:** Mobile-first Material Design 3 UI
- **Accessible:** WCAG 2.1 AAA target

## 🤝 Support

For issues or questions, please open an issue in the repository.

---

**Built with ❤️ using Blazor WebAssembly and MudBlazor**
