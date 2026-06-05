using IchigoHoshimiya.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace IchigoHoshimiya.Helpers;

public static class IconCompositor
{
    private const int IconSize = 192;
    private const int Padding = 16;
    private const int RosterIconSize = 128;
    private const int RosterPadding = 12;
    private const int RosterMaxColumns = 8;

    public static MemoryStream ComposeRoster(IReadOnlyList<string> iconPaths)
    {
        var columns = Math.Clamp(
            (int)Math.Ceiling(Math.Sqrt(iconPaths.Count)),
            1,
            RosterMaxColumns);
        var rows = (int)Math.Ceiling(iconPaths.Count / (double)columns);

        var width = RosterIconSize * columns + RosterPadding * (columns + 1);
        var height = RosterIconSize * rows + RosterPadding * (rows + 1);

        using var canvas = new Image<Rgba32>(width, height, new Rgba32(0, 0, 0, 0));

        for (var i = 0; i < iconPaths.Count; i++)
        {
            using var img = Image.Load<Rgba32>(iconPaths[i]);
            img.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(RosterIconSize, RosterIconSize),
                Mode = ResizeMode.Crop
            }));

            var col = i % columns;
            var row = i / columns;
            var x = RosterPadding + col * (RosterIconSize + RosterPadding);
            var y = RosterPadding + row * (RosterIconSize + RosterPadding);

            var position = new Point(x, y);
            canvas.Mutate(ctx => ctx.DrawImage(img, position, 1f));
        }

        var stream = new MemoryStream();
        canvas.SaveAsPng(stream);
        stream.Position = 0;
        return stream;
    }

    public static MemoryStream Compose(IReadOnlyList<EventParticipant> participants)
    {
        var images = new List<Image<Rgba32>>(participants.Count);

        try
        {
            foreach (var p in participants)
            {
                var img = Image.Load<Rgba32>(p.IconPath);
                img.Mutate(ctx => ctx.Resize(new ResizeOptions
                {
                    Size = new Size(IconSize, IconSize),
                    Mode = ResizeMode.Crop
                }));

                if (p.Died)
                {
                    img.Mutate(ctx => ctx.Grayscale().Brightness(0.6f));
                }

                images.Add(img);
            }

            var width = IconSize * images.Count + Padding * (images.Count + 1);
            var height = IconSize + Padding * 2;

            using var canvas = new Image<Rgba32>(width, height, new Rgba32(0, 0, 0, 0));
            var x = Padding;
            foreach (var img in images)
            {
                var position = new Point(x, Padding);
                canvas.Mutate(ctx => ctx.DrawImage(img, position, 1f));
                x += IconSize + Padding;
            }

            var stream = new MemoryStream();
            canvas.SaveAsPng(stream);
            stream.Position = 0;
            return stream;
        }
        finally
        {
            foreach (var img in images)
            {
                img.Dispose();
            }
        }
    }
}
