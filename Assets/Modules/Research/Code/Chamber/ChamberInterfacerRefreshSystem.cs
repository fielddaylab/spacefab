using FieldDay;
using FieldDay.Systems;
using SpaceFab;

namespace SpaceFab.Research {
    /// <summary>
    /// Clears the ChamberInterfacerState frame-flag at the end of each Research
    /// frame, so SlotMaterialUpdatedThisFrame is only true during the frame in
    /// which a slot write occurred. Receptive flags are persistent and are not
    /// touched here — only chamber systems toggle those.
    /// Runs on LateUpdate at order 1000 under ResearchMask, after any chamber
    /// system that wants to read the flag this frame.
    /// </summary>
    public class ChamberInterfacerRefreshSystem : SystemComponent {
        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(&ProcessWork,
                new SysUpdate(GameLoopPhase.LateUpdate, 1000, UpdateMasks.ResearchMask),
                new SysPermissions().ReadWriteShared<ChamberInterfacerState>());
        }

        private static void ProcessWork(float deltaTime) {
            ChamberInterfacerState interfacerState = Find.State<ChamberInterfacerState>();
            interfacerState.SlotMaterialUpdatedThisFrame = false;
            interfacerState.LastUpdatedMaterial = null;
        }
    }
}
