#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
using System;
using System.Collections.Generic;
using System.IO;

namespace SXG2025
{

    [InitializeOnLoad]
    public static class EditComPlayerSceneGui
    {
        // 砲塔プレハブのパス
        const string TurretPrefabPath = "Assets/BattleTanks/Prefabs/TurretPart.prefab";

        // 回転パーツプレハブのパス
        const string RotatorPrefabPath = "Assets/BattleTanks/Prefabs/RotJointPart.prefab";

        // ComPlayerBase 側の配列フィールド名
        const string TurretArrayFieldName = "m_turrets";

        // ComPlayerBase 側の配列フィールド名
        const string RotatorArrayFieldName = "m_rotJoints";


        const string DataFormatTankPath = "Assets/BattleTanks/Data/Resources/DataTank.asset";


        static int m_lastHash = 0;
        static int m_lastCost = -1;
        static double m_lastRecalcTime = 0;
        static int m_countOfTurrets = 0;
        static int m_countOfRotators = 0;
        static int m_countOfArmors = 0;
        static float m_tankMass = 0;
        static DataFormatTank m_editorDataTankCache = null;
        static bool m_showCollision = false;
        static GameObject m_collisionPrefab;

        static GUIStyle m_valueStyle;
        static GUIStyle m_warningValueStyle;

        static GUIStyle m_easyEditButtonStyle;
        static GUIContent m_easyEditButtonContent;

        static List<GameObject> m_errorObjectList = new();

        static Rect m_panelRect = new Rect(50, 10, 380, 0);
        static bool m_isDraggingPanel = false;
        static Vector2 m_dragStartMouse;
        static Vector2 m_dragStartPos;

        static bool m_showGuide = false;   // おすすめ手順（デフォルト閉）
        static bool m_showEasyAI = false;  // AI（慣れてきたら）
        static bool m_showSubmission = false;

        static Vector2 m_panelScroll;

        static GUIStyle m_costLabelStyle;
        static GUIStyle m_costValueStyle;
        static GUIStyle m_costValueWarningStyle;

        // ----------------------------
        // 追加：パネル最小化
        // ----------------------------
        static bool m_panelMinimized = false;

        // ----------------------------
        // 追加：装甲デフォルトマテリアル
        // ----------------------------
        const string ArmorMaterialsFolderName = "Materials";
        const string ArmorDefaultMaterialFileName = "Armor_Default.mat";
        const string ShaderName_URP_Lit = "Universal Render Pipeline/Lit";
        const string ShaderName_Standard = "Standard";



        static EditComPlayerSceneGui()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.hierarchyChanged += OnEditorChangeEvent;
            Undo.undoRedoPerformed += OnEditorChangeEvent;
            EditorApplication.projectChanged += OnEditorChangeEvent;

        }

        static void OnEditorChangeEvent()
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null) return;

            var root = stage.prefabContentsRoot;
            if (root == null) return;

            var comPlayer = root.GetComponent<ComPlayerBase>();
            if (comPlayer == null) return;

            // 即時修正（ログは普段は静かに）
            EnforceParentRuleByLifting(stage, comPlayer, showLog: false);

            // コスト再計算（選択依存を排除）
            RecalculateCostIfNeeded(root, force: true);
        }


        static void RemakeStyles()
        {
            // スタイル
            m_valueStyle = new GUIStyle(EditorStyles.label);

            m_warningValueStyle = new GUIStyle(EditorStyles.label);
            m_warningValueStyle.normal.textColor = Color.red;

            // 簡単編集ボタン
            if (m_easyEditButtonStyle == null)
            {
                m_easyEditButtonStyle = new GUIStyle(GUI.skin.button);
                m_easyEditButtonStyle.fontStyle = FontStyle.Bold;
                m_easyEditButtonStyle.fontSize = 12;
                m_easyEditButtonStyle.fixedHeight = 34;
                m_easyEditButtonStyle.alignment = TextAnchor.MiddleCenter;
                m_easyEditButtonStyle.wordWrap = true;
                m_easyEditButtonStyle.padding = new RectOffset(6, 6, 2, 2);
            }

            // --- Cost強調表示用スタイル ---
            if (m_costLabelStyle == null)
            {
                m_costLabelStyle = new GUIStyle(EditorStyles.label);
                m_costLabelStyle.fontStyle = FontStyle.Bold;
            }

            if (m_costValueStyle == null)
            {
                m_costValueStyle = new GUIStyle(EditorStyles.label);
                m_costValueStyle.fontStyle = FontStyle.Bold;
                m_costValueStyle.fontSize = 14;
            }

            if (m_costValueWarningStyle == null)
            {
                m_costValueWarningStyle = new GUIStyle(m_costValueStyle);
                m_costValueWarningStyle.normal.textColor = new Color(1f, 0.35f, 0.35f);
            }

            // アイコン付きラベル
            if (m_easyEditButtonContent == null)
            {
                var icon = EditorGUIUtility.IconContent("d_ToolHandleCenter");
                if (icon == null || icon.image == null)
                {
                    icon = EditorGUIUtility.IconContent("d_Settings");
                }
                m_easyEditButtonContent = new GUIContent("簡単プログラム編集を開く", icon.image,
                    "簡単編集モードを開きます（適用時にAIスクリプトを上書きします）");
            }
        }


        static void OnSceneGUI(SceneView sv)
        {
            // Prefab編集画面でなければ何もしない
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null) return;

            // 選択オブジェクトが ComPlayerBase を持っているか確認
            GameObject obj = stage.prefabContentsRoot;
            if (obj == null) return;
            var comPlayer = obj.GetComponent<ComPlayerBase>();
            if (comPlayer == null) return;

            // スタイル生成
            RemakeStyles();

            // 定期的に必要があればコスト計算
            if (0.2f < EditorApplication.timeSinceStartup - m_lastRecalcTime)
            {
                RecalculateCostIfNeeded(obj);
                m_lastRecalcTime = EditorApplication.timeSinceStartup;
            }

            {
                Handles.BeginGUI();

                // 画面サイズに合わせてパネルの最大幅を制限（邪魔になりにくい）
                float maxWidth = Mathf.Min(420f, sv.position.width * 0.85f);
                m_panelRect.width = maxWidth;

                // ----------------------------
                // 変更：最小化時は高さを小さくする
                // ----------------------------
                m_panelRect.height = m_panelMinimized ? 72.0f : 240.0f;

                // 枠（重いGUI.skin.boxより軽いスタイルに寄せる）
                var panelStyle = new GUIStyle(EditorStyles.helpBox);
                panelStyle.padding = new RectOffset(10, 10, 8, 8);

                // 先にArea開始
                GUILayout.BeginArea(m_panelRect, panelStyle);

                //------------------------------
                // ヘッダー（ドラッグ＆最小化）
                //------------------------------
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("＜戦車Prefab編集モード＞", EditorStyles.boldLabel);

                    GUILayout.FlexibleSpace();

                    // ----------------------------
                    // 追加：最小化ボタン（説明不要のUI）
                    // ----------------------------
                    string btn = m_panelMinimized ? "＋" : "－";
                    if (GUILayout.Button(btn, EditorStyles.miniButton, GUILayout.Width(26)))
                    {
                        m_panelMinimized = !m_panelMinimized;
                        sv.Repaint();

                        // ボタン押下とドラッグ開始が競合しやすいので、ここでGUIを抜ける
                        GUIUtility.ExitGUI();
                    }
                }

                // ヘッダー行でドラッグ移動できるようにする
                Rect headerRect = GUILayoutUtility.GetLastRect();
                headerRect.x = 0;
                headerRect.width = m_panelRect.width;

                var e = Event.current;
                if (e.type == EventType.MouseDown && e.button == 0 && headerRect.Contains(e.mousePosition))
                {
                    m_isDraggingPanel = true;
                    m_dragStartMouse = e.mousePosition;
                    m_dragStartPos = new Vector2(m_panelRect.x, m_panelRect.y);
                    e.Use();
                }
                else if (e.type == EventType.MouseDrag && m_isDraggingPanel)
                {
                    Vector2 delta = e.mousePosition - m_dragStartMouse;
                    m_panelRect.position = m_dragStartPos + delta;

                    // 画面外に飛ばないように軽くクランプ
                    m_panelRect.x = Mathf.Clamp(m_panelRect.x, 0, sv.position.width - m_panelRect.width);
                    m_panelRect.y = Mathf.Clamp(m_panelRect.y, 0, sv.position.height - 40);

                    sv.Repaint();
                    e.Use();
                }
                else if (e.type == EventType.MouseUp)
                {
                    m_isDraggingPanel = false;
                }

                // ----------------------------
                // 変更：最小化中は「Cost行」と「砲塔数行」だけ表示する
                // ----------------------------
                if (m_panelMinimized)
                {
                    GUILayout.Space(2);

                    //--------------------------------------------------
                    // 状態：Cost / 出撃回数（強調版）※最小化中も表示
                    //--------------------------------------------------
                    bool isCostOver = false;

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Cost:", m_costLabelStyle, GUILayout.Width(40));

                        if (m_lastCost <= GameConstants.DEFAULT_PLAYER_ENERGY)
                        {
                            GUILayout.Label(m_lastCost.ToString(), m_costValueStyle, GUILayout.Width(54));
                        }
                        else
                        {
                            GUILayout.Label(m_lastCost.ToString(), m_costValueWarningStyle, GUILayout.Width(54));
                            isCostOver = true;
                        }

                        GUILayout.Space(10);

                        GUILayout.Label("出撃回数:", m_costLabelStyle, GUILayout.Width(58));
                        if (0 < m_lastCost)
                        {
                            GUILayout.Label((GameConstants.DEFAULT_PLAYER_ENERGY / m_lastCost).ToString(), m_costValueStyle, GUILayout.Width(32));
                        }
                        else
                        {
                            GUILayout.Label("-", m_costValueStyle, GUILayout.Width(32));
                        }

                        if (isCostOver)
                        {
                            GUILayout.Space(8);
                            GUILayout.Label("COST OVER", m_costValueWarningStyle);
                        }

                        GUILayout.FlexibleSpace();
                    }

                    //--------------------------------------------------
                    // 砲塔数などの行 ※最小化中も表示
                    //--------------------------------------------------
                    GUILayout.Label(string.Format("砲塔:{0} / 回転:{1} / 装備:{2} / 質量:{3}Kg",
                        m_countOfTurrets, m_countOfRotators, m_countOfArmors, m_tankMass), EditorStyles.miniLabel);
                }
                else
                {
                    // 通常表示（今まで通り）
                    m_panelScroll = GUILayout.BeginScrollView(m_panelScroll, false, true);

                    GUILayout.Space(6);

                    //--------------------------------------------------
                    // 状態：Cost / 出撃回数（強調版）
                    //--------------------------------------------------
                    bool isCostOver = false;

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Cost:", m_costLabelStyle, GUILayout.Width(40));

                        if (m_lastCost <= GameConstants.DEFAULT_PLAYER_ENERGY)
                        {
                            GUILayout.Label(m_lastCost.ToString(), m_costValueStyle, GUILayout.Width(54));
                        }
                        else
                        {
                            GUILayout.Label(m_lastCost.ToString(), m_costValueWarningStyle, GUILayout.Width(54));
                            isCostOver = true;
                        }

                        GUILayout.Space(10);

                        GUILayout.Label("出撃回数:", m_costLabelStyle, GUILayout.Width(58));
                        if (0 < m_lastCost)
                        {
                            GUILayout.Label((GameConstants.DEFAULT_PLAYER_ENERGY / m_lastCost).ToString(), m_costValueStyle, GUILayout.Width(32));
                        }
                        else
                        {
                            GUILayout.Label("-", m_costValueStyle, GUILayout.Width(32));
                        }

                        if (isCostOver)
                        {
                            GUILayout.Space(8);
                            GUILayout.Label("COST OVER", m_costValueWarningStyle);
                        }

                        GUILayout.FlexibleSpace();
                    }

                    GUILayout.Label(string.Format("砲塔:{0} / 回転:{1} / 装備:{2} / 質量:{3}Kg",
                        m_countOfTurrets, m_countOfRotators, m_countOfArmors, m_tankMass));

                    GUILayout.Space(6);

                    //--------------------------------------------------
                    // おすすめ手順（デフォルト閉：邪魔になりやすいので）
                    //--------------------------------------------------
                    m_showGuide = EditorGUILayout.Foldout(m_showGuide, "おすすめ手順", true);
                    if (m_showGuide)
                    {
                        EditorGUILayout.HelpBox(
                            "① 砲塔を追加して位置を調整します\n" +
                            "② 装甲などで見た目を整えます\n" +
                            "③ ▶ 再生して動かしてみます（ゲームパッド操作）\n" +
                            "④ ①〜③を繰り返して調整します\n" +
                            "⑤ AI（戦い方）を調整します",
                            MessageType.None);
                    }

                    GUILayout.Space(4);

                    //--------------------------------------------------
                    // 作る（砲塔・回転部位）
                    //--------------------------------------------------
                    GUILayout.Label("作る（砲塔・装甲・回転部位）", EditorStyles.boldLabel);
                    using (new GUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("砲塔追加", GUILayout.Width(108)))
                        {
                            CompactArray(comPlayer);
                            TryAddTurretToTankInPrefabStage(comPlayer, stage);
                        }
                        if (GUILayout.Button("装甲(Cube)追加", GUILayout.Width(108)))
                        {
                            TryAddArmorCubeToTankInPrefabStage(comPlayer, stage);
                        }
                        if (GUILayout.Button("回転部位追加", GUILayout.Width(108)))
                        {
                            CompactArray(comPlayer);
                            TryAddRotatorToTankInPrefabStage(comPlayer, stage);
                        }
                        GUILayout.FlexibleSpace();
                    }

                    GUILayout.Space(4);

                    //--------------------------------------------------
                    // 整える（チェック）
                    //--------------------------------------------------
                    GUILayout.Label("整える（チェック）", EditorStyles.boldLabel);
                    using (new GUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("要素チェック", GUILayout.Width(92)))
                        {
                            bool isUpdate = false;

                            if (comPlayer.transform.localPosition != Vector3.zero || comPlayer.transform.localRotation != Quaternion.identity)
                            {
                                comPlayer.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                                EditorUtility.SetDirty(comPlayer);
                                isUpdate = true;
                            }

                            if (CompactArray(comPlayer))
                            {
                                isUpdate = true;
                            }

                            if (EnforceParentRuleByLifting(stage, comPlayer, showLog: true))
                            {
                                isUpdate = true;
                            }

                            if (isUpdate)
                            {
                                EditorSceneManager.MarkSceneDirty(stage.scene);
                            }
                        }

                        string collisionBtn = m_showCollision ? "コリジョン非表示" : "コリジョン表示";
                        if (GUILayout.Button(collisionBtn, GUILayout.Width(120)))
                        {
                            m_showCollision = !m_showCollision;
                            SceneView.RepaintAll();
                        }

                        GUILayout.FlexibleSpace();
                    }

                    // Save案内は “常時HelpBox” だと面積を食うので、短いLabelに
                    GUILayout.Label("※変更後は右上の「Save」で保存します。", EditorStyles.miniLabel);

                    GUILayout.Space(6);

                    //--------------------------------------------------
                    // AI（慣れてきたら）
                    //--------------------------------------------------
                    m_showEasyAI = EditorGUILayout.Foldout(m_showEasyAI, "AI（戦い方）を調整する", true);
                    if (m_showEasyAI)
                    {
                        EditorGUILayout.HelpBox(
                            "プログラムが苦手な人でも、選ぶだけで戦い方（AI）を調整できます。\n" +
                            "プログラムのアイデアが欲しい人にもおすすめです。\n" +
                            "※上書きは編集画面の「適用」を押したときです（開くだけでは変わりません）。",
                            MessageType.Info);

                        if (GUILayout.Button(m_easyEditButtonContent, GUILayout.Height(24), GUILayout.Width(220)))
                        {
                            EasyProgramEditorWindow.Open(comPlayer);
                        }
                    }


                    //--------------------------------------------------
                    // 提出用出力
                    //--------------------------------------------------
                    GUILayout.Space(8);
                    m_showSubmission = EditorGUILayout.Foldout(m_showSubmission, "提出（ZIP出力）", true);
                    if (m_showSubmission)
                    {
                        EditorGUILayout.HelpBox(
                            "この戦車のフォルダを提出用ZIPにまとめます。\n" +
                            "※デスクトップに出力します（Backupは含めません）。",
                            MessageType.None);

                        if (GUILayout.Button("提出用ZIPを作成", GUILayout.Width(160)))
                        {
                            TryCreateSubmissionZipFromCurrentPrefabStage(stage);
                        }
                    }

                    GUILayout.EndScrollView();
                }

                GUILayout.EndArea();

                Handles.EndGUI();
            }


            // エラーオブジェクト
            if (0 < m_errorObjectList.Count)
            {
                Handles.BeginGUI();
                GUILayout.BeginArea(new Rect(50, 150, 400, 64), GUI.skin.box);
                GUILayout.Label("規定違反パーツ (" + m_errorObjectList.Count + "個)", m_warningValueStyle);
                GUILayout.BeginHorizontal();
                for (int i = 0; i < m_errorObjectList.Count; ++i)
                {
                    var errorObj = m_errorObjectList[i];
                    GUILayout.Label(string.Format("{0} ", (errorObj != null) ? errorObj.name : "---"), m_warningValueStyle);
                }
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
                Handles.EndGUI();
            }



            // 砲塔、回転部位にテキスト表示
            Handles.BeginGUI();
            try
            {
                TryDrawFromSerializedProperty(comPlayer, sv);
            }
            finally
            {
                Handles.EndGUI();
            }

            // 地面を表示
            DrawGround();

            // レギュレーションサイズを表示
            {
                var dataTank = LoadEditorDataTankCache();
                var bounds = dataTank.m_regulationBounds;

                var oldZTest = Handles.zTest;
                var oldColor = Handles.color;
                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                if (0 < m_errorObjectList.Count)
                {
                    Handles.color = Color.red;  // 規定違反がある場合は枠線を赤にする
                }
                else
                {
                    Handles.color = new Color(0.5f, 1.0f, 0.7f);
                }
                Handles.DrawWireCube(bounds.center, bounds.size);
                Handles.zTest = oldZTest;
                Handles.color = oldColor;
            }
        }

        /// <summary>
        /// 地面を表示
        /// </summary>
        static void DrawGround()
        {
            const float GROUND_Y = -0.57f;                // 地面の高さ
            const float RADIUS = 5.0f;

            // 円の中心
            Vector3 center = Vector3.zero;
            center.y = GROUND_Y;

            // 半透明の茶色
            var fillColor = new Color(0.55f, 0.30f, 0.12f, 0.20f);
            var edgeColor = new Color(0.55f, 0.30f, 0.12f, 0.90f);

            // デプス
            var oldZTest = Handles.zTest;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

            // 内円
            Handles.color = fillColor;
            Handles.DrawSolidDisc(center, Vector3.up, RADIUS);

            // 外円
            Handles.color = edgeColor;
            Handles.DrawWireDisc(center, Vector3.up, RADIUS);

            // 戻す
            Handles.zTest = oldZTest;
            Handles.color = Color.white;
        }


        static void TryAddTurretToTankInPrefabStage(ComPlayerBase comPlayer, PrefabStage stage)
        {
            Vector3 DefTurretLocalPosition = new Vector3(0, 1.42f, 0);

            // 砲塔プレハブロード
            GameObject turretPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TurretPrefabPath);
            if (turretPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", string.Format("砲塔プレハブが見つかりません：{0}", TurretPrefabPath), "OK");
                return;
            }

            // Prefab Stage が有効か再確認
            if (stage == null)
            {
                EditorUtility.DisplayDialog("Error", "Prefab 編集画面が開かれていません。", "OK");
                return;
            }

            // Prefab Stage のシーン上でインスタンス化 (Undo 登録)
            GameObject newTurret = null;
            try
            {
                newTurret = PrefabUtility.InstantiatePrefab(turretPrefab, stage.scene) as GameObject;
                if (newTurret == null)
                {
                    EditorUtility.DisplayDialog("Error", "砲塔のインスタンス化に失敗しました。", "OK");
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("砲塔のインスタンス化に失敗：" + ex);
                return;
            }

            // Undo登録
            Undo.RegisterCreatedObjectUndo(newTurret, "Create Turret");

            // 親をタンクにする
            newTurret.transform.SetParent(comPlayer.transform, false);
            newTurret.transform.SetLocalPositionAndRotation(DefTurretLocalPosition, Quaternion.identity);

            // 命名
            {
                var prefix = turretPrefab.name + "_";
                int serial = GetNextTwoDigitSerialUnder(comPlayer.transform, prefix);
                newTurret.name = $"{prefix}{serial:00}";
            }

            // ComPlayerBaseの配列に追加
            SerializedObject so = new SerializedObject(comPlayer);
            SerializedProperty arrayProperty = so.FindProperty(TurretArrayFieldName);
            if (arrayProperty == null)
            {
                EditorUtility.DisplayDialog("Error", string.Format("シリアライズされたフィールド '{0}' が見つかりません。", TurretArrayFieldName), "OK");
                return;
            }

            // 配列への追加前に Undo登録
            Undo.RecordObject(comPlayer, "Add Turret Reference");

            int newIndex = arrayProperty.arraySize;
            arrayProperty.InsertArrayElementAtIndex(newIndex);
            var element = arrayProperty.GetArrayElementAtIndex(newIndex);
            element.objectReferenceValue = newTurret;
            so.ApplyModifiedProperties();

            // Prefab Stage のシーンをDirtyにする
            EditorSceneManager.MarkSceneDirty(stage.scene);

            // Inspector更新とログ
            EditorUtility.SetDirty(comPlayer);
            Debug.Log("砲塔を追加しました。Prefab編集画面のSaveを押して保存してください。");

            // 生成したオブジェクトを選択状態にする
            Selection.activeGameObject = newTurret;
            EditorGUIUtility.PingObject(newTurret);
        }

        /// <summary>
        /// 回転パーツを追加する
        /// </summary>
        static void TryAddRotatorToTankInPrefabStage(ComPlayerBase comPlayer, PrefabStage stage)
        {
            // 回転部プレハブロード
            GameObject rotatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RotatorPrefabPath);
            if (rotatorPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", string.Format("回転部プレハブが見つかりません：{0}", RotatorPrefabPath), "OK");
                return;
            }

            // Prefab Stage が有効か再確認
            if (stage == null)
            {
                EditorUtility.DisplayDialog("Error", "Prefab 編集画面が開かれていません。", "OK");
                return;
            }

            // Prefab Stage のシーン上でインスタンス化 (Undo 登録)
            GameObject newRotator = null;
            try
            {
                newRotator = PrefabUtility.InstantiatePrefab(rotatorPrefab, stage.scene) as GameObject;
                if (newRotator == null)
                {
                    EditorUtility.DisplayDialog("Error", "回転部位のインスタンス化に失敗しました。", "OK");
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("回転部位のインスタンス化に失敗：" + ex);
                return;
            }

            // Undo登録
            Undo.RegisterCreatedObjectUndo(newRotator, "Create Rotator");

            // 親をタンクにする
            newRotator.transform.SetParent(comPlayer.transform, false);
            newRotator.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            // 命名
            {
                var prefix = rotatorPrefab.name + "_";
                int serial = GetNextTwoDigitSerialUnder(comPlayer.transform, prefix);
                newRotator.name = $"{prefix}{serial:00}";
            }

            // ComPlayerBaseの配列に追加
            SerializedObject so = new SerializedObject(comPlayer);
            SerializedProperty arrayProperty = so.FindProperty(RotatorArrayFieldName);
            if (arrayProperty == null)
            {
                EditorUtility.DisplayDialog("Error", string.Format("シリアライズされたフィールド '{0}' が見つかりません。", RotatorArrayFieldName), "OK");
                return;
            }

            // 配列への追加前に Undo登録
            Undo.RecordObject(comPlayer, "Add Rotator Reference");

            int newIndex = arrayProperty.arraySize;
            arrayProperty.InsertArrayElementAtIndex(newIndex);
            var element = arrayProperty.GetArrayElementAtIndex(newIndex);
            element.objectReferenceValue = newRotator;
            so.ApplyModifiedProperties();

            // Prefab Stage のシーンをDirtyにする
            EditorSceneManager.MarkSceneDirty(stage.scene);

            // Inspector更新とログ
            EditorUtility.SetDirty(comPlayer);
            Debug.Log("回転部位を追加しました。Prefab編集画面のSaveを押して保存してください。");

            // 生成したオブジェクトを選択状態にする
            Selection.activeGameObject = newRotator;
            EditorGUIUtility.PingObject(newRotator);
        }


        static int GetNextTwoDigitSerialUnder(Transform root, string namePrefix)
        {
            int max = 0;

            var all = root.GetComponentsInChildren<Transform>(true);
            foreach (var tr in all)
            {
                if (tr == null) continue;

                var n = tr.gameObject.name;
                if (!n.StartsWith(namePrefix, StringComparison.Ordinal)) continue;

                // 末尾２桁を読む
                if (n.Length < namePrefix.Length + 2) continue;

                var tail = n.Substring(n.Length - 2, 2);
                if (int.TryParse(tail, out var v))
                {
                    if (max < v) max = v;
                }
            }

            // 次の番号を返す
            return max + 1;
        }


        /// <summary>
        /// 必要なら配列を作り直す
        /// </summary>
        static bool CompactArray(ComPlayerBase comPlayer)
        {
            if (comPlayer == null) return false;

            bool result = false;
            result |= CompactArrayCore(comPlayer, TurretArrayFieldName, "Compact Turrets Array");
            result |= CompactArrayCore(comPlayer, RotatorArrayFieldName, "Compact Rotators Array");

            return result;
        }

        static bool CompactArrayCore(ComPlayerBase comPlayer, string arrayFieldName, string comments)
        {
            if (comPlayer == null) return false;

            SerializedObject so = new SerializedObject(comPlayer);
            if (so == null) return false;
            SerializedProperty arrayProperty = so.FindProperty(arrayFieldName);
            if (arrayProperty == null) return false;

            // Collect non-null references
            List<UnityEngine.Object> keep = new();
            int originalSize = arrayProperty.arraySize;
            for (int i = 0; i < originalSize; ++i)
            {
                var element = arrayProperty.GetArrayElementAtIndex(i);
                if (element != null && element.propertyType == SerializedPropertyType.ObjectReference && element.objectReferenceValue != null)
                {
                    if (!keep.Contains(element.objectReferenceValue))
                    {
                        keep.Add(element.objectReferenceValue);
                    }
                }
            }

            // 数を比較
            if (originalSize == keep.Count)
            {
                return false;
            }

            // Undo
            Undo.RecordObject(so.targetObject, comments);

            arrayProperty.ClearArray();
            foreach (var obj in keep)
            {
                int newIndex = arrayProperty.arraySize;
                arrayProperty.InsertArrayElementAtIndex(newIndex);
                var element = arrayProperty.GetArrayElementAtIndex(newIndex);
                element.objectReferenceValue = obj;
            }
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(comPlayer);

            return true;

        }



        static DataFormatTank LoadEditorDataTankCache()
        {
            if (m_editorDataTankCache != null) return m_editorDataTankCache;

            var loaded = AssetDatabase.LoadAssetAtPath<DataFormatTank>(DataFormatTankPath);
            if (loaded == null)
            {
                Debug.LogWarning("Asset is not found : " + DataFormatTankPath);
                return null;
            }

            m_editorDataTankCache = loaded;
            return m_editorDataTankCache;
        }

        /// <summary>
        /// 必要ならコストを再計算
        /// </summary>
        static void RecalculateCostIfNeeded(GameObject root, bool force = false)
        {
            if (root == null) return;
            ComPlayerBase comPlayer = root.GetComponent<ComPlayerBase>();
            if (comPlayer == null) return;

            // 簡易ハッシュを比較して更新があった時だけ再計算する
            int h = ComputeSimpleHash(root);
            if (force || h != m_lastHash)
            {
                // コストデータをロード
                var dataTank = LoadEditorDataTankCache();

                // コスト再計算
                m_lastHash = h;
                m_lastCost = BaseTank.SystemCalculateTankCost(comPlayer,
                    out m_countOfTurrets, out m_countOfRotators, out m_countOfArmors, out m_tankMass,
                    dataTank, m_errorObjectList);

                m_lastRecalcTime = EditorApplication.timeSinceStartup;
                SceneView.RepaintAll();
            }

        }

        /// <summary>
        /// プレハブの簡易ハッシュ
        /// </summary>
        static int ComputeSimpleHash(GameObject root)
        {
            int hash = 17;

            unchecked
            {
                // 子の数
                hash = hash * 31 + root.GetComponentsInChildren<Transform>(true).Length;
                // MeshFilter / MeshRenderer の参照IDと頂点数
                foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    var m = mf.sharedMesh;
                    int id = (m != null) ? m.GetInstanceID() : 0;
                    int vc = (m != null) ? m.vertexCount : 0;
                    hash = hash * 31 + id;
                    hash = hash * 31 + vc;
                }
                foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var mats = mr.sharedMaterials;
                    hash = hash * 31 + ((mats != null) ? mats.Length : 0);
                    foreach (var mat in mats)
                    {
                        hash = hash * 31 + ((mat != null) ? mat.GetInstanceID() : 0);
                    }
                }
                // 子のスケール変更
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    // スケール
                    int sx = Mathf.RoundToInt(t.localScale.x * 1000.0f);
                    int sy = Mathf.RoundToInt(t.localScale.y * 1000.0f);
                    int sz = Mathf.RoundToInt(t.localScale.z * 1000.0f);
                    hash = hash * 397 ^ sx;
                    hash = hash * 397 ^ sy;
                    hash = hash * 397 ^ sz;

                    // 座標
                    int px = Mathf.RoundToInt(t.localPosition.x * 1000.0f);
                    int py = Mathf.RoundToInt(t.localPosition.y * 1000.0f);
                    int pz = Mathf.RoundToInt(t.localPosition.z * 1000.0f);
                    hash = hash * 397 ^ px;
                    hash = hash * 397 ^ py;
                    hash = hash * 397 ^ pz;

                    // 回転
                    int rx = Mathf.RoundToInt(t.localRotation.x * 10.0f);
                    int ry = Mathf.RoundToInt(t.localRotation.y * 10.0f);
                    int rz = Mathf.RoundToInt(t.localRotation.z * 10.0f);
                    hash = hash * 397 ^ rx;
                    hash = hash * 397 ^ ry;
                    hash = hash * 397 ^ rz;
                }
            }
            return hash;
        }



        #region 3D空間に砲塔、回転部位の番号を表示

        static void TryDrawFromSerializedProperty(ComPlayerBase comPlayer, SceneView sv)
        {
            var so = new SerializedObject(comPlayer);

            // 砲塔
            TryDrawParts(so, TurretArrayFieldName, "  Turret[{0}]", sv);

            // 回転部位
            TryDrawParts(so, RotatorArrayFieldName, "  Rotator[{0}]", sv);
        }

        static void TryDrawParts(SerializedObject so, string propertyName, string message, SceneView sv)
        {
            var array = so.FindProperty(propertyName);
            if (array == null || !array.isArray) return;

            for (int i = 0; i < array.arraySize; ++i)
            {
                var element = array.GetArrayElementAtIndex(i);
                if (element != null)
                {
                    var objRef = element.objectReferenceValue;
                    if (objRef != null)
                    {
                        Transform t = GetTransformFromObjectReference(objRef);
                        if (t != null)
                        {
                            DrawLabelAt(t.position, string.Format(message, i), sv);
                        }
                    }
                }
            }
        }

        static void DrawLabelAt(Vector3 worldPos, string text, SceneView sv)
        {
            Handles.Label(worldPos, text);

            // 小さなドット
            Handles.color = Color.cyan;
            Handles.DrawSolidDisc(worldPos, sv.camera.transform.forward, 0.02f);
            Handles.color = Color.white;
        }

        static Transform GetTransformFromObjectReference(UnityEngine.Object obj)
        {
            if (obj is GameObject go) return go.transform;
            if (obj is Component comp) return comp.transform;
            return null;
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]
        static void DrawCollisionGizmo(ComPlayerBase src, GizmoType gizmoType)
        {
            if (!m_showCollision) return;

            if (m_collisionPrefab == null)
            {
                m_collisionPrefab = Resources.Load<GameObject>("collision");
                if (m_collisionPrefab == null) return;
            }

            MeshFilter mf = m_collisionPrefab.GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
                Gizmos.DrawMesh(mf.sharedMesh, src.transform.position, src.transform.rotation, src.transform.lossyScale);
            }
        }

        #endregion



        const string EditorPrefsKey_EasyAI_FirstConfirmShown = "SXG2025.EasyAI.FirstConfirmShown";

        static bool ConfirmOpenEasyAIIfFirstTime()
        {
            // 既に表示済みなら確認なしで通す
            if (EditorPrefs.GetBool(EditorPrefsKey_EasyAI_FirstConfirmShown, false))
                return true;

            // 初回だけ表示（軽め・誤解防止）
            bool ok = EditorUtility.DisplayDialog(
                "かんたんAI作成（確認）",
                "この編集画面では、選ぶだけでAI（戦い方）を作れます。\n\n" +
                "※AIスクリプトが上書きされるのは「適用」を押したときです。\n" +
                "（開くだけでは上書きされません）",
                "開く",
                "キャンセル"
            );

            if (ok)
            {
                EditorPrefs.SetBool(EditorPrefsKey_EasyAI_FirstConfirmShown, true);
            }

            return ok;
        }



        const string ArmorNamePrefix = "Armor_";

        static void TryAddArmorCubeToTankInPrefabStage(ComPlayerBase comPlayer, PrefabStage stage)
        {
            if (comPlayer == null) return;

            if (stage == null)
            {
                EditorUtility.DisplayDialog("Error", "Prefab 編集画面が開かれていません。", "OK");
                return;
            }

            // Unity標準のCube生成（1x1x1 + BoxCollider）
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (go == null) return;

            Undo.RegisterCreatedObjectUndo(go, "Create Armor Cube");

            // 親をタンクにする（PrefabStage内の編集なのでOK）
            go.transform.SetParent(comPlayer.transform, false);

            // 初期位置（指定）
            go.transform.localPosition = new Vector3(0f, 0.5f, 2f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            // 命名（Armor_01, Armor_02...）
            int serial = GetNextTwoDigitSerialUnder(comPlayer.transform, ArmorNamePrefix);
            go.name = $"{ArmorNamePrefix}{serial:00}";

            // ----------------------------
            // 追加：装甲用デフォルトマテリアルの自動生成＆割り当て
            // ----------------------------
            try
            {
                var mat = GetOrCreateArmorDefaultMaterialForCurrentPrefab(stage);
                if (mat != null)
                {
                    var r = go.GetComponent<Renderer>();
                    if (r != null)
                    {
                        // 参加者が学習して自力で差し替える余地を残しつつ、
                        // 「追加した直後は灰色で味気ない」を避ける
                        r.sharedMaterial = mat;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("装甲デフォルトマテリアルの生成/割り当てに失敗しました: " + ex.Message);
            }

            // Prefab Stage のシーンをDirtyにする
            EditorSceneManager.MarkSceneDirty(stage.scene);

            // 選択状態にしてすぐ編集できるようにする
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);

            Debug.Log("装甲（Cube）を追加しました。位置・大きさ・色を調整し、右上のSaveで保存してください。");
        }

        static Material GetOrCreateArmorDefaultMaterialForCurrentPrefab(PrefabStage stage)
        {
            if (stage == null) return null;

            // 参加者フォルダが取れれば最優先。取れない場合はPrefabのあるフォルダを使う。
            string baseFolderAssetPath = null;

            if (TryGetParticipantFolderPathFromPrefabStage(stage, out var participantFolderAssetPath))
            {
                baseFolderAssetPath = participantFolderAssetPath;
            }
            else
            {
                var prefabAssetPath = stage.assetPath?.Replace("\\", "/");
                if (!string.IsNullOrEmpty(prefabAssetPath))
                {
                    baseFolderAssetPath = Path.GetDirectoryName(prefabAssetPath)?.Replace("\\", "/");
                }
            }

            if (string.IsNullOrEmpty(baseFolderAssetPath))
            {
                return null;
            }

            // Materialsフォルダ
            var materialsFolderAssetPath = CombineAssetPath(baseFolderAssetPath, ArmorMaterialsFolderName);
            EnsureFolderExists(materialsFolderAssetPath);

            // Armor_Default.mat
            var matAssetPath = CombineAssetPath(materialsFolderAssetPath, ArmorDefaultMaterialFileName);
            var existing = AssetDatabase.LoadAssetAtPath<Material>(matAssetPath);
            if (existing != null)
            {
                return existing;
            }

            // シェーダ選択（URP優先、無ければStandard）
            Shader shader = Shader.Find(ShaderName_URP_Lit);
            if (shader == null)
            {
                shader = Shader.Find(ShaderName_Standard);
            }
            if (shader == null)
            {
                Debug.LogWarning("Shaderが見つからないため、装甲デフォルトマテリアルを作成できません。");
                return null;
            }

            var mat = new Material(shader);

            // 初期色：少しだけ“戦車っぽい”色に（好みで調整OK）
            // URP/Lit: _BaseColor, Standard: _Color
            var defaultColor = new Color(0.35f, 0.45f, 0.30f, 1.0f); // 渋めグリーン
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", defaultColor);
            }
            else if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", defaultColor);
            }

            AssetDatabase.CreateAsset(mat, matAssetPath);
            AssetDatabase.SaveAssets();

            return mat;
        }

        static string CombineAssetPath(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return b;
            if (string.IsNullOrEmpty(b)) return a;
            a = a.Replace("\\", "/").TrimEnd('/');
            b = b.Replace("\\", "/").TrimStart('/');
            return $"{a}/{b}";
        }

        static void EnsureFolderExists(string folderAssetPath)
        {
            if (string.IsNullOrEmpty(folderAssetPath)) return;
            folderAssetPath = folderAssetPath.Replace("\\", "/").TrimEnd('/');

            if (AssetDatabase.IsValidFolder(folderAssetPath)) return;

            // 親から順に作る
            var parts = folderAssetPath.Split('/');
            if (parts.Length <= 1) return;

            string cur = parts[0]; // "Assets" の想定
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(cur, parts[i]);
                }
                cur = next;
            }
        }


        static bool TryGetParticipantFolderPathFromPrefabStage(PrefabStage stage, out string participantFolderAssetPath)
        {
            participantFolderAssetPath = null;
            if (stage == null) return false;

            // stage.assetPath は Prefabのアセットパス
            // 例：Assets/Participant/Player1234567 - 神谷/Player1234567.prefab
            string prefabAssetPath = stage.assetPath;
            if (string.IsNullOrEmpty(prefabAssetPath)) return false;

            prefabAssetPath = prefabAssetPath.Replace("\\", "/");
            participantFolderAssetPath = Path.GetDirectoryName(prefabAssetPath)?.Replace("\\", "/");
            return !string.IsNullOrEmpty(participantFolderAssetPath);
        }

        static void TryCreateSubmissionZipFromCurrentPrefabStage(PrefabStage stage)
        {
            if (!TryGetParticipantFolderPathFromPrefabStage(stage, out var folderAssetPath))
            {
                EditorUtility.DisplayDialog("挑戦者出力", "参加者フォルダを特定できませんでした。", "OK");
                return;
            }

            // 念のため確認（軽め）
            bool ok = EditorUtility.DisplayDialog(
                "挑戦者出力",
                $"このフォルダを提出用にZIP圧縮します。\n\n{folderAssetPath}\n\nよろしいですか？",
                "ZIP作成",
                "キャンセル"
            );
            if (!ok) return;

            try
            {
                if (ParticipantSubmissionContextMenu.TryCreateZipFromParticipantFolder(folderAssetPath, out var zipFullPath, reveal: true))
                {
                    EditorUtility.DisplayDialog("挑戦者出力", $"提出用ZIPを作成しました。\n{zipFullPath}", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "挑戦者出力",
                        "このPrefabは参加者フォルダ（Assets/Participant/Player…）配下ではないため、ZIPを作成できませんでした。",
                        "OK"
                    );
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("エラー", "圧縮中にエラーが発生しました:\n" + ex.Message, "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }



        #region 大砲と回転パーツの階層調整

        static bool IsAllowedParentForTurretOrRotator(Transform parent, Transform rootTr)
        {
            if (parent == null) return false;
            if (parent == rootTr) return true; // ルート直下 OK
            if (parent.GetComponent<RotJointPart>() != null) return true; // 回転パーツ直下 OK
            return false;
        }

        /// <summary>
        /// 砲塔（TurretPart）と回転（RotJointPart）を、
        /// 「ルート直下」または「回転パーツ直下」になるまで、親を1つずつ上げて自動修正する
        /// </summary>
        static bool EnforceParentRuleByLifting(PrefabStage stage, ComPlayerBase comPlayer, bool showLog)
        {
            if (stage == null || comPlayer == null) return false;

            var root = stage.prefabContentsRoot;
            if (root == null) return false;

            bool changed = false;
            var rootTr = root.transform;

            // コンポーネントで判定（名前に依存しない）
            var turrets = rootTr.GetComponentsInChildren<TurretPart>(true);
            var rotators = rootTr.GetComponentsInChildren<RotJointPart>(true);

            // Transform単位でまとめる（重複回避）
            var set = new HashSet<Transform>();
            foreach (var t in turrets) if (t != null) set.Add(t.transform);
            foreach (var r in rotators) if (r != null) set.Add(r.transform);

            foreach (var tr in set)
            {
                if (tr == null) continue;
                if (tr == rootTr) continue;

                // 許可される親になるまで、階層を一つ上げる
                int safety = 64;
                while (!IsAllowedParentForTurretOrRotator(tr.parent, rootTr) && safety-- > 0)
                {
                    var oldParent = tr.parent;
                    if (oldParent == null) break;

                    var newParent = oldParent.parent;
                    if (newParent == null)
                    {
                        // ルート外にいたら強制的にPrefabルート直下へ
                        newParent = rootTr;
                    }

                    Undo.SetTransformParent(tr, newParent, "Lift Turret/Rotator Parent");
                    tr.SetParent(newParent, true); // worldPositionStays = true

                    changed = true;

                    // これ以上上げられないなら終了
                    if (newParent == rootTr) break;
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(stage.scene);
                EditorUtility.SetDirty(comPlayer);

                if (showLog)
                {
                    Debug.LogWarning(
                        "砲塔（TurretPart）と回転パーツ（RotJointPart）は「ルート直下」または「回転パーツ直下」にのみ配置できます。\n" +
                        "ルール外の階層にあったため、自動で階層を持ち上げて修正しました。"
                    );
                }
            }

            return changed;
        }

        #endregion

    }

}

#endif
