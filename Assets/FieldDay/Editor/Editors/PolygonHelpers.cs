using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Editor;
using FieldDay.Rendering;
using ScriptableBake;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor {
    static public class PolygonHelpers {
        [MenuItem("CONTEXT/Collider2D/Save Mesh")]
        static private void ContextSaveMesh(MenuCommand cmd) {
            Collider2D collider = (Collider2D) cmd.context;
            SaveMesh(collider);
        }

        static private void SaveMesh(Collider2D collider) {
            Baking.PrepareUndo(collider, "Exporting mesh");

            EditorHelpers.ResourceSaveForm SaveForm = new EditorHelpers.ResourceSaveForm() {
                FileExtension = "mesh",
                Header = "Save Mesh",
                LastSaveLocationKey = PlayerSettings.productGUID + "/PolygonCollider2DMeshExportSavePath",
                Message = "Save this mesh"
            };

            //Vector3 scale = collider.transform.lossyScale;
            Matrix4x4 adjustment = collider.transform.worldToLocalMatrix;
            adjustment.m23 = 0;

            Mesh clone = collider.CreateMesh(false, false);
            MeshModUtility.UseShortIndexBuffer(clone);
            MeshModUtility.TransformPositions(clone, adjustment);
            try {
                EditorHelpers.SaveResourceAs(clone, collider.gameObject.name, SaveForm);
            } finally {
                EditorHelpers.DestroyResource(ref clone);
            }
        }
    }
}