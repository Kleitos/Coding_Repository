using System;

[System.Flags]
public enum TriggerAnimationBehavior
{
    PlayOnEnter = 1 << 0,
    RepeatWhileInside = 1 << 1,
    StopOnExit = 1 << 2
}
