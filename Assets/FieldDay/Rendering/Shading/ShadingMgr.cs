#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

#if UNITY_2019_1_OR_NEWER
#define USE_SRP
#endif // UNITY_2019_1_OR_NEWER

#if UNITY_2019_1_OR_NEWER && HAS_URP
#define USING_URP
#endif // UNITY_2019_1_OR_NEWER

using System;
using System.Collections.Generic;
using System.Reflection;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Debugging;
using FieldDay.Perf;
using UnityEngine;

#if USE_SRP
using UnityEngine.Rendering;
#endif // USE_SRP

#if USING_URP
using UnityEngine.Rendering.Universal;
#endif // USING_URP

namespace FieldDay.Rendering {
    public sealed class ShadingMgr {
        #region Types

        [Serializable]
        internal struct Config {
            public Material DefaultGraphicMaterial;
        }

        #endregion // Types

        private RenderTexture m_WarmupRenderTarget;

        private Material m_TextureWarmupMaterial;
        private Material m_ShaderWarmupMaterial;
        private RingBuffer<Material> m_WarmupMaterialQueue = new RingBuffer<Material>(64, RingBufferMode.Expand);
        private RingBuffer<Shader> m_WarmupShaderQueue = new RingBuffer<Shader>(64, RingBufferMode.Expand);
        private RingBuffer<Texture> m_WarmupTextureQueue = new RingBuffer<Texture>(64, RingBufferMode.Expand);

        #region Events

        internal void Initialize(Config config) {
            if (config.DefaultGraphicMaterial) {
                MaterialUtility.SetDefaultUIGraphicMaterial(config.DefaultGraphicMaterial);
            }

            m_WarmupRenderTarget = new RenderTexture(4, 4, 0, RenderTextureFormat.Default);

            DefaultShaderProps.Initialize();
            BayerMatrices.CreateBuffer();
        }

        internal void Shutdown() {
            BayerMatrices.DestroyBuffer();

            m_WarmupRenderTarget.Release();
            GameObject.DestroyImmediate(m_WarmupRenderTarget);
        }

        #endregion // Events

        #region Preloading

        public void QueueTexturePreload(Texture texture) {
            m_WarmupTextureQueue.PushBack(texture);
        }

        public void QueueShaderPreload(Shader shader) {
            m_WarmupShaderQueue.PushBack(shader);
        }

        public void QueueMaterialPreload(Material material) {
            m_WarmupMaterialQueue.PushBack(material);
        }

        private void PreparePreloadPipeline() {
            RenderTexture.active = m_WarmupRenderTarget;
            GL.PushMatrix();
            GL.LoadOrtho();
        }

        static private void CompletePreloadPipeline() {
            GL.PopMatrix();
            RenderTexture.active = null;
        }

        private bool PreloadOneTexture() {
            if (m_WarmupTextureQueue.TryPopFront(out var tex) && tex) {
                m_TextureWarmupMaterial.mainTexture = tex;
                m_TextureWarmupMaterial.SetPass(0);
                DrawTri();
                return true;
            }

            return false;
        }

        private bool PreloadOneShader() {
            if (m_WarmupShaderQueue.TryPopFront(out var shader) && shader) {
                m_ShaderWarmupMaterial.shader = shader;
                for (int i = 0, passCount = shader.passCount; i < passCount; i++) {
                    m_ShaderWarmupMaterial.SetPass(i);
                    DrawTri();
                }
                return true;
            }

            return false;
        }

        private bool PreloadOneMaterial() {
            if (m_WarmupMaterialQueue.TryPopFront(out var material) && material) {
                for (int i = 0, passCount = material.passCount; i < passCount; i++) {
                    material.SetPass(i);
                    DrawTri();
                }
                return true;
            }

            return false;
        }

        static private void DrawTri() {
            GL.Color(Color.white);
            GL.Begin(GL.TRIANGLES);
            
            GL.TexCoord(new Vector3(0, 0, 0));
            GL.Vertex(new Vector3(0, 0, 0));
            
            GL.TexCoord(new Vector3(0, 2, 0));
            GL.Vertex(new Vector3(0, 2, 0));

            GL.TexCoord(new Vector3(2, 0, 0));
            GL.Vertex(new Vector3(2, 0, 0));

            GL.End();
        }

        #endregion // Preloading
    }
}