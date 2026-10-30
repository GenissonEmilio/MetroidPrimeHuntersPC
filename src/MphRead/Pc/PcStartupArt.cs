using System;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace MphRead
{
    // Decoded from the player's ROM at runtime; no game art ships with the port.
    internal sealed class PcStartupArt : IDisposable
    {
        private int _logo;
        public void Load()
        {
            var recolor = Read.GetModelInstance("logofinal", dir: MetaDir.ToStart).Model.Recolors[0];
            var texture = recolor.Textures[3];
            var pixels = recolor.GetPixels(3, 3);
            var rgba = new byte[pixels.Count * 4];
            for (int i = 0; i < pixels.Count; i++)
            {
                rgba[i * 4] = pixels[i].Red; rgba[i * 4 + 1] = pixels[i].Green;
                rgba[i * 4 + 2] = pixels[i].Blue; rgba[i * 4 + 3] = pixels[i].Alpha;
            }
            _logo = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _logo);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, texture.Width, texture.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, rgba);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
        public static void Background(double time)
        {
            PcCanvas.Rect(-10000, -10000, 20000, 20000, new Color4(0.012f, 0.022f, 0.031f, 1));
            for (int i = 0; i < 130; i++)
            {
                float light = 0.18f + (float)(Math.Sin(time * 0.35 + i) + 1) * 0.09f;
                PcCanvas.Rect(i * 811 % 1280, i * 197 % 720, i % 7 == 0 ? 2 : 1, 1, new Color4(light, light + 0.05f, light + 0.08f, 1));
            }
            for (int i = 0; i < 5; i++) PcCanvas.Line(new(0, 78 + i * 138), new(1280, -32 + i * 138), new Color4(0.1f, 0.18f, 0.23f, 0.3f));
            PcCanvas.Rect(0, 0, 1280, 8, new Color4(0.32f, 0.38f, 0.42f, 1));
            PcCanvas.Rect(0, 712, 1280, 8, new Color4(0.32f, 0.38f, 0.42f, 1));
        }
        public static void SkipHint(Vector2i size, string text = "ESC / ENTER  PULAR CENA")
        {
            using var canvas = new PcCanvas(size);
            PcCanvas.Rect(968, 675, 286, 30, new Color4(0, 0, 0, 0.7f));
            PcCanvas.Text(982, 685, text, 1.5f, PcCanvas.Muted);
        }
        public void Logo(float x, float y, float width)
        {
            if (_logo == 0)
            {
                PcCanvas.Text(x, y + width * 0.13f, "METROID PRIME", width / 100, PcCanvas.White);
                PcCanvas.Text(x + width * 0.18f, y + width * 0.28f, "HUNTERS", width / 80, PcCanvas.Amber);
                return;
            }
            GL.Enable(EnableCap.Texture2D); GL.BindTexture(TextureTarget.Texture2D, _logo); GL.Color4(Color4.White);
            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0, 0); GL.Vertex2(x, y); GL.TexCoord2(1, 0); GL.Vertex2(x + width, y);
            GL.TexCoord2(1, 1); GL.Vertex2(x + width, y + width / 2); GL.TexCoord2(0, 1); GL.Vertex2(x, y + width / 2);
            GL.End(); GL.BindTexture(TextureTarget.Texture2D, 0); GL.Disable(EnableCap.Texture2D);
        }
        public void Dispose() { if (_logo != 0) GL.DeleteTexture(_logo); _logo = 0; }
    }
}
