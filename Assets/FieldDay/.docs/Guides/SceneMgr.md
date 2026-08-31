# Scenes and SceneMgr
## Intro

`SceneMgr`, accessed through `Game.Scenes`, is our custom runtime scene loading pipeline. It supports multiple scene types and a sophisticated callback system at multiple phases of the process.
## Scene References
Scenes are generally referenced through a `SceneReference` object, a serializable struct that contains both the scene path and its GUID. The GUID is not used during builds, but to allow for scenes to be moved around in the editor project hierarchy without breaking your serialized `SceneReference` objects.

`SceneReference` objects can also be created from a scene name by using `SceneReference.FromName(sceneName)`. This can be useful if circumstances make it difficult or awkward to maintain serialized references to certain scenes.
## Scene Types
There are three scene types: `Main`, `Aux`, and `Persistent`. Each is suitable for different purposes.
### Main
There can only be one `Main` scene loaded at a given time Loading a new `Main` scene will force the unload of any existing `Main` scene, as well as any `Aux` scenes (see below). These are the primary scene type. Attempting to load the `Main` scene while it is currently loaded will not normally result in a reload, unless requested as part of the load request parameters.

```csharp
// Loading from a path directly.
// This is available for prototyping purposes but discouraged
// in production code.
Game.Scenes.LoadMainScene("Assets/Scenes/MainScene.unity");

// Loading from a SceneReference.
Game.Scenes.LoadMainScene(someObject.NextSceneToLoad);

// Loading from a temporary SceneReference
// This is preferred to specifying the path manually
Game.Scenes.LoadMainScene(SceneReference.FromName("MainScene"));
```
### Aux
You can load an arbitrary number of `Aux` scenes on top of your `Main` scene. These are supplemental to the `Main` scene and loaded additively. They will be unloaded when the `Main` scene is unloaded, or when a manual unload is requested. They are useful for loading different backgrounds or level content, in addition to swapping out level content based on player progress in a game intended to be more seamless.

`Aux` scenes must be provided a `StringHash32` tag, to help with unloading scenes in batches, and can optionally be transformed by a `Matrix4x4`, to help position the scene where it is needed.

```csharp
Game.Scenes.LoadAuxScene(levelContent.AuxScene, "Content");
Game.Scenes.LoadAuxScene(levelContent.Background, "BG", loadManager.BackgroundPosition.localToWorldMatrix);
```
### Persistent
You can load an arbitrary number of `Persistent` scenes. These are loaded separately from your `Main` and `Aux` scenes, and are not affected by the loading and unloading of those scenes. They can only be unloaded manually. They are useful for loading in persistent interface elements, or content that spans multiple `Main` scenes.
## Load Context
You can pass along a small amount of arbitrary data to the scene load process to later retrieve and use to inform your scene initialization implementation. This is through the `SceneRequestContext` struct. It supports a several default values and space for 16 custom key-value pairs.
### Task
This is the most commonly used `SceneRequestContext` field. It contains either an `Int32` or a `StringHash32` (but not both), depending on what is most needed by the implementation. This might be a level index or a map name, to name a few examples.

```csharp
// tasks can be StringHash32...
SceneRequestContext loadContext = default;
loadContext.Task = "SomeMapName";

// or Int32
loadContext.Task = 4;

// retrieval
// ensure you know which type of data you're using before retrieval.
int taskIndex = loadContext.Task.Index;
StringHash32 mapName = loadContext.Task.Name;
```
### Entrance
This is similar to `Task`, in that it stores either an `Int32` or a `StringHash32`. This is useful for indicating where the player is arriving from. In a game with navigation, this might indicate where the player character should emerge from on the map. It might also inform which context the player is returning from, perhaps a minigame name.

```csharp
SceneRequestContext loadContext = default;
loadContext.Entrance = "TunnelEntranceZY";
```
### Flags
This is a bitfield for a set of 16 boolean values. This has no default or common use case, but could be useful in future titles to indicate some fine-grained information about game state.
```csharp
SceneRequestContext loadContext = default;
loadContext.Flags = PlayerRidingHorseFlag | PreserveMusicFlag;
```
### Key-Value Pairs
`SceneRequestContext` stores up to 16 key-value pairs for use. These have `StringHash32` keys and `Variant` values, which can store integers, floats, booleans, `StringHash32` values, and references to Unity objects.
```csharp
SceneRequestContext loadContext = default;
loadContext.Set("PlayerHealth", 50);
loadContext.Set("PlayerAppearance", "Blue");

// retrieval
int playerHealth = loadContext.Get("PlayerHealth").AsInt();
StringHash32 playerAppearance = loadContext.Get("PlayerAppearance").AsStringHash();
```
### Setting Context
`SceneRequestContext` objects can be submitted to the `SceneMgr` through a few separate functions.

`QueueMainLoadContext` will submit the given load context for an upcoming `Main` scene load. This can be called before or after the `LoadMainScene` call.

`QueueLoadContext` will submit a load context for a specific scene load.

`SetTaggedLoadContext` will submit a load context for all scenes with the given tag.

```csharp
Game.Scenes.LoadMainScene(minigames.PuzzleScene);

SceneRequestContext loadContext = default;
loadContext.Task = playerState.NextPuzzleLevel;
Game.Scenes.QueueMainLoadContext(loadContext);
```
### Reading Context
`SceneRequestContext` objects can be retrieved through the `GetLoadContext` API.

`GetLoadContext(out SceneRequestContext)` will retrieve the context for current main scene.

`GetLoadContext(Scene, out SceneRequestContext)` will retrieve the context for the given scene.

`GetLoadContext(GameObject | Component, out SceneRequestContext)` will retrieve the context for the scene containing the given scene object.

```csharp
if (Game.Scenes.GetLoadContext(out mainSceneContext)) {
  // override certain settings, etc
}

Game.Scenes.GetLoadContext(this, out mySceneContext);
if (mySceneContext.Task.Index == 0) {
  // do something
}
```
## Unloading Scenes
Scenes can be unloaded in three ways: the `Main` unload process, the `UnloadScene` API, and the `UnloadScenesByTag` API.
### Main Scene Unload
When the `Main` scene is unloaded in order to load a new `Main` scene, all loaded `Aux` scenes are unloaded with it.
### UnloadScene
The `UnloadScene` API will unload a scene by its path or `SceneReference`.
```csharp
Game.Scenes.UnloadScene(levelContent.AuxScene);
```
### UnloadScenesWithTag
The `UnloadScenesWithTag` will unload all scenes with the given `StringHash32` tag. This is useful for unloading batches of scenes by type.
```csharp
Game.Scenes.UnloadScenesWithTag("LevelSpecificContent");
```
## Load Phases
The scene loading pipeline is split into multiple phases.

1. **Unload Phase**
  a. If loading a Main scene
    - Invoke `OnMainSceneUnloading` event
    - Execute Main transition unload handler
    - Unload current Main scene and Aux scenes
    - Invoke `OnMainSceneUnloaded` event
2. **Scene File Load Phase**
  a. Load the requested Unity scene file
  b. For each scene file loaded
    - Queue root transform transformations, if a `Matrix4x4` was submitted
    - Queue lightmap import, if needed
    - Queue loads for any dynamically requested subscenes
    - Create scene-local pool root
    - Invoke `OnPrepareScene` event
3. **Preload Phase**
   a. Invoke `OnScenePreload` for all scenes loaded as a result of the **Object Load Phase**.
   b. Step through each `IScenePreload.Preload` callback in order, amortizing the work over multiple frames.
4. **Aux Fence**: If loading a Main scene
   a. Wait until all outstanding `Aux` scene load requests are complete
5. **Late Enable Dependency Fence**
   a. Wait until all registered dependencies are completed
   b. Wait until all queued `StreamedPack` loads are completed
   c. Wait until texture streaming and high-priority file load requests are completed.
6. **Asset Unload Phase**
   a. Unload unused assets
   b. Rebake light probes if needed
   c. If loading a Main scene
    - Run garbage collection
7. **Late Enable Phase**
  a. For each scene file loaded
    - Activate `LateEnable` GameObjects in the scene
    - Invoke all `ISceneLateInitialize.LateInitialize` components in the scene
    - Invoke all `ISceneCustomData.OnLateEnable` scene data
    - Invoke all `QueueOnEnable` actions for the scene
    - Invoke `OnAnySceneEnabled` event
  b. If loading a Main Scene
    - Invoke `OnMainSceneLateEnable` event
8. **Ready Dependency Fence**
   a. Wait until all registered dependencies are completed
   b. Wait until all queued `StreamedPack` loads are completed
   c. Wait until texture streaming and high-priority file load requests are completed.
9. **Ready Phase**
  a. For each scene file loaded
    - Invoke `OnSceneReady` event
    - Invoke all `ISceneLoadHandler.OnSceneLoad` components in the scene
    - Invoke all `ISceneCustomData.OnReady` scene data
    - Invoke all `QueueOnLoad` actions for the scene
  b. If loading a Main scene
    - Invoke `OnMainSceneReady` event
    - Execute Main transition load handler
    
As you can see, there are many callbacks provided throughout the pipeline in which to execute custom code, and several ways of delaying phases until custom work has been completed.

### Queued Callbacks
Each loaded scene maintains a queue of callbacks for `LateEnable`, `Loaded`/`Ready`, and `Unload`. This can be helpful for queuing larger pieces of initialization/unload to occur at specific times during the load process.

```csharp
// you can queue up on the main scene...
Game.Scenes.QueueOnEnable(ActivateSomeObjects);

// Or for the scene containing the specific GameObject/Component.
Game.Scenes.QueueOnEnable(this, ActivateSomeObjects);

// QueueOnLoad and QueueOnUnload support the same argument types
Game.Scenes.QueueOnLoad(StartPlayingAudio);
Game.Scenes.QueueOnUnload(this, ReleaseResources);
```

### Global Events
`SceneMgr` has a few global callbacks during the pipeline.

`OnPrepareScene` executes when a scene file is loaded.
`OnScenePreload` executes when a scene begins preloading.
`OnSceneReady` executes when a scene is fully finished loading.
`OnSceneUnload` executes when a scene begins unloading.

`OnAnySceneEnabled` executes when any scene finishes executing the LateEnable phase.
`OnAnySceneUnloaded` executes when any scene has finished unloading.

`OnMainSceneLateEnable` executes when a Main scene has finished executing the LateEnable phase.
`OnMainSceneReady` executes when a Main scene is fully finished loading.
`OnMainSceneUnloading` executes when a Main scene begins unloading.
`OnMainSceneUnloaded` executes when a Main scene is finished unloading.
### IScenePreload
Any component that implements `IScenePreload` will execute its `Preload` function during the Preload Phase. This function can be used for both lightweight and computationally expensive initialization work. A common approach is to use it to defer some initialization until after Awake, but before the scene is ready.

```csharp
// computationally simple object
// but there may be many of them
public class StickToGround : MonoBehaviour, IScenePreload {
  public IEnumerator<WorkSlicer.Result?> Preload() {
    transform.position = GamePhysics.GetApproximateGroundPosition(transform.position);
    return null;
  }
}

// computationally heavy asset preloader
[PreloadOrder(-100)] // this controls when preload functions are called relative to one another, to help handle dependencies
public class AssetPreloader : MonoBehaviour, IScenePreload {
  public SomeExpensiveAsset[] ExpensiveAssets;
  
  public IEnumerator<WorkSlicer.Result?> Preload() {
    foreach(var asset in ExpensiveAssets) {
      ProcessHeavyInitialization(asset);
      yield return null; // this indicates that this could be a good stopping point for the frame
    }
  }
}
```

This is an example of the `WorkSlicer` API in `BeauUtil`. To summarize, it helps distribute work across multiple frames. This is also known as amortization. It will execute work until there is either no more work remaining or enough milliseconds have passed that work should be paused until the next frame.

A `Preload` function can be written in multiple ways. If you return `null` directly, the work will be executed in a single pass. A `yield return` statement turns it into an iterator/enumerator function, whose work can be distributed across frames.

### ISceneLateInitialize
Any component that implements `ISceneLateInitialize` will execute its `LateInitialize` function during the LateEnable Phase. This can be helpful for initialization that needs to execute after all preloading and asset loading is done, but before the scene can be considered ready.

These can be ordered similarly to `IScenePreload` using the `LateInitializeOrder` attribute.

### ISceneLoadHandler
Any component that implements `ISceneLoadHandler` will execute its `OnSceneLoad` function during the Ready Phase. This can be helpful for things that need to start when the scene is fully ready to be presented to the player.

## Dependencies

### ISceneLoadDependency
Any registered `ISceneLoadDependency` objects will be checked during the Dependency Fence Phases to see if the pipeline can proceed. This can be useful for halting the pipeline until certain assets are loaded, or until a bucket of asynchronous initialization work has been completed.

An `ISceneLoadDependency` can be registered and deregistered using the `RegisterLoadDependency` and `DeregisterLoadDependency` functions.

```csharp
public class LeafPreloadDependency : ISceneLoadDependency {
  public bool IsLoaded(SceneLoadFence loadFence) {
    return ScriptUtility.ActiveThreadCount == 0;
  }
}

// Some initialization code
Game.Scenes.RegisterLoadDependency(new LeafPreloadDependency());
```

**Helpful Tip**: Use this on a `SharedStateComponent`, paired with a `SystemComponent` executing during scene load, to handle systems that handle object spawns, data loading, or other steps, as an implicit part of the pipeline.

### AsyncHandle Dependencies
During any phase of the pipeline, additional asynchronous work can be registered as a dependency for the next Dependency Fence Phase. Using the `Async` API in `BeauRoutine`, work can be queued to execute in the background, either at the end of the frame or, on platforms that support multithreading, on a background thread. This work can then be registered as a dependency, pausing the pipeline at the next fence until it has completed.

You can either schedule the work manually through `Async.Schedule` and pass that into `RegisterLoadDependency`, or through `Jobs.PushLoadDependency`, which executes those two operations internally.

```csharp
IEnumerator SomeWorkEnumerator() {
  // some expensive work
  yield return null;
  // do more expensive work
  yield return null;
  // etc
}

// manually setting up the work
AsyncHandle workHandle = Async.Schedule(SomeWorkEnumerator(), AsyncFlags.MainThreadOnly);
Game.Scenes.RegisterLoadDependency(workHandle);

// or calling Jobs.PushLoadDependency
Jobs.PushLoadDependency(SomeWorkEnumerator(), AsyncFlags.MainThreadOnly);
```
## Unload Phases
Unloading scenes have a comparatively simple pipeline.

1. Invoke all `QueueOnUnload` callbacks for the scene
2. Invoke all `ISceneCustomData.OnUnload` callbacks for scene data
3. Invoke all `ISceneUnloadHandler.OnSceneUnload` callbacks for scene components
4. Invoke `OnSceneUnload` event

## Transition Handler
Visual/audio transitions can be coordinated with the scene loading pipeline with a pair of `SceneTransitionHandler` callbacks. These will only be executed during a Main scene load.

At the beginning of the Main scene load, an **Unload Transition Handler** will be executed before unloading any existing Main scene. The pipeline will then be paused until the underlying coroutine is complete. In this callback, any fadeouts, input blocking, audio fading, and the like, should be executed. Further down the pipeline, a **Load Transition Handler** will be executed at the end of the Ready Phase. This will not block the pipeline from proceeding, allowing for a smooth and interactive transition to gameplay.

```csharp
Game.Scenes.RegisterTransitionHandlers(HandleUnload, HandleLoad);

IEnumerator HandleUnload(Scene scene, StringHash32 tag, MainSceneTransitionArgs args) {
  BlockAllInputs();
  yield return FadeToBlack();
}

IEnumerator HandleLoad(Scene scene, StringHash32 tag, MainSceneTransitionArgs args) {
  ResumeInputs();
  yield return FadeBackIn();
}
```

Arguments can be passed into this transition handler via a `MainSceneTransitionArgs` object. This contains both a `TransitionType` (`StringHash32`) and `Flags` (`SceneTransitionFlags`) fields. These can be interpreted by the handlers to change the appearance of the transition, making it take longer, appear in different colors, or be otherwise changed.

`MainSceneTransitionArgs` objects are passed into various overloads of the `LoadMainScene` and `ReloadMainScene` functions.
## Miscellaneous Considerations
When implementing a custom asset loading pipeline (i.e. scripts, streamed audio), you can check the `IsSafeToUnloadAssets` function to determine if it is safe to do so. This ensures that assets do not get unloaded unnecessarily before scene preloading has had a chance to execute. This way assets that are referenced in multiple scenes do not get unloaded and reloaded.