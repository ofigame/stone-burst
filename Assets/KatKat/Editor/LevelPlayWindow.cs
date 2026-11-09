using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KatKat.Game;
using KatKat.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>Sahneyi elle değiştirmeden istenen bölümü Game sahnesinde oynatır.</summary>
    public sealed class LevelPlayWindow : EditorWindow
    {
        private Vector2 _scroll;
        private List<LevelData> _levels;

        [MenuItem("Kat Kat/Seviyeler/Bölüm oyna...")]
        public static void Open() => GetWindow<LevelPlayWindow>("Kat Kat Bölümleri");

        private void OnEnable()
        {
            _levels = new List<LevelData>();
            foreach (var f in Directory.GetFiles(LevelValidationMenu.LevelsDir, "level_*.json").OrderBy(f => f, StringComparer.Ordinal))
            {
                try { _levels.Add(LevelParser.Parse(File.ReadAllText(f), Path.GetFileName(f))); }
                catch (FormatException e) { Debug.LogError(e.Message); }
            }
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Hepsini doğrula")) LevelValidationMenu.ValidateFromMenu();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _levels.Count; i++)
            {
                var l = _levels[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{i + 1}. {l.Title}", $"{l.Width}x{l.Height}, {l.MoveLimit} hamle{(l.RollEnabled ? ", rulo" : "")}");
                if (GUILayout.Button("Oyna", GUILayout.Width(60))) Play(i);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private static void Play(int index)
        {
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorPrefs.SetInt(GameController.EditorPlayLevelKey, index);
            EditorSceneManager.OpenScene(ProjectSetup.GameScene);
            EditorApplication.isPlaying = true;
        }
    }
}
