#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

namespace SXG2025
{
    public class ParticipantSubmissionWindow : EditorWindow
    {
        [MenuItem("プロバト/挑戦者出力")]
        private static void OpenWindow()
        {
            var window = GetWindow<ParticipantSubmissionWindow>();
            window.titleContent = new GUIContent("挑戦者出力");
        }

        /// <summary>
        /// 申込時に発行される参加番号（7桁）
        /// </summary>
        private int m_participantID = 0;
        /// <summary>
        /// ZIPファイル出力先
        /// </summary>
        private string m_outputPath = string.Empty;

        private void OnGUI()
        {
            GUILayout.Space(10);

            GUILayout.Label("作成した挑戦者データををZIP圧縮して出力します。");
            GUILayout.Label("出力されたZIPファイルを、Googleフォームで提出してください。");

            GUILayout.Space(20);

            // 参加番号
            var participantID = EditorGUILayout.IntField("　受付番号:", m_participantID);
            m_participantID = Mathf.Clamp(participantID, 0, 9999999);
            GUILayout.Label("　※connpassエントリー時に発行された受付番号を入力してください");

            GUILayout.Space(10);

            // 出力先フォルダ
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.TextField("　ZIPファイル出力先:", m_outputPath);
                if (GUILayout.Button("参照", GUILayout.Width(50), GUILayout.Height(18)))
                {
                    var path = EditorUtility.OpenFolderPanel("Select Folder", "", "");
                    m_outputPath = path.Replace("\\", "/").Replace(Application.dataPath, "Assets");
                    GUI.FocusControl("");
                }
            }
            GUILayout.Label("　※未指定の場合、デスクトップに出力されます");

            GUILayout.Space(10);

            if (GUILayout.Button("出力", GUILayout.Height(32)))
            {
                CreateParticipantZIP();
            }
        }

        private void CreateParticipantZIP()
        {
            var folderName = $"Player{m_participantID:D7}";
            var sourceFolderPath = $"Assets/Participant/{folderName}";

            if (!Directory.Exists(sourceFolderPath))
            {
                Debug.LogError($"参加番号:{m_participantID} の挑戦者が見つかりません。");
                return;
            }

            // 出力先のフォルダを未指定の場合、デスクトップにする
            if (string.IsNullOrEmpty(m_outputPath))
                m_outputPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            var outputPath = Path.Combine(m_outputPath, folderName + ".zip");

            // 出力先のフォルダが既存の場合、削除
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            try
            {
                // ZIP圧縮
                ZipFile.CreateFromDirectory(sourceFolderPath, outputPath,
                    System.IO.Compression.CompressionLevel.Optimal, includeBaseDirectory: false);

                // 完了ダイアログ
                EditorUtility.DisplayDialog("挑戦者出力",
                    $"参加番号:{folderName} のAIデータを圧縮しました。\n{outputPath}", "OK");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("エラー", "圧縮中にエラーが発生しました:\n" + ex.Message, "OK");
            }
            finally
            {
                // プログレスバーを閉じる
                EditorUtility.ClearProgressBar();
            }

        }
    }
}

#endif