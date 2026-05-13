using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Scenes;
using FieldDay.Scripting;
using Leaf;
using Leaf.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Comic {
    public sealed class ComicController : SceneController, ISceneLoadDependency {
        public SceneReference NextScene;
        
        [Header("-- DEBUG --")]
        [AssetName(typeof(StreamedPack))] public StringHash32 DEBUG_FallbackManifest;

        [NonSerialized] public StringHash32 PackageId;
        [NonSerialized] public StringHash32 ComicId;
        [NonSerialized] public LeafThreadHandle ThreadHandle;

        protected override IEnumerator<WorkSlicer.Result?> OnScenePreload() {
            Game.Scenes.GetLoadContext(this, out SceneRequestContext context);
            StringHash32 name = StringHash32.First(context.Task.Name, Game.IsDevBuild ? DEBUG_FallbackManifest : default);
            if (!name.IsEmpty) {
                PackageId = name;
                Game.Assets.LoadStreamedPackage(name);
            }
            return null;
        }

        protected override void OnSceneEnable() {
            Game.Scenes.RegisterLoadDependency(this);
            if (ComicSequenceManifest.Current != null) {
                ComicId = ComicSequenceManifest.Current.name;
            } else {
                ComicId = null;
            }

            if (!ComicId.IsEmpty) {
                using (var table = TempVarTable.Alloc()) {
                    table.Set("comicId", ComicId);
                    ScriptUtility.Invoke("ComicPreload", table);
                }
            }

            ScriptUtility.OnCutsceneEnd.Register(Finish);
        }

        protected override void OnSceneReady() {
            if (ComicId.IsEmpty) {
                GameLoop.QueuePreUpdate(Finish);
            } else {
                using (var table = TempVarTable.Alloc()) {
                    table.Set("comicId", ComicId);
                    ThreadHandle = ScriptUtility.Trigger("ComicExecute", table);
                    if (!ThreadHandle.IsRunning()) {
                        GameLoop.QueuePreUpdate(Finish);
                    }
                }
            }
        }

        private void Finish() {
            Game.Scenes.LoadMainScene(NextScene);
            Game.Scenes.QueueMainLoadContext(new SceneRequestContext() {
                Entrance = ComicId
            });
        }

        protected override void OnSceneUnload() {
            ScriptUtility.OnCutsceneEnd.Deregister(Finish);
            Game.Scenes.DeregisterLoadDependency(this);
            if (!PackageId.IsEmpty) {
                Game.Assets.UnloadStreamedPackage(PackageId);
            }
        }

        bool ISceneLoadDependency.IsLoaded(SceneLoadPhase loadPhase) {
            return ScriptUtility.CountFunctionThreads() <= 0;
        }
    }
}