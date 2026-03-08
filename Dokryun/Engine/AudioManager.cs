using Microsoft.Xna.Framework.Audio;

namespace Dokryun.Engine;

/// <summary>
/// 프로시저럴 사운드 이펙트 매니저 - wav 파일 없이 코드로 생성
/// </summary>
public static class AudioManager
{
    private static float _masterVolume = 0.35f;
    private static float _sfxVolume = 0.7f;
    private static readonly Dictionary<string, SoundEffect> _cache = new();
    private static bool _initialized;

    // Cooldown to prevent sound spam
    private static readonly Dictionary<string, float> _lastPlayTime = new();
    private static float _globalTime;

    public static float MasterVolume
    {
        get => _masterVolume;
        set => _masterVolume = Math.Clamp(value, 0f, 1f);
    }

    public static float SfxVolume
    {
        get => _sfxVolume;
        set => _sfxVolume = Math.Clamp(value, 0f, 1f);
    }

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        // Pre-generate all sound effects
        _cache["arrow_shoot"] = GenArrowShoot();
        _cache["hit_normal"] = GenHitNormal();
        _cache["hit_crit"] = GenHitCrit();
        _cache["player_hit"] = GenPlayerHit();
        _cache["enemy_die"] = GenEnemyDie();
        _cache["dash"] = GenDash();
        _cache["explosion"] = GenExplosion();
        _cache["boss_roar"] = GenBossRoar();
        _cache["boss_stomp"] = GenBossStomp();
        _cache["boss_phase"] = GenBossPhase();
        _cache["pickup"] = GenPickup();
        _cache["portal"] = GenPortal();
        _cache["combo"] = GenCombo();
        _cache["lightning"] = GenLightning();
        _cache["frost"] = GenFrost();
        _cache["arrow_rain"] = GenArrowRain();
    }

    public static void Update(float dt)
    {
        _globalTime += dt;
    }

    public static void Play(string name, float volumeScale = 1f, float pitchVariation = 0.1f, float cooldown = 0.03f)
    {
        if (!_initialized || !_cache.TryGetValue(name, out var sfx)) return;

        // Cooldown check
        if (_lastPlayTime.TryGetValue(name, out float lastTime) && _globalTime - lastTime < cooldown)
            return;
        _lastPlayTime[name] = _globalTime;

        float vol = Math.Clamp(_masterVolume * _sfxVolume * volumeScale, 0f, 1f);
        float pitch = (float)(Random.Shared.NextDouble() * 2 - 1) * pitchVariation;
        pitch = Math.Clamp(pitch, -1f, 1f);

        try
        {
            sfx.Play(vol, pitch, 0f);
        }
        catch { /* ignore audio failures */ }
    }

    // === Sound Generators ===

    private static SoundEffect GenArrowShoot()
    {
        // Short whoosh - descending noise burst
        int sampleRate = 22050;
        int samples = (int)(sampleRate * 0.08);
        var data = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float env = (1f - t) * (1f - t); // Quick decay
            float freq = 800f - 400f * t; // Descending
            float wave = MathF.Sin(2f * MathF.PI * freq * t) * 0.3f;
            float noise = ((float)Random.Shared.NextDouble() * 2f - 1f) * 0.7f;
            data[i] = (short)((wave + noise) * env * 8000);
        }
        return CreateSfx(data, sampleRate);
    }

    private static SoundEffect GenHitNormal()
    {
        // Punchy thud
        int sampleRate = 22050;
        int samples = (int)(sampleRate * 0.07);
        var data = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float env = MathF.Pow(1f - t, 3f);
            float freq = 220f - 160f * t;
            float wave = MathF.Sin(2f * MathF.PI * freq * i / sampleRate) * 0.6f;
            float noise = ((float)Random.Shared.NextDouble() * 2f - 1f) * 0.4f;
            data[i] = (short)((wave + noise) * env * 10000);
        }
        return CreateSfx(data, sampleRate);
    }

    private static SoundEffect GenHitCrit()
    {
        // Sharp impact + high ring
        int sampleRate = 22050;
        int samples = (int)(sampleRate * 0.12);
        var data = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            // Initial impact
            float env1 = t < 0.15f ? MathF.Pow(1f - t / 0.15f, 2f) : 0f;
            float impact = MathF.Sin(2f * MathF.PI * 180f * i / sampleRate) * 0.5f
                         + ((float)Random.Shared.NextDouble() * 2f - 1f) * 0.5f;
            // High metallic ring
            float env2 = MathF.Pow(1f - t, 2f) * 0.6f;
            float ring = MathF.Sin(2f * MathF.PI * 1200f * i / sampleRate) * 0.4f
                       + MathF.Sin(2f * MathF.PI * 1800f * i / sampleRate) * 0.2f;
            data[i] = (short)((impact * env1 + ring * env2) * 12000);
        }
        return CreateSfx(data, sampleRate);
    }

    private static SoundEffect GenPlayerHit()
    {
        // Low thud + high pain sting
        int sampleRate = 22050;
        int samples = (int)(sampleRate * 0.15);
        var data = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float env = MathF.Pow(1f - t, 2.5f);
            float low = MathF.Sin(2f * MathF.PI * 100f * i / sampleRate) * 0.6f;
            float mid = MathF.Sin(2f * MathF.PI * 350f * i / sampleRate) * 0.3f;
            float noise = ((float)Random.Shared.NextDouble() * 2f - 1f) * 0.3f;
            // Sting at start
            float sting = t < 0.1f ? MathF.Sin(2f * MathF.PI * 900f * i / sampleRate) * (1f - t / 0.1f) * 0.4f : 0f;
            data[i] = (short)((low + mid + noise + sting) * env * 10000);
        }
        return CreateSfx(data, sampleRate);
    }

    private static SoundEffect GenEnemyDie()
    {
        // Descending burst with crunch
        int sampleRate = 22050;
        int samples = (int)(sampleRate * 0.18);
        var data = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float env = MathF.Pow(1f - t, 2f);
            float freq = 400f - 350f * t;
            float wave = MathF.Sin(2f * MathF.PI * freq * i / sampleRate) * 0.4f;
            float noise = ((float)Random.Shared.NextDouble() * 2f - 1f) * 0.6f * (1f - t);
            // Crunch at start
            float crunch = t < 0.2f ? ((float)Random.Shared.NextDouble() * 2f - 1f) * 0.5f : 0f;
            data[i] = (short)((wave + noise + crunch) * env * 9000);
        }
        return CreateSfx(data, sampleRate);
    }

    private static SoundEffect GenDash()
    {
        // Fast whoosh
        int sampleRate = 22050;
        int samples = (int)(sampleRate * 0.1);
        var data = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float env = MathF.Sin(t * MathF.PI); // Bell curve
            float noise = ((float)Random.Shared.NextDouble() * 2f - 1f);
            float freq = 300f + 500f * t;
            float sweep = MathF.Sin(2f * MathF.PI * freq * t) * 0.3f;
            data[i] = (short)((noise * 0.7f + sweep) * env * 6000);
        }
        return CreateSfx(data, sampleRate);
    }

    private static SoundEffect GenExplosion()
    {
        // Big boom
        int sampleRate = 22050;
        int samples = (int)(sampleRate * 0.25);
        var data = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float env = MathF.Pow(1f - t, 1.5f);
            float freq = 80f + 40f * MathF.Sin(t * 20f);
            float low = MathF.Sin(2f * MathF.PI * freq * i / sampleRate) * 0.5f;
            float noise = ((float)Random.Shared.NextDouble() * 2f - 1f) * (1f - t * 0.5f);
            data[i] = (short)((low * 0.6f + noise * 0.4f) * env * 12000);
        }
        return CreateSfx(data, sampleRate);
    }

    private static SoundEffect GenBossRoar()
    {
        // Deep rumble with overtones
        int sampleRate = 22050;
        int samples = (int)(sampleRate * 0.4);
        var data = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float env = t < 0.1f ? t / 0.1f : MathF.Pow(1f - (t - 0.1f) / 0.9f, 1.5f);
            float f1 = MathF.Sin(2f * MathF.PI * 60f * i / sampleRate) * 0.5f;
            float f2 = MathF.Sin(2f * MathF.PI * 120f * i / sampleRate) * 0.3f;
            float f3 = MathF.Sin(2f * MathF.PI * 200f * i / sampleRate) * 0.15f;
            float noise = ((float)Random.Shared.NextDouble() * 2f - 1f) * 0.2f;
            data[i] = (short)((f1 + f2 + f3 + noise) * env * 11000);
        }
        return CreateSfx(data, sampleRate);
    }

    private static SoundEffect GenBossStomp()
    {
        // Heavy impact with ground shake feel
        int sampleRate = 22050;
        int samples = (int)(sampleRate * 0.2);
        var data = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float env = MathF.Pow(1f - t, 3f);
            float freq = 60f + 20f * MathF.Sin(t * 30f);
            float low = MathF.Sin(2f * MathF.PI * freq * i / sampleRate) * 0.7f;
            float click = t < 0.03f ? ((float)Random.Shared.NextDouble() * 2f - 1f) : 0f;
            data[i] = (short)((low + click) * env * 13000);
        }
        return CreateSfx(data, sampleRate);
    }

    private static SoundEffect GenBossPhase()
    {
        // Rising power-up whoosh
        int sampleRate = 22050;
        int samples = (int)(sampleRate * 0.5);
        var data = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float env = t < 0.7f ? t / 0.7f : MathF.Pow(1f - (t - 0.7f) / 0.3f, 2f);
            float freq = 100f + 800f * t * t;
            float wave = MathF.Sin(2f * MathF.PI * freq * i / sampleRate) * 0.4f;
            float wave2 = MathF.Sin(2f * MathF.PI * freq * 1.5f * i / sampleRate) * 0.2f;
            float noise = ((float)Random.Shared.NextDouble() * 2f - 1f) * 0.15f * t;
            data[i] = (short)((wave + wave2 + noise) * env * 10000);
        }
        return CreateSfx(data, sampleRate);
    }

    private static SoundEffect GenPickup()
    {
        // Bright ascending chime
        int sampleRate = 22050;
        int samples = (int)(sampleRate * 0.12);
        var data = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float env = MathF.Pow(1f - t, 1.5f);
            float freq = 600f + 400f * t;
            float wave = MathF.Sin(2f * MathF.PI * freq * i / sampleRate) * 0.5f;
            float wave2 = MathF.Sin(2f * MathF.PI * freq * 2f * i / sampleRate) * 0.2f;
            data[i] = (short)((wave + wave2) * env * 8000);
        }
        return CreateSfx(data, sampleRate);
    }

    private static SoundEffect GenPortal()
    {
        // Mystical shimmer
        int sampleRate = 22050;
        int samples = (int)(sampleRate * 0.3);
        var data = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float env = MathF.Sin(t * MathF.PI) * 0.8f;
            float freq = 400f + 200f * MathF.Sin(t * MathF.PI * 4f);
            float wave = MathF.Sin(2f * MathF.PI * freq * i / sampleRate) * 0.4f;
            float wave2 = MathF.Sin(2f * MathF.PI * (freq * 1.5f) * i / sampleRate) * 0.25f;
            data[i] = (short)((wave + wave2) * env * 7000);
        }
        return CreateSfx(data, sampleRate);
    }

    private static SoundEffect GenCombo()
    {
        // Quick rising ding
        int sampleRate = 22050;
        int samples = (int)(sampleRate * 0.08);
        var data = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float env = MathF.Pow(1f - t, 2f);
            float freq = 800f + 400f * t;
            float wave = MathF.Sin(2f * MathF.PI * freq * i / sampleRate);
            data[i] = (short)(wave * env * 6000);
        }
        return CreateSfx(data, sampleRate);
    }

    private static SoundEffect GenLightning()
    {
        // Electric crackle
        int sampleRate = 22050;
        int samples = (int)(sampleRate * 0.1);
        var data = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float env = MathF.Pow(1f - t, 2f);
            // Intermittent noise bursts
            float burst = MathF.Sin(t * 60f) > 0 ? 1f : 0.2f;
            float noise = ((float)Random.Shared.NextDouble() * 2f - 1f) * burst;
            float tone = MathF.Sin(2f * MathF.PI * 2000f * i / sampleRate) * 0.3f;
            data[i] = (short)((noise * 0.7f + tone) * env * 8000);
        }
        return CreateSfx(data, sampleRate);
    }

    private static SoundEffect GenFrost()
    {
        // Crystalline shimmer
        int sampleRate = 22050;
        int samples = (int)(sampleRate * 0.12);
        var data = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float env = MathF.Pow(1f - t, 1.5f);
            float wave = MathF.Sin(2f * MathF.PI * 1500f * i / sampleRate) * 0.3f
                       + MathF.Sin(2f * MathF.PI * 2200f * i / sampleRate) * 0.2f
                       + MathF.Sin(2f * MathF.PI * 3000f * i / sampleRate) * 0.1f;
            data[i] = (short)(wave * env * 7000);
        }
        return CreateSfx(data, sampleRate);
    }

    private static SoundEffect GenArrowRain()
    {
        // Whistling rain of arrows
        int sampleRate = 22050;
        int samples = (int)(sampleRate * 0.2);
        var data = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float env = t < 0.3f ? t / 0.3f : MathF.Pow(1f - (t - 0.3f) / 0.7f, 2f);
            float noise = ((float)Random.Shared.NextDouble() * 2f - 1f) * 0.5f;
            float whistle = MathF.Sin(2f * MathF.PI * (1200f - 400f * t) * i / sampleRate) * 0.4f;
            data[i] = (short)((noise + whistle) * env * 7000);
        }
        return CreateSfx(data, sampleRate);
    }

    private static SoundEffect CreateSfx(short[] data, int sampleRate)
    {
        var bytes = new byte[data.Length * 2];
        for (int i = 0; i < data.Length; i++)
        {
            bytes[i * 2] = (byte)(data[i] & 0xFF);
            bytes[i * 2 + 1] = (byte)((data[i] >> 8) & 0xFF);
        }
        return new SoundEffect(bytes, sampleRate, AudioChannels.Mono);
    }
}
