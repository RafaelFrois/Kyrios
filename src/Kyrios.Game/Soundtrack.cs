using Microsoft.Xna.Framework.Audio;

namespace Kyrios.Game;

/// <summary>
/// Composições e efeitos sonoros do jogo, todos sintetizados em código via <see cref="Synth"/> — sem
/// nenhum arquivo de áudio externo. Cada modo de corrida tem seu próprio tema, com escala e andamento
/// escolhidos pro clima de cada um: Corrida é animada, Eliminação é tensa, Contrarrelógio é urgente.
/// </summary>
public static class Soundtrack
{
    /// <summary>Corrida Mortal: menor natural, mais rápida e agressiva, com contratempo de caixa — 170 BPM.</summary>
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

    /// <summary>Tema de menu: bem mais calmo e esparso que os temas de corrida (sem bateria pesada), pra
    /// tocar na tela inicial e na seleção de modo sem cansar — 100 BPM, triângulo/seno suaves.</summary>
    public static SoundEffect BuildMenuTheme()
    {
        const float bpm = 100f;
        const float melodyRoot = 440f;
        const float bassRoot = 110f;

        Note[] melody =
        [
            new(0, 1f), new(4, 1f), new(7, 2f),
            new(9, 1f), new(7, 1f), new(4, 2f),
            new(2, 1f), new(4, 1f), new(7, 1f), new(9, 1f),
            new(7, 2f), new(4, 2f),
        ];

        Note[] bass =
        [
            new(0, 2f), new(0, 2f), new(7, 2f), new(7, 2f),
            new(0, 2f), new(0, 2f), new(9, 2f), new(7, 2f),
        ];

        Note[] shaker = MakeRepeatedHits(8, 2f);

        float[] melodyBuf = Synth.RenderVoice(melody, bpm, Waveform.Triangle, 0.2f, melodyRoot, sustain: 0.7f);
        float[] bassBuf = Synth.RenderVoice(bass, bpm, Waveform.Sine, 0.18f, bassRoot, sustain: 0.85f);
        float[] shakerBuf = Synth.RenderVoice(shaker, bpm, Waveform.Noise, 0.025f, 1f, sustain: 0.08f);

        return ToSoundEffect(Synth.Mix(melodyBuf, bassBuf, shakerBuf));
    }

    /// <summary>Arpejo curto e brilhante, ascendente — toca quando o jogador vence (1º lugar, campeão da
    /// Eliminação, ou novo recorde no Contrarrelógio).</summary>
    public static SoundEffect BuildVictoryJingle()
    {
        const float root = 440f;
        (int Semitone, float Duration)[] notes = [(0, 0.11f), (4, 0.11f), (7, 0.11f), (12, 0.11f), (16, 0.11f), (19, 0.4f)];
        return ToSoundEffect(RenderJingle(root, notes, Waveform.Square, 0.32f));
    }

    /// <summary>Notas curtas e descendentes em menor — toca quando o jogador é eliminado ou termina em
    /// último lugar.</summary>
    public static SoundEffect BuildDefeatJingle()
    {
        const float root = 440f;
        (int Semitone, float Duration)[] notes = [(0, 0.18f), (-2, 0.18f), (-4, 0.18f), (-7, 0.5f)];
        return ToSoundEffect(RenderJingle(root, notes, Waveform.Triangle, 0.3f));
    }

    /// <summary>Cadência simples de duas notas — toca num resultado "normal" (nem vitória nem derrota
    /// clara), tipo terminar no meio do pelotão ou o Contrarrelógio acabar sem bater recorde.</summary>
    public static SoundEffect BuildNeutralEndJingle()
    {
        const float root = 440f;
        (int Semitone, float Duration)[] notes = [(7, 0.14f), (4, 0.14f), (0, 0.32f)];
        return ToSoundEffect(RenderJingle(root, notes, Waveform.Square, 0.28f));
    }

    /// <summary>Bip curto de navegação — toca ao trocar o modo selecionado na tela de seleção.</summary>
    public static SoundEffect BuildMenuMoveBlip() =>
        ToSoundEffect(Synth.GenerateTone(660f, 0.05f, Waveform.Square, 0.25f, attack: 0.002f, release: 0.02f));

    /// <summary>Dois bips curtos subindo — toca ao confirmar o modo escolhido.</summary>
    public static SoundEffect BuildMenuConfirmBlip()
    {
        const float root = 660f;
        (int Semitone, float Duration)[] notes = [(0, 0.06f), (7, 0.1f)];
        return ToSoundEffect(RenderJingle(root, notes, Waveform.Square, 0.3f));
    }

    /// <summary>Brilho curto e agudo (triângulo, subindo em oitavas) — toca ao aparecer "CONQUISTA DESBLOQUEADA!".</summary>
    public static SoundEffect BuildAchievementChime()
    {
        const float root = 784f;
        (int Semitone, float Duration)[] notes = [(0, 0.07f), (7, 0.07f), (12, 0.07f), (19, 0.07f), (24, 0.32f)];
        return ToSoundEffect(RenderJingle(root, notes, Waveform.Triangle, 0.34f));
    }

    /// <summary>Mini-fanfarra (quadrada, com "repique" no final) — toca ao aparecer "NOVA SKIN DESBLOQUEADA!".</summary>
    public static SoundEffect BuildSkinFanfare()
    {
        const float root = 523f;
        (int Semitone, float Duration)[] notes = [(0, 0.09f), (4, 0.09f), (7, 0.09f), (12, 0.16f), (7, 0.08f), (12, 0.08f), (16, 0.38f)];
        return ToSoundEffect(RenderJingle(root, notes, Waveform.Square, 0.28f));
    }

    /// <summary>Duas notas descendo, secas — toca quando um carro é eliminado da Corrida Mortal.</summary>
    public static SoundEffect BuildEliminationSting()
    {
        const float root = 392f;
        (int Semitone, float Duration)[] notes = [(0, 0.08f), (-5, 0.16f)];
        return ToSoundEffect(RenderJingle(root, notes, Waveform.Square, 0.3f));
    }

    /// <summary>Zumbido grave e curto — toca ao tentar escolher algo bloqueado.</summary>
    public static SoundEffect BuildDenyBuzz() =>
        ToSoundEffect(Synth.GenerateTone(110f, 0.12f, Waveform.Square, 0.25f, attack: 0.002f, release: 0.03f));

    /// <summary>Bip agudo e seco — toca a cada segundo nos últimos instantes do Contrarrelógio.</summary>
    public static SoundEffect BuildCountdownTick() =>
        ToSoundEffect(Synth.GenerateTone(1200f, 0.05f, Waveform.Square, 0.3f, attack: 0.001f, release: 0.02f));

    /// <summary>Concatena uma sequência curta de notas (com sua própria duração cada) num único buffer —
    /// usado pelos jingles/bips, que não precisam do BPM/loop de <see cref="Synth.RenderVoice"/>.</summary>
    private static float[] RenderJingle(float rootFrequency, (int Semitone, float Duration)[] notes, Waveform waveform, float volume)
    {
        var tones = new float[notes.Length][];
        int total = 0;
        for (int i = 0; i < notes.Length; i++)
        {
            bool isLast = i == notes.Length - 1;
            float frequency = Synth.NoteFrequency(rootFrequency, notes[i].Semitone);
            tones[i] = Synth.GenerateTone(frequency, notes[i].Duration, waveform, volume, attack: 0.003f, release: isLast ? 0.12f : 0.02f);
            total += tones[i].Length;
        }

        var buffer = new float[total];
        int cursor = 0;
        foreach (float[] tone in tones)
        {
            Array.Copy(tone, 0, buffer, cursor, tone.Length);
            cursor += tone.Length;
        }

        return buffer;
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
