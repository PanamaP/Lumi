namespace Lumi.Core.DragDrop;

/// <summary>
/// Tracks the state of an active drag-and-drop operation.
/// </summary>
public class DragDropState
{
    public bool IsDragging { get; set; }
    public Element? Source { get; set; }
    public DragData? Data { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
}
