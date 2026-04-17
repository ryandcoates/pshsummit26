// DISCLAIMER: This class was entirely vibe-coded and not closely reviewed.
// Its puprose is to show progress when a collection is enumerated during the demo.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

public static class ProgressEnumerable
{
    public static int BarWidth { get; set; } = 25;

    /// <summary>
    /// Directory containing the speed sound WAV files
    /// (train.wav, car.wav, rocket.wav, bullet.wav).
    /// Must be set before iterating when using enableSound.
    /// </summary>
    public static string SoundDirectory
    {
        get => SpeedSound.SoundDirectory;
        set => SpeedSound.SoundDirectory = value;
    }

    /// <summary>
    /// Clears cached sound data so WAV files are reloaded
    /// from disk on the next iteration. Call this after
    /// replacing sound files.
    /// </summary>
    public static void ReloadSounds()
    {
        SpeedSound.ClearCache();
    }

    public static ProgressEnumerable<object> Wrap(
        IEnumerable source,
        string label = null,
        int updateIntervalMs = 100,
        bool enableSound = false)
    {
        long total = DetectCount(source);
        return new ProgressEnumerable<object>(
            Enumerable.Cast<object>(source), total, label,
            updateIntervalMs, enableSound);
    }

    public static ProgressEnumerable<object> Wrap(
        IEnumerable source,
        long total,
        string label = null,
        int updateIntervalMs = 100,
        bool enableSound = false)
    {
        return new ProgressEnumerable<object>(
            Enumerable.Cast<object>(source), total, label,
            updateIntervalMs, enableSound);
    }

    public static ProgressEnumerable<T> Wrap<T>(
        IEnumerable<T> source,
        string label = null,
        int updateIntervalMs = 100,
        bool enableSound = false)
    {
        long total = DetectCount(source);
        return new ProgressEnumerable<T>(
            source, total, label, updateIntervalMs,
            enableSound);
    }

    public static ProgressEnumerable<T> Wrap<T>(
        IEnumerable<T> source,
        long total,
        string label = null,
        int updateIntervalMs = 100,
        bool enableSound = false)
    {
        return new ProgressEnumerable<T>(
            source, total, label, updateIntervalMs,
            enableSound);
    }

    private static long DetectCount(IEnumerable source)
    {
        if (source is ICollection col)
            return col.Count;

        // Reflection fallback for IReadOnlyCollection<T>
        foreach (var iface in source.GetType().GetInterfaces())
        {
            if (iface.IsGenericType
                && iface.GetGenericTypeDefinition()
                    == typeof(IReadOnlyCollection<>))
            {
                var prop = iface.GetProperty("Count");
                if (prop != null)
                    return (int)prop.GetValue(source);
            }
        }

        return -1;
    }
}

public class ProgressEnumerable<T> : IEnumerable<T>
{
    private readonly IEnumerable<T> _source;
    private readonly long _total;
    private readonly string _label;
    private readonly int _updateIntervalMs;
    private readonly bool _enableSound;

    public ProgressEnumerable(
        IEnumerable<T> source, long total, string label,
        int updateIntervalMs, bool enableSound)
    {
        _source = source;
        _total = total;
        _label = label;
        _updateIntervalMs = updateIntervalMs;
        _enableSound = enableSound;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return new ProgressEnumerator<T>(
            _source.GetEnumerator(), _total, _label,
            _updateIntervalMs, _enableSound);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class ProgressEnumerator<T> : IEnumerator<T>
{
    private readonly IEnumerator<T> _inner;
    private readonly long _total;
    private readonly string _label;
    private readonly int _updateIntervalMs;
    private readonly bool _enableSound;
    private readonly Stopwatch _elapsed;
    private readonly Stopwatch _sinceLast;
    private long _count;
    private bool _finalized;
    private int _lastLineLength;

    private static readonly char[] Spinner =
        { '|', '/', '-', '\\' };

    private const string Rst = "\x1b[0m";
    private const string Bold = "\x1b[1m";
    private const string Dim = "\x1b[2m";
    private const string Cyan = "\x1b[36m";
    private const string Green = "\x1b[32m";
    private const string Yellow = "\x1b[33m";
    private const string DarkGray = "\x1b[90m";

    public ProgressEnumerator(
        IEnumerator<T> inner, long total, string label,
        int updateIntervalMs, bool enableSound)
    {
        _inner = inner;
        _total = total;
        _label = label;
        _updateIntervalMs = updateIntervalMs;
        _enableSound = enableSound;
        _elapsed = Stopwatch.StartNew();
        _sinceLast = Stopwatch.StartNew();
    }

    public T Current => _inner.Current;
    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        bool hasNext = _inner.MoveNext();
        if (hasNext)
        {
            _count++;
            if (_count == 2
                || _sinceLast.ElapsedMilliseconds
                    >= _updateIntervalMs)
            {
                Render();
                _sinceLast.Restart();
            }
        }
        else
        {
            RenderFinal();
        }
        return hasNext;
    }

    public void Reset() => _inner.Reset();

    public void Dispose()
    {
        RenderFinal();
        _inner.Dispose();
    }

    private static int VisibleLength(string s)
    {
        int len = 0;
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '\x1b' && i + 1 < s.Length
                && s[i + 1] == '[')
            {
                i += 2;
                while (i < s.Length && s[i] != 'm')
                    i++;
            }
            else
            {
                len++;
            }
        }
        return len;
    }

    private void WriteProgress(string line)
    {
        int visible = VisibleLength(line);
        Console.Error.Write("\r" + line);
        if (visible < _lastLineLength)
            Console.Error.Write(
                new string(' ',
                    _lastLineLength - visible));
        _lastLineLength = visible;
    }

    private void Render()
    {
        double secs = _elapsed.Elapsed.TotalSeconds;
        string rate = FormatRate(_count, secs);
        string time = FormatTime(_elapsed.Elapsed);
        string prefix = string.IsNullOrEmpty(_label)
            ? "" : _label;

        if (_enableSound && secs > 0.001)
        {
            double itemsPerSec = _count / secs;
            SpeedSound.Play(
                SpeedSound.GetTier(itemsPerSec));
        }

        if (_total > 0)
        {
            double frac = (double)_count / _total;
            int width = ProgressEnumerable.BarWidth;
            int filled = (int)(frac * width);
            string filledBar = new string('█', filled);
            string unfilledBar =
                new string(' ', width - filled);
            double eta = frac > 0
                ? secs / frac - secs : 0;
            string etaStr = FormatTime(
                TimeSpan.FromSeconds(eta));
            WriteProgress(
                $"{Cyan}{prefix}{Rst}" +
                $"|{Green}{filledBar}{Rst}" +
                $"{DarkGray}{unfilledBar}{Rst}| " +
                $"{Bold}{(int)(frac * 100),3}%{Rst}  " +
                $"{Yellow}{FormatCount(_count)}" +
                $"/{FormatCount(_total)}{Rst} " +
                $"{Dim}[{time}<{etaStr}, {rate}]{Rst}");
        }
        else
        {
            char spin = Spinner[_count % Spinner.Length];
            WriteProgress(
                $"{Cyan}{prefix}{Rst}" +
                $"{Cyan}{spin}{Rst} " +
                $"{Yellow}{FormatCount(_count)}{Rst} " +
                $"{Dim}[{time}, {rate}]{Rst}");
        }
    }

    private void RenderFinal()
    {
        if (_finalized) return;
        _finalized = true;
        _elapsed.Stop();
        if (_count <= 1)
        {
            if (_enableSound) SpeedSound.Stop();
            return;
        }

        double secs = _elapsed.Elapsed.TotalSeconds;
        string rate = FormatRate(_count, secs);
        string time = FormatTime(_elapsed.Elapsed);
        string prefix = string.IsNullOrEmpty(_label)
            ? "" : _label;

        if (_total > 0)
        {
            long display = _count < _total ? _count : _total;
            double frac = (double)_count / _total;
            int width = ProgressEnumerable.BarWidth;
            int filled = Math.Min((int)(frac * width), width);
            string bar = new string('█', filled)
                + new string(' ', width - filled);
            int pct = Math.Min((int)(frac * 100), 100);
            WriteProgress(
                $"{Green}{Bold}{prefix}{Rst}" +
                $"|{Green}{bar}{Rst}| " +
                $"{Green}{Bold}{pct,3}%{Rst}  " +
                $"{Yellow}{FormatCount(display)}" +
                $"/{FormatCount(_total)}{Rst} " +
                $"{Dim}[{time}, {rate}]{Rst}");
        }
        else
        {
            WriteProgress(
                $"{Green}{Bold}{prefix}{Rst}" +
                $"{Yellow}{FormatCount(_count)}{Rst} " +
                $"{Dim}[{time}, {rate}]{Rst}");
                }

        if (_enableSound)
        {
            if (SpeedSound.IsPlaying)
            {
                // Let the looping sound finish its
                // current iteration before stopping.
                SpeedSound.StopAfterCurrentLoop();
            }
            else
            {
                // Very fast run – sound never started.
                // Play once (no loop) and wait.
                double ips = _count
                    / Math.Max(secs, 0.001);
                SpeedSound.PlayOnceAndWait(
                    SpeedSound.GetTier(ips));
            }
        }

        Console.Error.WriteLine();
    }

    private static string FormatTime(TimeSpan ts)
    {
        return string.Format("{0:D2}:{1:D2}.{2:D3}",
            (int)ts.TotalMinutes, ts.Seconds,
            ts.Milliseconds);
    }

    private static string FormatRate(long count, double secs)
    {
        if (secs < 0.001) return "? it/s";
        double r = count / secs;
        if (r >= 1_000_000)
            return string.Format("{0:F1}M it/s", r / 1_000_000);
        if (r >= 1_000)
            return string.Format("{0:F1}K it/s", r / 1_000);
        return string.Format("{0:F1} it/s", r);
    }

    private static string FormatCount(long n)
    {
        if (n >= 1_000_000)
            return string.Format("{0:F1}M", (double)n / 1_000_000);
        if (n >= 10_000)
            return string.Format("{0:F1}K", (double)n / 1_000);
        return n.ToString();
    }
}

/// <summary>
/// Plays speed-themed WAV files via the Windows winmm.dll
/// PlaySound API. Sound files are loaded from a configurable
/// directory (default: a "Sounds" folder next to the .cs file
/// or wherever SoundDirectory is pointed). Each speed tier
/// maps to a WAV file on disk:
///
///   train.wav   – played below 100 it/s
///   car.wav     – played from 100 to 10K it/s
///   rocket.wav  – played from 10K to 1M it/s
///   bullet.wav  – played above 1M it/s
///
/// To use your own recordings, simply replace the WAV files.
/// Any standard PCM WAV file will work.
/// </summary>
internal static class SpeedSound
{
    [DllImport("winmm.dll", SetLastError = true)]
    private static extern bool PlaySound(
        byte[] pszSound, IntPtr hmod, uint fdwSound);

    private const uint SND_MEMORY    = 0x0004;
    private const uint SND_ASYNC     = 0x0001;
    private const uint SND_LOOP      = 0x0008;
    private const uint SND_NODEFAULT = 0x0002;
    private const uint SND_PURGE     = 0x0040;

    /// <summary>
    /// Directory containing the WAV files. Set this before
    /// iterating if the default location is not correct.
    /// Defaults to a "Sounds" subfolder next to the loaded
    /// assembly, or falls back to the current directory.
    /// </summary>
    public static string SoundDirectory { get; set; }

    // Cached WAV bytes per tier (loaded once from disk).
    private static readonly Dictionary<SpeedTier, byte[]>
        _cache = new Dictionary<SpeedTier, byte[]>();

    // Cached WAV duration in milliseconds per tier.
    private static readonly Dictionary<SpeedTier, int>
        _durationCache = new Dictionary<SpeedTier, int>();

    // Keep a reference so the GC doesn't collect the
    // byte array while winmm is reading it.
    private static byte[] _currentWav;
    private static SpeedTier _currentTier = SpeedTier.None;
    private static Stopwatch _playbackTimer;

    internal enum SpeedTier
    {
        None,
        Train,   // < 100 it/s
        Car,     // 100 – 10K it/s
        Rocket,  // 10K – 1M it/s
        Bullet   // > 1M it/s
    }

    // Maps each tier to its expected file name.
    private static readonly Dictionary<SpeedTier, string>
        TierFileNames = new Dictionary<SpeedTier, string>
    {
        { SpeedTier.Train,  "train.wav"  },
        { SpeedTier.Car,    "car.wav"    },
        { SpeedTier.Rocket, "rocket.wav" },
        { SpeedTier.Bullet, "bullet.wav" },
    };

    /// <summary>
    /// Volume multiplier per tier (1.0 = original,
    /// 0.5 = half volume, 2.0 = double volume).
    /// Samples are clamped so values above 1.0 won't
    /// wrap around, but may clip.
    /// </summary>
    private static readonly Dictionary<SpeedTier, double>
        TierVolume = new Dictionary<SpeedTier, double>
    {
        { SpeedTier.Train,  1.0 },
        { SpeedTier.Car,    1.0 },
        { SpeedTier.Rocket, 1.0 },
        { SpeedTier.Bullet, 2.0 },
    };

    internal static SpeedTier GetTier(double itemsPerSec)
    {
        if (itemsPerSec < 1_000)     return SpeedTier.Train;
        if (itemsPerSec < 20_000)    return SpeedTier.Car;
        if (itemsPerSec < 1_000_000) return SpeedTier.Rocket;
        return SpeedTier.Bullet;
    }

    internal static void Play(SpeedTier tier)
    {
        if (tier == _currentTier) return;
        _currentTier = tier;
        _currentWav = LoadWav(tier);
        if (_currentWav == null) return;
        try
        {
            PlaySound(
                _currentWav, IntPtr.Zero,
                SND_MEMORY | SND_ASYNC
                    | SND_LOOP | SND_NODEFAULT);
            _playbackTimer = Stopwatch.StartNew();
        }
        catch
        {
            // Non-Windows or winmm unavailable – ignore.
        }
    }

    internal static void ClearCache()
    {
        _cache.Clear();
        _durationCache.Clear();
    }

    private const int MaxWaitMs = 3000;
    private const int FallbackDurationMs = 2000;

    internal static bool IsPlaying =>
        _playbackTimer != null
        && _currentTier != SpeedTier.None;

    /// <summary>
    /// Waits for the currently looping sound to reach the
    /// end of its current iteration, then purges.
    /// </summary>
    internal static void StopAfterCurrentLoop()
    {
        if (_playbackTimer != null
            && _currentTier != SpeedTier.None)
        {
            int dur = GetCurrentDuration();
            int elapsed = (int)_playbackTimer
                .ElapsedMilliseconds;
            int posInLoop = elapsed % dur;
            int remaining = dur - posInLoop;
            // Console.Error.WriteLine();
            // Console.Error.WriteLine(
            //     $"[StopAfterCurrentLoop] tier={_currentTier}"
            //     + $" dur={dur}ms elapsed={elapsed}ms"
            //     + $" posInLoop={posInLoop}ms"
            //     + $" remaining={remaining}ms");
            if (remaining > 0)
                Thread.Sleep(
                    Math.Min(remaining, MaxWaitMs));
        }

        Stop();
    }

    /// <summary>
    /// Plays the sound for the given tier exactly once
    /// (no loop) and blocks until playback finishes.
    /// Used when the enumeration was too fast for
    /// Render() to start the sound.
    /// </summary>
    internal static void PlayOnceAndWait(SpeedTier tier)
    {
        var wav = LoadWav(tier);
        if (wav == null) return;
        try
        {
            _currentWav = wav;
            _currentTier = tier;
            // Play once asynchronously (no loop) and
            // sleep for the full duration. No purge, so
            // the audio hardware can drain completely.
            PlaySound(
                wav, IntPtr.Zero,
                SND_MEMORY | SND_ASYNC
                    | SND_NODEFAULT);
            int dur =
                _durationCache.TryGetValue(
                    tier, out int d) && d > 0
                ? d
                : FallbackDurationMs;
            Thread.Sleep(
                Math.Min(dur, MaxWaitMs));
        }
        catch { }
        _currentTier = SpeedTier.None;
        _currentWav = null;
        _playbackTimer = null;
    }

    private static int GetCurrentDuration()
    {
        if (_currentTier != SpeedTier.None
            && _durationCache.TryGetValue(
                   _currentTier, out int d)
            && d > 0)
            return d;
        return FallbackDurationMs;
    }

    internal static void Stop()
    {
        try
        {
            PlaySound(null, IntPtr.Zero, SND_PURGE);
        }
        catch { }
        _currentTier = SpeedTier.None;
        _currentWav = null;
        _playbackTimer = null;
    }

    // ── File loading ────────────────────────────────────

    private static string ResolveSoundDirectory()
    {
        if (!string.IsNullOrEmpty(SoundDirectory))
            return SoundDirectory;

        // Try to find a Sounds folder next to this assembly.
        try
        {
            string asmDir = Path.GetDirectoryName(
                typeof(SpeedSound).Assembly.Location);
            if (!string.IsNullOrEmpty(asmDir))
            {
                string candidate =
                    Path.Combine(asmDir, "Sounds");
                if (Directory.Exists(candidate))
                    return candidate;
            }
        }
        catch { }

        // Fallback: Sounds subfolder in current directory.
        return Path.Combine(
            Environment.CurrentDirectory, "Sounds");
    }

    private static byte[] LoadWav(SpeedTier tier)
    {
        if (_cache.TryGetValue(tier, out byte[] cached))
            return cached;

        string dir = ResolveSoundDirectory();
        if (!TierFileNames.TryGetValue(tier, out string fileName))
            return null;

        string path = Path.Combine(dir, fileName);
        byte[] data = null;
        try
        {
            if (File.Exists(path))
            {
                data = File.ReadAllBytes(path);
                _durationCache[tier] =
                    GetWavDurationMs(data);
                if (TierVolume.TryGetValue(
                        tier, out double vol)
                    && Math.Abs(vol - 1.0) > 0.001)
                {
                    data = ScaleVolume(data, vol);
                }
            }
            else if (!_warnedMissing)
            {
                _warnedMissing = true;
                Console.Error.WriteLine(
                    $"\n[SpeedSound] WAV not found: {path}"
                    + "\n[SpeedSound] Set "
                    + "[ProgressEnumerable]::SoundDirectory"
                    + " to the folder containing your WAV "
                    + "files.");
            }
        }
        catch { }

        _cache[tier] = data;
        return data;
    }

    /// <summary>
    /// <summary>
    /// Scales the PCM sample data in a WAV byte array
    /// by the given volume multiplier. Supports 16-bit
    /// and 8-bit PCM. Returns a new array (the original
    /// is not modified).
    /// </summary>
    private static byte[] ScaleVolume(
        byte[] wav, double volume)
    {
        byte[] result = (byte[])wav.Clone();

        // Find fmt chunk to get bits per sample,
        // and data chunk to know which bytes to scale.
        int bitsPerSample = 0;
        int dataOffset = -1;
        int dataSize = 0;
        int pos = 12;
        while (pos + 8 <= result.Length)
        {
            int chunkSize =
                BitConverter.ToInt32(result, pos + 4);

            if (result[pos]     == (byte)'f'
                && result[pos + 1] == (byte)'m'
                && result[pos + 2] == (byte)'t'
                && result[pos + 3] == (byte)' '
                && chunkSize >= 16
                && pos + 22 <= result.Length)
            {
                bitsPerSample =
                    BitConverter.ToInt16(
                        result, pos + 22);
            }

            if (result[pos]     == (byte)'d'
                && result[pos + 1] == (byte)'a'
                && result[pos + 2] == (byte)'t'
                && result[pos + 3] == (byte)'a'
                && chunkSize > 0)
            {
                dataOffset = pos + 8;
                dataSize = chunkSize;
            }

            if (chunkSize < 0) break;
            pos += 8 + chunkSize;
            if (chunkSize % 2 != 0) pos++;
        }

        if (dataOffset < 0 || dataSize <= 0)
            return result;

        int end = Math.Min(
            dataOffset + dataSize, result.Length);

        bool clipped = false;

        if (bitsPerSample == 16)
        {
            for (int i = dataOffset;
                i + 1 < end; i += 2)
            {
                int sample =
                    BitConverter.ToInt16(result, i);
                int scaled = (int)(sample * volume);
                if (scaled < -32768 || scaled > 32767)
                    clipped = true;
                sample = Math.Max(-32768,
                    Math.Min(32767, scaled));
                result[i] = (byte)(sample & 0xFF);
                result[i + 1] =
                    (byte)((sample >> 8) & 0xFF);
            }
        }
        else if (bitsPerSample == 8)
        {
            // 8-bit PCM is unsigned, centered at 128.
            for (int i = dataOffset; i < end; i++)
            {
                int sample = result[i] - 128;
                int scaled = (int)(sample * volume);
                if (scaled < -128 || scaled > 127)
                    clipped = true;
                sample = Math.Max(-128,
                    Math.Min(127, scaled));
                result[i] = (byte)(sample + 128);
            }
        }

        if (clipped)
            Console.Error.WriteLine(
                "[SpeedSound] Warning: audio clipping "
                + $"occurred at volume {volume:F2}. "
                + "Consider lowering the volume "
                + "multiplier.");

        return result;
    }

    /// Parses the WAV header to compute duration in ms.
    /// Searches for the "data" chunk rather than assuming
    /// a fixed offset, since WAV files may contain extra
    /// chunks (LIST, fact, etc.) between fmt and data.
    /// </summary>
    private static int GetWavDurationMs(byte[] wav)
    {
        if (wav == null || wav.Length < 44)
            return 0;

        // Walk chunks starting after the RIFF header
        // (12 bytes: "RIFF" + size + "WAVE").
        int byteRate = 0;
        int dataSize = 0;
        int pos = 12;
        while (pos + 8 <= wav.Length)
        {
            int chunkSize =
                BitConverter.ToInt32(wav, pos + 4);

            if (wav[pos]     == (byte)'f'
                && wav[pos + 1] == (byte)'m'
                && wav[pos + 2] == (byte)'t'
                && wav[pos + 3] == (byte)' '
                && chunkSize >= 16
                && pos + 20 <= wav.Length)
            {
                // Byte rate is at offset 8 within the
                // fmt chunk data (after format, channels,
                // and sample rate).
                byteRate =
                    BitConverter.ToInt32(wav, pos + 16);
            }

            if (wav[pos]     == (byte)'d'
                && wav[pos + 1] == (byte)'a'
                && wav[pos + 2] == (byte)'t'
                && wav[pos + 3] == (byte)'a'
                && chunkSize > 0)
            {
                dataSize = chunkSize;
            }

            if (chunkSize < 0) break;
            // Advance past chunk header + data; chunks
            // are word-aligned.
            pos += 8 + chunkSize;
            if (chunkSize % 2 != 0) pos++;
        }

        if (byteRate <= 0 || dataSize <= 0)
            return 0;
        return (int)(
            (long)dataSize * 1000 / byteRate);
    }

    private static bool _warnedMissing;
}
