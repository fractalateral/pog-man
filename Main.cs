using Godot;
using System;

using System.Collections.Generic;

public class Main : Node {
    // Main scene that will not be deleted throughout the game.

    [Signal]
    public delegate void GameStart();

    public PacLevel CurrentLevel = new PacLevel();
    public String LevelTemplatePath = "res://level/PacLevel.tscn";
    public PacMenuScreen MenuScreen;

    public bool DebugMode = false;

    public Texture PogManTexture = ResourceLoader.Load("res://player/PogManCharacter.png") as Texture;

    public Godot.Collections.Dictionary<String, int> HighScores = new Godot.Collections.Dictionary<String, int>();

    private int carriedScore;
    private int carriedLives;

    public int finalScore = 0;
    
    public override void _Ready() {
        MenuScreen = GetNode<PacMenuScreen>("PacMenuScreen");

        LoadHighScores();
    }

    public void SwitchPacLevel(int LevelNumber) {
        carriedScore = CurrentLevel.GetScore();
        carriedLives = CurrentLevel.Lives;

        CurrentLevel.QueueFree();

        CurrentLevel = (PacLevel) (ResourceLoader.Load(LevelTemplatePath) as PackedScene).Instance();

        CurrentLevel.Level = LevelNumber;
        CurrentLevel.StartingScore = carriedScore;
        CurrentLevel.Lives = carriedLives;

        CurrentLevel.Connect("LevelComplete", this, "SwitchPacLevel");
        CurrentLevel.Connect("EndGame", this, "_GameEndSequence");
        AddChild(CurrentLevel);

        PauseGame(false);
        EmitSignal("GameStart");
    }

    public void SwitchPogManSkin(String path) {
        PogManTexture = ResourceLoader.Load(path) as Texture;
    }

    public void PauseGame(bool? forceState = null) {
        // Toggle whether game is paused. Optional parameter to force paused true or false.
        GetTree().Paused = (forceState != null ? (bool) forceState : !GetTree().Paused);
    }

    public void _GameEndSequence(int finalScore) {
        PauseGame(true);

        this.finalScore = finalScore;
        GetNode<PacMenuScreen>("PacMenuScreen").DisplayMenu("REGISTER");

        CurrentLevel.QueueFree();

        CurrentLevel = new PacLevel();
        PauseGame(false);
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (@event is InputEventKey eventKey) {
            if (eventKey.Pressed && eventKey.Scancode == (int) KeyList.Escape) {
                PauseGame();
            }
        }
    }

    public void SaveHighScores() {
        File savedScores = new File();
        savedScores.Open("user://savedScores.save", File.ModeFlags.Write);

        savedScores.StoreLine(JSON.Print(HighScores));
        savedScores.Close();
    }

    public void LoadHighScores() {
        File savedScores = new File();
        savedScores.Open("user://savedScores.save", File.ModeFlags.Read);

        HighScores = new Godot.Collections.Dictionary<string, int>((Godot.Collections.Dictionary) JSON.Parse(savedScores.GetLine()).Result);
        savedScores.Close();

        // Size() method doesn't work. Bluh.
        int HighScoresLength = 0;
        foreach (KeyValuePair<String, int> kv in HighScores) {
            HighScoresLength++;
        }

        // Get array of HighScores elements.
        KeyValuePair<String, int>[] keyValuePairScores = new KeyValuePair<String, int>[HighScoresLength];
        int keyValueIterator = 0;
        foreach (KeyValuePair<String, int> kv in HighScores) {
            keyValuePairScores[keyValueIterator] = kv;
            keyValueIterator++;
        }

        // Sort keyValuePairScores.
        Array.Sort(keyValuePairScores, delegate(KeyValuePair<String, int> A, KeyValuePair<String, int> B) {
            return B.Value.CompareTo(A.Value);
        });

        int optionCounter = 0;
        foreach (KeyValuePair<String, int> kv in keyValuePairScores) {
            if (optionCounter < 7) {
                GetNode<OptionsPane>("PacMenuScreen/HighScorePanel/ColorRect/OptionsPane").AddOptionNoSelect(kv.Key + ": " + kv.Value.ToString().PadLeft(8));
                optionCounter++;
            } else {
                break;
            }
        }
        while (optionCounter < 7) {
            GetNode<OptionsPane>("PacMenuScreen/HighScorePanel/ColorRect/OptionsPane").AddOptionNoSelect("-");
            optionCounter++;
        }
        GetNode<OptionsPane>("PacMenuScreen/HighScorePanel/ColorRect/OptionsPane").AddOption("RETURN");
    }
}
