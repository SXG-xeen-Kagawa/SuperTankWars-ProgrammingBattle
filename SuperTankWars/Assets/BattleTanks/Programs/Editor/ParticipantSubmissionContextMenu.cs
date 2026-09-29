#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace SXG2025
{
    public static class ParticipantSubmissionContextMenu
    {
        const string ParticipantRoot = "Assets/Participant";
        const string BackupFolderName = "Backup";
        const string GameAssetsRoot = "Assets/GameAssets/";
        const string BattleTanksRoot = "Assets/BattleTanks/";
        const string PackagesRoot = "Packages/";
        const string FreeAssetsRoot = "Assets/FreeAssets/";

        // Player1234567 / PlayerOtr1234567
        static readonly Regex PlayerFolderRegex = new Regex(@"^(Player|PlayerOtr)\d{7}(\b|[\s　＿_－\-・].*)?$", RegexOptions.Compiled);




        [MenuItem("Assets/プロバト/提出用ZIPを作成", false, 1)]
        static void CreateZipFromSelectedFolder()
        {
            string assetPath = GetSelectedFolderAssetPath();
            if (string.IsNullOrEmpty(assetPath))
                return;

            //string folderName = Path.GetFileName(assetPath);

            // 出力先：デスクトップ
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            //string zipPath = Path.Combine(desktop, folderName + ".zip");
            string folderName = Path.GetFileName(assetPath);
            string zipBaseName = ExtractPlayerIdPrefix(folderName);
            string zipPath = Path.Combine(desktop, zipBaseName + ".zip");

            string assetCheckError = CheckTankPrefabAssets(assetPath);
            if (!string.IsNullOrEmpty(assetCheckError))
            {
                EditorUtility.DisplayDialog(
                    "挑戦者出力",
                    "提出用ZIPは作成していません。\n\n" + assetCheckError,
                    "OK");
                return;
            }


            if (File.Exists(zipPath))
                File.Delete(zipPath);

            try
            {
                // Backup除外でZIP作成
                CreateZipExcludingBackup(assetPath, zipPath);

                // 完了ダイアログ
                EditorUtility.DisplayDialog(
                    "挑戦者出力",
                    $"提出用ZIPを作成しました。\n\nフォルダ：{assetPath}\n出力先：{zipPath}",
                    "OK"
                );

                // 出力先をOSで開く（体験が良い）
                EditorUtility.RevealInFinder(zipPath);
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

        static string ExtractPlayerIdPrefix(string folderName)
        {
            // 先頭が Player1234567 / PlayerOtr1234567 なら、その部分だけ返す
            var m = Regex.Match(folderName, @"^(Player|PlayerOtr)\d{7}");
            return m.Success ? m.Value : folderName;
        }

        static string CheckTankPrefabAssets(string participantFolderAssetPath)
        {
            var errors = new List<string>();
            var checkedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { participantFolderAssetPath });

            foreach (string prefabGuid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid).Replace("\\", "/");
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null || prefab.GetComponentsInChildren<ComPlayerBase>(true).Length == 0)
                    continue;

                foreach (string dependency in AssetDatabase.GetDependencies(prefabPath, true))
                {
                    string dependencyPath = dependency.Replace("\\", "/");
                    if (IsAllowedAssetPath(dependencyPath, participantFolderAssetPath)
                        || !IsCheckedAssetType(dependencyPath)
                        || !checkedPaths.Add(dependencyPath))
                        continue;

                    errors.Add($"プレハブ：{prefabPath}\n外部アセット：{dependencyPath}");
                }

                CheckAssignedImages(prefab, prefabPath, participantFolderAssetPath, errors, checkedPaths);
            }

            if (errors.Count == 0)
                return null;

            return "[ERROR] 次のアセットが提出フォルダ外にあります。\n\n"
                + string.Join("\n\n", errors);
        }

        static void CheckAssignedImages(GameObject prefab, string prefabPath,
            string participantFolderAssetPath, List<string> errors, HashSet<string> checkedPaths)
        {
            foreach (ComPlayerBase comPlayer in prefab.GetComponentsInChildren<ComPlayerBase>(true))
            {
                var serializedObject = new SerializedObject(comPlayer);
                CheckAssignedImage(serializedObject, "m_faceImage", "Face Image",
                    prefabPath, participantFolderAssetPath, errors, checkedPaths);
                CheckAssignedImage(serializedObject, "m_promoCardImage", "Promo Card Image",
                    prefabPath, participantFolderAssetPath, errors, checkedPaths);
            }
        }

        static void CheckAssignedImage(SerializedObject serializedObject, string propertyName, string label,
            string prefabPath, string participantFolderAssetPath, List<string> errors, HashSet<string> checkedPaths)
        {
            SerializedProperty image = serializedObject.FindProperty(propertyName);
            if (image == null || image.objectReferenceValue == null)
                return;

            string imagePath = AssetDatabase.GetAssetPath(image.objectReferenceValue).Replace("\\", "/");
            if (IsAllowedAssetPath(imagePath, participantFolderAssetPath)
                || !checkedPaths.Add(imagePath))
                return;

            errors.Add($"プレハブ：{prefabPath}\n{label}：{imagePath}");
        }

        static bool IsAllowedAssetPath(string assetPath, string participantFolderAssetPath)
        {
            return assetPath.StartsWith(GameAssetsRoot, StringComparison.OrdinalIgnoreCase)
                || assetPath.StartsWith(BattleTanksRoot, StringComparison.OrdinalIgnoreCase)
                || assetPath.StartsWith(PackagesRoot, StringComparison.OrdinalIgnoreCase)
                || assetPath.StartsWith(FreeAssetsRoot, StringComparison.OrdinalIgnoreCase)
                || assetPath.StartsWith(participantFolderAssetPath + "/", StringComparison.OrdinalIgnoreCase);
        }

        static bool IsCheckedAssetType(string assetPath)
        {
            Type assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            return assetType != null
                && (typeof(Material).IsAssignableFrom(assetType)
                    || typeof(Texture).IsAssignableFrom(assetType)
                    || typeof(Sprite).IsAssignableFrom(assetType));
        }



        [MenuItem("Assets/プロバト/提出用ZIPを作成", true, 1)]
        static bool ValidateCreateZipFromSelectedFolder()
        {
            string assetPath = GetSelectedFolderAssetPath();
            if (string.IsNullOrEmpty(assetPath))
                return false;

            // Assets/Participant 配下のみ許可
            if (!assetPath.StartsWith(ParticipantRoot + "/", StringComparison.Ordinal))
                return false;

            // フォルダ名が Player{7桁} / PlayerOtr{7桁} のみ許可
            string folderName = Path.GetFileName(assetPath);
            return PlayerFolderRegex.IsMatch(folderName);
        }

        static string GetSelectedFolderAssetPath()
        {
            var obj = Selection.activeObject;
            if (obj == null)
                return null;

            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
                return null;

            if (!AssetDatabase.IsValidFolder(path))
                return null;

            return path.Replace("\\", "/");
        }

        static void CreateZipExcludingBackup(string sourceFolderAssetPath, string zipFullPath)
        {
            // AssetPath -> 絶対パス
            string projectRoot = Directory.GetParent(Application.dataPath).FullName.Replace("\\", "/");
            string sourceFullPath = Path.Combine(projectRoot, sourceFolderAssetPath).Replace("\\", "/");

            using (var zip = ZipFile.Open(zipFullPath, ZipArchiveMode.Create))
            {
                var files = Directory.GetFiles(sourceFullPath, "*", SearchOption.AllDirectories);

                int total = files.Length;
                for (int i = 0; i < total; i++)
                {
                    string fileFullPath = files[i].Replace("\\", "/");

                    // Backupフォルダ配下は除外
                    // 例：.../Assets/Participant/Player1234567/Backup/xxxx
                    if (fileFullPath.Contains("/" + BackupFolderName + "/"))
                        continue;

                    // Backupフォルダ自身のmetaも除外（Backup.meta）
                    if (fileFullPath.EndsWith("/" + BackupFolderName + ".meta", StringComparison.Ordinal))
                        continue;

                    // エントリ名（ZIP内の相対パス）を sourceFullPath 基準で作る
                    string rel = fileFullPath.Substring(sourceFullPath.Length).TrimStart('/', '\\');

                    // 進捗（任意）
                    if (0 == (i % 50))
                    {
                        float p = (total > 0) ? (i / (float)total) : 0f;
                        EditorUtility.DisplayProgressBar("挑戦者出力", "ZIP作成中...", p);
                    }

                    zip.CreateEntryFromFile(fileFullPath, rel, System.IO.Compression.CompressionLevel.Optimal);
                }
            }
        }


        public static bool TryCreateZipFromParticipantFolder(string participantFolderAssetPath, out string zipFullPath, bool reveal = true)
        {
            return TryCreateZipFromParticipantFolder(participantFolderAssetPath, out zipFullPath, out _, reveal);
        }

        public static bool TryCreateZipFromParticipantFolder(string participantFolderAssetPath,
            out string zipFullPath, out string assetCheckError, bool reveal = true)
        {
            zipFullPath = null;
            assetCheckError = null;

            if (string.IsNullOrEmpty(participantFolderAssetPath))
                return false;

            participantFolderAssetPath = participantFolderAssetPath.Replace("\\", "/");

            if (!AssetDatabase.IsValidFolder(participantFolderAssetPath))
                return false;

            string folderName = Path.GetFileName(participantFolderAssetPath);
            if (!PlayerFolderRegex.IsMatch(folderName))
                return false;

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string zipBaseName = ExtractPlayerIdPrefix(folderName);
            zipFullPath = Path.Combine(desktop, zipBaseName + ".zip");

            assetCheckError = CheckTankPrefabAssets(participantFolderAssetPath);
            if (!string.IsNullOrEmpty(assetCheckError))
            {
                zipFullPath = null;
                return false;
            }

            if (File.Exists(zipFullPath))
                File.Delete(zipFullPath);

            CreateZipExcludingBackup(participantFolderAssetPath, zipFullPath);

            if (reveal)
                EditorUtility.RevealInFinder(zipFullPath);

            return true;
        }
    }
}
#endif
