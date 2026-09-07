namespace CardMaker.AI.Models;

/// <summary>
/// Registro centralizzato e immutabile dei profili di modelli AI ufficialmente supportati per inferenza locale.
/// Copre le fasce hardware 4 GB, 8 GB, 16 GB, 32 GB tramite la famiglia Gemma (Google).
/// </summary>
public static class AiModelRegistry
{
    public const string AutoModelKey = "Auto";

    public static readonly AiModelDefinition Gemma2B = new(
        Key: "gemma-2-2b",
        DisplayName: "Gemma 2 2B Instruct (Q4_K_M)",
        FileName: "gemma-2-2b-it-Q4_K_M.gguf",
        RecommendedRamGb: 4,
        EstimatedMemoryUsageGb: 1.8,
        Quantization: "Q4_K_M",
        Family: "Gemma",
        DownloadUrl: "https://huggingface.co/bartowski/gemma-2-2b-it-GGUF",
        DownloadUrlDirect: "https://huggingface.co/bartowski/gemma-2-2b-it-GGUF/resolve/main/gemma-2-2b-it-Q4_K_M.gguf",
        ExpectedSizeBytes: 1_708_582_752L,
        Description: "Ottimizzato per macchine a basse risorse (>= 4 GB RAM). Veloce e compatto.",
        DefaultContextSize: 2048,
        MinFreeDiskSpaceBytes: 2_500_000_000L,
        Capability: AiModelCapability.Text,
        Format: "GGUF",
        RecommendedHardware: "CPU x64 / ARM64 (>= 4 GB RAM)");

    public static readonly AiModelDefinition Gemma4B = new(
        Key: "gemma-3-4b",
        DisplayName: "Gemma 3 4B Instruct (Q4_K_M)",
        FileName: "google_gemma-3-4b-it-Q4_K_M.gguf",
        RecommendedRamGb: 8,
        EstimatedMemoryUsageGb: 3.2,
        Quantization: "Q4_K_M",
        Family: "Gemma",
        DownloadUrl: "https://huggingface.co/bartowski/google_gemma-3-4b-it-GGUF",
        DownloadUrlDirect: "https://huggingface.co/bartowski/google_gemma-3-4b-it-GGUF/resolve/main/google_gemma-3-4b-it-Q4_K_M.gguf",
        ExpectedSizeBytes: 2_489_758_112L,
        Description: "Profilo bilanciato consigliato per macchine standard (>= 8 GB RAM). Eccellente aderenza al formato JSON.",
        DefaultContextSize: 2048,
        MinFreeDiskSpaceBytes: 4_000_000_000L,
        Capability: AiModelCapability.Text,
        Format: "GGUF",
        RecommendedHardware: "CPU x64 / ARM64 (>= 8 GB RAM)");

    public static readonly AiModelDefinition Gemma9B = new(
        Key: "gemma-2-9b",
        DisplayName: "Gemma 2 9B Instruct (Q4_K_M)",
        FileName: "gemma-2-9b-it-Q4_K_M.gguf",
        RecommendedRamGb: 16,
        EstimatedMemoryUsageGb: 6.2,
        Quantization: "Q4_K_M",
        Family: "Gemma",
        DownloadUrl: "https://huggingface.co/bartowski/gemma-2-9b-it-GGUF",
        DownloadUrlDirect: "https://huggingface.co/bartowski/gemma-2-9b-it-GGUF/resolve/main/gemma-2-9b-it-Q4_K_M.gguf",
        ExpectedSizeBytes: 5_761_057_728L,
        Description: "Profilo avanzato ad alta fedeltà semantica (>= 16 GB RAM). Consigliato per testi ricchi ed elaborati.",
        DefaultContextSize: 4096,
        MinFreeDiskSpaceBytes: 8_000_000_000L,
        Capability: AiModelCapability.Text,
        Format: "GGUF",
        RecommendedHardware: "CPU x64 multi-core / AVX2 (>= 16 GB RAM)");

    public static readonly AiModelDefinition Gemma27B = new(
        Key: "gemma-2-27b",
        DisplayName: "Gemma 2 27B Instruct (Q4_K_M)",
        FileName: "gemma-2-27b-it-Q4_K_M.gguf",
        RecommendedRamGb: 32,
        EstimatedMemoryUsageGb: 17.5,
        Quantization: "Q4_K_M",
        Family: "Gemma",
        DownloadUrl: "https://huggingface.co/bartowski/gemma-2-27b-it-GGUF",
        DownloadUrlDirect: "https://huggingface.co/bartowski/gemma-2-27b-it-GGUF/resolve/main/gemma-2-27b-it-Q4_K_M.gguf",
        ExpectedSizeBytes: 16_645_381_632L,
        Description: "Massima potenza e creatività per workstation e desktop potenti (>= 32 GB RAM).",
        DefaultContextSize: 4096,
        MinFreeDiskSpaceBytes: 24_000_000_000L,
        Capability: AiModelCapability.Text,
        Format: "GGUF",
        RecommendedHardware: "Workstation CPU multi-threaded / GPU acceleration (>= 32 GB RAM)");

    public static readonly AiModelDefinition Sd15Turbo = new(
        Key: "sd-1.5-turbo",
        DisplayName: "Stable Diffusion 1.5 Turbo (Q8_0)",
        FileName: "sd_turbo-f16-q8_0.gguf",
        RecommendedRamGb: 4,
        EstimatedMemoryUsageGb: 1.2,
        Quantization: "Q8_0",
        Family: "StableDiffusion",
        DownloadUrl: "https://huggingface.co/Green-Sky/SD-Turbo-GGUF",
        DownloadUrlDirect: "https://huggingface.co/Green-Sky/SD-Turbo-GGUF/resolve/main/sd_turbo-f16-q8_0.gguf",
        ExpectedSizeBytes: 2_023_745_376L,
        Description: "Leggero e fulmineo (1-4 step). Consigliato per macchine a basse risorse (>= 4 GB RAM) ed esecuzione CPU.",
        DefaultSteps: 4,
        MinFreeDiskSpaceBytes: 2_500_000_000L,
        Capability: AiModelCapability.Image,
        Format: "GGUF",
        RecommendedVramGb: 2.0,
        RecommendedHardware: "CPU x64 / Vulkan / CUDA (>= 4 GB RAM)");

    public static readonly AiModelDefinition DreamShaper8 = new(
        Key: "dreamshaper-8",
        DisplayName: "DreamShaper 8 SD 1.5 LCM (IQ4_NL)",
        FileName: "dreamshaper_8LCM-iq4_nl.gguf",
        RecommendedRamGb: 8,
        EstimatedMemoryUsageGb: 1.8,
        Quantization: "IQ4_NL",
        Family: "StableDiffusion",
        DownloadUrl: "https://huggingface.co/stduhpf/dreamshaper-8LCM-im-GGUF-sdcpp",
        DownloadUrlDirect: "https://huggingface.co/stduhpf/dreamshaper-8LCM-im-GGUF-sdcpp/resolve/main/dreamshaper_8LCM-iq4_nl.gguf",
        ExpectedSizeBytes: 1_570_742_432L,
        Description: "Stile artistico fantasy/TCG eccellente in 4-8 step. Consigliato per configurazioni standard (>= 8 GB RAM).",
        DefaultSteps: 8,
        MinFreeDiskSpaceBytes: 2_500_000_000L,
        Capability: AiModelCapability.Image,
        Format: "GGUF",
        RecommendedVramGb: 3.0,
        RecommendedHardware: "CPU / GPU CUDA / DirectML / Metal (>= 8 GB RAM)");

    public static readonly AiModelDefinition SdxlLightning = new(
        Key: "sdxl-lightning-4step",
        DisplayName: "SDXL Lightning 4-Step (Q4_0)",
        FileName: "sdxl_lightning_4step.q4_0.gguf",
        RecommendedRamGb: 16,
        EstimatedMemoryUsageGb: 2.8,
        Quantization: "Q4_0",
        Family: "StableDiffusionXL",
        DownloadUrl: "https://huggingface.co/mzwing/SDXL-Lightning-GGUF",
        DownloadUrlDirect: "https://huggingface.co/mzwing/SDXL-Lightning-GGUF/resolve/main/sdxl_lightning_4step.q4_0.gguf",
        ExpectedSizeBytes: 2_584_858_432L,
        Description: "Qualita elevata e risoluzione nativa 768x768 in 4 step. Consigliato per macchine con >= 16 GB RAM.",
        DefaultSteps: 4,
        DefaultWidth: 768,
        DefaultHeight: 768,
        MinFreeDiskSpaceBytes: 4_000_000_000L,
        Capability: AiModelCapability.Image,
        Format: "GGUF",
        RecommendedVramGb: 6.0,
        RecommendedHardware: "GPU dedicata CUDA / DirectML (>= 16 GB RAM)");

    private static readonly AiModelDefinition[] TextModelsInternal =
    [
        Gemma2B,
        Gemma4B,
        Gemma9B,
        Gemma27B
    ];

    private static readonly AiModelDefinition[] ImageModelsInternal =
    [
        Sd15Turbo,
        DreamShaper8,
        SdxlLightning
    ];

    private static readonly AiModelDefinition[] AllModelsInternal =
    [
        Gemma2B,
        Gemma4B,
        Gemma9B,
        Gemma27B,
        Sd15Turbo,
        DreamShaper8,
        SdxlLightning
    ];

    public static IReadOnlyList<AiModelDefinition> GetAllModels() => AllModelsInternal;

    public static IReadOnlyList<AiModelDefinition> GetTextModels() => TextModelsInternal;

    public static IReadOnlyList<AiModelDefinition> GetImageModels() => ImageModelsInternal;

    public static AiModelDefinition? FindByKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Equals(AutoModelKey, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        for (var i = 0; i < AllModelsInternal.Length; i++)
        {
            if (AllModelsInternal[i].Key.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return AllModelsInternal[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Seleziona il profilo di modello testo consigliato in base alla RAM fisica totale rilevata (in byte).
    /// </summary>
    public static AiModelDefinition ResolveRecommendedModel(long totalPhysicalRamBytes)
    {
        const long gb = 1024L * 1024L * 1024L;

        // Se >= 30 GB (tolleranza rispetto a 32 GB fisici reali)
        if (totalPhysicalRamBytes >= 30L * gb)
        {
            return Gemma27B;
        }

        // Se >= 14 GB (tolleranza rispetto a 16 GB)
        if (totalPhysicalRamBytes >= 14L * gb)
        {
            return Gemma9B;
        }

        // Se >= 7 GB (tolleranza rispetto a 8 GB)
        if (totalPhysicalRamBytes >= 7L * gb)
        {
            return Gemma4B;
        }

        // Default a 4 GB (o meno)
        return Gemma2B;
    }

    /// <summary>
    /// Seleziona il profilo di modello immagini consigliato in base alla RAM fisica totale rilevata (in byte).
    /// </summary>
    public static AiModelDefinition ResolveRecommendedImageModel(long totalPhysicalRamBytes)
    {
        const long gb = 1024L * 1024L * 1024L;

        if (totalPhysicalRamBytes >= 14L * gb)
        {
            return SdxlLightning;
        }

        if (totalPhysicalRamBytes >= 7L * gb)
        {
            return DreamShaper8;
        }

        return Sd15Turbo;
    }
}
