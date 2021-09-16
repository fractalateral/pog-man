using Godot;
using System;

using System.Collections.Generic;

public class OptionsPane : Container {
    [Export]
    public Godot.Collections.Array<String> OPTIONS_LIST;
    [Export]
    public String RETURN_SCREEN; // Index or path.

    [Signal]
    public delegate void SwitchPogManSkin(String path);

    public String[] PacSkinsPaths = new String[] {
        "res://player/PogManCharacter.png",
        "res://player/PogMan_OG.png",
        "res://player/PogMan_Pepe.png"
    };

    public Texture PointerTexture;
    public Texture BlankTexture;

    [Signal]
    public delegate void OptionSelected(String optionName, OptionsPane pane);

    public int PointerIndex = 0;
    public int HPointerIndex = 0;
    public int VisualSwapperIndex = 0;

    private Node OptionTemplate;
    public OptionsPane parentMenu;
    public PacMenuScreen parentPacMenuScreen;

    public List<Node> Options = new List<Node> ();
    

    public override void _Ready() {
        PointerTexture = BlankTexture = ResourceLoader.Load("res://ui/PacPointerPlaceholder.png") as Texture;
        BlankTexture = ResourceLoader.Load("res://ui/black16x16.png") as Texture;

        OptionTemplate = GetNode("Option1");
        GetNode("Option1").QueueFree();
        GetNode("Option2").QueueFree();

        parentPacMenuScreen = GetNode<PacMenuScreen>("../../..");
    }

    public void AddOption(String text) {
        if (text.Length.Equals(1)) {
            Options.Add(OptionTemplate.Duplicate());
            Options[Options.Count - 1].GetNode<Label>("Label").Text = text;
            AddChild(Options[Options.Count - 1]);
        } else if (text.Substring(0, (text.Length < 5 ? 0 : 5)).Equals(".SWAP")) {
            this.AddVisualSwapper((String[]) this.Get(text.Substring(6, text.Length-6)));
        } else {
            Options.Add(OptionTemplate.Duplicate());
            Options[Options.Count - 1].GetNode<Label>("Label").Text = text;
            AddChild(Options[Options.Count - 1]);
        }
    }

    public void AddOptionNoSelect(String text) {
        Node OptionNoSelect = OptionTemplate.Duplicate();
        OptionNoSelect.GetNode<Label>("Label").Text = text;
        OptionNoSelect.GetNode<TextureRect>("Selector").QueueFree();
        AddChild(OptionNoSelect);
    }

    public void AddVisualSwapper(String[] TexturePaths) {
        Options.Add(OptionTemplate.Duplicate());
        Options[Options.Count - 1].GetNode<Label>("Label").QueueFree();
        TextureRect SwappableTextureNode = new TextureRect();
        SwappableTextureNode.Name = "SwappableTextureNode";
        SwappableTextureNode.Texture = new AtlasTexture();
        ((AtlasTexture) SwappableTextureNode.Texture).Atlas = ResourceLoader.Load(TexturePaths[0]) as Texture;
        ((AtlasTexture) SwappableTextureNode.Texture).Region = new Rect2(0, 16, 16, 16);

        Options[Options.Count - 1].AddChild(SwappableTextureNode);
        AddChild(Options[Options.Count - 1]);
    }

    public void RemoveOption(String text) {
        // To be implemented.
    }

    public void AddOptionAt(String text, int index) {
        // To be implemented.
    }

    public void RemoveOptionAt(int index) {
        Options[index].QueueFree();
        Options.RemoveAt(index);
    }

    public void SetPointer(int index) {
        PointerIndex = index;

        if (PointerIndex >= Options.Count) PointerIndex -= Options.Count;
        else if (PointerIndex < 0) PointerIndex += Options.Count;

        for (int x = 0; x < Options.Count; x++) {
            Options[x].GetNode<TextureRect> ("Selector").Texture = (x == PointerIndex ? PointerTexture : BlankTexture);
        }
    }

    public void MovePointer(int amount) {
        SetPointer(PointerIndex + amount);
    }

    public void MoveHPointer(int amount) {
        HPointerIndex += amount;
        if (Options[PointerIndex].GetNodeOrNull<Label>("Label") != null) {
            if (Options[PointerIndex].GetNode<Label>("Label").Text.Length == 1) {
                char swappableChar = Options[PointerIndex].GetNode<Label>("Label").Text[0];
                swappableChar = (char) ((int) swappableChar + amount);
                if (swappableChar < (int) 'A') swappableChar = (char) ((int) swappableChar + 26);
                else if (swappableChar > (int) 'Z') swappableChar = (char) ((int) swappableChar - 26);
                Options[PointerIndex].GetNode<Label>("Label").Text = swappableChar.ToString();
            }
        }
    }

    public void ChooseOption(InputEvent inputEvent) {
        // Chooses option at index PointerIndex.
        if (Options[PointerIndex].GetNodeOrNull("Label") == null) {
            ((AtlasTexture) (Options[PointerIndex].GetNode<TextureRect>("SwappableTextureNode").Texture)).Atlas = ResourceLoader.Load(PacSkinsPaths[VisualSwapperIndex]) as Texture;
            EmitSignal("SwitchPogManSkin", PacSkinsPaths[VisualSwapperIndex]);
            VisualSwapperIndex += 1;
            if (VisualSwapperIndex >= PacSkinsPaths.Length) VisualSwapperIndex = 0;
        } else {
            EmitSignal("OptionSelected", Options[PointerIndex].GetNode<Label>("Label").Text, this);
        }
    }
}
