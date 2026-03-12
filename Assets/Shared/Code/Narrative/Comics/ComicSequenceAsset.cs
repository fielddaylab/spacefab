using FieldDay.Assets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Comic {
    [CreateAssetMenu(menuName = "SpaceFab/Comic Sequence Asset")]
    public class ComicSequenceAsset : NamedAsset
    {
        public ComicPageAsset[] Pages;
    }
}