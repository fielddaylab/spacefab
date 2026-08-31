using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Debugger;
using TinyIL;
using UnityEngine;

namespace FieldDay.Rendering {
    static public class MeshRendererUtility {
        static private readonly List<Material> s_MaterialWorkList = new List<Material>(4);
        static private MaterialPropertyBlock s_EmptyPropertyBlock;
        static private MaterialPropertyBlock s_SpriteWorkPropertyBlock;

        /// <summary>
        /// Sets the shared material at the given index.
        /// </summary>
        static public void SetSharedMaterialAtIndex(this Renderer renderer, int materialIndex, Material newMaterial) {
            renderer.GetSharedMaterials(s_MaterialWorkList);
            s_MaterialWorkList[materialIndex] = newMaterial;
#if UNITY_2022_2_OR_NEWER
            renderer.SetSharedMaterials(s_MaterialWorkList);
#else
            if (materialIndex == 0 && s_MaterialWorkList.Count == 1) {
                renderer.sharedMaterial = newMaterial;
            } else {
                renderer.sharedMaterials = s_MaterialWorkList.ToArray();
            }
#endif // UNITY_2022_2_OR_NEWER
            s_MaterialWorkList.Clear();
        }

        /// <summary>
        /// Sets the material at the given index.
        /// </summary>
        static public void SetMaterialAtIndex(this Renderer renderer, int materialIndex, Material newMaterial) {
            renderer.GetMaterials(s_MaterialWorkList);
            s_MaterialWorkList[materialIndex] = newMaterial;
#if UNITY_2022_2_OR_NEWER
            renderer.SetMaterials(s_MaterialWorkList);
#else
            if (materialIndex == 0 && s_MaterialWorkList.Count == 1) {
                renderer.material = newMaterial;
            } else {
                renderer.materials = s_MaterialWorkList.ToArray();
            }
#endif // UNITY_2022_2_OR_NEWER
            s_MaterialWorkList.Clear();
        }

        /// <summary>
        /// Returns the number of materials set on this renderer.
        /// </summary>
        [IntrinsicIL("ldarg.0; call [arg renderer]::GetMaterialCount(); ret")]
        static public int GetMaterialCount(this Renderer renderer) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Clears the property block.
        /// </summary>
        static public void ClearPropertyBlock(this Renderer renderer) {
            MaterialPropertyBlock block = s_EmptyPropertyBlock ?? (s_EmptyPropertyBlock = new MaterialPropertyBlock());
            renderer.SetPropertyBlock(block);
        }

        /// <summary>
        /// Clears the property block for the given material index.
        /// </summary>
        static public void ClearPropertyBlock(this Renderer renderer, int materialIndex) {
            MaterialPropertyBlock block = s_EmptyPropertyBlock ?? (s_EmptyPropertyBlock = new MaterialPropertyBlock());
            renderer.SetPropertyBlock(block, materialIndex);
        }

        /// <summary>
        /// Sets a property block to reference a specific section of a texture.
        /// </summary>
        static public void SetSprite(this MaterialPropertyBlock propertyBlock, int texturePropertyId, int textureOffsetScalePropertyId, Sprite sprite) {
            if (sprite == null) {
                propertyBlock.SetTexture(texturePropertyId, Texture2D.whiteTexture);
            } else {
                SpriteRectMeshInfo rectMeshInfo = SpriteMeshUtility.ComputeRectMesh(sprite);
                propertyBlock.SetTexture(texturePropertyId, rectMeshInfo.Texture);
                propertyBlock.SetVector(textureOffsetScalePropertyId, RectUVs.ComputeScaleOffset(rectMeshInfo.Texcoords));
            }
        }

        /// <summary>
        /// Sets a property block to reference a specific section of a texture.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void SetSprite(this MaterialPropertyBlock propertyBlock, Sprite sprite) {
            SetSprite(propertyBlock, DefaultShaderProps.MainTex, DefaultShaderProps.MainTexScaleOffset, sprite);
        }

        /// <summary>
        /// Sets a renderer to reference a specific section of a texture.
        /// </summary>
        static public void SetSprite(this Renderer renderer, int texturePropertyId, int textureOffsetScalePropertyId, Sprite sprite) {
            MaterialPropertyBlock block = s_SpriteWorkPropertyBlock ?? (s_SpriteWorkPropertyBlock = new MaterialPropertyBlock());
            block.Clear();
            renderer.GetPropertyBlock(block);
            SetSprite(block, texturePropertyId, textureOffsetScalePropertyId, sprite);
            renderer.SetPropertyBlock(block);
            block.Clear();
        }

        /// <summary>
        /// Sets a renderer to reference a specific section of a texture.
        /// </summary>
        static public void SetSprite(this Renderer renderer, Sprite sprite) {
            SetSprite(renderer, DefaultShaderProps.MainTex, DefaultShaderProps.MainTexScaleOffset, sprite);
        }
    }
}