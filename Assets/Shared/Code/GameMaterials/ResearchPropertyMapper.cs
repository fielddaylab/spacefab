namespace SpaceFab.Materials
{
    /// <summary>
    /// Seam between the runtime Research chip vocabulary (ResearchChipId, from
    /// the prototype) and the production-side property-label vocabulary
    /// (MaterialPropertyLabel). Once the prototype's chip enum lands, this is
    /// the only place that translates between the two.
    ///
    /// The static-vs-dynamic partition is NOT handled here - it is a property of
    /// MaterialPropertyLabel itself, classified by MaterialPropertyLabelUtility,
    /// and is invisible to the chip side. That keeps the chip vocabulary free of
    /// save-encoding concerns: chips know about labels, labels know about
    /// persistence/partitioning, and neither side has to know about the other's
    /// internal structure.
    /// </summary>
    public static class ResearchPropertyMapper
    {
        // TODO: ResearchChipId-side bridge. Once the prototype's ResearchChipId
        // enum and ResearchChipUtility port into the production codebase, add:
        //
        //   public static MaterialPropertyLabel ToPropertyLabel(ResearchChipId chip)
        //   {
        //       chip = ResearchChipUtility.Unalias(chip);
        //       // Map the canonical chip to its MaterialPropertyLabel. Property
        //       // chips map to the persistent label they confirm; observation
        //       // chips map to their evidence label.
        //       ...
        //   }
        //
        //   public static ResearchChipId FromPropertyLabel(MaterialPropertyLabel label)
        //   {
        //       // Inverse, returning the canonical (un-aliased) ResearchChipId
        //       // for a given label. Used when projecting saved progress back
        //       // into the in-session ResearchInventory on minigame entry.
        //       ...
        //   }
        //
        // Callers that need persistent-vs-not classification go through
        // MaterialPropertyLabelUtility.IsPersistent, not this file.
    }
}
