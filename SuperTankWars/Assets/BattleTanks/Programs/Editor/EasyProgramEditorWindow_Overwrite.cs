#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SXG2025
{
    public partial class EasyProgramEditorWindow : EditorWindow
    {
        bool TryApplyOverwrite()
        {
            // -------------------------
            // 0) 確認ダイアログ
            // -------------------------
            int ret = EditorUtility.DisplayDialogComplex(
                "AIスクリプトを上書き",
                "現在のAIスクリプトを上書きします。\n（自分で編集した内容も消失します）\n\n続行しますか？",
                "バックアップして続行",
                "キャンセル",
                "続行（バックアップなし）"
            );
            if (ret == 1) return false;

            bool doBackup = (ret == 0);

            try
            {
                // -------------------------
                // 1) PrefabStageチェック
                // -------------------------
                var stage = PrefabStageUtility.GetCurrentPrefabStage();
                if (stage == null)
                {
                    EditorUtility.DisplayDialog("失敗", "Prefab編集モードで開いてください。", "OK");
                    return false;
                }

                if (m_target == null)
                {
                    EditorUtility.DisplayDialog("失敗", "対象の戦車が見つかりません。Prefabを開き直してください。", "OK");
                    return false;
                }

                // -------------------------
                // 2) 上書き対象の.csパス取得
                // -------------------------
                var mono = MonoScript.FromMonoBehaviour(m_target);
                if (mono == null)
                    throw new Exception("対象スクリプトが取得できませんでした。");

                string scriptAssetPath = AssetDatabase.GetAssetPath(mono);
                if (string.IsNullOrEmpty(scriptAssetPath) || !scriptAssetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    throw new Exception($"対象スクリプトのパスが不正です: {scriptAssetPath}");

                // UnityのassetPath(Assets/...) → 絶対パスに変換（プロジェクトルート基準）
                string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
                string scriptFullPath = Path.GetFullPath(Path.Combine(projectRoot, scriptAssetPath));

                // -------------------------
                // 3) バックアップ（任意）
                // -------------------------
                if (doBackup)
                {
                    MakeBackupToSiblingFolder(scriptFullPath);
                }

                // -------------------------
                // 4) 「PrefabのSerializeField」も同期（m_targetRulesなど）
                //    ※コンパイル前にPrefabへ保存しておくのが重要
                // -------------------------
                ApplyConfigToPrefab_OrThrow(stage);

                // -------------------------
                // 5) 生成（まずは既存のBuild関数を使用）
                // -------------------------
                string namespaceName = m_target.GetType().Namespace ?? "SXG2025";
                string className = m_target.GetType().Name;

                // プログラム出力
                string generated = "";
                if (m_config.mode == EasyModeKind.GamepadTest)
                {
                    generated = BuildCsText_GamepadTest(namespaceName, className);
                }
                else
                {
                    generated = BuildCsText_BuildByPreset(namespaceName, className);
                }

                // -------------------------
                // 6) .cs 上書き（UTF-8 BOMあり）
                // -------------------------
                File.WriteAllText(
                    scriptFullPath,
                    generated,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
                );

                // -------------------------
                // 7) 再インポート
                // -------------------------
                AssetDatabase.ImportAsset(scriptAssetPath);
                // Refreshは必要になってからでOK（重いので）
                // AssetDatabase.Refresh();

                Debug.Log($"[EasyProgram] Generated and overwritten: {scriptAssetPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("失敗", $"上書きに失敗しました。\n\n{ex.Message}", "OK");
                return false;
            }
        }

        // =========================
        // Prefab同期（SerializeFieldを書き換えてPrefabを保存）
        // =========================
        void ApplyConfigToPrefab_OrThrow(PrefabStage stage)
        {
            // 砲塔数（EditorWindow側の仮実装を利用）
            int turretCount = GetTurretCount(m_target);

            // ここでは例として「共通設定」をm_targetRules全要素へ適用
            // 実際は m_config.fire.shared.targetRule などから取る
            EasyAI.TargetRule sharedRule = EasyAI.TargetRule.MinDistance;

            var rules = new EasyAI.TargetRule[Mathf.Max(0, turretCount)];
            for (int i = 0; i < rules.Length; i++)
                rules[i] = sharedRule;

            // m_targetRules を SerializedObject で書き換え
            if (!TryApplyTargetRulesToTarget(m_target, rules))
            {
                // 正しく設定されていない 
            }

            // PrefabStageの内容をPrefabアセットに書き戻す（stage.SavePrefabが無い環境向け）
            if (stage.prefabContentsRoot == null)
                throw new Exception("prefabContentsRoot が取得できません。");

            if (string.IsNullOrEmpty(stage.assetPath))
                throw new Exception("PrefabStageのassetPathが取得できません。");

            EditorUtility.SetDirty(stage.prefabContentsRoot);
            EditorUtility.SetDirty(m_target);

            // Prefabへ保存
            PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath);

            // 念のため
            AssetDatabase.SaveAssets();
        }

        static bool TryApplyTargetRulesToTarget(ComPlayerBase target, EasyAI.TargetRule[] rules)
        {
            if (target == null)
            {
                //throw new Exception("target が null です。");
                return false;
            }

            Undo.RecordObject(target, "Easy AI: Apply Target Rules");

            var so = new SerializedObject(target);
            var prop = so.FindProperty("m_targetRules");

            if (prop == null)
            {
                //throw new Exception("m_targetRules が見つかりません（フィールド名を確認してください）。");
                return false;
            }

            if (!prop.isArray)
                throw new Exception("m_targetRules は配列ではありません。");

            prop.arraySize = rules.Length;

            for (int i = 0; i < rules.Length; i++)
            {
                var elem = prop.GetArrayElementAtIndex(i);
                // enum配列は通常これでOK
                elem.enumValueIndex = (int)rules[i];
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);

            return true;
        }

        // =========================
        // Backup（同階層Backupフォルダ）
        // =========================
        static string MakeBackupToSiblingFolder(string fullCsPath)
        {
            string dir = Path.GetDirectoryName(fullCsPath);
            string backupDir = Path.Combine(dir, "Backup");
            Directory.CreateDirectory(backupDir);

            string file = Path.GetFileName(fullCsPath);
            string backupPath = Path.Combine(backupDir, $"{file}.bak_{DateTime.Now:yyyyMMdd_HHmmss}");

            File.Copy(fullCsPath, backupPath, overwrite: false);
            return backupPath;
        }



        /*
        bool TryApplyOverwrite()
        {
            int ret = EditorUtility.DisplayDialogComplex(
                "AIスクリプトを上書き",
                "現在のAIスクリプトを上書きします。\n（自分で編集した内容も消失します）\n\n続行しますか？",
                "バックアップして続行",
                "キャンセル",
                "続行（バックアップなし）"
            );
            if (ret == 1) return false;

            bool doBackup = (ret == 0);

            try
            {
                var stage = PrefabStageUtility.GetCurrentPrefabStage();
                if (stage == null)
                {
                    EditorUtility.DisplayDialog("失敗", "Prefab編集モードで開いてください。", "OK");
                    return false;
                }


                // 1) 対象スクリプト（.cs）パス取得
                var mono = MonoScript.FromMonoBehaviour(m_target);
                if (mono == null)
                {
                    EditorUtility.DisplayDialog("失敗", "対象スクリプトが取得できませんでした。", "OK");
                    return false;
                }

                string assetPath = AssetDatabase.GetAssetPath(mono);
                if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    EditorUtility.DisplayDialog("失敗", $"対象スクリプトのパスが不正です:\n{assetPath}", "OK");
                    return false;
                }

                string fullPath = Path.GetFullPath(assetPath);

                // 2) バックアップ
                if (doBackup)
                {
                    MakeBackup(fullPath);
                }

                // 3) かんたんAI設定 → TargetRule配列を作る
                int turretCount = GetTurretCount(m_target);
                EasyAI.TargetRule[] rules = new EasyAI.TargetRule[Mathf.Max(0, turretCount)];
                for (int i=0; i < rules.Length; ++i)
                {
                    rules[i] = EasyAI.TargetRule.MinDistance;
                }

                // 4) Prefab へ m_targetRules を反映して保存
                ApplyTargetRulesToPrefab(m_target, rules);
                string prefabAssetPath = stage.assetPath;
                if (string.IsNullOrEmpty(prefabAssetPath))
                {
                    throw new System.Exception("PrefabStageのassetPathが取得できません。");
                }
                PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, prefabAssetPath);
                AssetDatabase.SaveAssets();



                // 3) 生成（いったんRandomMove固定）→
                //    ※後で m_config.movePreset で分岐してブロックを差し替える
                string namespaceName = m_target.GetType().Namespace ?? "SXG2025";
                string className = m_target.GetType().Name;



                // 5) コード生成
                string generated = null;
                if (m_config.mode == EasyModeKind.GamepadTest)
                {
                    generated = BuildCsText_GamepadTest(namespaceName, className);
                }
                else
                {
                    generated = BuildCsText_BuildByPreset(namespaceName, className);
                }

                // 4) 上書き
                File.WriteAllText(fullPath, generated, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

                // 5) 再インポート
                AssetDatabase.ImportAsset(assetPath);
                AssetDatabase.Refresh();

                Debug.Log($"[EasyProgram] Generated and overwritten: {assetPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("失敗", $"上書きに失敗しました。\n\n{ex.Message}", "OK");
                return false;
            }
        }

        static void ApplyTargetRulesToPrefab(ComPlayerBase target, EasyAI.TargetRule[] rules)
        {
            Undo.RecordObject(target, "Easy AI: Apply Target Rules");

            var so = new SerializedObject(target);
            var prop = so.FindProperty("m_targetRules");
            if (prop == null)
            {
                return; // m_targetRulesが見つからない場合は無視 
            }
            if (!prop.isArray)
            {
                throw new Exception("m_targetRules が見つからない、または配列ではありません。");
            }

            prop.arraySize = rules.Length;
            for (int i=0; i < rules.Length; ++i)
            {
                var elem = prop.GetArrayElementAtIndex(i);
                elem.enumValueIndex = (int)rules[i];
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }


        private string MakeBackup(string fullCsPath)
        {
            string dir = Path.GetDirectoryName(fullCsPath);
            string backupDir = Path.Combine(dir, "Backup");
            Directory.CreateDirectory(backupDir);

            string file = Path.GetFileName(fullCsPath);
            //string backupPath = Path.Combine(backupDir, $"{file}.bak_{DateTime.Now:yyyyMMdd_HHmmss}");
            var backupPath = Path.Combine(backupDir, $"{Path.GetFileNameWithoutExtension(file)}_{DateTime.Now:yyyyMMdd_HHmmss}.cs.txt");

            File.Copy(fullCsPath, backupPath, overwrite: false);
            return backupPath;
        }
        */


        // テンプレ・ブロックの置き場所
        const string TEMPLATE_PATH =        "Assets/BattleTanks/Programs/Editor/EasyAI/Templates/PlayerAI_Template.cs.txt";

        const string MOVE_GAMEPAD_PATH = "Assets/BattleTanks/Programs/Editor/EasyAI/Blocks/Move/Move_Gamepad.cs.txt";

        const string MOVE_RANDOM_PATH = "Assets/BattleTanks/Programs/Editor/EasyAI/Blocks/Move/Move_Random.cs.txt";
        const string MOVE_RUSH_PATH = "Assets/BattleTanks/Programs/Editor/EasyAI/Blocks/Move/Move_Rush.cs.txt";
        const string MOVE_KEEP_DISTANCE_PATH = "Assets/BattleTanks/Programs/Editor/EasyAI/Blocks/Move/Move_KeepDistance.cs.txt";
        const string MOVE_ORBITING_ARENA_PATH = "Assets/BattleTanks/Programs/Editor/EasyAI/Blocks/Move/Move_OrbitingArena.cs.txt";

        const string AIM_NORMAL_PATH = "Assets/BattleTanks/Programs/Editor/EasyAI/Blocks/Aim/Aim_Normal.cs.txt";
        const string AIM_NULL_PATH = "Assets/BattleTanks/Programs/Editor/EasyAI/Blocks/Aim/Aim_Null.cs.txt";

        const string FIRE_NORMAL_PATH = "Assets/BattleTanks/Programs/Editor/EasyAI/Blocks/Fire/Fire_Normal.cs.txt";
        const string FIRE_NULL_PATH = "Assets/BattleTanks/Programs/Editor/EasyAI/Blocks/Fire/Fire_Null.cs.txt";

        const string MOVE_HELPERS_PATH =    "Assets/BattleTanks/Programs/Editor/EasyAI/Blocks/Move/Move_Helpers.cs.txt";



        string BuildCsText_GamepadTest(string ns, string className)
        {
            var template = AssetDatabase.LoadAssetAtPath<TextAsset>(TEMPLATE_PATH);
            var moveBlock = AssetDatabase.LoadAssetAtPath<TextAsset>(MOVE_GAMEPAD_PATH);
            var aimBlock = AssetDatabase.LoadAssetAtPath<TextAsset>(AIM_NULL_PATH);
            var fireBlock = AssetDatabase.LoadAssetAtPath<TextAsset>(FIRE_NULL_PATH);

            if (template == null) throw new Exception($"Template not found: {TEMPLATE_PATH}");
            if (moveBlock == null) throw new Exception($"Move block not found: {MOVE_GAMEPAD_PATH}");
            if (aimBlock == null) throw new Exception($"Move block not found: {AIM_NULL_PATH}");
            if (fireBlock == null) throw new Exception($"Move block not found: {FIRE_NULL_PATH}");

            // テンプレにコードを置換 
            string text = template.text;
            text = text.Replace("{{DATE}}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            text = text.Replace("{{NAMESPACE}}", ns);
            text = text.Replace("{{CLASSNAME}}", className);
            text = text.Replace("{{MOVE_CODE}}", moveBlock.text);
            text = text.Replace("{{MOVE_HELPERS}}", "");
            text = text.Replace("{{AIM_CODE}}", aimBlock.text);
            text = text.Replace("{{FIRE_CODE}}", fireBlock.text);

            // テンプレ置換漏れチェック 
            if (text.Contains("{{"))
                throw new Exception("Template placeholders remain. Replace is missing.");

            return text;
        }


        // まずは最短で動く生成（後でテンプレ＋ブロック方式に置き換えてOK）
        string BuildCsText_BuildByPreset(string ns, string className)
        {
            string moveProgramPath = "";
            switch (m_config.movePreset)
            {
                case MovePreset.Random:
                    moveProgramPath = MOVE_RANDOM_PATH;
                    break;
                case MovePreset.Rush:
                    moveProgramPath = MOVE_RUSH_PATH;
                    break;
                case MovePreset.KeepDistance:
                    moveProgramPath = MOVE_KEEP_DISTANCE_PATH;
                    break;
                case MovePreset.OrbitingArena:
                    moveProgramPath = MOVE_ORBITING_ARENA_PATH;
                    break;
                default:
                    throw new Exception($"Move block not created: {m_config.movePreset}.");
            }


            var template = AssetDatabase.LoadAssetAtPath<TextAsset>(TEMPLATE_PATH);
            var moveBlock = AssetDatabase.LoadAssetAtPath<TextAsset>(moveProgramPath);
            var moveHelpers = AssetDatabase.LoadAssetAtPath<TextAsset>(MOVE_HELPERS_PATH);
            var aimBlock = AssetDatabase.LoadAssetAtPath<TextAsset>(AIM_NORMAL_PATH);
            var fireBlock = AssetDatabase.LoadAssetAtPath<TextAsset>(FIRE_NORMAL_PATH);

            if (template == null) throw new Exception($"Template not found: {TEMPLATE_PATH}");
            if (moveBlock == null) throw new Exception($"Move block not found: {moveProgramPath}");
            if (moveHelpers == null) throw new Exception($"Move helper not found: {MOVE_HELPERS_PATH}");
            if (aimBlock == null) throw new Exception($"Aim block not found: {AIM_NORMAL_PATH}");
            if (fireBlock == null) throw new Exception($"Fire block not found: {FIRE_NORMAL_PATH}");

            // テンプレにコードを置換 
            string text = template.text;
            text = text.Replace("{{DATE}}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            text = text.Replace("{{NAMESPACE}}", ns);
            text = text.Replace("{{CLASSNAME}}", className);
            text = text.Replace("{{MOVE_CODE}}", moveBlock.text);
            text = text.Replace("{{MOVE_HELPERS}}", moveHelpers.text);
            text = text.Replace("{{AIM_CODE}}", aimBlock.text);
            text = text.Replace("{{FIRE_CODE}}", fireBlock.text);

            // テンプレ置換漏れチェック 
            if (text.Contains("{{"))
                throw new Exception("Template placeholders remain. Replace is missing.");

            return text;
        }
    }
}
#endif