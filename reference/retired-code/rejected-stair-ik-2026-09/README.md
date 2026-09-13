# Rejected articulated stair gait

The author rejected bent knees, planted-foot IK and the special stair gait on
2026-09-13, requesting the original single-piece legs and walking animation with
smooth root height changes. This snapshot is excluded from compilation. Do not
restore it as the movement target. `Controller.Steps.cs` retains collision-tested
low steps and shared camera/model height easing.
