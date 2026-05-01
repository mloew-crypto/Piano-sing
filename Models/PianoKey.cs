namespace PianoApp.Models;

public sealed class PianoKey(string noteName, double left, double top, double width, double height, bool isBlack)
{
    public string NoteName { get; } = noteName;
    public double Left { get; } = left;
    public double Top { get; } = top;
    public double Width { get; } = width;
    public double Height { get; } = height;
    public bool IsBlack { get; } = isBlack;

    public bool ContainsPoint(double x, double y) =>
        x >= Left && x <= Left + Width && y >= Top && y <= Top + Height;
}
