using Godot;
using System;

public class FloatingText : Label {
    public override void _Ready() {
        // this.PauseMode = Node.PauseModeEnum.Process;

        // GetNode("../..").Call("PauseGame");

        Timer LabelExpiration = new Timer();
        LabelExpiration.Autostart = true;
        LabelExpiration.WaitTime = 0.6f;
        LabelExpiration.Connect("timeout", this, "_Timeout");
        this.AddChild(LabelExpiration);
    }

    public void _Timeout() {
        // GetNode("../..").Call("PauseGame");
        this.QueueFree();
    }
}
