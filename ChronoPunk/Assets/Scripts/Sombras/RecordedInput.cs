using UnityEngine;

public enum InputActionType
{
    Move,
    Jump,
    Shoot,
    Aim,
    StopRecording
}

public abstract class RecordedInput
{
    public int fixedFrameStamp;
    public InputActionType actionType;
}

public class MoveInput : RecordedInput
{
    public Vector2 direction;
}

public class JumpInput : RecordedInput
{
    public bool isPressed; // true para presionar, false para soltar (JumpCut)
}

public class ShootInput : RecordedInput { } // No necesita campos adicionales

public class StopRecordingInput : RecordedInput { } // No necesita campos adicionales
