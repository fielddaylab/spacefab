using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// Shared layout helpers for Research UI lists. Today: vertical
    /// centered and top-aligned stacking of ResearchObservationChip rects,
    /// used by the wiki chip load utilities and ObservationPickerLoadUtility
    /// (picker chips).
    /// </summary>
    public static class ResearchUILayoutUtility {
        // Lays the first `count` chips out vertically, centered on the
        // parent transform's local Y=0. Chip 0 sits at the top. X/Z are
        // left untouched so each chip's authored horizontal alignment
        // stays intact. Heights are read from each RectTransform's
        // rect.height so the layout adapts to prefab changes without a
        // magic constant. Returns the total content height (sum of
        // rect.heights + (count-1) * gap) so callers can size containers.
        public static float LayoutVerticalCentered(IReadOnlyList<ResearchObservationChip> chips, int count, float gap) {
            if (chips == null || count <= 0) {
                return 0f;
            }
            if (count > chips.Count) {
                count = chips.Count;
            }

            // 1. Sum heights to compute the total column height including gaps.
            float totalHeight = 0f;
            for (int i = 0; i < count; i++) {
                ResearchObservationChip chip = chips[i];
                if (chip == null) continue;
                RectTransform rect = chip.transform as RectTransform;
                totalHeight += rect != null ? rect.rect.height : 0f;
            }
            totalHeight += gap * (count - 1);

            // 2. Walk top-to-bottom starting at +totalHeight/2, placing
            // each chip's center at cursor - height/2, then advancing
            // the cursor downward by height + gap.
            float cursor = totalHeight * 0.5f;
            for (int i = 0; i < count; i++) {
                ResearchObservationChip chip = chips[i];
                if (chip == null) continue;
                RectTransform rect = chip.transform as RectTransform;
                float height = rect != null ? rect.rect.height : 0f;
                if (rect != null) {
                    Vector3 pos = rect.anchoredPosition3D;
                    pos.y = cursor - height * 0.5f;
                    rect.anchoredPosition3D = pos;
                } else {
                    Vector3 pos = chip.transform.localPosition;
                    pos.y = cursor - height * 0.5f;
                    chip.transform.localPosition = pos;
                }
                cursor -= height + gap;
            }

            return totalHeight;
        }

        // Lays the first `count` chips out vertically against the top edge
        // of `parent`, chip 0 first and the column growing downward. X/Z
        // are left untouched so each chip's authored horizontal alignment
        // stays intact. Heights are read from each RectTransform's
        // rect.height so the layout adapts to prefab changes without a
        // magic constant. Returns the total content height (sum of
        // rect.heights + (count-1) * gap) so callers can size containers.
        public static float LayoutVerticalAlignedTop(RectTransform parent, IReadOnlyList<ResearchObservationChip> chips, int count, float gap) {
            if (parent == null || chips == null || count <= 0) {
                return 0f;
            }
            if (count > chips.Count) {
                count = chips.Count;
            }

            // Walk top-to-bottom from the parent rect's top edge, working in
            // the parent's local space (origin at the parent's own pivot).
            // The running height total doubles as the return value.
            Rect parentRect = parent.rect;
            float cursor = parentRect.yMax;
            float totalHeight = 0f;
            for (int i = 0; i < count; i++) {
                ResearchObservationChip chip = chips[i];
                if (chip == null) continue;
                RectTransform rect = chip.transform as RectTransform;
                float height = rect != null ? rect.rect.height : 0f;
                if (rect != null) {
                    // anchoredPosition is measured from the chip's own anchor
                    // reference point, which is rarely the parent's top edge
                    // (center-anchored chips would land half a parent down).
                    // Subtract that point out so the chip lands on the cursor
                    // no matter how it was anchored, then offset by the chip's
                    // pivot so it is the top edge that meets the cursor.
                    float anchorT = Mathf.Lerp(rect.anchorMin.y, rect.anchorMax.y, rect.pivot.y);
                    float anchorY = Mathf.Lerp(parentRect.yMin, parentRect.yMax, anchorT);
                    Vector3 pos = rect.anchoredPosition3D;
                    pos.y = cursor - (1f - rect.pivot.y) * height - anchorY;
                    rect.anchoredPosition3D = pos;
                } else {
                    Vector3 pos = chip.transform.localPosition;
                    pos.y = cursor - height * 0.5f;
                    chip.transform.localPosition = pos;
                }
                cursor -= height + gap;
                totalHeight += height;
            }
            totalHeight += gap * (count - 1);

            return totalHeight;
        }
    }
}
