using UnityEngine;
using UnityEngine.UI;

namespace SpaceFab.Fabrication
{
    public class StationResultDisplay : MonoBehaviour
    {
        public Image[] Rating;
        public Sprite Filled, Empty;

        public void SetRating(float precision)
        {
            int rating = (int)(precision * 10f / 3f);
            for (int i = 0; i < Rating.Length; i++)
                Rating[i].sprite = rating > i ? Filled : Empty;
        }
    }
}