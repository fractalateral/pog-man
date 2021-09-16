using Godot;
using System;
using System.Collections.Generic;

using Globals;


public class PacLevel : Node {


    private int MinX = -4;
    private int MaxX = 228;
    private int MinY = -4;
    private int MaxY = 276;

    private PogMan _player;
    private Maze _maze;
    private PelletBoard _pelletBoard;
    private PacUI _ui;
    public AudioStreamPlayer2D SFX_PelletEatenPlayer;
    public AudioStreamPlayer2D SFX_GhostEatenPlayer;
    private int _score = 0;
    private int _taskScoreLimit = 4000;
    private int _taskScoreTracker = 0; // Increases with _score. Resets and spawns a task upon reaching 4000.
    public int StartingScore;
    private Vector2 _startingPos = new Vector2(0, 0);
    private List<Pellet> _activePellets = new List<Pellet>();
    private Timer _phaseTimer;

    private Timer scaredTimer = new Timer();
    private Area2D _ghostRegenZone;

    public int GhostsEaten = 0;

    public Vector2 GhostStartPoint = new Vector2(28*4, 20*4);
    public Vector2[] GhostScatterPoints = new Vector2[4] {
        new Vector2(0,0),
        new Vector2(224,0),
        new Vector2(0,288),
        new Vector2(224,288)
    };

    public Vector2 GhostEmergePoint = new Vector2(28*4, 12*4);
    public Vector2 TaskPoint = new Vector2(28*4, 26*4);
    public Vector2 PlayerStartPoint = new Vector2(28*4, 36*4);

    public Ghost Blinky;

    public int Level = 1;
    public int Lives = 3;
    public float[][] LevelTimelines = new float[][] {
        // Final value of each is set to max value, but timer stops so functionally infinite.
        new float[] {7, 20, 7, 20, 5, 20, 5, float.MaxValue},
        new float[] {7, 20, 7, 20, 5, 30997f/30f, 0.01f, float.MaxValue},
        new float[] {7, 20, 7, 20, 5, 31117f/30f, 0.01f, float.MaxValue}
    };
    public int GhostAIPhase = 0; // Max 6 for a total of 8 phases.
    public GhostBehaviour LevelBehaviourPhase = GhostBehaviour.CHASE_1; // Switches to opposite at start.

    [Signal]
    public delegate void SwitchBehaviourMode(String mode, GhostBehaviour behaviour);
    [Signal]
    public delegate void GhostFlashingAnimation(bool toggle);

    public bool DebugMode = false;

    [Signal]
    public delegate void LevelReady();

    [Signal]
    public delegate void LevelComplete(int nextLevel);

    [Signal]
    public delegate void EndGame(int finalScore);

    public override void _Ready() {
        // Connect Main node's GameStart signal to _GameStart method.
        GetNode<Main>("..").Connect("GameStart", this, "_ReadyAnim");

        // Initialize private references to child nodes.
        _maze = GetNode<Maze> ("Maze");
        _pelletBoard = GetNode<PelletBoard> ("PelletBoard");
        _ui = GetNode<PacUI> ("PacUI");
        _phaseTimer = GetNode<Timer> ("PhaseTimer");
        _ghostRegenZone = GetNode<Area2D> ("GhostRegenZone");

        // Initialize references to AudioStreamPlayer2Ds for sound effects.
        SFX_PelletEatenPlayer = GetNode<AudioStreamPlayer2D> ("SFX_PelletEatenPlayer");
        SFX_GhostEatenPlayer = GetNode<AudioStreamPlayer2D> ("SFX_GhostEatenPlayer");

        // Set score and configure task score limit for starting score of current level.
        _taskScoreLimit += StartingScore;
        SetScore(StartingScore);
    }

    public void _ReadyAnim() {
        _ui.GetNode<AnimationPlayer>("ReadyContainer/AnimationPlayer").Play("ready_anim");
        _ui.GetNode<AnimationPlayer>("ReadyContainer/AnimationPlayer").Connect("animation_finished", this, "_GameStart");
    }

    public void _GameStart(String animName) {
        // Initialize Ghost Regen Zone's position.
        _ghostRegenZone.Position = GhostStartPoint + new Vector2(0, 4);

        // Initialize player.
        _player = (PogMan) (ResourceLoader.Load("res://player/PogMan.tscn") as PackedScene).Instance();
        _player.GetNode<Sprite>("Sprite").Texture = GetNode<Main>("..").PogManTexture;
        _player.Position = PlayerStartPoint;
        AddChild(_player);
        _player.Connect("PlayerDeath", this, "_PlayerLoss");

        // Initialize four ghosts. (See CreateGhost method)
        for (int x = 1; x <= 4; x++)  {
            CreateGhost((GhostBehaviour) x);
        }

        EmitSignal("LevelReady");

        // Set up behaviour phase timer.
        _phaseTimer.Connect("timeout", this, "NextPhase");
        if (!GetNode<Main>("..").DebugMode) GetTree().CallGroup("DEBUG", "set_visible", false);
        NextPhase();

        // Do not repeat _GameStart for this instance.
        GetNode<Main>("..").Disconnect("GameStart", this, "_ReadyAnim");
    }


    public void NextPhase() {
        // This method emits the signal "SwitchBehaviourMode" to all active ghosts according to the current behaviour phase in LevelTimelines.

        int LevelTimelineIndex = (Level == 1 ? 0 : (Level >= 1 && Level <= 4 ? 1 : (Level >= 5 ? 2 : -1)));

        _phaseTimer.WaitTime = LevelTimelines[LevelTimelineIndex][GhostAIPhase];
        _phaseTimer.Start();

        GhostAIPhase++;

        LevelBehaviourPhase = (LevelBehaviourPhase.Equals(GhostBehaviour.SCATTER) ? GhostBehaviour.CHASE_1 : GhostBehaviour.SCATTER);
        EmitSignal("SwitchBehaviourMode", LevelBehaviourPhase);
        GetNode<Label> ("PacUI/DebugPanel/PhaseLabel").Text = LevelBehaviourPhase.ToString();

        if (GhostAIPhase == LevelTimelines[LevelTimelineIndex].Length) _phaseTimer.Stop();
    }


    public void _PlayerLoss() {
        // Called when player node has entered the body of a ghost in a hostile behaviour mode.
        // Plays sound effects and ensures that all non-essential, non-player nodes are paused.

        GetNode<Main>("..").PauseGame(true);

        _player.SetPhysicsProcess(false);
        _player.GetNode<Sprite>("Sprite").PauseMode = Node.PauseModeEnum.Process;
        _player.GetNode<AnimationPlayer>("Sprite/AnimationPlayer").Play("death");
        if (!_player.GetNode<AnimationPlayer>("Sprite/AnimationPlayer").IsConnected("animation_finished", this, "_PlayerLossAfterAnim")) _player.GetNode<AnimationPlayer>("Sprite/AnimationPlayer").Connect("animation_finished", this, "_PlayerLossAfterAnim");
    }

    public void _PlayerLossAfterAnim(String animation_name) {
        // Called after player death animation is finished. Makes preparations to either start from a new life or return to the title screen.

        // Reset level properties.
        GhostsEaten = 0;

        // Reset player.
        _player.Position = PlayerStartPoint;
        _player.GetNode<Sprite>("Sprite").PauseMode = Node.PauseModeEnum.Inherit;
        _player.GetNode<AnimationPlayer>("Sprite/AnimationPlayer").PlaybackSpeed = 8;
        _player.SetPhysicsProcess(true);
        _player.GetNode<AnimationPlayer>("Sprite/AnimationPlayer").Play("walk");
        _player.GetNode<AnimationPlayer>("Sprite/AnimationPlayer").Disconnect("animation_finished", this, "_PlayerLossAfterAnim");
        _player.QuickMoveFrames = 0;

        _player._OnReadyMethod();

        // Reset ghosts. (For ghosts, queue free all and create new.)
        foreach (Node n in GetChildren()) {
            if (n.IsInGroup("GHOSTS")) {
                ((Ghost) n).DebugLine.QueueFree();
                n.QueueFree();
            }
        }

        // Remove debug lines.
        foreach (Node n in _ui.GetChildren()) {
            if (n.GetClass().Equals(new Line2D().GetClass())) n.QueueFree();
        }

        for (int x = 1; x <= 4; x++)  {
            CreateGhost((GhostBehaviour) x);
        }

        // Pause game, 
        GetNode<Main>("..").PauseGame();
        if (Lives < 1) {
            EmitSignal("EndGame", _score);
        } else {
            EmitSignal("LevelReady");
            Lives--;
            _ui.RefreshLivesTextures(Lives);
        }
        if (!GetNode<Main>("..").DebugMode) GetTree().CallGroup("DEBUG", "set_visible", false);
    }

    public void CreateGhost(GhostBehaviour ghostType) {
        Ghost _newGhost = (Ghost) (ResourceLoader.Load<PackedScene>("res://ghost/Ghost.tscn")).Instance();
        _newGhost.Position = GhostStartPoint;
        _newGhost.BaseBehaviourMode = ghostType;

        _newGhost.Connect("Eaten", this, "GhostEaten");

        if ((int) ghostType == 1) Blinky = _newGhost;

        _newGhost.AddToGroup("GHOSTS");

        this.AddChild(_newGhost);
    }

    public void GhostEaten(Vector2 GhostPos) {
        GhostsEaten += 1;

        // Increase score by 200 and display updated score.
        SetScore(_score + 200 * (int) Math.Pow(2, GhostsEaten-1));
        if (_score >= 10000 && _score - (200 * (int) Math.Pow(2, GhostsEaten-1)) < 10000) {
            Lives++;
            _ui.RefreshLivesTextures(Lives);
        }

        SpawnFloatingText((200 * (int) Math.Pow(2, GhostsEaten-1)).ToString(), GhostPos);
    }

    public void SpawnFloatingText(String content, Vector2 pos) {
        FloatingText GhostEatIndicator = (FloatingText) (ResourceLoader.Load<PackedScene>("res://misc/FloatingText.tscn")).Instance();
        GhostEatIndicator.Text = content;
        GhostEatIndicator.RectPosition += pos;
        this.AddChild(GhostEatIndicator);
    }

    public Vector2 RequestMove(Vector2 pos, Vector2 dir, float CharWidth, MoveParam[] MoveModifiers = null) {
        MoveModifiers = MoveModifiers ?? new MoveParam[0]; // Instantiate empty MoveParam array if not passed.

        // Do not approve move request if maze is not yet loaded.
        if (_maze == null) {
            return pos + new Vector2(CharWidth, CharWidth) / 2;
        }

        // Create types of tiles allowed to move through.
        // Note that if MoveModifiers contains MoveParam.IMMOBILE, this array will only contain 1 invalid tile that can be moved through (index -2).
        int[] TilesAllowed = 
            (!Array.Exists(MoveModifiers, m => m.Equals(MoveParam.IMMOBILE)) ? ( new int[] {
                -1,
                (Array.Exists(MoveModifiers, m => m.Equals(MoveParam.PASS_THROUGH_GATES)) ? 1 : -1) // Allow ghost gate movement if retreating or emerging.
            }) : ( new int[] {
                -2,
                -2
            }
        ));

        // GD.Print("Req: " + (pos/4).Floor() + " to move " + dir + ", width " + CharWidth);
        // pos should be in pixels, in top-left corner form.
        // dir should be a normalized directional Vector2. (ie. Vector2(0, 1))
        // CharWidth should be in units of 4x4px squares.
        Vector2 GridPos = (pos / 4).Floor();
        for (double x = Math.Round(GridPos.x); x < GridPos.x + CharWidth/4; x++) {
            for (double y = Math.Round(GridPos.y); y < GridPos.y + CharWidth/4; y++) {
                // Check if each cell in _maze which will be moved into has a value contained within TilesAllowed. Otherwise, return pos.
                
                if (!Array.Exists(TilesAllowed, t => t.Equals(_maze.GetCell((int) (x + dir.x), (int) (y + dir.y))) )) {
                    // GD.Print("Fail: (" + (x + dir.x) + ", " + (y + dir.y) + ")");
                    return pos + new Vector2(CharWidth, CharWidth) / 2;
                }
            }
        }

        // If prior checks are successful, return new position for caller (in px).
        // GD.Print("Success! ");

        Vector2 NewPosCorner = pos + (dir*4);
        if (NewPosCorner.x <= MinX - CharWidth) {
            // Warp if moving left off-screen.
            return NewPosCorner + new Vector2(MaxX, 0) + new Vector2(CharWidth, 0) + (new Vector2(CharWidth, CharWidth)/2);
        } else if (NewPosCorner.x >= MaxX) {
            // Warp if moving right off-screen.
            return NewPosCorner - new Vector2(MaxX, 0) - (new Vector2(CharWidth, -CharWidth)/2);
        } else if (NewPosCorner.y <= MinY - CharWidth) {
            // Warp if moving up off-screen.
            return NewPosCorner + new Vector2(0, MaxY) + new Vector2(0, CharWidth) + (new Vector2(CharWidth, CharWidth)/2);
        } else if (NewPosCorner.y >= MaxY) {
            // Warp if moving down off-screen.
            return NewPosCorner - new Vector2(0, MaxY) - (new Vector2(-CharWidth, CharWidth)/2);
        } else {
            // No warping.
            return NewPosCorner + (new Vector2(CharWidth, CharWidth)/2);
        }
    }

    public void AddPellet(Pellet _newPellet) {
        _newPellet.Connect("PelletEaten", this, "RemovePellet");
        _activePellets.Add(_newPellet);
        this.AddChild(_newPellet);
    }

    public void RemovePellet(Pellet PelletRemoved, int StunTime) {
        SetScore(_score + PelletRemoved.GetValue());
        if (_score >= 10000 && _score - PelletRemoved.GetValue() < 10000) {
            Lives++;
            _ui.RefreshLivesTextures(Lives);
        }

        _activePellets.Remove(PelletRemoved);

        SFX_PelletEatenPlayer.Play(0);

        if (StunTime > 0) {
            // Enter scared mode.
            _phaseTimer.Paused = true;
            EmitSignal("SwitchBehaviourMode", GhostBehaviour.FLEE);
            GetNode<Label> ("PacUI/DebugPanel/PhaseLabel").Text = "FLEE";

            // Set timer to end scared mode.
            scaredTimer.QueueFree();
            EmitSignal("GhostFlashingAnimation", false);
            scaredTimer = new Timer();
            scaredTimer.ProcessMode = (Timer.TimerProcessMode) 0;
            scaredTimer.WaitTime = StunTime - 4;
            this.AddChild(scaredTimer);
            scaredTimer.Connect("timeout", this, "_EndScaredModeAnim", new Godot.Collections.Array(scaredTimer));
            scaredTimer.Start(StunTime - 4);

            GhostsEaten = 0;
        }

        if (_activePellets.Count <= 0) {
            _maze.GetNode<AnimationPlayer>("EndLevelAnimPlayer").Connect("animation_finished", this, "EndLevel");
            _maze.GetNode<AnimationPlayer>("EndLevelAnimPlayer").Play("levelend");
            GetNode<Main>("..").PauseGame();
        }
    }

    public void AddScore(int value) {
        _score += value;
        _taskScoreTracker += value;

        // Check if it is time to spawn a task.
        if (_score >= _taskScoreLimit && _score - value < _taskScoreLimit) {
            _taskScoreLimit += 4000;
            SpawnTask();
        }
    }

    public void SpawnTask() {
        PacTask t = (PacTask) (ResourceLoader.Load("res://pickups/PacTask.tscn") as PackedScene).Instance();
        t.SetTaskType(Level-1 < _ui.LevelTexturesSequence.Length ? _ui.LevelTexturesSequence[Level-1] : 7);
        t.Connect("TaskCompleted", this, "_TaskCompleted");
        t.Position = TaskPoint;
        this.CallDeferred("add_child", t);
    }

    public void _TaskCompleted(int value) {
        SetScore(_score + value);
    }

    public void _EndScaredModeAnim(Timer scaredTimer) {
        scaredTimer.Disconnect("timeout", this, "_EndScaredModeAnim");
        scaredTimer.Connect("timeout", this, "_EndScaredMode");
        EmitSignal("GhostFlashingAnimation", true);
        scaredTimer.Start(4);
    }

    public void _EndScaredMode() {
        EmitSignal("GhostFlashingAnimation", false);
        this.GhostsEaten = 0;
        scaredTimer.QueueFree();
        _phaseTimer.Paused = false;
        EmitSignal("SwitchBehaviourMode", LevelBehaviourPhase);

        GetNode<Label> ("PacUI/DebugPanel/PhaseLabel").Text = LevelBehaviourPhase.ToString();
    }

    public void EndLevel(String animName) {
        EmitSignal("LevelComplete", Level+1);
    }

    public void SetGhostScatterPoint(Vector2 pos, int index) {
        GhostScatterPoints[index] = pos;
    }

    public Vector2 GetGhostScatterPoint(int index) {
        return GhostScatterPoints[index];
    }

    public PogMan GetPlayer() {
        return _player;
    }

    public Maze GetMaze() {
        return _maze;
    }

    public PelletBoard GetPelletBoard() {
        return _pelletBoard;
    }

    public int GetScore() {
        return _score;
    }

    public void SetScore(int score) {
        AddScore(score - _score);
        _ui.GetNode<Label> ("UIPanel/ScoreLabel").Text = ("SC  " + _score.ToString().PadLeft(8).Replace(' ', '0'));
        GD.Print(_score);
    }

    public Vector2 GetStartingPos() {
        return _startingPos;
    }

    public Vector2 GetPlayerPosition() {
        return _player.Position;
    }
}
