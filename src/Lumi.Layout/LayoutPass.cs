namespace Lumi.Layout;

using Lumi.Core;
using Lumi.Styling;

public class LayoutPass : IDisposable
{
    private readonly StyleResolver _styleResolver;
    private readonly YogaLayoutEngine _layoutEngine;

    public LayoutPass(StyleResolver styleResolver)
    {
        _styleResolver = styleResolver;
        _layoutEngine = new YogaLayoutEngine();
    }

    /// <summary>
    /// Run a complete frame pass: Style Resolve → Layout.
    /// Paint is handled externally.
    /// </summary>
    public void Run(Element root, float viewportWidth, float viewportHeight, PseudoClassState? pseudoState = null)
    {
        _styleResolver.ResolveStyles(root, pseudoState);
        _layoutEngine.CalculateLayout(root, viewportWidth, viewportHeight);
    }

    public void Dispose()
    {
        _layoutEngine.Dispose();
        GC.SuppressFinalize(this);
    }
}
