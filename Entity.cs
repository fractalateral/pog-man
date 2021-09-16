using Godot;
using System;

using Globals;


// Class extended by both PogMan and Ghost subclasses.
public abstract class Entity : Area2D {
    protected int MinX = 0;
    protected int MaxX = 220;
    protected int MinY = 0;
    protected int MaxY = 288;

    public const float Speed = 0.3f; // In seconds per 16x16px cell.

    protected float _width;

    protected Vector2 _queuedMotion = new Vector2();
    protected Vector2 _motion = new Vector2(1, 0);

    public Tween MovementTween;
    public Sprite AnimSprite;
    public AnimationPlayer AnimPlayer;
    public PacLevel LevelScene;
    public Main MainScene;

    protected Random rand = new Random();

    public override void _Ready() {
        _width = 14; // Assumes a square-shaped hitbox.

        MovementTween = GetNode<Tween> ("MovementTween");
        AnimSprite = GetNode<Sprite> ("Sprite");
        AnimPlayer = GetNode<AnimationPlayer> ("Sprite/AnimationPlayer");

        LevelScene = GetNode<PacLevel>("..");
        MainScene = GetNode<Main>("../..");

        AnimPlayer.Play("walk");

        LevelScene.Connect("LevelReady", this, "_OnReadyMethod");

        this.SetPhysicsProcess(false);
    }

    public abstract void _OnReadyMethod(); // Override in sub-classes to run after _Ready().

    public void InputLoop() {
        // Check arrow key input and assign to _queuedMotion.
        if (Input.IsActionPressed("ui_right")) _queuedMotion = new Vector2(1, 0);
        if (Input.IsActionPressed("ui_up")) _queuedMotion = new Vector2(0, -1);
        if (Input.IsActionPressed("ui_left")) _queuedMotion = new Vector2(-1, 0);
        if (Input.IsActionPressed("ui_down")) _queuedMotion = new Vector2(0, 1);
    }

    public bool RequestMovement(Vector2 MotionVector, MoveParam[] MoveModifiers = null) {
        MoveModifiers = MoveModifiers ?? new MoveParam[0]; // Instantiate empty MoveParam array if not passed.
        
        // Request movement from MainScene using MotionVector parameter.
        Vector2 MoveReq = LevelScene.RequestMove(this.Position - new Vector2(_width, _width)/2, MotionVector, _width, MoveModifiers);

        if (MoveReq != Position) {
            return true;
        } else {
            return false;
        }
    }
}
