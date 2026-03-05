using BeauUtil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spacefab
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
    }
}