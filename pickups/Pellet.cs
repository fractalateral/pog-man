using Godot;
using System;

public class Pellet : Area2D {

    public int PelletType = 0;

    public String[] PelletSpritePaths = new String[] {"res://pickups/Pellet.png", "res://pickups/BigPellet.png"};

    [Signal]
    public delegate void PelletEaten(Pellet PelletRemoved, int StunTime);

    public override void _Ready() {
        this.Connect("area_entered", this, nameof(_AreaEntered));
    }

    public void _AreaEntered(PhysicsBody2D area) {
        if (area.IsInGroup("EatBox")) {
            EmitSignal(nameof(PelletEaten), this, PelletType * 12);
            QueueFree();
        }
    }

    public void SetPelletType(int type) {
        PelletType = type;
        this.GetNode<Sprite> ("Sprite").Texture = ResourceLoader.Load<Texture> (PelletSpritePaths[type]); 
    }

    public int GetValue() {
        return 10 + (PelletType*40);
    }
}
