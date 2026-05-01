# Piano — .NET MAUI

5-octave piano app (C1–C6). Tap keys or use the keyboard: **1–6** = octave, **A W S E D F T G Y H U J** (and **B** for the 12th note) for notes.

Built with .NET MAUI so you can run on **Windows** and **Android** (e.g. publish to Google Play).

## Migration from WPF

This project was migrated from WPF to .NET MAUI. The same behaviour is preserved:

- **NoteMapping.json** — Maps note names (e.g. `"C2"`, `"C#4"`) to audio file paths. Optional `"_basePath"` for the folder containing the audio files.
- **Audio** — WAV (or platform-supported) files. On Windows, use a path next to the app or set `_basePath`. On Android, you can bundle `NoteMapping.json` and optionally the **Audio** folder as app assets (see below).

## Build and run

1. **Install .NET MAUI workload** (required for Android and optional for Windows):

   ```bash
   dotnet workload install maui
   ```

2. **Windows**

   ```bash
   dotnet build -f net8.0-windows10.0.19041.0
   dotnet run -f net8.0-windows10.0.19041.0
   ```

   Place `NoteMapping.json` (and your audio files per `_basePath`) next to the built exe or in the configured path.

3. **Android (e.g. for Google Play)**

   ```bash
   dotnet build -f net8.0-android
   dotnet run -f net8.0-android
   ```

   - **NoteMapping.json** is included as a MauiAsset and loaded from the app package when no file is found in app data or base directory.
   - To ship audio inside the app: add your WAVs as **MauiAsset** (e.g. under `Resources/Raw/Audio/` and reference them in `NoteMapping.json` with paths like `Audio/C1.wav`). When the mapping is loaded from the app package, those paths are resolved from the package.
   - Or use `_basePath` to a folder on device storage and deploy/copy `NoteMapping.json` and audio there.

## Publish for Google Play

- Set **ApplicationId** and version in the `.csproj` (e.g. `ApplicationId`, `ApplicationVersion`, `ApplicationVersionCode`).
- Build a release APK or AAB:

  ```bash
  dotnet publish -f net8.0-android -c Release
  ```

- Sign the app (configure signing in the project or with `dotnet publish` options). The output is under `bin/Release/net8.0-android/`.

## Project layout

- **MainPage.xaml / MainPage.xaml.cs** — UI and input (touch, keyboard) and playback.
- **Services/PianoDrawable.cs** — Draws the keyboard in `GraphicsView`.
- **Services/NoteMappingService.cs** — Loads `NoteMapping.json` and resolves audio paths (file system or app package).
- **Models/PianoKey.cs** — Key geometry and hit-testing.
- **MauiProgram.cs**, **App.xaml** — MAUI app setup; **Plugin.Maui.Audio** is used for playback.
