using Godot;
using System;
using System.Collections.Generic;

using Globals;

public class Ghost : Entity {

    [Signal]
    public delegate void Emerged(GhostBehaviour BaseBehaviourMode);
    [Signal]
    public delegate void Eaten(Vector2 GhostPos);

    [Export]
    public GhostBehaviour BaseBehaviourMode = GhostBehaviour.CHASE_1; // Base behaviour mode. Does not change in runtime.
    public GhostBehaviour BehaviourMode = GhostBehaviour.EMERGE; // Current behaviour mode.

    public MoveParam[] MovementParameters;

    private Vector2[] _directions = new Vector2[] {new Vector2(1, 0), new Vector2(0, 1), new Vector2(-1, 0), new Vector2(0, -1)};
    public Vector2 ChaseTarget;

    public PogMan PlayerReference;

    public Line2D DebugLine;

    public Texture BaseTexture;


    public override void _OnReadyMethod() {
        this.SetPhysicsProcess(true);

        // Initialize debug line.
        PlayerReference = GetNode<PogMan> ("../PogMan");
        DebugLine = new Line2D();
        DebugLine.Width = 1;
        DebugLine.Set("visible", true);
        DebugLine.AddToGroup("DEBUG");
        GetNode("../PacUI").AddChild(DebugLine);

        BaseTexture = ResourceLoader.Load("res://ghost/amogus_16bit_ghost_" + (
            (int) BaseBehaviourMode == 1 ? "red" : (
            (int) BaseBehaviourMode == 2 ? "green" : (
            (int) BaseBehaviourMode == 3 ? "pink" : ( "orange"
        )))) + ".png") as Texture;

        AnimSprite.Texture = BaseTexture;

        AILoop();

        MovementTween.Connect("tween_all_completed", this, "AILoop");
        LevelScene.Connect("SwitchBehaviourMode", this, "NextBehaviourMode");
        LevelScene.Connect("GhostFlashingAnimation", this, "_SetFlashingAnim");
        this.Connect("area_entered", this, "_AreaEntered");


        LevelScene.Disconnect("LevelReady", this, "_OnReadyMethod");
    }

    public void _AreaEntered(Area2D area) {
        // AnimPlayer.Play("kill");

        if (area.IsInGroup("EatBox") && BehaviourMode.Equals(GhostBehaviour.FLEE)) {
            BehaviourMode = GhostBehaviour.DYING;

            LevelScene.GetNode<AudioStreamPlayer2D> ("SFX_GhostEatenPlayer").Play(0);

            AnimSprite.Texture = BaseTexture;
            AnimPlayer.Play("death");
            AnimPlayer.Connect("animation_finished", this, "_DyingAnimFinished");

            area.GetParent<PogMan>().QuickMoveFrames = 6;
        } else if (area.IsInGroup("GhostRegen") && BehaviourMode.Equals(GhostBehaviour.EATEN)) {
            // Regenerate.
            AnimPlayer.Play("walk");
            AnimSprite.Modulate = AnimSprite.Modulate * new Color(1, 1, 1, 4f);

            BehaviourMode = GhostBehaviour.EMERGE; // Overrides check in NextBehaviourMode() preventing phase changes on eaten/emerging ghosts.
        }
    }

    public void _DyingAnimFinished(String AnimName) {
        // Get eaten.
        EmitSignal("Eaten", this.Position);
        AnimPlayer.Play("walk_ghost");
        _SetFlashingAnim(false);
        AnimSprite.Modulate = AnimSprite.Modulate * new Color(1, 1, 1, 0.25f);
        BehaviourMode = GhostBehaviour.EATEN;

        AnimPlayer.Disconnect("animation_finished", this, "_DyingAnimFinished");
    }

    public void NextBehaviourMode(GhostBehaviour newBehaviourMode) {
        if (!BehaviourMode.Equals(GhostBehaviour.EATEN) && !BehaviourMode.Equals(GhostBehaviour.EMERGE) && !BehaviourMode.Equals(GhostBehaviour.DYING)) {
            _motion *= -1;
            if (newBehaviourMode.Equals(GhostBehaviour.CHASE_1)) {
                BehaviourMode = BaseBehaviourMode;
            } else {
                BehaviourMode = newBehaviourMode;
            }

            if (!AnimSprite.Texture.Equals(BaseTexture)) AnimSprite.Texture = BaseTexture;
            AnimPlayer.Play("walk");
            switch (newBehaviourMode) {
                case GhostBehaviour.FLEE:
                    AnimSprite.Texture = ResourceLoader.Load("res://ghost/amogus_16bit_ghost_scared.png") as Texture;
                    break;
                default:
                    AnimSprite.Texture = BaseTexture;
                    break;
            }
        }
    }

    public void AILoop() {
        // Get list of viable movements using stored copy of maze.
        Vector2[] ValidMoves = FindPossibleMovements();

        // Switch target to suit current BehaviourMode.
        switch (BehaviourMode) {
            case GhostBehaviour.CHASE_1:
                ChaseTarget = PlayerReference.Position;
                break;
            case GhostBehaviour.CHASE_2:
                Vector2 PogManPos = PlayerReference.Position;
                Vector2 BlinkyPos = GetNode<PacLevel>("..").Blinky.Position;

                ChaseTarget = ((BlinkyPos - PogManPos).Normalized().Rotated((float) Math.PI) * BlinkyPos.DistanceTo(PogManPos)) + PogManPos;
                break;
            case GhostBehaviour.CHASE_3:
                ChaseTarget = PlayerReference.Position + (PlayerReference.GetMotion() * 32);
                break;
            case GhostBehaviour.CHASE_4:
                if (Position.DistanceTo(PlayerReference.Position) >= 64) {
                    goto case GhostBehaviour.CHASE_1;
                } else {
                    goto case GhostBehaviour.SCATTER;
                }
            case GhostBehaviour.FLEE:
                ChaseTarget = this.Position + (_directions[rand.Next(0, _directions.Length)] * 8);
                break;
            case GhostBehaviour.SCATTER:
                ChaseTarget = LevelScene.GetGhostScatterPoint((int) (BaseBehaviourMode) - 1);
                break;
            case GhostBehaviour.EATEN:
                ChaseTarget = new Vector2(28*4, 15*4);
                break;
            case GhostBehaviour.EMERGE:
                if (this.Position.y > LevelScene.GhostEmergePoint.y + 4) {
                    ChaseTarget = LevelScene.GhostEmergePoint;
                } else {
                    this.BehaviourMode = (LevelScene.GhostAIPhase-1 % 2 == 0 ? GhostBehaviour.SCATTER : BaseBehaviourMode); // Overrides check in NextBehaviourMode() preventing phase changes on eaten/emerging ghosts.
                }
                break;
        }

        DebugLine.ClearPoints();
        DebugLine.AddPoint(this.Position);
        DebugLine.AddPoint(ChaseTarget);

        // Queue motion for square closest to target.
        float SmallestDistFromTarget = 1000;
        foreach (Vector2 dir in ValidMoves) {
            float DistanceFromTarget = (this.Position + (dir*4)).DistanceTo(ChaseTarget);
            if (DistanceFromTarget <= SmallestDistFromTarget) {
                SmallestDistFromTarget = DistanceFromTarget;
                _queuedMotion = dir;
            }
        }

        if (BehaviourMode.Equals(GhostBehaviour.EATEN) || BehaviourMode.Equals(GhostBehaviour.EMERGE)) {
            MovementParameters = new MoveParam[] {MoveParam.PASS_THROUGH_GATES};
        } else if (BehaviourMode.Equals(GhostBehaviour.DYING)) {
            MovementParameters = new MoveParam[] {MoveParam.IMMOBILE};
        } else {
            MovementParameters = new MoveParam[] {};
        }

        if (RequestMovement(_queuedMotion, MovementParameters)) {
            _motion = _queuedMotion.Round();
            if (_motion.DistanceTo(new Vector2(-1, 0)) < Math.Sqrt(2) - 0.5f) AnimSprite.FlipH = true;
            else if (_motion.DistanceTo(new Vector2(1, 0)) < Math.Sqrt(2) - 0.5f) AnimSprite.FlipH = false;
        }

        Vector2 MoveReq = LevelScene.RequestMove(this.Position - new Vector2(8, 8), _motion, 16, MovementParameters);

        // Warp or move smoothly based on whether distance is >= 4.
        if (this.Position.DistanceTo(MoveReq) > 4) {
            MovementTween.InterpolateProperty(this, "position", this.Position, MoveReq, 0);
        } else {
            MovementTween.InterpolateProperty(this, "position", this.Position, MoveReq, BehaviourMode.Equals(GhostBehaviour.FLEE) ? Speed/2 : BehaviourMode.Equals(GhostBehaviour.EATEN) ? Speed/12 : Speed/4);
        }
        
        MovementTween.Start();
    }

    public Vector2[] FindPossibleMovements() {
        // Get position in maze.
        Vector2 MazePos = ((this.Position - new Vector2(8, 8)) / 4).Floor();

        // Return array of possible (non-obstructed) movements.
        List<Vector2> dirList = new List<Vector2> ();
        foreach (Vector2 v in _directions) {
            if (this.RequestMovement(v, MovementParameters) && _motion != -v) dirList.Add(v);
        }

        // Turn around if there are no other valid moves, or if 16px away from emerge point AND currently emerging AND 50% chance check passed.
        if (dirList.Count <= 0 || (BehaviourMode.Equals(GhostBehaviour.EMERGE) && Position.DistanceTo(LevelScene.GhostEmergePoint) == 16 && rand.Next(0, 4) != 0)) {
            return new Vector2[] {-_motion};
        } else {
            return dirList.ToArray();
        }
    }

    public void _SetFlashingAnim(bool toggle) {
        if (toggle && this.BehaviourMode.Equals(GhostBehaviour.FLEE)) {
            GetNode<AnimationPlayer>("Sprite/FlashingAnimPlayer").Play("flash");
        } else {
            GetNode<AnimationPlayer>("Sprite/FlashingAnimPlayer").Play("flash");
            GetNode<AnimationPlayer>("Sprite/FlashingAnimPlayer").Stop();
            GetNode<Sprite>("Sprite").Modulate = AnimSprite.Modulate * new Color(1, 1, 1, 1);
        }
    }

    /*public void WarpLoop() {
        // Deprecated.

        if (this.Position.x <= MinX - 16) {
            this.Position = new Vector2(MaxX + 15, this.Position.y);
        } else if (this.Position.x >= MaxX + 16) {
            this.Position = new Vector2(MinX - 15, this.Position.y);
        } else if (this.Position.y <= MinY - 16) {
            this.Position = new Vector2(this.Position.x, MaxY + 15);
        } else if (this.Position.y >= MaxY + 16) {
            this.Position = new Vector2(this.Position.x, MinY - 15);
        }
    }*/
}
