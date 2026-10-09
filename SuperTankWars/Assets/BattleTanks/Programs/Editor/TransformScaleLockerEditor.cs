#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SXG2025
{
    // すべてのTransformに対するカスタムInspector（条件付きでScaleを無効化）
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Transform))]
    public class TransformScaleLockerEditor : UnityEditor.Editor
    {
        UnityEditor.Editor m_defaultEditor;
        Type m_defaultEditorType;

        void OnEnable()
        {
            // Unity内蔵のTransformInspectorをReflectionで呼び出す
            m_defaultEditorType = Type.GetType("UnityEditor.TransformInspector, UnityEditor");
            if (m_defaultEditorType != null)
            {
                m_defaultEditor = CreateEditor(targets, m_defaultEditorType);
            }
        }

        void OnDisable()
        {
            if (m_defaultEditor != null)
            {
                DestroyImmediate(m_defaultEditor);
                m_defaultEditor = null;
            }
        }

        static bool IsInCurrentPrefabStage(Transform tr)
        {
            if (tr == null) return false;

            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null) return false;

            var root = stage.prefabContentsRoot;
            if (root == null) return false;

            // PrefabStageの編集対象の配下か
            return tr == root.transform || tr.IsChildOf(root.transform);
        }

        static bool IsScaleLockedTarget(Transform tr)
        {
            if (tr == null) return false;

            // Prefab編集画面の中だけに限定
            if (!IsInCurrentPrefabStage(tr)) return false;

            // TurretPart / RotJointPart が付いている場合のみロック
            // （※コンポーネントは同namespaceとは限らないので型参照が解決できる前提）
            return tr.GetComponent<TurretPart>() != null || tr.GetComponent<RotJointPart>() != null;
        }

        public override void OnInspectorGUI()
        {
            // デフォルトInspectorが取れない場合はフォールバック
            if (m_defaultEditor == null)
            {
                DrawDefaultInspector();
                return;
            }

            // 複数選択時：1つでもロック対象があればScaleロック
            bool lockScale = false;
            foreach (var obj in targets)
            {
                var tr = obj as Transform;
                if (IsScaleLockedTarget(tr))
                {
                    lockScale = true;
                    break;
                }
            }

            if (!lockScale)
            {
                // 通常はUnity標準のTransformInspectorをそのまま表示
                m_defaultEditor.OnInspectorGUI();
                return;
            }

            // ---- ロック対象の場合：Position/Rotationは標準っぽく、Scaleだけ操作不能にする ----
            // 内蔵TransformInspectorを完全に部分フックするのは難しいため、
            // ここではSerializedPropertyで描画（見た目はシンプルですが確実にロックできます）
            serializedObject.Update();

            var pos = serializedObject.FindProperty("m_LocalPosition");
            var rot = serializedObject.FindProperty("m_LocalRotation");
            var scale = serializedObject.FindProperty("m_LocalScale");

            // Position
            if (pos != null) EditorGUILayout.PropertyField(pos, new GUIContent("Position"));

            // Rotation（Quaternionのままだと見た目がイマイチなので、Eulerに変換して描画）
            if (rot != null)
            {
                // UnityのTransformInspectorと同等のUIに寄せるため、Eulerを使う
                // 複数選択の混在（hasMultipleDifferentValues）は簡易対応
                EditorGUI.BeginChangeCheck();
                Vector3 euler;

                if (rot.hasMultipleDifferentValues)
                {
                    // 混在時はダミー表示
                    euler = Vector3.zero;
                    EditorGUI.showMixedValue = true;
                }
                else
                {
                    var q = (target as Transform).localRotation;
                    euler = q.eulerAngles;
                }

                euler = EditorGUILayout.Vector3Field("Rotation", euler);

                EditorGUI.showMixedValue = false;

                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var obj in targets)
                    {
                        var tr = obj as Transform;
                        if (tr == null) continue;
                        Undo.RecordObject(tr, "Change Rotation");
                        tr.localRotation = Quaternion.Euler(euler);
                        EditorUtility.SetDirty(tr);
                    }
                }
            }

            // Scale（無効化して表示）
            using (new EditorGUI.DisabledScope(true))
            {
                if (scale != null) EditorGUILayout.PropertyField(scale, new GUIContent("Scale"));
            }

            EditorGUILayout.HelpBox(
                "このオブジェクトはスケール変更できません。",
                MessageType.Info
            );

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif