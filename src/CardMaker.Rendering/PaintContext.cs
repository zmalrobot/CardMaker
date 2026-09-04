using CardMaker.Contracts.Geometry;
using CardMaker.Contracts.Layout;
using CardMaker.Rendering.Text;

namespace CardMaker.Rendering;

internal sealed record PaintContext(
    CardRenderRequest Request,
    CardGeometry Geometry,
    ValueBinder Binder,
    ConditionEvaluator Evaluator,
    List<RenderWarning> Warnings,
    TextEngine TextEngine)
{
    public IRenderResources Resources => Request.Resources;

    public void Warn(string code, string message, string? layerId = null) =>
        Warnings.Add(new RenderWarning(code, message, layerId));
}

