using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Sound;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using SceneCameraManager = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CameraManager;

namespace ReflavorRP;

public sealed unsafe class SpeechNotificationSound(IPluginLog log) : IDisposable
{
    private readonly object gate = new();
    private IXAudio2* engine;
    private IXAudio2MasteringVoice* output;
    private IXAudio2SourceVoice* voice;
    private byte[] samples = [];
    private GCHandle pinnedSamples;
    private bool unavailable;
    private bool disposed;

    public void Play(Vector3 position, float volume) => PlayCore(position, volume);

    public void PlayCentered(float volume) => PlayCore(null, volume);

    private void PlayCore(Vector3? position, float volume)
    {
        lock (gate)
        {
            if (disposed || unavailable || !float.IsFinite(volume) || volume <= 0)
                return;
            try
            {
                var manager = SoundManager.Instance();
                var cameras = SceneCameraManager.Instance();
                var camera = cameras == null ? null : cameras->CurrentCamera;
                if (manager == null || manager->Disabled || manager->IsSndMaster || (position.HasValue && camera == null))
                    return;
                // This is our own audio stream: explicitly follow the game's master/system gain and mute rules.
                var gameVolume = manager->MasterVolume * manager->ActiveVolume * manager->GetEffectiveVolume(SoundBus.System);
                if (!float.IsFinite(gameVolume) || gameVolume <= 0)
                    return;

                if (engine == null)
                    Initialize();
                var direction = Vector2.Zero;
                if (position is { } sourcePosition)
                {
                    direction = ChatSpatial.GetCameraDirection(sourcePosition, camera);
                }
                var gains = GetChannelGains(direction);
                float* matrix = stackalloc float[2] { gains.X, gains.Y };
                Check(voice->Stop(0, 0));
                Check(voice->FlushSourceBuffers());
                Check(voice->SetOutputMatrix((IXAudio2Voice*)output, 1, 2, matrix, 0));
                Check(voice->SetVolume(Math.Clamp(volume, 0, 2f) * Math.Clamp(gameVolume, 0, 1), 0));
                var buffer = new XAUDIO2_BUFFER
                {
                    Flags = 0x40, // XAUDIO2_END_OF_STREAM: a single, non-looping sample.
                    AudioBytes = (uint)samples.Length,
                    pAudioData = (byte*)pinnedSamples.AddrOfPinnedObject(),
                };
                Check(voice->SubmitSourceBuffer(&buffer, null));
                Check(voice->Start(0, 0));
            }
            catch (Exception exception)
            {
                // An unavailable device must not break chat or repeatedly retry on every message.
                unavailable = true;
                ReleaseResources();
                log.Warning(exception, "Could not play the speech notification; reload the plugin to retry.");
            }
        }
    }

    private void Initialize()
    {
        using (var stream = typeof(SpeechNotificationSound).Assembly.GetManifestResourceStream("ReflavorRP.SpeechNotification.pcm")
            ?? throw new FileNotFoundException("The bundled speech sample is missing."))
        using (var memory = new MemoryStream())
        {
            stream.CopyTo(memory);
            samples = memory.ToArray();
        }
        if (samples.Length == 0 || samples.Length % 2 != 0)
            throw new InvalidDataException("The speech sample must be mono, 16-bit PCM at 44100 Hz.");
        pinnedSamples = GCHandle.Alloc(samples, GCHandleType.Pinned);

        IXAudio2* newEngine = null;
        Check(DirectX.XAudio2Create(&newEngine, 0, 0xFFFFFFFF));
        engine = newEngine;
        IXAudio2MasteringVoice* newOutput = null;
        Check(engine->CreateMasteringVoice(&newOutput, 2, 44100, 0, null, null,
            AUDIO_STREAM_CATEGORY.AudioCategory_GameEffects));
        output = newOutput;
        var format = new WAVEFORMATEX
        {
            wFormatTag = 1, nChannels = 1, nSamplesPerSec = 44100,
            nAvgBytesPerSec = 88200, nBlockAlign = 2, wBitsPerSample = 16,
        };
        IXAudio2SourceVoice* newVoice = null;
        Check(engine->CreateSourceVoice(&newVoice, &format, 0, 2, null, null, null));
        voice = newVoice;
    }

    internal static Vector2 GetChannelGains(Vector2 cameraDirection)
    {
        var length = cameraDirection.Length();
        var pan = length > 0.001f ? Math.Clamp(cameraDirection.X / length, -1, 1) : 0;
        return new Vector2(MathF.Sqrt((1 - pan) / 2), MathF.Sqrt((1 + pan) / 2));
    }

    private static void Check(HRESULT result) => Marshal.ThrowExceptionForHR(result.Value);

    public void Dispose()
    {
        lock (gate)
        {
            disposed = true;
            ReleaseResources();
        }
    }

    private void ReleaseResources()
    {
        // Destroy voices before releasing the sample memory used by the audio thread.
        if (voice != null)
        {
            voice->DestroyVoice();
            voice = null;
        }
        if (output != null)
        {
            output->DestroyVoice();
            output = null;
        }
        if (engine != null)
        {
            engine->Release();
            engine = null;
        }
        if (pinnedSamples.IsAllocated)
            pinnedSamples.Free();
        samples = [];
    }
}
