using Godot;
using System;

public class PacMenuScreen : Node2D {
    private Main MainScene;
    private static Container mainMenuPanel;
    private static Container loadingPanel;
    private static Container optionsMenuPanel;
    private static Container highScorePanel;
    private static Container creditsPanel;
    private static Container registerPanel;

    public Godot.Collections.Dictionary<String, Container> MenuPanels;

    public Container CurrentPanel = null;
    public OptionsPane CurrentOptionsPane = null;

    public override void _Ready() {
        MainScene = GetNode<Main>("..");

        mainMenuPanel = GetNode<Container>("MainMenuPanel");
        loadingPanel = GetNode<Container>("LoadingPanel");
        optionsMenuPanel = GetNode<Container>("OptionsMenuPanel");
        creditsPanel = GetNode<Container>("CreditsPanel");
        highScorePanel = GetNode<Container>("HighScorePanel");
        registerPanel = GetNode<Container>("RegisterPanel");

        MenuPanels = new Godot.Collections.Dictionary<String, Container>() {
            {"MAIN", mainMenuPanel},
            {"LOADING", loadingPanel},
            {"OPTIONS", optionsMenuPanel},
            {"HI-SCORES", highScorePanel},
            {"CREDITS", creditsPanel},
            {"REGISTER", registerPanel}
        };

        foreach (var menuKeyPair in MenuPanels) {
            OptionsPane menuOptPane = menuKeyPair.Value.GetNode<OptionsPane>("ColorRect/OptionsPane");
            menuOptPane.Connect("OptionSelected", this, "MenuOptionSelected");
            menuOptPane.Connect("SwitchPogManSkin", MainScene, "SwitchPogManSkin");
            foreach (String presetOption in menuOptPane.OPTIONS_LIST) {
                menuOptPane.AddOption(presetOption);
            }
        }

        String[] HighScoreKeys = new String[7];
        MainScene.HighScores.Keys.CopyTo(HighScoreKeys, 0);
        int[] HighScoreValues = new int[7];
        MainScene.HighScores.Values.CopyTo(HighScoreValues, 0);

        DisplayMenu("MAIN");
        mainMenuPanel.GetNode<OptionsPane>("ColorRect/OptionsPane").SetPointer(0);
    }

    public override void _Input(InputEvent inputEvent) {
        if (CurrentPanel != null && CurrentOptionsPane != null) {
            if (Input.IsActionPressed("ui_up")) CurrentOptionsPane.MovePointer(-1);
            else if (Input.IsActionPressed("ui_down")) CurrentOptionsPane.MovePointer(1);
            else if (Input.IsActionPressed("ui_left")) CurrentOptionsPane.MoveHPointer(-1);
            else if (Input.IsActionPressed("ui_right")) CurrentOptionsPane.MoveHPointer(1);
            else if (Input.IsActionPressed("ui_accept")) CurrentOptionsPane.ChooseOption(inputEvent);
        }
    }

    public void MenuOptionSelected(String optionName, OptionsPane pane) {
        switch (optionName) {
            case "START":
                MainScene.LevelTemplatePath = "res://level/PacLevel.tscn";
                DisplayMenu("NONE");
                MainScene.SwitchPacLevel(1);
                break;
            case "LVLDEBUG":
                GD.Print("LVLDEBUG");
                MainScene.LevelTemplatePath = "res://level/PacLevelEZ.tscn";
                MainScene.DebugMode = true;
                MainScene.SwitchPacLevel(1);
                break;
            case "EXIT":
                GetTree().CallDeferred("quit");
                break;
            case "RETURN":
                DisplayMenu(pane.RETURN_SCREEN);
                break;
            default:
                DisplayMenu(optionName);
                break;
        }
    }

    public void DisplayMenu(Container contRef) {
        CurrentPanel = contRef;
        CurrentOptionsPane = CurrentPanel.GetNodeOrNull<OptionsPane>("ColorRect/OptionsPane");

        foreach (var p in MenuPanels) {
            if (p.Value.Equals(contRef)) {
                p.Value.Visible = true;
            } else {
                p.Value.Visible = false;
            }
        }
    }

    public void DisplayMenu(String dictRef) {
        if (!dictRef.Equals("NONE")) {
            CurrentPanel = MenuPanels[dictRef];
            CurrentOptionsPane = CurrentPanel.GetNodeOrNull<OptionsPane>("ColorRect/OptionsPane");
        }

        foreach (var p in MenuPanels) {
            if (p.Key.Equals(dictRef)) {
                p.Value.Visible = true;
            } else {
                p.Value.Visible = false;
            }
        }
    }
}
