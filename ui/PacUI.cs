using Godot;
using System;

public class PacUI : Node2D {
    public HBoxContainer LevelContainer;
    public Texture TasksTexture;
    public AtlasTexture[] LevelTextures = new AtlasTexture[9];
    public TextureRect[] LevelTexturesNodes = new TextureRect[19];
    public TextureRect[] DisplayedLevelTextures = new TextureRect[7];
    public int[] LevelTexturesSequence = new int[] {
        0,
        1,
        2, 2,
        3, 3,
        4, 4,
        5, 5,
        6, 6,
        7, 7, 7, 7, 7, 7, 7
    };

    public int amount = 0;

    AtlasTexture LiveTexture;
    AtlasTexture EmptyLiveTexture;

    TextureRect[] LivesTextureRects = new TextureRect[4];

    public override void _Ready() {
        // Initialize LiveTexture and EmptyLiveTexture.
        LiveTexture = new AtlasTexture();
        LiveTexture.Atlas = GetNode<Main>("../..").PogManTexture;
        LiveTexture.Region = new Rect2(0, 16, 16, 16);

        EmptyLiveTexture = null;

        LevelContainer = GetNode("UIPanel/LevelContainer") as HBoxContainer;
        TasksTexture = ResourceLoader.Load("res://pickups/PacTasks.png") as Texture;

        // Initialize LevelTextures array.
        for (int x = 0; x < LevelTextures.Length; x++) {
            AtlasTexture t = new AtlasTexture();
            t.Atlas = TasksTexture;
            t.Region = new Rect2(x*16, 0, 16, 16);
        }

        // Initialize TextureRect nodes representing LevelTexturesSequence.
        for (int x = 0; x < LevelTexturesNodes.Length; x++) {
            LevelTexturesNodes[x] = new TextureRect();
            LevelTexturesNodes[x].Texture = LevelTextures[LevelTexturesSequence[x]];
        }

        // Initialize empty TextureRect nodes that will actually be modified, stored in DisplayedLevelTextures.
        for (int x = 0; x < DisplayedLevelTextures.Length; x++) {
            DisplayedLevelTextures[x] = new TextureRect();
            LevelContainer.AddChild(DisplayedLevelTextures[x]);
        }


        RefreshLevelTextures((int) GetNode("..").Get("Level"));
        RefreshLivesTextures((int) GetNode<PacLevel>("..").Get("Lives"));
    }

    public void RefreshLevelTextures(int level) {
        int StartIndex = level-7;
        int EndIndex = level;

        int displayedNodesIndex = 0;

        for (int x = StartIndex; x < EndIndex; x++) {
            AtlasTexture a = new AtlasTexture();
            a.Atlas = TasksTexture;

            // Paint index in LevelTexturesSequence if 0 <= index <= 18, if < 0 paint blank 16x16 (index 8), or if > LevelTexturesSequence.Length paint index 7.
            if (x >= 0) {
                if (x <= LevelTexturesSequence.Length - 1) {
                    a.Region = new Rect2(LevelTexturesSequence[x]*16, 0, 16, 16);
                } else {
                    a.Region = new Rect2(7*16, 0, 16, 16);
                }
            } else {
                a.Region = new Rect2(8*16, 0, 16, 16);
            }

            DisplayedLevelTextures[displayedNodesIndex].Texture = a;
            displayedNodesIndex++;
        }
    }

    public void RefreshLivesTextures(int Lives) {
        for (int x = 0; x < 4; x++) {
            LivesTextureRects[x] = GetNode<TextureRect>("UIPanel/LivesContainer/TextureRect" + (x+1));
        }

        for (int x = 0; x < LivesTextureRects.Length; x++) {
            if (Lives > x) {
                LivesTextureRects[x].Texture = LiveTexture;
            } else {
                LivesTextureRects[x].Texture = EmptyLiveTexture;
            }
        }
    }
}
