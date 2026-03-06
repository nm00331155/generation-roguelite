using Godot;
using System.Collections.Generic;

namespace GenerationRoguelite.UI;

public partial class FloatTextSpawner : Node2D
{
    private const int MaxActiveTexts = 2;
    private const float DisplaySeconds = 1.5f;
    private const float RisePixels = 120f;

    [Export]
    public CanvasLayer? TargetLayer { get; set; }

    private readonly List<Label> _activeLabels = new();

    public void Spawn(string text, Color color, Vector2 position)
    {
        if (_activeLabels.Count >= MaxActiveTexts)
        {
            var oldest = _activeLabels[0];
            _activeLabels.RemoveAt(0);
            if (IsInstanceValid(oldest))
            {
                oldest.QueueFree();
            }
        }

        var label = new Label
        {
            Text = text,
            Modulate = color,
            Position = position,
            ThemeTypeVariation = "Label",
        };
        label.AddThemeFontSizeOverride("font_size", 54);

        if (TargetLayer is not null)
        {
            TargetLayer.AddChild(label);
        }
        else
        {
            AddChild(label);
        }

        _activeLabels.Add(label);

        var tween = CreateTween();
        tween.TweenProperty(label, "position:y", position.Y - RisePixels, DisplaySeconds);
        tween.Parallel().TweenProperty(label, "modulate:a", 0f, DisplaySeconds);
        tween.TweenCallback(Callable.From(() =>
        {
            _activeLabels.Remove(label);
            if (IsInstanceValid(label))
            {
                label.QueueFree();
            }
        }));
    }
}
