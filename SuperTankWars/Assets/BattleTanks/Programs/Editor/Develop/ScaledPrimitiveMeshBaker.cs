#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ScaledPrimitiveMeshBaker
{
    [MenuItem("Tools/SXG/Bake Scaled Cylinder Mesh (0.35)")]
    public static void BakeScaledCylinder035()
    {
        const float scale = 0.35f;
        const string savePath = "Assets/BattleTanks/Meshes/Cylinder_035.asset";

        // 一時的にCylinderを生成して元Meshを取得
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        var mf = go.GetComponent<MeshFilter>();
        var srcMesh = mf != null ? mf.sharedMesh : null;

        if (srcMesh == null)
        {
            Object.DestroyImmediate(go);
            EditorUtility.DisplayDialog("Error", "Cylinderの元Meshが取得できませんでした。", "OK");
            return;
        }

        // Mesh複製
        var dstMesh = Object.Instantiate(srcMesh);
        dstMesh.name = "Cylinder_035";

        // 頂点をスケール（ローカル頂点を直接縮小）
        var vertices = dstMesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = vertices[i] * scale;
        }
        dstMesh.vertices = vertices;

        // 法線・Boundsを更新
        dstMesh.RecalculateNormals();
        dstMesh.RecalculateBounds();
        dstMesh.RecalculateTangents();

        // 保存先フォルダがなければ作る（簡易）
        var folder = System.IO.Path.GetDirectoryName(savePath).Replace("\\", "/");
        if (!AssetDatabase.IsValidFolder(folder))
        {
            // 例：Assets/BattleTanks/Meshes のように2階層以上を想定
            var parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        // 既存があれば置き換え
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(savePath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(dstMesh, existing);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        else
        {
            AssetDatabase.CreateAsset(dstMesh, savePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Object.DestroyImmediate(go);

        EditorUtility.DisplayDialog("OK", $"保存しました:\n{savePath}", "OK");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Mesh>(savePath);
    }
}
#endif
