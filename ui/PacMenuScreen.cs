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

    public String[] PacSkinsPaths = new String[] {
        "res://player/PogManCharacter.png",
        "res://player/PogMan_OG.png",
        "res://player/PogMan_Pepe.png"
    };

    private String[] MainMenuPanel_Options = new String[] {
        "START",
        "OPTIONS",
        "CREDITS",
        "HI-SCORES",
        "LVLDEBUG",
        "EXIT"
    };

    private String[] OptionsMenuPanel_Options = new String[] {
        ".VAR PacSkinsPaths",
        "RETURN"
    };

    private String[] CreditsMenuPanel_Options = new String[] {
        ".VAR Credits",
        "RETURN"
    };

    private String[] RegisterMenuPanel_Options = new String[] {
        "A",
        "A",
        "A",
        "REGISTER",
        "RETURN"
    };

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

    public void MainMenuOptionSelected(String optionName, int optionIndex) {
        switch (optionName) {
            case "START":
                MainScene.LevelTemplatePath = "res://level/PacLevel.tscn";
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
            default:
                DisplayMenu(optionName);
                break;
        }
    }

    /*public void OptionsMenuOptionSelected(String optionName, int optionIndex) {
        switch (optionName) {
            case "RETURN":
                DisplayMenu(0);
                break;
        }
    }

    public void CreditsMenuOptionSelected(String optionName, int optionIndex) {
        switch (optionName) {
            case "RETURN":
                DisplayMenu(0);
                break;
        }
    }

    public void HighScoreMenuOptionSelected(String optionName, int optionIndex) {
        switch (optionName) {
            case "RETURN":
                DisplayMenu(0);
                break;
        }
    }

    public void RegisterMenuOptionSelected(String optionName, int optionIndex) {
        switch (optionName) {
            case "REGISTER":
                String registerName = "";
                foreach (Node n in CurrentOptionsPane.Options) {
                    if (n.GetNode<Label>("Label").Text.Length == 1) {
                        registerName += n.GetNode<Label>("Label").Text;
                    }
                }
                if (!MainScene.HighScores.ContainsKey(registerName)) MainScene.HighScores.Add(registerName, MainScene.finalScore);
                else if (((Godot.Collections.Dictionary<String, int>)MainScene.HighScores)[registerName] < MainScene.finalScore) MainScene.HighScores[registerName] = MainScene.finalScore;
                MainScene.SaveHighScores();

                DisplayMenu(0);
                break;
            case "RETURN":
                DisplayMenu(0);
                break;
        }
    }*/
/*
    public void DisplayMenu(int index) {
        if (index >= 0) {
            CurrentPanel = MenuPanels[index];
            CurrentOptionsPane = CurrentPanel.GetNodeOrNull<OptionsPane>("ColorRect/OptionsPane");

            for (int x = 0; x < MenuPanels.Length; x++) {
                if (x == index) {
                    MenuPanels[x].Visible = true;
                } else {
                    MenuPanels[x].Visible = false;
                }
            }
        } else {
            CurrentPanel = null;
            CurrentOptionsPane = null;

            for (int x = 0; x < MenuPanels.Length; x++) {
                MenuPanels[x].Visible = false;
            }
        }
    }

    public void DisplayMenu(String path) {
        CurrentPanel = GetNode<Container>(path);
        CurrentOptionsPane = CurrentPanel.GetNodeOrNull<OptionsPane>("ColorRect/OptionsPane");

        for (int x = 0; x < MenuPanels.Length; x++) {
            if (MenuPanels[x].Name.Equals(path)) {
                MenuPanels[x].Visible = true;
            } else {
                MenuPanels[x].Visible = false;
            }
        }
    }*/

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
