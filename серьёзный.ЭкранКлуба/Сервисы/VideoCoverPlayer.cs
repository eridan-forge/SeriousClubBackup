using System;
using System.Windows.Controls;

namespace серьёзный.ЭкранКлуба.Сервисы;

public static class VideoCoverPlayer
{
    public static MediaElement Create(string file)
    {
        var media = new MediaElement
        {
            Source = new Uri(file),
            LoadedBehavior = MediaState.Manual,
            UnloadedBehavior = MediaState.Stop,
            IsMuted = true,
            Stretch = System.Windows.Media.Stretch.UniformToFill
        };

        media.MediaEnded += (_, _) =>
        {
            media.Position = TimeSpan.Zero;
            media.Play();
        };

        media.Play();

        return media;
    }
}