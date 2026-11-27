using KatKat.Core;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KatKat.EditorTools
{
    /// <summary>2026-11-27: görevde olmayan renkteki grubun yıldız çubuğuna dalgalı, ışık izli uçuşu (244. bölüm) ara kareleri.</summary>
    public static class CaptureFlight
    {
        public static void RunBatch()
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(PeelCapture.OutDir, "ucus"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (var ms in new[] { 380f, 500f, 620f })
            {
                float at = ms;
                GameCapture.ShotPublic($"ucus/l244_{(int)at}ms.png", 1080, 1920, gc =>
                {
                    gc.StartLevel(243);
                    var st = gc.Session.State;
                    for (int i = 0; i < st.Board.Count; i++)
                    {
                        var cell = st.Board[i];
                        if (!cell.IsSelectable || cell.Layers.Count == 0) continue;
                        bool goal = false;
                        foreach (var g in st.Goals) goal |= g.Type == GoalType.CollectColor && g.Color == cell.Top;
                        if (goal) continue;
                        var cmd = Command.Tap(st.Board.X(i), st.Board.Y(i));
                        if (RulesEngine.Validate(st, cmd, out _) != RejectReason.None) continue;
                        gc.OnCellPressed(cmd.X, cmd.Y);
                        for (float t = 0f; t < at; t += 16f) gc.Board.Advance(16f);
                        return;
                    }
                });
            }
        }
    }
}
