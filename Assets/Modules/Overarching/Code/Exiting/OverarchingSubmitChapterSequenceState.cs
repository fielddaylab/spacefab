using BeauRoutine;
using FieldDay;
using FieldDay.SharedState;
using SpaceFab.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Overarching
{
    public enum OverarchingSubmitChapterPhase
    {
        Waiting,
        Starting,
        ShutdownSequenceSystem,
        MoveToNextChapter,
        TransitionComplete
    }

    public class OverarchingSubmitChapterSequenceState : SharedStateComponent, IRegistrationCallbacks
    {
        public OverarchingSubmitChapterPhase Phase;
        public DynamicButton SubmitButton;

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            SubmitButton.onClick.AddListener(() => {
                GameLoop.ResumeUpdates(UpdateMasks.ShutdownMask);
                Find.State<OverarchingSubmitChapterSequenceState>().Phase = OverarchingSubmitChapterPhase.Starting;
                });
        }
    }
}
