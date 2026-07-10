using UnityEngine;
using BeauUtil.Debugger;

namespace FieldDay.Rendering {
    static public class FullscreenRender {
        static private int s_ImmediateDepth;

        #region Queued Render Nodes

        static public void Enqueue(Material material, MaterialPropertyBlock propertyBlock = null, Camera targetCamera = null, int layer = 0) {
            RenderParams renderParms = new RenderParams(material);
            renderParms.matProps = propertyBlock;
            renderParms.camera = targetCamera;
            renderParms.layer = layer;
            Enqueue(ref renderParms);
        }

        static public void Enqueue(ref RenderParams renderParms) {
            Mesh mesh = FullscreenMesh.CreateMesh();
            Graphics.RenderMesh(renderParms, mesh, 0, Matrix4x4.identity);
        }

        #endregion // Queued Render Nodes

        #region Immediate

        /// <summary>
        /// Pushes into immediate mode.
        /// </summary>
        static public void PushImmediate() {
            if (s_ImmediateDepth++ == 0) {
                GL.PushMatrix();
                GL.LoadOrtho();
            }
        }

        /// <summary>
        /// Pops from immediate mode.
        /// </summary>
        static public void PopImmediate() {
            Assert.True(s_ImmediateDepth > 0, "Unbalanced Push/PopImmediate calls");

            if (s_ImmediateDepth-- == 1) {
                GL.PopMatrix();
            }
        }

        /// <summary>
        /// Renders a fullscreen triangle.
        /// </summary>
        static public void ImmTri() {
            Assert.True(s_ImmediateDepth > 0, "PushImmediate was not called");

            GL.Begin(GL.TRIANGLES);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(0, 2, 0);
            GL.Vertex3(2, 0, 0);
            GL.End();
        }

        /// <summary>
        /// Renders a fullscreen quad.
        /// </summary>
        static public void ImmQuad() {
            Assert.True(s_ImmediateDepth > 0, "PushImmediate was not called");

            GL.Begin(GL.QUADS);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(0, 1, 0);
            GL.Vertex3(1, 1, 0);
            GL.Vertex3(1, 0, 0);
            GL.End();
        }

        #endregion // Immediate
    }
}