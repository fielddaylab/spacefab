using System;


static public class LayerMasks {
    
    // Layer 0: Default
    public const int Default_Index = 0;
    public const int Default_Mask = 1;
    // Layer 1: TransparentFX
    public const int TransparentFX_Index = 1;
    public const int TransparentFX_Mask = 2;
    // Layer 2: Ignore Raycast
    public const int IgnoreRaycast_Index = 2;
    public const int IgnoreRaycast_Mask = 4;
    // Layer 4: Water
    public const int Water_Index = 4;
    public const int Water_Mask = 16;
    // Layer 5: UI
    public const int UI_Index = 5;
    public const int UI_Mask = 32;
    // Layer 24: InterruptUI
    public const int InterruptUI_Index = 24;
    public const int InterruptUI_Mask = 16777216;
}
static public class SortingLayers {
    
    // Layer Default
    public const int Default = 0;
    // Layer GridBase
    public const int GridBase = 1225722235;
    // Layer GridNodes
    public const int GridNodes = -1740079061;
    // Layer GridOverlay
    public const int GridOverlay = 1497403345;
    // Layer UI
    public const int UI = 264871581;
    // Layer Pop-Up UI
    public const int Pop_UpUI = 1568843517;
    // Layer Shared UI
    public const int SharedUI = 1447053067;
}
static public class UnityTags {
    
    // Tag Untagged
    public const string Untagged = "Untagged";
    // Tag Respawn
    public const string Respawn = "Respawn";
    // Tag Finish
    public const string Finish = "Finish";
    // Tag EditorOnly
    public const string EditorOnly = "EditorOnly";
    // Tag MainCamera
    public const string MainCamera = "MainCamera";
    // Tag Player
    public const string Player = "Player";
    // Tag GameController
    public const string GameController = "GameController";
}