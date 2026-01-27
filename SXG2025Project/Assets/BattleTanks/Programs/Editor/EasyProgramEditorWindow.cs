#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SXG2025
{
    public partial class EasyProgramEditorWindow : EditorWindow
    {
        // =========================
        // 公開：開く
        // =========================
        ComPlayerBase m_target;

        public static void Open(ComPlayerBase target)
        {
            var w = GetWindow<EasyProgramEditorWindow>(utility: true, title: "かんたんAI作成");
            w.minSize = new Vector2(460, 520);
            w.m_target = target;

            if (w.m_target != target)
            {
                w.m_target = target;
                w.m_config = null;
                w.m_dirty = false;
                w.m_turretFoldouts.Clear();
            }

            w.EnsureConfigInitialized();
            w.Show();
            w.Focus();
        }

        // =========================
        // 設定データ
        // =========================
        enum EasyModeKind
        {
            GamepadTest = 0,   // A：動作検証用ゲームパッド操作（内容固定）
            BuildByPreset = 1, // B：選んでプログラム作成
        }

        enum MovePreset
        {
            Random = 0,
            Rush = 1,
            KeepDistance = 2,
            OrbitingArena = 3,
        }

        enum FireConfigMode
        {
            Shared = 0,   // 全砲塔共通
            PerTurret = 1 // 砲塔ごとに個別
        }

        //enum TargetRule
        //{
        //    MinDistance = 0,
        //    MaxEnergy = 1,
        //    MinEnergy = 2,
        //    InFront = 3,
        //    InBehind = 4,
        //}

        enum FireRule
        {
            WhenAimed = 0,
            Interval = 1,
            DistanceUnder = 2,
        }

        [Serializable]
        class EasyAiConfig
        {
            public EasyModeKind? mode = null;

            // 移動（Bのみ）
            public MovePreset movePreset = MovePreset.Random;

            // 射撃（Bのみ）
            public AimConfig aim = new AimConfig();
        }

        [Serializable]
        class AimConfig
        {
            public FireConfigMode mode = FireConfigMode.Shared;
            public EasyAI.TargetRule sharedTargetRule = EasyAI.TargetRule.MinDistance;
            public List<EasyAI.TargetRule> perTurretTargetRules = new();
        }


        EasyAiConfig m_config;
        bool m_dirty;

        // Foldout状態（砲塔ごと）
        readonly List<bool> m_turretFoldouts = new();

        // =========================
        // Unityイベント
        // =========================
        void OnEnable()
        {
            EnsureConfigInitialized();
        }

        void OnGUI()
        {
            EnsureConfigInitialized();

            // Prefab Stageの整合性チェック（できるだけ軽く）
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null)
            {
                EditorGUILayout.HelpBox("Prefab編集モードで開いてください。", MessageType.Warning);
                return;
            }

            if (m_target == null)
            {
                EditorGUILayout.HelpBox("対象の戦車が見つかりません。Prefab編集モードで戦車Prefabを開き直してください。", MessageType.Error);
                return;
            }

            DrawTargetInfo(stage);
            EditorGUILayout.Space(8);

            // ---- 変更検知（初期は非表示のため、dirtyはユーザー操作時のみ立てる）----
            EditorGUI.BeginChangeCheck();

            DrawModeSelector();

            EditorGUILayout.Space(8);

            if (m_config.mode == EasyModeKind.GamepadTest)
            {
                DrawGamepadTestInfo();
            }
            else if (m_config.mode == EasyModeKind.BuildByPreset)
            {
                DrawMovePresetUI();
                //DrawFireUI_WithSharedOrPerTurret(stage);

                DrawAimUI_WithSharedOrPerTurret_TargetRuleOnly();
            }

            if (EditorGUI.EndChangeCheck())
            {
                m_dirty = true;
            }

            DrawApplyAreaIfDirty();
        }

        // =========================
        // 描画：上部情報
        // =========================
        void DrawTargetInfo(PrefabStage stage)
        {
            EditorGUILayout.LabelField("対象", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Prefab Root", stage.prefabContentsRoot, typeof(GameObject), allowSceneObjects: true);
                EditorGUILayout.ObjectField("ComPlayerBase", m_target, typeof(ComPlayerBase), allowSceneObjects: true);
            }

            EditorGUILayout.HelpBox(
                "このウィンドウでは、選ぶだけでAIテンプレートを作成できます。\n" +
                "※実際の上書きは「適用して上書き保存」を押したときだけ行います。",
                MessageType.Info);
        }

        // =========================
        // 描画：A/B選択
        // =========================
        void DrawModeSelector()
        {
            EditorGUILayout.LabelField("■作成方法", EditorStyles.boldLabel);

            int mode = -1;
            if (m_config.mode != null)
            {
                mode = (int)m_config.mode;
            }
            mode = GUILayout.SelectionGrid(
                mode,
                new[]
                {
                    "ゲームパッドで動作検証（テスト用）",
                    "選んでAIを作成（かんたん）"
                },
                1
            );
            if (0 <= mode)
            {
                m_config.mode = (EasyModeKind)mode;
            }
        }

        // =========================
        // 描画：A（内容固定）
        // =========================
        void DrawGamepadTestInfo()
        {
            EditorGUILayout.LabelField("■内容", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "テスト用のゲームパッド操作コードを生成します（内容は固定です）。\n" +
                "・戦車スペック（質量など）に応じた動きの確認に使えます。\n" +
                "・提出前に無効化する運用にしたい場合は、レギュレーションに従ってください。",
                MessageType.Info);
        }

        // =========================
        // 描画：B（移動）
        // =========================
        void DrawMovePresetUI()
        {
            EditorGUILayout.LabelField("■移動", EditorStyles.boldLabel);

            m_config.movePreset = (MovePreset)EditorGUILayout.EnumPopup("移動プリセット", m_config.movePreset);

            // ここは今は概要だけ。後でプリセットごとのパラメータUIを追加できます。
            switch (m_config.movePreset)
            {
                case MovePreset.Random:
                    EditorGUILayout.HelpBox("ランダムで目標座標を決めて、そこへ向かって移動します。", MessageType.None);
                    break;
                case MovePreset.Rush:
                    EditorGUILayout.HelpBox("最寄りの敵に近づくことを優先します。", MessageType.None);
                    break;
                case MovePreset.KeepDistance:
                    EditorGUILayout.HelpBox("敵からなるべく離れる移動をします。", MessageType.None);
                    break;
                case MovePreset.OrbitingArena:
                    EditorGUILayout.HelpBox("闘技場を円形に周回移動します。", MessageType.None);
                    break;
            }
        }



        void DrawAimUI_WithSharedOrPerTurret_TargetRuleOnly()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("■射撃（狙う相手）", EditorStyles.boldLabel);

            int turretCount = GetTurretCount(m_target);
            EditorGUILayout.LabelField($"砲塔数：{turretCount}", EditorStyles.miniLabel);

            // ★重要：モードに関係なく毎回整合（前のPrefabの状態が残るのを防ぐ）
            EnsurePerTurretTargetRuleListSize(turretCount);
            EnsureFoldoutListSize(turretCount);

            // モード切り替え
            var oldMode = m_config.aim.mode;
            m_config.aim.mode = (FireConfigMode)GUILayout.Toolbar(
                (int)m_config.aim.mode,
                new[] { "全砲塔共通", "砲塔ごとに個別" }
            );

            // Shared -> PerTurret への切替時だけ共通設定をコピー
            if (oldMode != m_config.aim.mode && m_config.aim.mode == FireConfigMode.PerTurret)
            {
                for (int i = 0; i < turretCount; i++)
                    m_config.aim.perTurretTargetRules[i] = m_config.aim.sharedTargetRule;

                for (int i = 0; i < m_turretFoldouts.Count; i++)
                    m_turretFoldouts[i] = false; // 誤操作防止：切替直後は閉じる

                m_dirty = true;
            }

            EditorGUILayout.Space(6);

            // ---- 共通設定 ----
            if (m_config.aim.mode == FireConfigMode.Shared)
            {
                m_config.aim.sharedTargetRule =
                    (EasyAI.TargetRule)EditorGUILayout.EnumPopup("狙う相手", m_config.aim.sharedTargetRule);

                DrawTargetRuleHelp(m_config.aim.sharedTargetRule);
                return;
            }

            // ---- 個別設定 ----
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("全部開く", GUILayout.Width(80)))
                    for (int i = 0; i < turretCount; i++) m_turretFoldouts[i] = true;

                if (GUILayout.Button("全部閉じる", GUILayout.Width(80)))
                    for (int i = 0; i < turretCount; i++) m_turretFoldouts[i] = false;

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("共通設定を全砲塔にコピー", GUILayout.Width(190)))
                {
                    for (int i = 0; i < turretCount; i++)
                        m_config.aim.perTurretTargetRules[i] = m_config.aim.sharedTargetRule;

                    m_dirty = true;
                }
            }

            EditorGUILayout.Space(4);

            for (int i = 0; i < turretCount; i++)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);

                m_turretFoldouts[i] = EditorGUILayout.Foldout(
                    m_turretFoldouts[i],
                    $"砲塔 {i}",
                    toggleOnLabelClick: true
                );

                if (m_turretFoldouts[i])
                {
                    EditorGUI.indentLevel++;

                    m_config.aim.perTurretTargetRules[i] =
                        (EasyAI.TargetRule)EditorGUILayout.EnumPopup("狙う相手", m_config.aim.perTurretTargetRules[i]);

                    DrawTargetRuleHelp(m_config.aim.perTurretTargetRules[i]);

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }
        }

        void EnsurePerTurretTargetRuleListSize(int turretCount)
        {
            if (m_config.aim.perTurretTargetRules == null)
                m_config.aim.perTurretTargetRules = new List<EasyAI.TargetRule>();

            while (m_config.aim.perTurretTargetRules.Count < turretCount)
                m_config.aim.perTurretTargetRules.Add(m_config.aim.sharedTargetRule);

            while (turretCount < m_config.aim.perTurretTargetRules.Count)
                m_config.aim.perTurretTargetRules.RemoveAt(m_config.aim.perTurretTargetRules.Count - 1);
        }

        static void DrawTargetRuleHelp(EasyAI.TargetRule rule)
        {
            string msg = rule switch
            {
                EasyAI.TargetRule.MinDistance => "一番近い相手を狙います。迷ったらこれがおすすめです。",
                EasyAI.TargetRule.MaxEnergy => "残チームエネルギーが一番多い相手（現時点で優勢な相手）を狙います。",
                EasyAI.TargetRule.MinEnergy => "残チームエネルギーが一番少ない相手（現時点で劣勢な相手）を狙います。",
                EasyAI.TargetRule.InFront => "自分の正面に近い相手を狙います。",
                EasyAI.TargetRule.Behind => "自分の背面に近い相手を狙います。",
                _ => "狙う相手を選びます。"
            };

            EditorGUILayout.HelpBox(msg, MessageType.None);
        }

        /*
        // =========================
        // 描画：B（射撃：共通／個別＋Foldout）
        // =========================
        void DrawFireUI_WithSharedOrPerTurret(PrefabStage stage)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("■射撃", EditorStyles.boldLabel);

            int turretCount = GetTurretCount(m_target);
            EditorGUILayout.LabelField($"砲塔数：{turretCount}", EditorStyles.miniLabel);

            // 現在値を退避して、切り替え瞬間を検知
            var oldMode = m_config.fire.mode;

            m_config.fire.mode = (FireConfigMode)GUILayout.Toolbar(
                (int)m_config.fire.mode,
                new[] { "全砲塔共通", "砲塔ごとに個別" }
            );

            // Shared -> PerTurret に変わった瞬間だけ「共通設定を自動コピー」
            if (oldMode != m_config.fire.mode && m_config.fire.mode == FireConfigMode.PerTurret)
            {
                EnsurePerTurretListSize(turretCount);

                for (int i = 0; i < turretCount; i++)
                    CopyFireConfig(m_config.fire.shared, m_config.fire.perTurret[i]);

                EnsureFoldoutListSize(turretCount);

                // 誤操作防止優先：切替直後は全部閉じる
                for (int i = 0; i < m_turretFoldouts.Count; i++)
                    m_turretFoldouts[i] = false;

                m_dirty = true; // ユーザーが切り替えた＝編集した扱い
            }

            EditorGUILayout.Space(6);

            if (m_config.fire.mode == FireConfigMode.Shared)
            {
                DrawTurretFireConfigUI("共通設定", m_config.fire.shared);
                return;
            }

            // ---- 個別（Foldout）----
            EnsurePerTurretListSize(turretCount);
            EnsureFoldoutListSize(turretCount);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("全部開く", GUILayout.Width(80)))
                {
                    for (int i = 0; i < turretCount; i++) m_turretFoldouts[i] = true;
                }
                if (GUILayout.Button("全部閉じる", GUILayout.Width(80)))
                {
                    for (int i = 0; i < turretCount; i++) m_turretFoldouts[i] = false;
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("共通設定を全砲塔にコピー", GUILayout.Width(190)))
                {
                    for (int i = 0; i < turretCount; i++)
                        CopyFireConfig(m_config.fire.shared, m_config.fire.perTurret[i]);

                    m_dirty = true;
                }
            }

            EditorGUILayout.Space(4);

            for (int i = 0; i < turretCount; i++)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);

                m_turretFoldouts[i] = EditorGUILayout.Foldout(
                    m_turretFoldouts[i],
                    $"砲塔 {i}",
                    toggleOnLabelClick: true
                );

                if (m_turretFoldouts[i])
                {
                    EditorGUI.indentLevel++;
                    DrawTurretFireConfigUI(null, m_config.fire.perTurret[i]);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }
        }

        void DrawTurretFireConfigUI(string label, TurretFireConfig cfg)
        {
            if (!string.IsNullOrEmpty(label))
                EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

            cfg.targetRule = (EasyAI.TargetRule)EditorGUILayout.EnumPopup("狙う相手", cfg.targetRule);
            cfg.fireRule = (FireRule)EditorGUILayout.EnumPopup("発射条件", cfg.fireRule);

            switch (cfg.fireRule)
            {
                case FireRule.Interval:
                    cfg.intervalSec = EditorGUILayout.Slider("発射間隔（秒）", cfg.intervalSec, 0.1f, 3.0f);
                    break;

                case FireRule.DistanceUnder:
                    cfg.maxDistance = EditorGUILayout.Slider("発射距離（以下）", cfg.maxDistance, 1f, 60f);
                    break;
            }
        }
        */


        // =========================
        // 適用ボタン（dirty時のみ表示）
        // =========================
        void DrawApplyAreaIfDirty()
        {
            if (!m_dirty)
                return;

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "変更があります。\n" +
                "「適用して上書き保存」を押すと、現在のAIスクリプトを上書きします（バックアップ推奨）。",
                MessageType.Warning);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("適用して上書き保存", GUILayout.Width(220), GUILayout.Height(34)))
                {
                    if (TryApplyOverwrite())
                    {
                        m_dirty = false;
                    }
                }

                GUILayout.FlexibleSpace();
            }
        }

        /*
        bool TryApplyOverwrite()
        {
            // NOTE: ここでは「ウィンドウ設計の途中」なので、上書き処理は未実装の骨格だけ残します。
            // 実装予定の流れ：
            // 1) PrefabStage/対象ComPlayerBaseから MonoScript を取得
            // 2) csパス取得
            // 3) 3択ダイアログ（バックアップして続行／キャンセル／続行）
            // 4) バックアップ
            // 5) Aなら固定コード生成、Bなら設定に応じてコード生成
            // 6) File.WriteAllText → AssetDatabase.ImportAsset/Refresh

            int ret = EditorUtility.DisplayDialogComplex(
                "AIスクリプトを上書き",
                "現在のAIスクリプトを上書きします。\n（自分で編集した内容も消失します）\n\n続行しますか？",
                "バックアップして続行",
                "キャンセル",
                "続行（バックアップなし）"
            );

            if (ret == 1) return false;

            // 仮：ここで本来は上書き実行
            Debug.Log($"[EasyProgramEditorWindow] Apply requested. mode={m_config.mode}");
            return true;
        }
        */

        // =========================
        // 補助：砲塔数など
        // =========================
        int GetTurretCount(ComPlayerBase target)
        {
            if (target == null) return 0;

            int count = 0;
            target.GetTurrets(turrets =>
            {
                count = (turrets == null) ? 0 : turrets.Length;
            });

            return Mathf.Max(0, count);
        }

        // =========================
        // 補助：リスト整合
        // =========================
        void EnsureConfigInitialized()
        {
            if (m_config == null)
                m_config = new EasyAiConfig();

            if (m_config.aim == null)
                m_config.aim = new();

            if (m_config.aim.perTurretTargetRules == null)
                m_config.aim.perTurretTargetRules = new();

            /*
            if (m_config.fire == null)
                m_config.fire = new FireConfig();

            if (m_config.fire.shared == null)
                m_config.fire.shared = new TurretFireConfig();

            if (m_config.fire.perTurret == null)
                m_config.fire.perTurret = new List<TurretFireConfig>();
            */
        }

        /*
        void EnsurePerTurretListSize(int turretCount)
        {
            if (m_config.fire.perTurret == null)
                m_config.fire.perTurret = new List<TurretFireConfig>();

            while (m_config.fire.perTurret.Count < turretCount)
                m_config.fire.perTurret.Add(new TurretFireConfig());

            while (turretCount < m_config.fire.perTurret.Count)
                m_config.fire.perTurret.RemoveAt(m_config.fire.perTurret.Count - 1);
        }
        */

        void EnsureFoldoutListSize(int turretCount)
        {
            while (m_turretFoldouts.Count < turretCount)
                m_turretFoldouts.Add(false); // 初期は閉じる

            while (turretCount < m_turretFoldouts.Count)
                m_turretFoldouts.RemoveAt(m_turretFoldouts.Count - 1);
        }

        /*
        static void CopyFireConfig(TurretFireConfig src, TurretFireConfig dst)
        {
            dst.targetRule = src.targetRule;
            dst.fireRule = src.fireRule;
            dst.intervalSec = src.intervalSec;
            dst.maxDistance = src.maxDistance;
        }
        */
    }
}
#endif


/*
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SXG2025
{
    public class EasyProgramEditorWindow : EditorWindow
    {
        ComPlayerBase m_target;


        enum EasyModeKind
        {
            GamepadTest = 0,
            BuildByPreset = 1
        }
        enum MovePreset
        {
            Default,
            Rush,
        }

        [Serializable]
        class EasyAiConfig
        {
            public EasyModeKind m_mode;
            public MovePreset m_movePreset;
            public List<TurretFireConfig> m_turretConfigs = new();
        }

        [Serializable]
        class TurretFireConfig
        {
            public TargetRule m_targetRule;
            public FireRule m_fileRule;
        }

        enum TargetRule { Nearest, LowestHp, FrontMost };
        enum FireRule { WhenAimed, Interval, DistanceUnder };


        EasyAiConfig m_config = new();
        bool m_dirty = false;
        EasyModeKind? m_mode = null;
        EasyAiConfig m_lastApplied;


        public static void Open(ComPlayerBase target)
        {
            var w = GetWindow<EasyProgramEditorWindow>(utility: true, title: "かんたんAI作成");
            w.minSize = new Vector2(420, 360);
            w.m_target = target;
            w.Show();
            w.Focus();
        }


        void OnGUI()
        {
            // 対象prefabの情報表示(readonly) 
            DrawTargetInfo();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("■作成方法", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            // A/B選択 
            m_config.m_mode = (EasyModeKind)GUILayout.SelectionGrid(
                (int)m_config.m_mode,
                new []
                {
                    "ゲームパッドで操作検証（テスト用）",
                    "選んでAIを作成（かんたん）"
                },
                1);

            EditorGUILayout.Space(8);

            if (m_config.m_mode == EasyModeKind.GamepadTest)
            {
                EditorGUILayout.HelpBox(
                    "テスト用のゲームパッド操作コードを生成します。\n" +
                    "（内容は固定です）",
                    MessageType.Info);
            }
            else
            {
                //DrawMovePresetUI();
                //DrawTurretFireUI();
            }

            if (EditorGUI.EndChangeCheck())
            {
                m_dirty = true;
            }

            // 変更があれば適用ボタンを出す 
            if (m_dirty)
            {
                EditorGUILayout.HelpBox(
                    "変更があります。適用するとAIスクリプトを上書きします（バックアップ推奨）。",
                    MessageType.Warning);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("適用して上書き保存", GUILayout.Width(200), GUILayout.Height(32)))
                    {
                        if (TryApplyOverwrite())
                        {
                            m_dirty = false;
                        }
                    }
                    GUILayout.FlexibleSpace();
                }
            }
        }



        private void DrawTargetInfo()
        {
            // まずは概要だけ：ターゲット表示
            EditorGUILayout.LabelField("対象戦車（Prefab編集モード）", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("ComPlayerBase", m_target, typeof(ComPlayerBase), allowSceneObjects: true);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "ここで選ぶだけでAIテンプレートを組み立てられます。\n" +
                "※「適用」でAIスクリプトを上書きします（バックアップ推奨）。",
                MessageType.Info);
        }
    }
}
#endif
*/