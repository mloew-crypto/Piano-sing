using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using Plugin.Maui.Audio;

namespace PianoApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        // Registers IAudioManager; 3.x uses platform defaults on Android (music/media stream).
        builder
            .UseMauiApp<App>()
            .AddAudio(_ => { }, _ => { });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
