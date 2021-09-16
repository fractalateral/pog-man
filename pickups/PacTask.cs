using Godot;
using System;

public class PacTask : Area2D {
    public int taskType = 0; // Ranges from 0-7.
    private int _value = 100;

    [Signal]
    public delegate void TaskCompleted(int value);

    public override void _Ready() {
        this.Connect("area_entered", this, "_AreaEntered");
        SetTaskType(taskType);

        Timer expireTimer = new Timer();
        expireTimer.WaitTime = 20;
        expireTimer.ProcessMode = (Godot.Timer.TimerProcessMode) 1;
        expireTimer.Connect("timeout", this, "_Expire");
        AddChild(expireTimer);
        expireTimer.Start(0);
    }

    public void _Expire() {
        EmitSignal(nameof(TaskCompleted), 0);
        QueueFree();
    }

    public void SetTaskType(int taskType) {
        this.taskType = taskType;
        ((AtlasTexture) (GetNode<Sprite>("Sprite").Texture)).Region = new Rect2(taskType * 16, 0, 16, 16);
        _value = (int) (100 * Math.Pow(2, taskType));
    }

    public void _AreaEntered(PhysicsBody2D area) {
        if (area.IsInGroup("EatBox")) {
            EmitSignal(nameof(TaskCompleted), _value);

            FloatingText TaskLabel = (FloatingText) (ResourceLoader.Load("res://misc/FloatingText.tscn") as PackedScene).Instance();
            TaskLabel.RectPosition = this.Position - TaskLabel.RectSize/4;
            TaskLabel.Text = this._value.ToString();
            GetNode("..").AddChild(TaskLabel);

            QueueFree();
        }
    }
}
