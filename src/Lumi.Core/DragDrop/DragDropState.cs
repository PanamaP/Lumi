namespace Lumi.Core.DragDrop;

/// <summary>
/// Tracks the state of an active drag-and-drop operation.
/// </summary>
public class DragDropState
{
    public bool IsDragging { get; internal set; }
    public Element? Source { get; internal set; }
    public DragData? Data { get; internal set; }
    public float X { get; internal set; }
    public float Y { get; internal set; }
}
