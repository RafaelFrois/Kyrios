namespace Kyrios.Game;

/// <summary>Forma de onda usada por <see cref="Synth.GenerateTone"/> — cada uma dá uma textura de
/// sintetizador diferente (quadrada/pulso soam "8-bit", triângulo é mais suave, ruído serve pra percussão).</summary>
public enum Waveform
{
    Sine,
    Square,
    Pulse25,
    Triangle,
    Sawtooth,
    Noise,
}

/// <summary>Uma nota numa sequência: semitom relativo à frequência-base da voz (null = pausa) e duração
/// em batidas (beats) — a unidade de tempo é dada pelo BPM passado a <see cref="Synth.RenderVoice"/>.</summary>
public readonly record struct Note(int? Semitone, float Beats);

/// <summary>
/// Sintetizador simples: gera formas de onda amostra por amostra em código (sem nenhum arquivo de áudio
/// externo), do mesmo jeito que o resto do jogo desenha tudo com formas em vez de sprites prontos. Serve
/// de base tanto pra trilha sonora (<see cref="Soundtrack"/>) quanto pros efeitos sonoros.
/// </summary>
public static class Synth
{
    public const int SampleRate = 22050;

    public static float NoteFrequency(float rootFrequency, int semitoneOffset) =>
        rootFrequency * MathF.Pow(2f, semitoneOffset / 12f);

    /// <summary>Gera um tom puro de uma forma de onda só, com uma rampa curta de entrada/saída (attack/
    /// release) pra não estalar no começo/fim.</summary>
    public static float[] GenerateTone(float frequency, float durationSeconds, Waveform waveform, float volume = 0.5f, float attack = 0.004f, float release = 0.02f)
    {
        int sampleCount = Math.Max(0, (int)(SampleRate * durationSeconds));
        var samples = new float[sampleCount];
        if (sampleCount == 0)
        {
            return samples;
        }

        var random = waveform == Waveform.Noise ? new Random() : null;
        float phase = 0f;
        float phaseStep = frequency / SampleRate;

        int attackSamples = (int)(SampleRate * attack);
        int releaseSamples = (int)(SampleRate * release);

        for (int i = 0; i < sampleCount; i++)
        {
            float raw = waveform switch
            {
                Waveform.Sine => MathF.Sin(phase * 2f * MathF.PI),
                Waveform.Square => phase < 0.5f ? 1f : -1f,
                Waveform.Pulse25 => phase < 0.25f ? 1f : -1f,
                Waveform.Triangle => (4f * MathF.Abs(phase - 0.5f)) - 1f,
                Waveform.Sawtooth => (2f * phase) - 1f,
                Waveform.Noise => (float)((random!.NextDouble() * 2.0) - 1.0),
                _ => 0f,
            };

            float envelope = 1f;
            if (attackSamples > 0 && i < attackSamples)
            {
                envelope = i / (float)attackSamples;
            }
            else if (releaseSamples > 0 && i > sampleCount - releaseSamples)
            {
                envelope = (sampleCount - i) / (float)releaseSamples;
            }

            samples[i] = raw * volume * envelope;

            phase += phaseStep;
            if (phase >= 1f)
            {
                phase -= 1f;
            }
        }

        return samples;
    }

    /// <summary>
    /// Renderiza uma sequência de notas (uma "voz") num único buffer contínuo. O comprimento total é
    /// calculado a partir da soma de batidas de uma vez só (em vez de somar a duração de cada nota
    /// individualmente) pra não acumular erro de arredondamento e desalinhar o loop com as outras vozes.
    /// </summary>
    /// <param name="sustain">Fração do "slot" de cada nota que realmente soa — o resto fica em silêncio.
    /// Perto de 1 pra notas sustentadas (melodia/baixo), bem menor (~0.1-0.25) pra percussão curta.</param>
    public static float[] RenderVoice(IReadOnlyList<Note> notes, float bpm, Waveform waveform, float volume, float rootFrequency, float sustain = 0.85f)
    {
        float secondsPerBeat = 60f / bpm;

        float totalBeats = 0f;
        foreach (Note note in notes)
        {
            totalBeats += note.Beats;
        }

        int totalSamples = (int)MathF.Round(totalBeats * secondsPerBeat * SampleRate);
        var buffer = new float[totalSamples];

        float cursorBeats = 0f;
        foreach (Note note in notes)
        {
            int startSample = (int)MathF.Round(cursorBeats * secondsPerBeat * SampleRate);
            cursorBeats += note.Beats;
            int endSample = Math.Min(totalSamples, (int)MathF.Round(cursorBeats * secondsPerBeat * SampleRate));
            int slotSamples = endSample - startSample;

            if (note.Semitone is int semitone && slotSamples > 0)
            {
                float slotSeconds = slotSamples / (float)SampleRate;
                float toneSeconds = MathF.Max(slotSeconds * sustain, 1f / SampleRate);
                float frequency = NoteFrequency(rootFrequency, semitone);
                float[] tone = GenerateTone(frequency, toneSeconds, waveform, volume);

                int copyLength = Math.Min(tone.Length, buffer.Length - startSample);
                if (copyLength > 0)
                {
                    Array.Copy(tone, 0, buffer, startSample, copyLength);
                }
            }
        }

        return buffer;
    }

    /// <summary>Soma várias vozes num só buffer (preenchendo com zero quem for mais curta) e normaliza
    /// se o pico ultrapassar 1 — evita estourar/cortar o áudio quando várias vozes tocam juntas.</summary>
    public static float[] Mix(params float[][] voices)
    {
        int length = 0;
        foreach (float[] voice in voices)
        {
            length = Math.Max(length, voice.Length);
        }

        var result = new float[length];
        foreach (float[] voice in voices)
        {
            for (int i = 0; i < voice.Length; i++)
            {
                result[i] += voice[i];
            }
        }

        float peak = 0f;
        foreach (float sample in result)
        {
            peak = MathF.Max(peak, MathF.Abs(sample));
        }

        if (peak > 1f)
        {
            float scale = 1f / peak;
            for (int i = 0; i < result.Length; i++)
            {
                result[i] *= scale;
            }
        }

        return result;
    }

    public static byte[] ToPcm16(float[] samples)
    {
        var bytes = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            float clamped = Math.Clamp(samples[i], -1f, 1f);
            var value = (short)(clamped * short.MaxValue);
            bytes[i * 2] = (byte)(value & 0xFF);
            bytes[(i * 2) + 1] = (byte)((value >> 8) & 0xFF);
        }

        return bytes;
    }
}
