using System.Numerics;

using Dalamud.Utility.Numerics;

namespace Dalamud.Fools23;

public struct SimpleRect
{
    private Vector4 vector;

    public SimpleRect(float x, float y, float width, float height)
    {
        this.vector = new Vector4(x, y, width, height);
    }

    public SimpleRect(Vector4 vector)
    {
        this.vector = vector;
    }

    public SimpleRect()
    {
        this.vector = Vector4.Zero;
    }

    public readonly bool IntersectsWith(SimpleRect other)
    {
        return other.vector.X < this.vector.X + this.vector.Z &&
               this.vector.X < other.vector.X + other.vector.Z &&
               other.vector.Y < this.vector.Y + this.vector.W &&
               this.vector.Y < other.vector.Y + other.vector.W;
    }

    public readonly SimpleRect RelativeTo(Vector2 origin)
    {
        var relative = this.vector
                          .WithX(origin.X - (this.vector.Z / 2))
                          .WithY(origin.Y - (this.vector.W / 2));
        return new SimpleRect(relative);
    }
}
