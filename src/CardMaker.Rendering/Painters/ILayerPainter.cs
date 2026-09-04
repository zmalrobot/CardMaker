using CardMaker.Contracts.Layout;
using SkiaSharp;

namespace CardMaker.Rendering.Painters;

internal interface ILayerPainter
{
    bool CanPaint(LayerDefinition layer);
    void Paint(SKCanvas canvas, LayerDefinition layer, SKRect dest, double opacity, PaintContext context);
}

