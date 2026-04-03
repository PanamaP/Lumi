namespace Lumi.Layout;

using Lumi.Core;
using Yoga;
using static Yoga.YG;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public unsafe class YogaLayoutEngine : IDisposable
{
    private readonly Dictionary<Element, IntPtr> _nodeMap = [];
    private IntPtr _rootNodePtr;
    private bool _disposed;

    private static readonly Dictionary<IntPtr, (Element Element, ElementMeasureDelegate Measure)> _measureMap = [];

    /// <summary>
    /// Optional delegate for measuring leaf elements (text, images).
    /// Set this before calling CalculateLayout to enable auto-sizing.
    /// </summary>
    public ElementMeasureDelegate? MeasureFunc { get; set; }

    /// <summary>
    /// Perform a full layout pass on the element tree.
    /// Syncs the Yoga node tree, applies styles, calculates layout,
    /// and reads results back into each Element's LayoutBox.
    /// The root node is always sized to fill the available window space.
    /// </summary>
    public void CalculateLayout(Element root, float availableWidth, float availableHeight)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _measureMap.Clear();
        var rootNode = SyncNode(root);
        _rootNodePtr = (IntPtr)rootNode;

        // Force the root node to fill the entire window (like the viewport in a browser)
        NodeStyleSetWidth(rootNode, availableWidth);
        NodeStyleSetHeight(rootNode, availableHeight);

        NodeCalculateLayout(rootNode, availableWidth, availableHeight, YGDirection.YGDirectionLTR);
        ReadLayoutResults(root, 0, 0);
    }

    private YGNode* SyncNode(Element element)
    {
        YGNode* node;

        if (_nodeMap.TryGetValue(element, out var existing))
        {
            node = (YGNode*)existing;
        }
        else
        {
            node = NodeNew();
            _nodeMap[element] = (IntPtr)node;
        }

        ApplyStyle(node, element.ComputedStyle);

        // Children of scroll containers should not shrink — they must keep
        // their natural size so the overflow creates scrollable content
        if (element.Parent?.ComputedStyle.Overflow == Overflow.Scroll)
        {
            NodeStyleSetFlexShrink(node, 0);
        }

        // For leaf elements (text, images) with no children, register a measure callback
        // Yoga nodes with measure functions must not have children
        bool isLeaf = element.Children.Count == 0 &&
                       MeasureFunc != null &&
                       element is TextElement or ImageElement;

        if (isLeaf)
        {
            _measureMap[(IntPtr)node] = (element, MeasureFunc!);
            NodeSetMeasureFunc(node, &YogaMeasureCallback);
        }
        else
        {
            NodeRemoveAllChildren(node);

            for (var i = 0; i < element.Children.Count; i++)
            {
                var childNode = SyncNode(element.Children[i]);
                NodeInsertChild(node, childNode, (UIntPtr)i);
            }
        }

        return node;
    }

    /// <summary>
    /// Static measure callback invoked by Yoga for leaf nodes (text, images).
    /// Retrieves the element from the static map and measures it.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static YGSize YogaMeasureCallback(
        YGNode* node, float width, YGMeasureMode widthMode, float height, YGMeasureMode heightMode)
    {
        if (!_measureMap.TryGetValue((IntPtr)node, out var entry))
            return new YGSize { width = 0, height = 0 };

        float availableWidth = widthMode == YGMeasureMode.YGMeasureModeUndefined ? float.MaxValue : width;
        float availableHeight = heightMode == YGMeasureMode.YGMeasureModeUndefined ? float.MaxValue : height;

        var (measuredWidth, measuredHeight) = entry.Measure(entry.Element, availableWidth, availableHeight);

        // Clamp to constraints based on measure mode
        if (widthMode == YGMeasureMode.YGMeasureModeAtMost)
            measuredWidth = Math.Min(measuredWidth, width);
        else if (widthMode == YGMeasureMode.YGMeasureModeExactly)
            measuredWidth = width;

        if (heightMode == YGMeasureMode.YGMeasureModeAtMost)
            measuredHeight = Math.Min(measuredHeight, height);
        else if (heightMode == YGMeasureMode.YGMeasureModeExactly)
            measuredHeight = height;

        return new YGSize { width = measuredWidth, height = measuredHeight };
    }

    private static void ApplyStyle(YGNode* node, ComputedStyle style)
    {
        // Display (Block maps to Flex since Yoga is flex-only)
        NodeStyleSetDisplay(node, style.Display == DisplayMode.None
            ? YGDisplay.YGDisplayNone
            : YGDisplay.YGDisplayFlex);

        // Dimensions — negative values encode percentages (e.g. -100 = 100%)
        if (float.IsNaN(style.Width))
            NodeStyleSetWidthAuto(node);
        else if (style.Width < 0)
            NodeStyleSetWidthPercent(node, -style.Width);
        else
            NodeStyleSetWidth(node, style.Width);

        if (float.IsNaN(style.Height))
            NodeStyleSetHeightAuto(node);
        else if (style.Height < 0)
            NodeStyleSetHeightPercent(node, -style.Height);
        else
            NodeStyleSetHeight(node, style.Height);

        // Min/Max dimensions (skip infinite max values — Yoga defaults to unlimited)
        if (style.MinWidth < 0)
            NodeStyleSetMinWidthPercent(node, -style.MinWidth);
        else
            NodeStyleSetMinWidth(node, style.MinWidth);

        if (style.MinHeight < 0)
            NodeStyleSetMinHeightPercent(node, -style.MinHeight);
        else
            NodeStyleSetMinHeight(node, style.MinHeight);

        if (!float.IsPositiveInfinity(style.MaxWidth))
        {
            if (style.MaxWidth < 0)
                NodeStyleSetMaxWidthPercent(node, -style.MaxWidth);
            else
                NodeStyleSetMaxWidth(node, style.MaxWidth);
        }

        if (!float.IsPositiveInfinity(style.MaxHeight))
        {
            if (style.MaxHeight < 0)
                NodeStyleSetMaxHeightPercent(node, -style.MaxHeight);
            else
                NodeStyleSetMaxHeight(node, style.MaxHeight);
        }

        // Flex container properties
        // Block elements use column direction (vertical stacking, like HTML default)
        var effectiveDirection = style.FlexDirection;
        if (style.Display == DisplayMode.Block)
            effectiveDirection = FlexDirection.Column;

        NodeStyleSetFlexDirection(node, effectiveDirection switch
        {
            FlexDirection.Row => YGFlexDirection.YGFlexDirectionRow,
            FlexDirection.RowReverse => YGFlexDirection.YGFlexDirectionRowReverse,
            FlexDirection.Column => YGFlexDirection.YGFlexDirectionColumn,
            FlexDirection.ColumnReverse => YGFlexDirection.YGFlexDirectionColumnReverse,
            _ => YGFlexDirection.YGFlexDirectionColumn
        });

        NodeStyleSetFlexWrap(node, style.FlexWrap switch
        {
            FlexWrap.Wrap => YGWrap.YGWrapWrap,
            FlexWrap.WrapReverse => YGWrap.YGWrapWrapReverse,
            _ => YGWrap.YGWrapNoWrap
        });

        NodeStyleSetJustifyContent(node, style.JustifyContent switch
        {
            JustifyContent.FlexStart => YGJustify.YGJustifyFlexStart,
            JustifyContent.FlexEnd => YGJustify.YGJustifyFlexEnd,
            JustifyContent.Center => YGJustify.YGJustifyCenter,
            JustifyContent.SpaceBetween => YGJustify.YGJustifySpaceBetween,
            JustifyContent.SpaceAround => YGJustify.YGJustifySpaceAround,
            JustifyContent.SpaceEvenly => YGJustify.YGJustifySpaceEvenly,
            _ => YGJustify.YGJustifyFlexStart
        });

        NodeStyleSetAlignItems(node, MapAlignItems(style.AlignItems));
        NodeStyleSetAlignSelf(node, MapAlignItems(style.AlignSelf));

        // Flex item properties
        NodeStyleSetFlexGrow(node, style.FlexGrow);
        NodeStyleSetFlexShrink(node, style.FlexShrink);

        if (float.IsNaN(style.FlexBasis))
            NodeStyleSetFlexBasisAuto(node);
        else
            NodeStyleSetFlexBasis(node, style.FlexBasis);

        // Gap (spacing between flex items)
        if (style.Gap > 0)
            NodeStyleSetGap(node, YGGutter.YGGutterAll, style.Gap);
        if (!float.IsNaN(style.RowGap) && style.RowGap > 0)
            NodeStyleSetGap(node, YGGutter.YGGutterRow, style.RowGap);
        if (!float.IsNaN(style.ColumnGap) && style.ColumnGap > 0)
            NodeStyleSetGap(node, YGGutter.YGGutterColumn, style.ColumnGap);

        // Margin (4 edges)
        NodeStyleSetMargin(node, YGEdge.YGEdgeTop, style.Margin.Top);
        NodeStyleSetMargin(node, YGEdge.YGEdgeRight, style.Margin.Right);
        NodeStyleSetMargin(node, YGEdge.YGEdgeBottom, style.Margin.Bottom);
        NodeStyleSetMargin(node, YGEdge.YGEdgeLeft, style.Margin.Left);

        // Padding (4 edges)
        NodeStyleSetPadding(node, YGEdge.YGEdgeTop, style.Padding.Top);
        NodeStyleSetPadding(node, YGEdge.YGEdgeRight, style.Padding.Right);
        NodeStyleSetPadding(node, YGEdge.YGEdgeBottom, style.Padding.Bottom);
        NodeStyleSetPadding(node, YGEdge.YGEdgeLeft, style.Padding.Left);

        // Position type
        NodeStyleSetPositionType(node, style.Position switch
        {
            Position.Absolute or Position.Fixed => YGPositionType.YGPositionTypeAbsolute,
            _ => YGPositionType.YGPositionTypeRelative
        });

        // Position offsets (only set if specified)
        if (!float.IsNaN(style.Top))
            NodeStyleSetPosition(node, YGEdge.YGEdgeTop, style.Top);
        if (!float.IsNaN(style.Right))
            NodeStyleSetPosition(node, YGEdge.YGEdgeRight, style.Right);
        if (!float.IsNaN(style.Bottom))
            NodeStyleSetPosition(node, YGEdge.YGEdgeBottom, style.Bottom);
        if (!float.IsNaN(style.Left))
            NodeStyleSetPosition(node, YGEdge.YGEdgeLeft, style.Left);

        // Overflow
        NodeStyleSetOverflow(node, style.Overflow switch
        {
            Overflow.Hidden => YGOverflow.YGOverflowHidden,
            Overflow.Scroll => YGOverflow.YGOverflowScroll,
            _ => YGOverflow.YGOverflowVisible
        });
    }

    private static YGAlign MapAlignItems(AlignItems align) => align switch
    {
        AlignItems.FlexStart => YGAlign.YGAlignFlexStart,
        AlignItems.FlexEnd => YGAlign.YGAlignFlexEnd,
        AlignItems.Center => YGAlign.YGAlignCenter,
        AlignItems.Stretch => YGAlign.YGAlignStretch,
        AlignItems.Baseline => YGAlign.YGAlignBaseline,
        _ => YGAlign.YGAlignStretch
    };

    private void ReadLayoutResults(Element element, float parentAbsX, float parentAbsY)
    {
        if (!_nodeMap.TryGetValue(element, out var ptr))
            return;

        var node = (YGNode*)ptr;

        var relX = NodeLayoutGetLeft(node);
        var relY = NodeLayoutGetTop(node);
        var width = NodeLayoutGetWidth(node);
        var height = NodeLayoutGetHeight(node);

        float absX, absY;

        if (element.ComputedStyle.Position == Position.Fixed)
        {
            // Fixed elements are positioned relative to the viewport
            absX = relX;
            absY = relY;
        }
        else
        {
            absX = parentAbsX + relX;
            absY = parentAbsY + relY;
        }

        element.LayoutBox = new LayoutBox(absX, absY, width, height);

        // Compute scroll dimensions for overflow:scroll elements
        if (element.ComputedStyle.Overflow == Overflow.Scroll)
        {
            float contentWidth = 0;
            float contentHeight = 0;
            foreach (var child in element.Children)
            {
                if (!_nodeMap.TryGetValue(child, out var childPtr)) continue;
                var childNode = (YGNode*)childPtr;
                float childLeft = NodeLayoutGetLeft(childNode);
                float childTop = NodeLayoutGetTop(childNode);
                float childW = NodeLayoutGetWidth(childNode);
                float childH = NodeLayoutGetHeight(childNode);
                contentWidth = Math.Max(contentWidth, childLeft + childW);
                contentHeight = Math.Max(contentHeight, childTop + childH);
            }
            element.ScrollWidth = Math.Max(width, contentWidth);
            element.ScrollHeight = Math.Max(height, contentHeight);
        }

        foreach (var child in element.Children)
        {
            ReadLayoutResults(child, absX, absY);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Free the root node recursively (frees all children too)
        if (_rootNodePtr != IntPtr.Zero)
        {
            NodeFreeRecursive((YGNode*)_rootNodePtr);
            _rootNodePtr = IntPtr.Zero;
        }

        _nodeMap.Clear();
        GC.SuppressFinalize(this);
    }
}
