namespace CardMaker.AI.Abstractions;

/// <summary>
/// Contratto marcatore per future estensioni alla generazione di immagini (es. Stable Diffusion, SD.cpp).
/// Garantisce che CardMaker.AI sia progettata come libreria AI polivalente e non limitata solo a LLM testuali.
/// </summary>
public interface IImageGenerationEngine : IAiModelSession
{
    // Riservato per future implementazioni di text-to-image (es. Stable Diffusion / SD.cpp)
}
