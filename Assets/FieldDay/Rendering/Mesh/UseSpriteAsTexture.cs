using System;
using BeauUtil;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.Rendering {
    [RequireComponent(typeof(Renderer)), ExecuteAlways]
    public sealed class UseSpriteAsTexture : MonoBehaviour {
        [SerializeField] private Sprite m_Sprite;
        [NonSerialized] private Renderer m_Renderer;

        /// <summary>
        /// Which sprite to render
        /// </summary>
        public Sprite Sprite {
            get { return m_Sprite; }
            set {
                if (m_Sprite != value) {
                    m_Sprite = value;
                    this.CacheComponent(ref m_Renderer).SetSprite(m_Sprite);
                }
            }
        }

        private void OnEnable() {
#if UNITY_EDITOR
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) {
                return;
            }
#endif // UNITY_EDITOR
            this.CacheComponent(ref m_Renderer);
            m_Renderer.SetSprite(m_Sprite);
        }

#if UNITY_EDITOR
        [NonSerialized] private Sprite m_LastAppliedSprite;

        private void OnValidate() {
            if (m_LastAppliedSprite != m_Sprite) {
                m_LastAppliedSprite = m_Sprite;
                this.CacheComponent(ref m_Renderer).SetSprite(m_Sprite);
            }
        }
#endif // UNITY_EDITOR
    }
}