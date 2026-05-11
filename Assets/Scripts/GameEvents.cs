/*
Global Event System
*/

using System;

public static class GameEvents
{
    public static Action<int> OnPickupCollected;

    // Tutorial Detection actions
    public static Action<string> OnZoneEnter;
}
