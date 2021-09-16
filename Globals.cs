using Godot;
using System;

namespace Globals {
    public class Globals : Node {};

    public enum MoveParam {
        PASS_THROUGH_GATES = 0,
        IMMOBILE = 1
    };

    public enum GhostBehaviour {
        EMERGE = 0,
        CHASE_1 = 1,
        CHASE_2 = 2,
        CHASE_3 = 3,
        CHASE_4 = 4,
        SCATTER = 5,
        FLEE = 6,
        EATEN = 7,
        DYING = 8
    };
}
