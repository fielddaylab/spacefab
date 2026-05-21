using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Research {
    /// <summary>
    /// Shared layout helpers for Research UI lists. Today: vertical
    /// centered stacking of ResearchObservationChip rects, used by both
    /// HypothesisPanelVisualSystem (hypothesis slot chips) and
    /// ObservationPickerLoadUtility (picker chips).
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
    }
}
