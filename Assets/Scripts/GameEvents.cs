/*
Global Event System
*/

using System;

public static class GameEvents
{
    public static Action<int> OnPickupCollected;

    // Tutorial Detection actions, progress checkpoints broadcast the message to display when progress
    // conditions are met.
    public static Action<string> OnZoneEnter;
}
