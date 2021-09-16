using Godot;
using System;

public class PelletBoard : TileMap {
    public override void _Ready() {
        PacLevel LevelScene = GetNode<PacLevel>("..");
        this.Visible = false;
        
        // Spawn normal pellets.
        foreach (Vector2 cell in GetUsedCellsById(0)) {
            Pellet _newPellet = (Pellet) (ResourceLoader.Load<PackedScene>("res://pickups/Pellet.tscn")).Instance();
            _newPellet.Position = cell * 2 + this.Position;
            _newPellet.SetPelletType(0);
            LevelScene.CallDeferred("AddPellet", _newPellet);
        }

        // Spawn big pellets.
        foreach (Vector2 cell in GetUsedCellsById(1)) {
            Pellet _newPellet = (Pellet) (ResourceLoader.Load<PackedScene>("res://pickups/Pellet.tscn")).Instance();
            _newPellet.Position = cell * 2 + this.Position;
            _newPellet.SetPelletType(1);
            LevelScene.CallDeferred("AddPellet", _newPellet);
        }

        // Set ghost scatter locations.
        Godot.Collections.Array GhostScatterCells = GetUsedCellsById(2);
        for (int x = 0; x < GhostScatterCells.Count; x++) {
            LevelScene.CallDeferred("SetGhostScatterPoint", 2*((Vector2) GhostScatterCells[x]) + new Vector2(1, 1), x);
        }
    }
}
