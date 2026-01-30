#if UNITY_EDITOR
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
        [MenuItem("プロバト/挑戦者作成")]
        private static void OpenWindow()
        {
            var window = GetWindow<ParticipantCreationWindow>();
            window.titleContent = new GUIContent("挑戦者作成");

            // 最低サイズ
            window.minSize = new Vector2(600, 420);

            // 初期サイズ 
            var rect = window.position;
            rect.width = 620;
            rect.height = 500;
            window.position = rect;
        }

        /// <summary>
        /// 画像ファイルパス
        /// </summary>
        private string m_iconPath = "";

        private string m_promoImagePath = "";


        public enum EntryMethod
        {
            Connpass,
            Other
        }


        private class Data : ScriptableSingleton<Data>
        {
            /// <summary>
            /// 申し込みサイト
            /// </summary>
            public EntryMethod entryMethod = EntryMethod.Connpass;

            /// <summary>
            /// 申込時に発行される参加番号（7桁）
            /// </summary>
            public int participantID = 0;

            /// <summary>
            /// EntryMethod.Other用のランダム値 
            /// </summary>
            public int randomID = 0;

            /// <summary>
            /// 所属名
            /// </summary>
            public string organization = "所属";
            /// <summary>
            /// 挑戦者名
            /// </summary>
            public string playerName = "挑戦者";
            /// <summary>
            /// 戦車名
            /// </summary>
            public string tankName = "戦車名";
            /// <summary>
            /// 画像スプライト
            /// </summary>
            public Sprite sprite = null;
            /// <summary>
            /// プロモーションカード画像 
            /// </summary>
            public Sprite promoCard = null;

            public bool isRunnning = false;

            public string GetName()
            {
                if (entryMethod == EntryMethod.Connpass)
                {
                    return $"Player{participantID:D7}";
                } else
                {
                    return $"PlayerOtr{randomID:D7}";
                }
            }
        }


        const int MAX_ID = 9999999;
        const int MAX_NUM = 10;

        private const string NOTICE_TEXT = "※公序良俗に反する内容や、第三者の権利（著作権・商標権・肖像権等）を侵害する画像・名称は設定しないでください。";

        private Vector2 m_scroll;

        private void OnGUI()
        {
            m_scroll = EditorGUILayout.BeginScrollView(m_scroll);

            var data = Data.instance;

            GUILayout.Space(10);
            GUILayout.Label("■必須項目");

            GUILayout.Label("　エントリーしたサイトを選んでください。");

            data.entryMethod = (EntryMethod)GUILayout.Toolbar(
                (int)data.entryMethod,
                new[] { "connpass", "Peatix／その他" }
            );

            GUILayout.Space(6);

            using (new GUILayout.HorizontalScope())
            {
                if (data.entryMethod == EntryMethod.Connpass)
                {
                    var id = EditorGUILayout.IntField("　参加ID（受付番号）:", data.participantID);
                    data.participantID = Mathf.Clamp(id, 0, MAX_ID);
                }
                else
                {
                    if (data.randomID <= 0)
                        data.randomID = GenerateOtherEntryId();

                    EditorGUILayout.LabelField("　参加ID:", data.randomID.ToString());

                    if (GUILayout.Button("再生成", GUILayout.Width(60), GUILayout.Height(18)))
                    {
                        data.randomID = GenerateOtherEntryId();
                        GUI.FocusControl("");
                    }
                }
            }

            if (data.entryMethod == EntryMethod.Connpass)
                EditorGUILayout.HelpBox("connpassの「受付番号」を入力してください。", MessageType.Info);
            else
                EditorGUILayout.HelpBox("受付番号がないため、参加IDは自動で割り当てます（変更不要）。", MessageType.Info);

            GUILayout.Space(20);
            GUILayout.Label("■任意項目（後から変更可）");
            EditorGUILayout.HelpBox("未設定でもエントリーはできますが、設定していただいた方が絶対楽しいです。", MessageType.None);

            // 所属名
            data.organization = EditorGUILayout.TextField("　所属名:", data.organization);
            if (MAX_NUM < data.organization.Length)
                data.organization = data.organization[..MAX_NUM];

            // 挑戦者名
            data.playerName = EditorGUILayout.TextField("　挑戦者名:", data.playerName);
            if (MAX_NUM < data.playerName.Length)
                data.playerName = data.playerName[..MAX_NUM];

            // 戦車名
            data.tankName = EditorGUILayout.TextField("　戦車名:", data.tankName);
            if (MAX_NUM < data.tankName.Length)
                data.tankName = data.tankName[..MAX_NUM];

            // アイコン画像
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.TextField("　アイコン画像:", m_iconPath);
                if (GUILayout.Button("参照", GUILayout.Width(50), GUILayout.Height(18)))
                {
                    var defaultDir = Path.Combine(Application.dataPath, "GameAssets/Textures");
                    var path = EditorUtility.OpenFilePanel("Select Image", defaultDir, "png,jpg,jpeg");
                    if (!string.IsNullOrEmpty(path))
                        m_iconPath = path.Replace("\\", "/").Replace(Application.dataPath, "Assets");
                    GUI.FocusControl("");
                }
                if (GUILayout.Button("クリア", GUILayout.Width(50), GUILayout.Height(18)))
                {
                    m_iconPath = "";
                    GUI.FocusControl("");
                }
            }

            // プロモーションカード画像
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.TextField("　プロモーションカード画像:", m_promoImagePath);
                if (GUILayout.Button("参照", GUILayout.Width(50), GUILayout.Height(18)))
                {
                    var defaultDir = Path.Combine(Application.dataPath, "GameAssets/Textures");
                    var path = EditorUtility.OpenFilePanel("Select Image", defaultDir, "png,jpg,jpeg");
                    if (!string.IsNullOrEmpty(path))
                        m_promoImagePath = path.Replace("\\", "/").Replace(Application.dataPath, "Assets");
                    GUI.FocusControl("");
                }
                if (GUILayout.Button("クリア", GUILayout.Width(50), GUILayout.Height(18)))
                {
                    m_promoImagePath = "";
                    GUI.FocusControl("");
                }
            }

            GUILayout.Label("　※所属名・挑戦者名は、全角半角問わず 最大10文字 としてください。");
            GUILayout.Label("　※アイコン画像ファイルの最大サイズは 256×256 です。");
            GUILayout.Label("　※プロモーションカード画像ファイルの最大サイズは 960×540 です。");
            GUILayout.Label("　" + NOTICE_TEXT);

            GUILayout.Space(10);

            // 作成可否（最低限の事故防止）
            bool canCreate = true;
            if (data.entryMethod == EntryMethod.Connpass && data.participantID <= 0)
                canCreate = false;

            using (new EditorGUI.DisabledScope(!canCreate))
            {
                if (GUILayout.Button("作成", GUILayout.Height(32)))
                {
                    AddParticipant();
                }
            }

            if (!canCreate)
                EditorGUILayout.HelpBox("参加ID（受付番号）を入力してください。", MessageType.Warning);

            EditorGUILayout.EndScrollView();
        }

        /*
        private void OnGUI()
        {
            var data = Data.instance;

            GUILayout.Space(10);
            GUILayout.Label("■必須項目");

            //-----------------------------------------------------

            GUILayout.Label("　エントリーしたサイトを選んでください。");

            // 申込方法（排他）
            data.entryMethod = (EntryMethod)GUILayout.Toolbar(
                (int)data.entryMethod,
                new[] { "connpass", "Peatix／その他" }
            );

            GUILayout.Space(6);

            // 参加ID
            using (new GUILayout.HorizontalScope())
            {
                if (data.entryMethod == EntryMethod.Connpass)
                {
                    var id = EditorGUILayout.IntField("　参加ID（受付番号）:", data.participantID);
                    data.participantID = Mathf.Clamp(id, 0, MAX_ID);
                }
                else
                {
                    // 未設定なら生成
                    if (data.randomID <= 0)
                        data.randomID = GenerateOtherEntryId();

                    EditorGUILayout.LabelField("　参加ID:", data.randomID.ToString());

                    if (GUILayout.Button("再生成", GUILayout.Width(60), GUILayout.Height(18)))
                    {
                        data.randomID = GenerateOtherEntryId();
                        GUI.FocusControl("");
                    }
                }
            }

            // 短い説明はHelpBoxにまとめる（Label連打より読みやすい）
            if (data.entryMethod == EntryMethod.Connpass)
                EditorGUILayout.HelpBox("connpassの「受付番号」を入力してください。", MessageType.Info);
            else
                EditorGUILayout.HelpBox("受付番号がないため、参加IDは自動で割り当てます（変更不要）。", MessageType.Info);



            //-----------------------------------------------------

            //// 参加番号
            //var participantID = EditorGUILayout.IntField("　受付番号:", data.participantID);
            //data.participantID = Mathf.Clamp(participantID, 0, MAX_ID);
            //GUILayout.Label("　※connpassエントリー時に発行された受付番号を入力してください");

            GUILayout.Space(20);
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
            // 戦車名
            data.tankName = EditorGUILayout.TextField("　戦車名:", data.tankName);
            if (MAX_NUM < data.tankName.Length)
                data.tankName = data.tankName.Remove(MAX_NUM, data.tankName.Length - MAX_NUM);
            // アイコン画像
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.TextField("　アイコン画像:", m_iconPath);
                if (GUILayout.Button("参照", GUILayout.Width(50), GUILayout.Height(18)))
                {
                    var defaultDir = System.IO.Path.Combine(Application.dataPath, "GameAssets/Textures");
                    var path = EditorUtility.OpenFilePanel("Select Image", defaultDir, "png,jpg,jpeg");
                    m_iconPath = path.Replace("\\", "/").Replace(Application.dataPath, "Assets");
                    GUI.FocusControl("");
                }
            }
            // プロモーションカード画像
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.TextField("　プロモーションカード画像:", m_promoImagePath);
                if (GUILayout.Button("参照", GUILayout.Width(50), GUILayout.Height(18)))
                {
                    var defaultDir = System.IO.Path.Combine(Application.dataPath, "GameAssets/Textures");
                    var path = EditorUtility.OpenFilePanel("Select Image", defaultDir, "png,jpg,jpeg");
                    m_promoImagePath = path.Replace("\\", "/").Replace(Application.dataPath, "Assets");
                    GUI.FocusControl("");
                }
            }
            GUILayout.Label("　※所属名・挑戦者名は、全角半角問わず 最大10文字 としてください。");
            GUILayout.Label("　※アイコン画像ファイルの最大サイズは 256*256 です。");
            GUILayout.Label("　※プロモーションカード画像ファイルの最大サイズは 960*540 です。");
            GUILayout.Label("　※公序良俗に反する画像や名前は設定しないでください。");

            GUILayout.Space(10);

            if (GUILayout.Button("作成", GUILayout.Height(32)))
            {
                AddParticipant();
            }
        }
        */

        private static int GenerateOtherEntryId()
        {
            return UnityEngine.Random.Range(1, MAX_ID+1);
        }

        private void AddParticipant()
        {
            var data = Data.instance;
            var name = data.GetName();

            var folderPath = $"Assets/Participant/{name}";

            if (Directory.Exists(folderPath))
            {
                string error = data.entryMethod == EntryMethod.Connpass
                    ? $"!!ERROR!!\n\n参加番号:{data.participantID} の戦車は既に作成済みです。 \n- {folderPath}"
                    : $"!!ERROR!!\n\n参加ID:{data.randomID} の戦車は既に作成済みです。 \n- {folderPath}";

                Debug.LogError(error);
                EditorUtility.DisplayDialog("挑戦者登録", error, "OK");
                return;
            }

            Directory.CreateDirectory(folderPath);

            // アイコン画像
            if (!string.IsNullOrEmpty(m_iconPath) && File.Exists(m_iconPath))
            {
                var ext = Path.GetExtension(m_iconPath);
                var dstPath = $"{folderPath}/{name}_icon{ext}";
                File.Copy(m_iconPath, dstPath, true);

                AssetDatabase.Refresh();

                var ti = AssetImporter.GetAtPath(dstPath) as TextureImporter;
                if (ti != null)
                {
                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = SpriteImportMode.Single;
                    ti.SaveAndReimport();
                }

                data.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(dstPath);
            }

            // プロモ画像
            if (!string.IsNullOrEmpty(m_promoImagePath) && File.Exists(m_promoImagePath))
            {
                var ext = Path.GetExtension(m_promoImagePath);
                var dstPath = $"{folderPath}/{name}_promo{ext}";
                File.Copy(m_promoImagePath, dstPath, true); // ←正しいコピー元

                AssetDatabase.Refresh();

                var ti = AssetImporter.GetAtPath(dstPath) as TextureImporter;
                if (ti != null)
                {
                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = SpriteImportMode.Single;
                    ti.SaveAndReimport();
                }

                data.promoCard = AssetDatabase.LoadAssetAtPath<Sprite>(dstPath);
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
        /*
        private void AddParticipant()
        {
            var data = Data.instance;
            var name = data.GetName();

            var folderPath = $"Assets/Participant/{name}";

            if (Directory.Exists(folderPath))
            {
                string error = "";
                if (data.entryMethod == EntryMethod.Connpass)
                {
                    error = $"!!ERROR!!\n\n参加番号:{data.participantID} の戦車は既に作成済みです。 \n- {folderPath}";
                } else
                {
                    error = $"!!ERROR!!\n\n参加ID:{data.randomID} の戦車は既に作成済みです。 \n- {folderPath}";
                }
                Debug.LogError(error);
                EditorUtility.DisplayDialog("挑戦者登録", error, "OK");

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

            // 画像作成
            if (File.Exists(m_promoImagePath))
            {
                var iconExtension = Path.GetExtension(m_promoImagePath);
                var iconPath = $"{folderPath}/{name}{iconExtension}";
                File.Copy(m_iconPath, iconPath, true);

                AssetDatabase.Refresh();

                // 画像のテクスチャタイプ変更
                var textureImporter = AssetImporter.GetAtPath(iconPath) as TextureImporter;
                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.spriteImportMode = SpriteImportMode.Single;
                textureImporter.SaveAndReimport();

                var iconAsset = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                data.promoCard = iconAsset;
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
        */

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
                    component.SetPlayerData(data.organization, data.playerName, data.tankName, data.sprite, data.promoCard);
                }

                // Prefab作成
                var folderPath = $"Assets/Participant/{name}";
                var prefabPath = $"{folderPath}/{name}.prefab";
                PrefabUtility.SaveAsPrefabAsset(contentsRoot, prefabPath);

                //PrefabUtility.UnloadPrefabContents(contentsRoot);

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

                    // 生成フォルダをProjectで開く 
                    var folderAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderPath);
                    if (folderAsset != null)
                    {
                        // フォルダを開く 
                        EditorUtility.FocusProjectWindow();
                        ProjectWindowUtil.ShowCreatedAsset(prefabAsset);

                        // Prefabを選び直す 
                        Selection.activeObject = prefabAsset;
                        EditorGUIUtility.PingObject(prefabAsset);
                    }

                    // プレハブを開いてあげる 
                    AssetDatabase.OpenAsset(prefabAsset);

                    // 確認ダイアログ表示 
                    if (Data.instance.entryMethod == EntryMethod.Connpass)
                    {
                        EditorUtility.DisplayDialog("挑戦者登録",
                            $"参加番号:{Data.instance.participantID} のAIを登録しました。\n{folderPath}", "OK");
                    } else
                    {
                        EditorUtility.DisplayDialog("挑戦者登録",
                            $"参加ID:{Data.instance.randomID} のAIを登録しました。\n{folderPath}", "OK");
                    }

                    // ダイアログ閉じたらウインドウも閉じる(1フレーム後)
                    EditorApplication.delayCall += () =>
                    {
                        var window = Resources.FindObjectsOfTypeAll<ParticipantCreationWindow>();
                        foreach (var win in window)
                        {
                            win.Close();
                        }
                    };
                };
            }
            catch
            {
                // アンロードしてメモリ解放 
                PrefabUtility.UnloadPrefabContents(contentsRoot);
            }
            finally
            {
                if (contentsRoot != null)
                {
                    PrefabUtility.UnloadPrefabContents(contentsRoot);
                }
            }

        }
    }
}

#endif