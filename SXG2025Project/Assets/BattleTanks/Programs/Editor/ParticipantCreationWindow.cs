using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine;

namespace SXG2025
{
    public class ParticipantCreationWindow : EditorWindow
    {
        [MenuItem("SXG2025/挑戦者作成")]
        private static void OpenWindow()
        {
            var window = GetWindow<ParticipantCreationWindow>();
            window.titleContent = new GUIContent("挑戦者作成");
        }

        /// <summary>
        /// 画像ファイルパス
        /// </summary>
        private string m_iconPath = "";

        private class Data : ScriptableSingleton<Data>
        {
            /// <summary>
            /// 申込時に発行される参加番号（7桁）
            /// </summary>
            public int participantID = 0;
            /// <summary>
            /// 所属名
            /// </summary>
            public string organization = "所属";
            /// <summary>
            /// 挑戦者名
            /// </summary>
            public string playerName = "挑戦者";
            /// <summary>
            /// 画像スプライト
            /// </summary>
            public Sprite sprite = null;

            public bool isRunnning = false;

            public string GetName()
                => $"Player{participantID:D7}";
        }

        private void OnGUI()
        {
            var data = Data.instance;

            GUILayout.Space(10);
            GUILayout.Label("■必須項目");

            // 参加番号
            var participantID = EditorGUILayout.IntField("　受付番号:", data.participantID);
            data.participantID = Mathf.Clamp(participantID, 0, 9999999);
            GUILayout.Label("　※connpassエントリー時に発行された受付番号を入力してください");

            GUILayout.Space(10);
            GUILayout.Label("■任意項目（後から変更可）");

            // 所属名
            const int MAX_NUM = 10; // 最大文字数
            data.organization = EditorGUILayout.TextField("　所属名:", data.organization);
            if (MAX_NUM < data.organization.Length)
                data.organization = data.organization.Remove(MAX_NUM, data.organization.Length - MAX_NUM);
            // 挑戦者名
            data.playerName = EditorGUILayout.TextField("　挑戦者名:", data.playerName);
            if (MAX_NUM < data.playerName.Length)
                data.playerName = data.playerName.Remove(MAX_NUM, data.playerName.Length - MAX_NUM);
            // アイコン画像
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.TextField("　アイコン画像選択:", m_iconPath);
                if (GUILayout.Button("参照", GUILayout.Width(50), GUILayout.Height(18)))
                {
                    var path = EditorUtility.OpenFilePanel("Select Image", "", "png,jpg,jpeg");
                    m_iconPath = path.Replace("\\", "/").Replace(Application.dataPath, "Assets");
                    GUI.FocusControl("");
                }
            }
            GUILayout.Label("　※所属名・挑戦者名は、全角半角問わず 最大10文字 としてください");
            GUILayout.Label("　※アイコン画像ファイルの最大サイズは 256*256 です");
            GUILayout.Label("　※公序良俗に反する画像や名前は設定しないでください");

            GUILayout.Space(10);

            if (GUILayout.Button("作成", GUILayout.Height(32)))
            {
                AddParticipant();
            }
        }

        private void AddParticipant()
        {
            var data = Data.instance;
            var name = data.GetName();

            var folderPath = $"Assets/Participant/{name}";

            if (Directory.Exists(folderPath))
            {
                Debug.LogError($"参加番号:{data.participantID} の挑戦者は既に作成済みです。 - {folderPath}");
                return;
            }

            // フォルダ作成
            Directory.CreateDirectory(folderPath);

            // 画像作成
            if (File.Exists(m_iconPath))
            {
                var iconExtension = Path.GetExtension(m_iconPath);
                var iconPath = $"{folderPath}/{name}{iconExtension}";
                File.Copy(m_iconPath, iconPath, true);

                AssetDatabase.Refresh();

                // 画像のテクスチャタイプ変更
                var textureImporter = AssetImporter.GetAtPath(iconPath) as TextureImporter;
                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.spriteImportMode = SpriteImportMode.Single;
                textureImporter.SaveAndReimport();

                var iconAsset = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                data.sprite = iconAsset;
            }

            // csファイル作成
            var csTemplatePath = "Assets/BattleTanks/Programs/Editor/SXGParticipant.cs.txt";
            var csPath = $"{folderPath}/{name}.cs";
            var csText = File.ReadAllText(csTemplatePath);
            csText = csText.Replace("#SCRIPTNAME#", name);
            File.WriteAllText(csPath, csText, Encoding.GetEncoding("utf-8"));

            data.isRunnning = true;

            AssetDatabase.Refresh();
            CompilationPipeline.RequestScriptCompilation();
        }

        [DidReloadScripts]
        public static void OnDidReloadScripts()
        {
            const string SOURCE_PREFAB_PATH = "Assets/BattleTanks/Programs/Editor/PlayerPrefabBase.prefab";

            var data = Data.instance;
            var name = data.GetName();

            if (!data.isRunnning)
                return;
            data.isRunnning = false;

            // Prefab用ゲームオブジェクト作成
            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SOURCE_PREFAB_PATH);
            if (sourcePrefab == null)
            {
                EditorUtility.DisplayDialog("Error", string.Format("ソースプレハブが見つかりません：{0}\nパスを確認してください。", SOURCE_PREFAB_PATH), "OK");
                return;
            }
            string sourcePath = AssetDatabase.GetAssetPath(sourcePrefab);
            GameObject contentsRoot = null;
            try
            {
                contentsRoot = PrefabUtility.LoadPrefabContents(sourcePath);
                if (contentsRoot == null)
                {
                    EditorUtility.DisplayDialog("Error", "PrefabContentsのロードに失敗しました。", "OK");
                    return;
                }

                var type = Type.GetType($"{name}.{name}, Participant"); // 名前空間とアセンブリを含める
                if (type != null)
                {
                    var component = contentsRoot.AddComponent(type) as ComPlayerBase;
                    component.SetPlayerData(data.organization, data.playerName, data.sprite);
                }

                // Prefab作成
                var folderPath = $"Assets/Participant/{name}";
                var prefabPath = $"{folderPath}/{name}.prefab";
                PrefabUtility.SaveAsPrefabAsset(contentsRoot, prefabPath);

                AssetDatabase.Refresh();

                EditorApplication.delayCall = () =>
                {
                    // AIリスト登録
                    var participantListPath = "Assets/GameAssets/Data/ParticipantList.asset";
                    var participantListAsset = AssetDatabase.LoadAssetAtPath<ParticipantList>(participantListPath);
                    var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    participantListAsset.m_comPlayers.Insert(0, prefabAsset.GetComponent<ComPlayerBase>());

                    EditorUtility.SetDirty(participantListAsset);
                    AssetDatabase.SaveAssets();

                    // Prefabフォーカス
                    Selection.activeObject = prefabAsset;
                    EditorGUIUtility.PingObject(prefabAsset);

                    EditorUtility.DisplayDialog("挑戦者登録", $"参加番号:{Data.instance.participantID} のAIを登録しました。\n{folderPath}", "OK");
                };
            }
            catch
            {
                // アンロードしてメモリ解放 
                PrefabUtility.UnloadPrefabContents(contentsRoot);
            }

        }
    }
}