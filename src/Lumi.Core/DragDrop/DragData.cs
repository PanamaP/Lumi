namespace Lumi.Core.DragDrop;

/// <summary>
/// Data payload carried during a drag-and-drop operation.
/// </summary>
public class DragData
{
    public string? Text { get; set; }
    public string[]? Files { get; set; }
    public object? Custom { get; set; }
}
