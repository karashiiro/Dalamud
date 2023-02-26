namespace Dalamud.Fools23.Objects;

public class TgPlayer : TgAnimSpriteObject
{
    public TgPlayer()
    {
        this.CollisionRect = new SimpleRect(20, 20, 10, 10);
    }

    public override void Draw()
    {
    }

    public override void Update(float dt)
    {
    }

    public override void OnCollisionTrigger(TgObject target, TippyGameScene scene)
    {
        if (target is TgBullet)
        {
            scene.GameOver();
        }
    }
}
