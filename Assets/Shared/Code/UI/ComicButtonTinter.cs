using BeauRoutine;
using FieldDay;
using FieldDay.UI;
using FieldDay.UI.Widgets;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.UI {
    public sealed class ComicButtonTinter : MonoBehaviour, IColorPaletteTint {
        public Graphic[] Background;
        public Graphic[] Content;

        public void SetTint(ColorPalette2F palette) {
            ColorPalette.Apply(palette.Background, Background);
            ColorPalette.Apply(palette.Content, Content);
        }

        public void SetTint(ColorPalette4F palette) {
            ColorPalette.Apply(palette.Background, Background);
            ColorPalette.Apply(palette.Content, Content);
        }
    }
}