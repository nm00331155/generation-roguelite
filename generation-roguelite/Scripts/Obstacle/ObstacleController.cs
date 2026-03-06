using Godot;

namespace GenerationRoguelite.Obstacle;

public partial class ObstacleController : Node2D
{
    [Signal]
    public delegate void HitPlayerEventHandler();

    private const float Size = 108f;

    private ColorRect _visual = null!;

    public float Speed { get; set; } = 300f;

    public string ObstacleType { get; private set; } = "enemy";

    public bool IsDestroyed { get; private set; }

    public override void _Ready()
    {
        _visual = GetNode<ColorRect>("Visual");
    }

    public override void _Process(double delta)
    {
        if (IsDestroyed)
        {
            return;
        }

        Position += Vector2.Left * Speed * (float)delta;
        if (Position.X < -200f)
        {
            QueueFree();
        }
    }

    public void ConfigureType(string type)
    {
        ObstacleType = type;
        _visual.Color = type switch
        {
            "trap" => new Color(1f, 0.58f, 0.18f),
            "falling" => new Color(1f, 0.88f, 0.22f),
            _ => new Color(0.95f, 0.24f, 0.24f),
        };
    }

    public bool Intersects(Rect2 playerRect)
    {
        if (IsDestroyed)
        {
            return false;
        }

        var obstacleRect = new Rect2(Position.X - Size * 0.5f, Position.Y - Size, Size, Size);
        return obstacleRect.Intersects(playerRect);
    }

    public void DestroyByAttack()
    {
        if (IsDestroyed)
        {
            return;
        }

        IsDestroyed = true;
        QueueFree();
    }
}
