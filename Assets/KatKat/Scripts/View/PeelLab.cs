using System.Collections.Generic;
using KatKat.Core;
using UnityEngine;

namespace KatKat.View
{
    /// <summary>
    /// Faz 1 deneyi: tek büyük yığın. Dokununca üst katman soyulur, parça kendi renginin hedef simgesine uçar.
    /// Boşalınca sıradaki sabit üç katmanlı düzen gelir; dört rengin hepsi üstte görünür.
    /// Kural motoru kullanmaz; yalnızca görsel deney sahnesidir.
    /// </summary>
    public sealed class PeelLab : MonoBehaviour
    {
        // Alttan üste. Her rengin en az bir kez üstte soyulduğu sabit döngü.
        public static readonly LayerColor[][] Presets =
        {
            new[] { LayerColor.Yellow, LayerColor.Blue, LayerColor.Coral },
            new[] { LayerColor.Coral, LayerColor.Yellow, LayerColor.Purple },
            new[] { LayerColor.Purple, LayerColor.Coral, LayerColor.Blue },
            new[] { LayerColor.Blue, LayerColor.Purple, LayerColor.Yellow },
        };

        public const float DesignWidth = 6.4f;
        public const float DesignHeight = 10f;
        public const float StackSize = 2.7f;

        private Camera _cam;
        private PeelDirector _director;
        private StackView _stack;
        private readonly StackView[] _goals = new StackView[4];
        private readonly TextMesh[] _goalText = new TextMesh[4];
        private readonly int[] _counts = new int[4];
        private readonly float[] _pulse = new float[4];
        private TextMesh _hint;
        private int _preset;
        private bool _slow;

        public StackView Stack => _stack;
        public PeelDirector Director => _director;

        public Vector3 GoalPosition(LayerColor c) => _goals[(int)c].transform.position;

        private void Start()
        {
            Build(Camera.main);
        }

        /// <summary>Sahneyi kurar. Editör kare yakalama aracı da bunu doğrudan çağırır.</summary>
        public void Build(Camera cam)
        {
            _cam = cam;
            OrthoFit.Frame(_cam, new Vector2(DesignWidth, DesignHeight));
            float top = _cam.orthographicSize;

            _director = new GameObject("PeelDirector").AddComponent<PeelDirector>();
            _director.transform.SetParent(transform, false);

            _stack = new GameObject("LabStack").AddComponent<StackView>();
            _stack.transform.SetParent(transform, false);
            _stack.transform.localPosition = new Vector3(0f, -0.4f, 0f);
            _stack.Init(StackSize, 10);
            _stack.SetLayers(Presets[0]);

            var font = KatKat.UI.UiTypography.WorldBold;
            for (int i = 0; i < 4; i++)
            {
                var g = new GameObject("Goal_" + (LayerColor)i).AddComponent<StackView>();
                g.transform.SetParent(transform, false);
                g.transform.localPosition = new Vector3(-2.4f + i * 1.6f, top - 1.4f, 0f);
                g.Init(0.62f, 20, false);
                g.SetLayers(new[] { (LayerColor)i });
                _goals[i] = g;
                _goalText[i] = MakeText(font, g.transform, new Vector3(0f, -0.62f, 0f), 0.05f, "0");
            }
            _hint = MakeText(font, transform, new Vector3(0f, -top + 1.6f, 0f), 0.045f, "Dokun: üst katmanı soy");
        }

        private static TextMesh MakeText(Font font, Transform parent, Vector3 pos, float charSize, string text)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = font;
            tm.fontSize = 64;
            tm.characterSize = charSize;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color32(0x2E, 0x6F, 0x73, 0xFF);
            tm.text = text;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = font.material;
            mr.sortingOrder = 50;
            return tm;
        }

        /// <summary>Üst katmanı soyar. Yığın boşsa sıradaki düzeni kurar.</summary>
        public PeelSequence PeelTop()
        {
            if (_stack.Layers.Count == 0)
            {
                _preset = (_preset + 1) % Presets.Length;
                _stack.SetLayers(Presets[_preset]);
                return null;
            }
            var top = _stack.Layers[_stack.Layers.Count - 1];
            return _director.Peel(_stack, GoalPosition(top), OnArrive);
        }

        private void OnArrive(PeelSequence s)
        {
            int i = (int)s.Color;
            _counts[i]++;
            _goalText[i].text = _counts[i].ToString();
            _pulse[i] = 1f;
        }

        private void Update()
        {
            if (_cam == null) return;
            OrthoFit.Frame(_cam, new Vector2(DesignWidth, DesignHeight));

            for (int i = 0; i < 4; i++)
            {
                if (_pulse[i] <= 0f) continue;
                _pulse[i] = Mathf.Max(0f, _pulse[i] - Time.unscaledDeltaTime * 4f);
                float s = 1f + 0.25f * Mathf.Sin(_pulse[i] * Mathf.PI);
                _goals[i].transform.localScale = new Vector3(s, s, 1f);
            }

            if (Input.GetKeyDown(KeyCode.S)) ToggleSlow();
            if (Input.GetMouseButtonDown(0))
            {
                // Alt şerit deney düğmelerine ayrıldı.
                if (Input.mousePosition.y < Screen.height * 0.12f) return;
                // Girdi kapısı: soyulma sürerken yeni dokunuş kabul edilmez, kuyruğa alınmaz.
                if (_director.ActiveCount > 0) return;
                PeelTop();
            }
        }

        private void ToggleSlow()
        {
            _slow = !_slow;
            _director.TimeScale = _slow ? 0.2f : 1f;
        }

        private void OnGUI()
        {
            float h = Screen.height * 0.08f, w = Screen.width * 0.42f;
            var style = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(h * 0.32f) };
            if (GUI.Button(new Rect(Screen.width * 0.05f, Screen.height - h * 1.25f, w, h), _slow ? "Normal hız" : "Yavaş çekim (x0.2)", style)) ToggleSlow();
            if (GUI.Button(new Rect(Screen.width * 0.53f, Screen.height - h * 1.25f, w, h), "Sonraki düzen", style))
            {
                if (_director.ActiveCount == 0)
                {
                    _preset = (_preset + 1) % Presets.Length;
                    _stack.SetLayers(Presets[_preset]);
                }
            }
        }
    }
}
