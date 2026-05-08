using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using PianoApp.Models;
using PianoApp.Services;
using Plugin.Maui.Audio;

namespace PianoApp;

public partial class MainPage : ContentPage
{
    private const int WhiteKeyWidth = 28;
    private const int WhiteKeyHeight = 180;
    private const int BlackKeyWidth = 28;
    private const int BlackKeyHeight = 110;
    private const int OctaveCount = 5;
    private const int StartOctave = 1;

    private static readonly string[] NoteNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
    private static readonly bool[] IsBlackKey = [false, true, false, true, false, false, true, false, true, false, true, false];

    private readonly List<PianoKey> _keys = [];
    private readonly HashSet<string> _pressedKeys = [];
    private readonly NoteMappingService _mapping = new();
    private readonly Dictionary<string, IAudioPlayer> _preloadedPlayers = [];
    private int _preloadIndex;
    private PianoDrawable? _drawable;
    private IAudioManager? _audioManager;

    private IAudioManager? AudioManager => _audioManager ??= Application.Current?.Handler?.MauiContext?.Services?.GetService<IAudioManager>();

    public MainPage()
    {
        InitializeComponent();
        LoadNoteMapping();
        BuildPianoKeys();
        _drawable = new PianoDrawable(_keys, _pressedKeys);
        PianoCanvas.Drawable = _drawable;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        // Size request for piano width: 5 octaves × 7 white + 1 (C6)
        double pianoWidth = (OctaveCount * 7 + 1) * WhiteKeyWidth;
        PianoCanvas.WidthRequest = pianoWidth;
        PianoCanvas.HeightRequest = WhiteKeyHeight;
        _preloadIndex = 0;
        PreloadNext();
    }

    private void LoadNoteMapping()
    {
        _mapping.Load();
    }

    private void BuildPianoKeys()
    {
        double x = 0;
        for (int oct = 0; oct < OctaveCount; oct++)
        {
            int octaveNum = StartOctave + oct;
            int whiteIndex = 0;
            for (int i = 0; i < 12; i++)
            {
                string noteName = NoteNames[i] + octaveNum;
                bool isBlack = IsBlackKey[i];
                double left, top, width, height;
                if (isBlack)
                {
                    width = BlackKeyWidth;
                    height = BlackKeyHeight;
                    double crackCenter = x + whiteIndex * WhiteKeyWidth;
                    left = crackCenter - BlackKeyWidth / 2;
                    top = 0;
                }
                else
                {
                    width = WhiteKeyWidth;
                    height = WhiteKeyHeight;
                    left = x + whiteIndex * WhiteKeyWidth;
                    top = 0;
                    whiteIndex++;
                }
                _keys.Add(new PianoKey(noteName, left, top, width, height, isBlack));
            }
            x += 7 * WhiteKeyWidth;
        }
        const string c6Name = "C6";
        _keys.Add(new PianoKey(c6Name, x, 0, WhiteKeyWidth, WhiteKeyHeight, false));
    }

    private string? GetAudioPath(string noteName) => _mapping.GetAudioPath(noteName);

    private async void PreloadNext()
    {
        if (AudioManager == null) return;
        while (_preloadIndex < _keys.Count)
        {
            var key = _keys[_preloadIndex++];
            string? pathOrPackage = GetAudioPath(key.NoteName);
            if (pathOrPackage == null) continue;
            try
            {
                Stream stream;
                if (pathOrPackage.StartsWith("package:", StringComparison.Ordinal))
                {
                    string relativePath = pathOrPackage.Substring("package:".Length);
                    stream = await FileSystem.OpenAppPackageFileAsync(relativePath);
                    if (stream == null) continue;
                }
                else
                {
                    stream = File.OpenRead(pathOrPackage);
                }
                var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                if (stream is not FileStream)
                    await stream.DisposeAsync();
                else
                    await ((FileStream)stream).DisposeAsync();
                memoryStream.Position = 0;
                var player = AudioManager.CreatePlayer(memoryStream);
                if (player != null)
                    _preloadedPlayers[key.NoteName] = player;
            }
            catch { }
            Dispatcher.Dispatch(PreloadNext);
            return;
        }
    }

    private void HighlightKey(string noteName, bool pressed)
    {
        if (pressed)
            _pressedKeys.Add(noteName);
        else
            _pressedKeys.Remove(noteName);
        PianoCanvas.Invalidate();
    }

    private async void PlayNote(string noteName)
    {
        if (_preloadedPlayers.TryGetValue(noteName, out var player))
        {
            try
            {
                player.Seek(0);
                player.Play();
            }
            catch { }
            return;
        }
        string? pathOrPackage = GetAudioPath(noteName);
        if (pathOrPackage == null) return;
        try
        {
            Stream stream;
            if (pathOrPackage.StartsWith("package:", StringComparison.Ordinal))
            {
                string relativePath = pathOrPackage.Substring("package:".Length);
                stream = await FileSystem.OpenAppPackageFileAsync(relativePath);
                if (stream == null) return;
            }
            else
            {
                stream = File.OpenRead(pathOrPackage);
            }
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            if (stream is not FileStream)
                await stream.DisposeAsync();
            else
                await ((FileStream)stream).DisposeAsync();
            memoryStream.Position = 0;
            var newPlayer = AudioManager?.CreatePlayer(memoryStream);
            newPlayer?.Play();
        }
        catch { }
    }

    private void OnStartInteraction(object? sender, TouchEventArgs e)
    {
        if (e.Touches.Length == 0) return;
        PointF p = e.Touches[0];
        foreach (var key in _keys.OrderByDescending(k => k.IsBlack))
        {
            if (!key.ContainsPoint(p.X, p.Y)) continue;
            _pressedKeys.Add(key.NoteName);
            PianoCanvas.Invalidate();
            PlayNote(key.NoteName);
            break;
        }
    }

    private void OnEndInteraction(object? sender, TouchEventArgs e)
    {
        _pressedKeys.Clear();
        PianoCanvas.Invalidate();
    }

}
