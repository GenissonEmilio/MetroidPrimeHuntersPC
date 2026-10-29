using System;
using System.IO;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MphRead
{
    internal static class PcCapture
    {
        public static string Save(Vector2i size, string? folder = null, string? name = null)
        {
            folder ??= Path.Combine(PcRuntime.StateRoot, "evidence");
            Directory.CreateDirectory(folder);
            byte[] pixels = new byte[checked(size.X * size.Y * 4)];
            GL.ReadPixels(0, 0, size.X, size.Y, OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
            using var image = Image.LoadPixelData<Rgba32>(pixels, size.X, size.Y);
            image.Mutate(context => context.Flip(FlipMode.Vertical));
            string path = Path.Combine(folder, name ?? $"hunters-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.png");
            image.SaveAsPng(path);
            File.WriteAllText(Path.Combine(folder, "gpu.txt"), $"{GL.GetString(StringName.Vendor)}\n{GL.GetString(StringName.Renderer)}\n{GL.GetString(StringName.Version)}\nGL error: {GL.GetError()}");
            return path;
        }
    }
}
