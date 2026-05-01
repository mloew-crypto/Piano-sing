using Microsoft.Maui.Graphics;
using PianoApp.Models;

namespace PianoApp.Services;

public class PianoDrawable : IDrawable
{
    private readonly IList<PianoKey> _keys;
    private readonly HashSet<string> _pressedKeys;

    public PianoDrawable(IList<PianoKey> keys, HashSet<string> pressedKeys)
    {
        _keys = keys;
        _pressedKeys = pressedKeys;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        foreach (var key in _keys)
        {
            bool pressed = _pressedKeys.Contains(key.NoteName);
            canvas.FillColor = pressed
                ? (key.IsBlack ? Color.FromRgb(0x44, 0x44, 0x44) : Color.FromRgb(0xcc, 0xc4, 0xb4))
                : (key.IsBlack ? Colors.Black : Color.FromRgb(0xff, 0xfa, 0xf0)); // FloralWhite
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 1;
            canvas.FillRectangle((float)key.Left, (float)key.Top, (float)key.Width, (float)key.Height);
            canvas.DrawRectangle((float)key.Left, (float)key.Top, (float)key.Width, (float)key.Height);
        }
    }
}
