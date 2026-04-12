using Lumi.Core;
using Lumi.Core.DragDrop;

namespace Lumi.Tests;

public class DragDropTests
{
    private static BoxElement CreateHittableElement(float x, float y, float w, float h)
    {
        var el = new BoxElement("div");
        el.LayoutBox = new LayoutBox(x, y, w, h);
        return el;
    }

    private static Application CreateApp(Element root)
    {
        var app = new Application();
        app.Root = root;
        return app;
    }

    [Fact]
    public void DragStart_And_DragEnd_Lifecycle()
    {
        var root = CreateHittableElement(0, 0, 200, 200);
        var source = CreateHittableElement(10, 10, 50, 50);
        source.IsDraggable = true;
        root.AddChild(source);

        var app = CreateApp(root);

        bool dragStarted = false;
        bool dragEnded = false;
        source.OnDragStart += _ => dragStarted = true;
        source.OnDragEnd += () => dragEnded = true;

        // Mouse down on draggable element
        app.ProcessInput([new MouseEvent { Type = MouseEventType.ButtonDown, X = 20, Y = 20, Button = MouseButton.Left }]);
        Assert.False(dragStarted);

        // Move past threshold (>5px)
        app.ProcessInput([new MouseEvent { Type = MouseEventType.Move, X = 30, Y = 30 }]);
        Assert.True(dragStarted);

        // Mouse up ends drag
        app.ProcessInput([new MouseEvent { Type = MouseEventType.ButtonUp, X = 30, Y = 30, Button = MouseButton.Left }]);
        Assert.True(dragEnded);
    }

    [Fact]
    public void DragThreshold_NoDragIfMovedLessThan5px()
    {
        var root = CreateHittableElement(0, 0, 200, 200);
        var source = CreateHittableElement(10, 10, 50, 50);
        source.IsDraggable = true;
        root.AddChild(source);

        var app = CreateApp(root);

        bool dragStarted = false;
        source.OnDragStart += _ => dragStarted = true;

        // Mouse down
        app.ProcessInput([new MouseEvent { Type = MouseEventType.ButtonDown, X = 20, Y = 20, Button = MouseButton.Left }]);

        // Move only 3px (below 5px threshold)
        app.ProcessInput([new MouseEvent { Type = MouseEventType.Move, X = 22, Y = 22 }]);
        Assert.False(dragStarted);

        // Mouse up without drag
        app.ProcessInput([new MouseEvent { Type = MouseEventType.ButtonUp, X = 22, Y = 22, Button = MouseButton.Left }]);
        Assert.False(dragStarted);
        Assert.False(app.DragState.IsDragging);
    }

    [Fact]
    public void Drop_FiresOnTargetElement()
    {
        var root = CreateHittableElement(0, 0, 200, 200);
        var source = CreateHittableElement(10, 10, 50, 50);
        source.IsDraggable = true;
        var target = CreateHittableElement(100, 100, 50, 50);
        root.AddChild(source);
        root.AddChild(target);

        var app = CreateApp(root);

        DragData? droppedData = null;
        source.OnDragStart += data => data.Text = "hello";
        target.OnDrop += data => droppedData = data;

        // Start drag
        app.ProcessInput([new MouseEvent { Type = MouseEventType.ButtonDown, X = 20, Y = 20, Button = MouseButton.Left }]);
        app.ProcessInput([new MouseEvent { Type = MouseEventType.Move, X = 30, Y = 30 }]);

        // Move to target and drop
        app.ProcessInput([new MouseEvent { Type = MouseEventType.Move, X = 120, Y = 120 }]);
        app.ProcessInput([new MouseEvent { Type = MouseEventType.ButtonUp, X = 120, Y = 120, Button = MouseButton.Left }]);

        Assert.NotNull(droppedData);
        Assert.Equal("hello", droppedData.Text);
    }

    [Fact]
    public void DragData_CarriesTextAndFiles()
    {
        var data = new DragData
        {
            Text = "test text",
            Files = ["/path/to/file1.txt", "/path/to/file2.png"],
            Custom = 42
        };

        Assert.Equal("test text", data.Text);
        Assert.Equal(2, data.Files.Length);
        Assert.Equal("/path/to/file1.txt", data.Files[0]);
        Assert.Equal("/path/to/file2.png", data.Files[1]);
        Assert.Equal(42, data.Custom);
    }

    [Fact]
    public void DragOver_FiresOnHoveredElements()
    {
        var root = CreateHittableElement(0, 0, 200, 200);
        var source = CreateHittableElement(10, 10, 50, 50);
        source.IsDraggable = true;
        var hover = CreateHittableElement(100, 100, 50, 50);
        root.AddChild(source);
        root.AddChild(hover);

        var app = CreateApp(root);

        bool dragOverFired = false;
        hover.OnDragOver += _ => dragOverFired = true;

        // Start drag
        app.ProcessInput([new MouseEvent { Type = MouseEventType.ButtonDown, X = 20, Y = 20, Button = MouseButton.Left }]);
        app.ProcessInput([new MouseEvent { Type = MouseEventType.Move, X = 30, Y = 30 }]);

        // Move over hover target
        app.ProcessInput([new MouseEvent { Type = MouseEventType.Move, X = 120, Y = 120 }]);
        Assert.True(dragOverFired);
    }

    [Fact]
    public void FileDropEvent_TriggersOnDropOnTarget()
    {
        var root = CreateHittableElement(0, 0, 200, 200);
        var target = CreateHittableElement(10, 10, 50, 50);
        root.AddChild(target);

        var app = CreateApp(root);

        DragData? droppedData = null;
        target.OnDrop += data => droppedData = data;

        app.ProcessInput([new FileDropEvent { Files = ["C:\\file.txt", "C:\\image.png"], X = 20, Y = 20 }]);

        Assert.NotNull(droppedData);
        Assert.NotNull(droppedData.Files);
        Assert.Equal(2, droppedData.Files.Length);
        Assert.Equal("C:\\file.txt", droppedData.Files[0]);
    }

    [Fact]
    public void NonDraggableElement_DoesNotStartDrag()
    {
        var root = CreateHittableElement(0, 0, 200, 200);
        var source = CreateHittableElement(10, 10, 50, 50);
        // IsDraggable is false by default
        root.AddChild(source);

        var app = CreateApp(root);

        bool dragStarted = false;
        source.OnDragStart += _ => dragStarted = true;

        app.ProcessInput([new MouseEvent { Type = MouseEventType.ButtonDown, X = 20, Y = 20, Button = MouseButton.Left }]);
        app.ProcessInput([new MouseEvent { Type = MouseEventType.Move, X = 30, Y = 30 }]);

        Assert.False(dragStarted);
        Assert.False(app.DragState.IsDragging);
    }
}
