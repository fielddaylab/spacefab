using FieldDay.Components;
using Leaf;
using SpaceFab.Overarching;
using System;

namespace SpaceFab {
    /// <summary>
    /// Manifest of Leaf scripts that are scoped to one or more specific minigames.
    /// Authored on a persistent additive scene (contract, chapter, or other host) alongside
    /// the existing always-on FieldDay.Scripting.ScriptLoader. Each binding pairs a LeafAsset
    /// with the set of minigames it is allowed to fire in; the MinigameScopedScriptLoader on
    /// each minigame scene aggregates every active ScopedMinigameScripts in the registry and
    /// registers only the matching subset against the script DB for the duration of that
    /// minigame visit.
    ///
    /// Per-entity component (BatchedComponent) so a chapter scene and a contract scene can
    /// both contribute bindings at the same time without colliding.
    /// </summary>
    public sealed class ScopedMinigameScripts : BatchedComponent {
        public MinigameScriptBinding[] Bindings;
    }

    [Serializable]
    public struct MinigameScriptBinding {
        public LeafAsset Script;
        public MinigameId[] AllowedIn;
    }
}
