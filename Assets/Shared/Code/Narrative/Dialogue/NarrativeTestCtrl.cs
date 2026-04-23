using FieldDay.Scenes;
using FieldDay.Scripting;

public class NarrativeTestCtrl : SceneController {
    protected override void OnSceneReady() {
        ScriptUtility.Trigger("TestTrigger");
    }
}