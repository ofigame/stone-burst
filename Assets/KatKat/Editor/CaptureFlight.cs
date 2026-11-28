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
            foreach (bool wantGoal in new[] { false, true })
            foreach (var ms in new[] { 120f, 260f, 400f, 540f, 680f, 820f, 960f, 1100f })
            {
                float at = ms; bool wg = wantGoal;
                GameCapture.ShotPublic($"ucus/l244_{(wg ? "gorev" : "yildiz")}_{(int)at:0000}ms.png", 1080, 1920, gc =>
                {
                    gc.StartLevel(243);
                    Canvas.ForceUpdateCanvases(); // hedef simgeleri yerleşmiş olsun (yoksa uçuş eski düzene iner)
                    var st = gc.Session.State;
                    for (int i = 0; i < st.Board.Count; i++)
                    {
                        var cell = st.Board[i];
                        if (!cell.IsSelectable || cell.Layers.Count == 0) continue;
                        bool goal = false;
                        foreach (var g in st.Goals) goal |= g.Type == GoalType.CollectColor && g.Color == cell.Top;
                        if (goal != wg) continue;
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
