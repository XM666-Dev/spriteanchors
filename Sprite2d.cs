using Godot;
using System;

public partial class Sprite2d : Sprite2D
{
    public override void _Ready()
    {
        var stick = GetNode<ColorRect>("../Stick");
        stick.GlobalPosition = GlobalTransform * ((Vector2[])GetMeta("left_hand"))[0]; //My Stick Go Here!
    }
}
