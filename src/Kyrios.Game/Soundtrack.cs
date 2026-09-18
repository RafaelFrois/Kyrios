using Microsoft.Xna.Framework.Audio;

namespace Kyrios.Game;

/// <summary>
/// Composições e efeitos sonoros do jogo, todos sintetizados em código via <see cref="Synth"/> — sem
/// nenhum arquivo de áudio externo. Cada modo de corrida tem seu próprio tema, com escala e andamento
/// escolhidos pro clima de cada um: Corrida é animada, Eliminação é tensa, Contrarrelógio é urgente.
/// </summary>
public static class Soundtrack
{
    /// <summary>Corrida clássica: pentatônica maior animada e uma batida constante e confiante — 150 BPM.</summary>
    public static SoundEffect BuildSprintTheme()
    {
        const float bpm = 150f;
        const float melodyRoot = 440f; // A4
        const float bassRoot = 110f; // A2
        const float kickRoot = 90f;

        Note[] melody =
        [
            new(0, 0.5f), new(4, 0.5f), new(7, 0.5f), new(9, 0.5f), new(7, 0.5f), new(4, 0.5f), new(2, 0.5f), new(0, 0.5f),
            new(4, 0.5f), new(7, 0.5f), new(12, 0.5f), new(9, 0.5f), new(7, 0.5f), new(4, 0.5f), new(2, 0.5f), new(0, 0.5f),
            new(0, 0.5f), new(4, 0.5f), new(7, 0.5f), new(9, 0.5f), new(7, 0.5f), new(4, 0.5f), new(2, 0.5f), new(0, 0.5f),
            new(4, 0.5f), new(7, 0.5f), new(9, 0.5f), new(12, 0.5f), new(9, 0.5f), new(7, 0.5f), new(4, 0.5f), new(0, 0.5f),
        ];

        Note[] bass =
        [
            new(0, 1f), new(7, 1f), new(0, 1f), new(7, 1f),
            new(0, 1f), new(7, 1f), new(0, 1f), new(9, 1f),
            new(0, 1f), new(7, 1f), new(0, 1f), new(7, 1f),
            new(0, 1f), new(7, 1f), new(0, 1f), new(7, 1f),
        ];

        Note[] kick =
        [
            new(0, 1f), new(null, 1f), new(0, 1f), new(null, 1f),
            new(0, 1f), new(null, 1f), new(0, 1f), new(null, 1f),
            new(0, 1f), new(null, 1f), new(0, 1f), new(null, 1f),
            new(0, 1f), new(null, 1f), new(0, 1f), new(null, 1f),
        ];

        Note[] hihat = MakeRepeatedHits(32, 0.5f);

        float[] melodyBuf = Synth.RenderVoice(melody, bpm, Waveform.Square, 0.18f, melodyRoot, sustain: 0.8f);
        float[] bassBuf = Synth.RenderVoice(bass, bpm, Waveform.Triangle, 0.22f, bassRoot, sustain: 0.9f);
        float[] kickBuf = Synth.RenderVoice(kick, bpm, Waveform.Sine, 0.32f, kickRoot, sustain: 0.2f);
        float[] hihatBuf = Synth.RenderVoice(hihat, bpm, Waveform.Noise, 0.06f, 1f, sustain: 0.12f);

        return ToSoundEffect(Synth.Mix(melodyBuf, bassBuf, kickBuf, hihatBuf));
    }

    /// <summary>Eliminação: menor natural, mais rápida e agressiva, com contratempo de caixa — 170 BPM.</summary>
    public static SoundEffect BuildEliminationTheme()
    {
        const float bpm = 170f;
        const float melodyRoot = 440f;
        const float bassRoot = 110f;
        const float kickRoot = 85f;

        Note[] melody =
        [
            new(0, 0.25f), new(0, 0.25f), new(3, 0.25f), new(0, 0.25f),
            new(5, 0.25f), new(3, 0.25f), new(0, 0.25f), new(7, 0.25f),
            new(0, 0.25f), new(0, 0.25f), new(3, 0.25f), new(0, 0.25f),
            new(5, 0.25f), new(7, 0.25f), new(8, 0.25f), new(7, 0.25f),
            new(0, 0.25f), new(0, 0.25f), new(3, 0.25f), new(0, 0.25f),
            new(5, 0.25f), new(3, 0.25f), new(0, 0.25f), new(7, 0.25f),
            new(8, 0.25f), new(7, 0.25f), new(5, 0.25f), new(3, 0.25f),
            new(0, 0.25f), new(-2, 0.25f), new(0, 0.25f), new(3, 0.25f),
        ];

        Note[] bass =
        [
            new(0, 0.5f), new(7, 0.5f), new(0, 0.5f), new(7, 0.5f), new(0, 0.5f), new(5, 0.5f), new(0, 0.5f), new(7, 0.5f),
            new(0, 0.5f), new(7, 0.5f), new(0, 0.5f), new(7, 0.5f), new(0, 0.5f), new(5, 0.5f), new(0, 0.5f), new(3, 0.5f),
        ];

        Note[] kick = MakeRepeatedHits(8, 1f);
        Note[] snare =
        [
            new(null, 1f), new(0, 1f), new(null, 1f), new(0, 1f),
            new(null, 1f), new(0, 1f), new(null, 1f), new(0, 1f),
        ];
        Note[] hihat = MakeRepeatedHits(16, 0.5f);

        float[] melodyBuf = Synth.RenderVoice(melody, bpm, Waveform.Sawtooth, 0.16f, melodyRoot, sustain: 0.75f);
        float[] bassBuf = Synth.RenderVoice(bass, bpm, Waveform.Triangle, 0.22f, bassRoot, sustain: 0.85f);
        float[] kickBuf = Synth.RenderVoice(kick, bpm, Waveform.Sine, 0.34f, kickRoot, sustain: 0.18f);
        float[] snareBuf = Synth.RenderVoice(snare, bpm, Waveform.Noise, 0.18f, 1f, sustain: 0.2f);
        float[] hihatBuf = Synth.RenderVoice(hihat, bpm, Waveform.Noise, 0.05f, 1f, sustain: 0.1f);

        return ToSoundEffect(Synth.Mix(melodyBuf, bassBuf, kickBuf, snareBuf, hihatBuf));
    }

    /// <summary>Contrarrelógio: um motivo em "tique-taque" seguido de uma corrida ascendente/descendente e
    /// baixo em contratempo, pra dar aquela urgência de relógio correndo contra você — 160 BPM.</summary>
    public static SoundEffect BuildTimeAttackTheme()
    {
        const float bpm = 160f;
        const float melodyRoot = 440f;
        const float bassRoot = 110f;
        const float kickRoot = 90f;

        Note[] melody =
        [
            new(0, 0.5f), new(0, 0.5f), new(0, 0.5f), new(0, 0.5f),
            new(3, 0.5f), new(5, 0.5f), new(7, 0.5f), new(8, 0.5f),
            new(7, 0.5f), new(5, 0.5f), new(3, 0.5f), new(0, 0.5f),
            new(0, 0.5f), new(7, 0.5f), new(0, 0.5f), new(8, 0.5f),
        ];

        Note[] bass =
        [
            new(null, 0.5f), new(0, 0.5f), new(null, 0.5f), new(7, 0.5f),
            new(null, 0.5f), new(0, 0.5f), new(null, 0.5f), new(5, 0.5f),
            new(null, 0.5f), new(0, 0.5f), new(null, 0.5f), new(7, 0.5f),
            new(null, 0.5f), new(0, 0.5f), new(null, 0.5f), new(5, 0.5f),
        ];

        Note[] kick = MakeRepeatedHits(8, 1f);
        Note[] hihat = MakeRepeatedHits(16, 0.5f);

        float[] melodyBuf = Synth.RenderVoice(melody, bpm, Waveform.Square, 0.17f, melodyRoot, sustain: 0.55f);
        float[] bassBuf = Synth.RenderVoice(bass, bpm, Waveform.Triangle, 0.2f, bassRoot, sustain: 0.8f);
        float[] kickBuf = Synth.RenderVoice(kick, bpm, Waveform.Sine, 0.32f, kickRoot, sustain: 0.2f);
        float[] hihatBuf = Synth.RenderVoice(hihat, bpm, Waveform.Noise, 0.06f, 1f, sustain: 0.12f);

        return ToSoundEffect(Synth.Mix(melodyBuf, bassBuf, kickBuf, hihatBuf));
    }

    /// <summary>Arpejo curto e brilhante de três notas (tríade maior) — toca ao cruzar um checkpoint.</summary>
    public static SoundEffect BuildCheckpointChime()
    {
        const float root = 880f;
        float[] n1 = Synth.GenerateTone(Synth.NoteFrequency(root, 0), 0.08f, Waveform.Square, 0.35f, attack: 0.002f, release: 0.02f);
        float[] n2 = Synth.GenerateTone(Synth.NoteFrequency(root, 4), 0.08f, Waveform.Square, 0.35f, attack: 0.002f, release: 0.02f);
        float[] n3 = Synth.GenerateTone(Synth.NoteFrequency(root, 7), 0.14f, Waveform.Square, 0.35f, attack: 0.002f, release: 0.04f);

        var buffer = new float[n1.Length + n2.Length + n3.Length];
        Array.Copy(n1, 0, buffer, 0, n1.Length);
        Array.Copy(n2, 0, buffer, n1.Length, n2.Length);
        Array.Copy(n3, 0, buffer, n1.Length + n2.Length, n3.Length);

        return ToSoundEffect(buffer);
    }

    /// <summary>Baque grave (colisão) + estalo de ruído (o "crunch" da lataria) sobrepostos — toca em
    /// qualquer batida (parede, carro ou obstáculo).</summary>
    public static SoundEffect BuildCollisionThud()
    {
        float[] thump = Synth.GenerateTone(85f, 0.14f, Waveform.Triangle, 0.55f, attack: 0.002f, release: 0.1f);
        float[] crunch = Synth.GenerateTone(1f, 0.09f, Waveform.Noise, 0.35f, attack: 0.001f, release: 0.06f);

        return ToSoundEffect(Synth.Mix(thump, crunch));
    }

    /// <summary>
    /// Ronco de motor em loop: dois dentes-de-serra levemente desafinados um do outro (90Hz e 93Hz) pra
    /// dar textura de "grave". A duração é o período comum das duas frequências (1/3 de segundo — ambas
    /// cabem em um número inteiro de ciclos nesse intervalo), então o loop não estala ao repetir.
    /// </summary>
    public static SoundEffect BuildEngineLoop()
    {
        const float freqA = 90f;
        const float freqB = 93f;
        int samples = Synth.SampleRate / 3;

        var buffer = new float[samples];
        float phaseA = 0f;
        float phaseB = 0f;
        float stepA = freqA / Synth.SampleRate;
        float stepB = freqB / Synth.SampleRate;

        for (int i = 0; i < samples; i++)
        {
            float waveA = (2f * phaseA) - 1f;
            float waveB = (2f * phaseB) - 1f;
            buffer[i] = (waveA * 0.5f) + (waveB * 0.35f);

            phaseA += stepA;
            if (phaseA >= 1f)
            {
                phaseA -= 1f;
            }

            phaseB += stepB;
            if (phaseB >= 1f)
            {
                phaseB -= 1f;
            }
        }

        return ToSoundEffect(buffer);
    }

    private static Note[] MakeRepeatedHits(int count, float beatsEach)
    {
        var notes = new Note[count];
        for (int i = 0; i < count; i++)
        {
            notes[i] = new Note(0, beatsEach);
        }

        return notes;
    }

    private static SoundEffect ToSoundEffect(float[] samples) =>
        new(Synth.ToPcm16(samples), Synth.SampleRate, AudioChannels.Mono);
}
