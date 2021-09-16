using Godot;
using System;

using Globals;

public class PogMan : Entity {
    [Signal]
    public delegate void PlayerDeath();

    public Area2D EatBox;
    
    public int QuickMoveFrames = 0; // Use to quickly move through ghosts that are eaten.

    public override void _OnReadyMethod() {
        if (AnimPlayer.IsConnected("animation_finished", LevelScene, "_PlayerLossAfterAnim")) AnimPlayer.Disconnect("animation_finished", LevelScene, "_PlayerLossAfterAnim");

        this.SetPhysicsProcess(true);

        if (!MovementTween.IsConnected("tween_all_completed", this, "MovementLoop")) MovementTween.Connect("tween_all_completed", this, "MovementLoop");
        if (!IsConnected("area_entered", this, "_AreaEntered")) Connect("area_entered", this, "_AreaEntered");
        MovementLoop();

        if (LevelScene.IsConnected("LevelReady", this, "_OnReadyMethod")) LevelScene.Disconnect("LevelReady", this, "_OnReadyMethod");
    }

    public void _AreaEntered(Area2D area) {
        if (area.IsInGroup("GHOSTS")) {
            if (((Ghost) area).BehaviourMode < (GhostBehaviour) 6) {
                EmitSignal("PlayerDeath");
            }
        }
    }

    public override void _PhysicsProcess(float delta) {
        InputLoop();

        // Movement loop is operated by self-perpetuating signal in Tween node. (See public void RequestMovement())
    }

    public void MovementLoop() {
        // Try to approve queued motion direction.
        if (RequestMovement(_queuedMotion)) {
            _motion = _queuedMotion;
            Rotation = _motion.Angle();
        }

        Vector2 MoveReq = LevelScene.RequestMove(this.Position - new Vector2(8, 8), _motion, 16);

        // Warp or move smoothly based on whether distance is >= 4.
        if (this.Position.DistanceTo(MoveReq) > 4) {
            MovementTween.InterpolateProperty(this, "position", this.Position, MoveReq, 0);
        } else if (QuickMoveFrames > 0) {
            MovementTween.InterpolateProperty(this, "position", this.Position, MoveReq, Speed/64);
            QuickMoveFrames--;
        } else {
            MovementTween.InterpolateProperty(this, "position", this.Position, MoveReq, Speed/4);
        }
        
        MovementTween.Start();
    }

/*
    public void MovementLoop() {
        // Deprecated.

        this.Rotation = _motion.Angle();
        this.MoveAndSlide(_motion * Speed);
    }*/

/*
    public void WarpLoop() {
        // Deprecated.

        if (this.Position.x <= MinX - 8) {
            this.Position = new Vector2(MaxX + 15, this.Position.y);
        } else if (this.Position.x >= MaxX + 8) {
            this.Position = new Vector2(MinX - 15, this.Position.y);
        } else if (this.Position.y <= MinY - 8) {
            this.Position = new Vector2(this.Position.x, MaxY + 15);
        } else if (this.Position.y >= MaxY + 8) {
            this.Position = new Vector2(this.Position.x, MinY - 15);
        }
    }*/

    public Vector2 GetMotion() {
        return _motion;
    }

    
}
