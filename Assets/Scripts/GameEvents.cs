/*
Global Event System
*/

using System;

public static class GameEvents
{
    public static Action<int> OnPickupCollected;

    public static Action OnJump;

    // Tutorial Detection actions, progress checkpoints broadcast the message to display when progress
    // conditions are met.
    public static Action<string> OnZoneEnter;

    //Called when player dies, leaves, is caught or otherwise 
    //True for a successful completiuon, false for a failure
    public static Action<bool> gameOver;

}
