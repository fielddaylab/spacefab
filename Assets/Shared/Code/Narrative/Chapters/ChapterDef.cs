using BeauUtil;
using FieldDay.Assets;
using SpaceFab.Design;
using SpaceFab.Materials;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab
{
    [CreateAssetMenu(menuName = "SpaceFab/Chapter Asset")]
    public class ChapterDef : NamedAsset
    {
        [AssetName(typeof(MaterialAsset))] [SerializeField] private StringHash32[] m_availableMaterials;

        public StringHash32[] AvailableMaterials() { return m_availableMaterials; }
    }
}