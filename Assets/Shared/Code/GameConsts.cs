using BeauUtil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    public static class GameConsts
    {
        public static int StartGameSceneIndex = 0; // index of the first scene -- usually Boot
    }

    public static class GameEvents
    {
        // Title
        static public readonly StringHash32 TitleNewGameGroupClicked = "title:new-game-group-clicked";
        static public readonly StringHash32 TitleContinueGameGroupClicked = "title:continue-game-group-clicked";
        static public readonly StringHash32 TitleOptionsGroupClicked = "title:options-group-clicked";
        static public readonly StringHash32 TitleCreditsClicked = "title:credits-group-clicked";

        static public readonly StringHash32 TitleStartGameClicked = "title:start-game-clicked";
        static public readonly StringHash32 TitleBackFromInputClicked = "title:back-from-input-clicked";

        static public readonly StringHash32 TitleProfileStarting = "title:profile-starting";

        // Save
        public static readonly StringHash32 ProfileSaveBegin = "save:profile-save-begin";
        public static readonly StringHash32 ProfileSaveError = "save:profile-save-error";
        public static readonly StringHash32 ProfileSaveSuccess = "save:profile-save-success";
        public static readonly StringHash32 ProfileSaveAttemptCompleted = "save:profile-save-attempt-completed";

        // Shared
        public static readonly StringHash32 ClickPauseGame = "shared:click-pause-game";
        public static readonly StringHash32 OnGamePaused = "shared:on-game-paused";
        public static readonly StringHash32 ClickResumeGame = "shared:click-resume-game";
        public static readonly StringHash32 OnGameResumed = "shared:on-game-resumed";
    }

    public static class UpdateMasks
    {
        public const int PauseUpdateMask = 1 << 0;
    }

    static public class LayerMasks
    {

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

        // Layer 24: Interrupt UI
        public const int Interrupt_UI_Index = 24;
        public const int Interrupt_UI_Mask = 1 << 24;
    }
}