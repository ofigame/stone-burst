using NUnit.Framework;
using UnityEngine;

/// <summary>
/// PlayMode testleri sırasında ses çıkışı kapalı (kullanıcı 2026-11-18: "testlerde sesleri kulaklığıma vermesen olur mu" →
/// "Evet, sustur"). Genel dinleyici düzeyi 0: klipler yine yüklenir ve çalar (LastClip, IsPlaying denetimleri aynı),
/// yalnız duyulmaz. Ad alanı dışında: derlemedeki bütün testleri kapsar. Bitince eski düzey geri yüklenir.
/// </summary>
[SetUpFixture]
public sealed class MuteAudioDuringTests
{
    private float _saved = 1f;

    [OneTimeSetUp]
    public void Mute()
    {
        _saved = AudioListener.volume;
        AudioListener.volume = 0f;
    }

    [OneTimeTearDown]
    public void Restore() => AudioListener.volume = _saved;
}
