using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace MphRead
{
    public static class PcLayout
    {
        public static Vector2 ToCanvas(Vector2 point, Vector2i size)
        {
            float scale = Math.Min(size.X / 1280f, size.Y / 720f);
            if (scale <= 0) return new Vector2(-1);
            return (point - new Vector2((size.X - 1280 * scale) / 2, (size.Y - 720 * scale) / 2)) / scale;
        }

        public static int HitRow(Vector2 point, int count)
        {
            if (point.X < 72 || point.X > 676 || point.Y < 226) return -1;
            int row = (int)((point.Y - 226) / 58);
            return row < count && point.Y < 226 + row * 58 + 50 ? row : -1;
        }
    }

    // An original, code-drawn bitmap alphabet; no game assets or external fonts.
    internal sealed class PcCanvas : IDisposable
    {
        public static readonly Color4 Cyan = new(0.37f, 0.91f, 0.91f, 1);
        public static readonly Color4 White = new(0.87f, 0.94f, 0.96f, 1);
        public static readonly Color4 Muted = new(0.43f, 0.61f, 0.66f, 1);
        public static readonly Color4 Amber = new(1f, 0.67f, 0.29f, 1);
        private readonly int _program;
        private readonly int _matrixMode;
        private readonly int _framebuffer;
        private static readonly Dictionary<char, string> _glyphs = new();

        static PcCanvas()
        {
            string[] glyphs = {
                "A:0E11111F111111", "B:1E11111E11111E", "C:0F10101010100F", "D:1E11111111111E",
                "E:1F10101E10101F", "F:1F10101E101010", "G:0F10101711110F", "H:1111111F111111",
                "I:0E04040404040E", "J:0702020212120C", "K:11121418141211", "L:1010101010101F",
                "M:111B1515111111", "N:11191513111111", "O:0E11111111110E", "P:1E11111E101010",
                "Q:0E11111115120D", "R:1E11111E141211", "S:0F10100E01011E", "T:1F040404040404",
                "U:1111111111110E", "V:11111111110A04", "W:11111115151B11", "X:11110A040A1111",
                "Y:11110A04040404", "Z:1F01020408101F", "0:0E11131519110E", "1:040C040404040E",
                "2:0E11010204081F", "3:1E01010E01011E", "4:02060A121F0202", "5:1F10101E01011E",
                "6:0E10101E11110E", "7:1F010204080808", "8:0E11110E11110E", "9:0E11110F01010E",
                ".:00000000000C0C", ",:00000000000408", "::00040000040000", "-:0000001F000000",
                "+:0004041F040400", "/:01010204081010", "[:0E08080808080E", "]:0E02020202020E",
                "(:02040808080402", "):08040202020408", ">:10080402040810", "<:01020408040201",
                "=:00001F001F0000", "?:0E110102040004", "!:04040404040004", "_:0000000000001F",
                "%:19190204081313", "|:04040404040404"
            };
            foreach (string glyph in glyphs) _glyphs[glyph[0]] = glyph[2..];
            _glyphs['\''] = "04040800000000";
            _glyphs['\"'] = "0A0A1400000000";
        }

        public PcCanvas(Vector2i size)
        {
            _program = GL.GetInteger(GetPName.CurrentProgram);
            _matrixMode = GL.GetInteger(GetPName.MatrixMode);
            _framebuffer = GL.GetInteger(GetPName.FramebufferBinding);
            GL.PushAttrib(AttribMask.AllAttribBits);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.UseProgram(0);
            GL.Viewport(0, 0, Math.Max(size.X, 1), Math.Max(size.Y, 1));
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.Disable(EnableCap.Texture2D);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.Disable(EnableCap.Texture2D);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.StencilTest);
            GL.Disable(EnableCap.AlphaTest);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.ScissorTest);
            GL.ColorMask(true, true, true, true);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            float scale = Math.Max(0.001f, Math.Min(size.X / 1280f, size.Y / 720f));
            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.Ortho(0, size.X / scale, size.Y / scale, 0, -1, 1);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.Translate((size.X / scale - 1280) / 2, (size.Y / scale - 720) / 2, 0);
        }

        public static void Rect(float x, float y, float w, float h, Color4 color)
        {
            GL.Color4(color);
            GL.Begin(PrimitiveType.Quads);
            Quad(x, y, w, h);
            GL.End();
        }

        private static void Quad(float x, float y, float w, float h)
        {
            GL.Vertex2(x, y); GL.Vertex2(x + w, y); GL.Vertex2(x + w, y + h); GL.Vertex2(x, y + h);
        }

        public static void Text(float x, float y, string text, float scale, Color4 color)
        {
            string normalized = text.ToUpperInvariant().Normalize(NormalizationForm.FormD);
            GL.Color4(color);
            GL.Begin(PrimitiveType.Quads);
            foreach (char character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
                if (_glyphs.TryGetValue(character, out string? glyph))
                {
                    for (int row = 0; row < 7; row++)
                    {
                        int bits = Int32.Parse(glyph.AsSpan(row * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        for (int col = 0; col < 5; col++)
                            if ((bits & (1 << (4 - col))) != 0) Quad(x + col * scale, y + row * scale, scale, scale);
                    }
                }
                x += 6 * scale;
            }
            GL.End();
        }

        public static void Background(bool overlay)
        {
            Rect(-10000, -10000, 20000, 20000, new Color4(0.015f, 0.035f, 0.06f, overlay ? 0.94f : 1));
            for (int i = 0; i < 24; i++) Rect(720, 64 + i * 26, 480, 1, new Color4(0.08f, 0.2f, 0.25f, 0.4f));
            GL.Color4(new Color4(0.12f, 0.36f, 0.4f, 1));
            GL.Begin(PrimitiveType.Lines);
            for (int ring = 0; ring < 3; ring++)
            {
                float radius = 120 + ring * 34;
                for (int segment = 0; segment < 72; segment++)
                {
                    if (segment % 12 > 8) continue;
                    double a = segment * Math.PI / 36;
                    double b = (segment + 1) * Math.PI / 36;
                    GL.Vertex2(970 + Math.Cos(a) * radius, 305 + Math.Sin(a) * radius);
                    GL.Vertex2(970 + Math.Cos(b) * radius, 305 + Math.Sin(b) * radius);
                }
            }
            GL.End();
            Rect(72, 60, 36, 3, Amber);
            Text(122, 56, "HUNTERS / PC EDITION", 2, Cyan);
            Text(72, 116, overlay ? "SISTEMAS EM ESPERA" : "METROID PRIME", 4, White);
            Text(74, 166, overlay ? "PAUSA" : "HUNTERS", 3, Amber);
            Rect(72, 626, 1136, 1, new Color4(0.15f, 0.3f, 0.34f, 1));
            Text(72, 654, "WASD / SETAS  NAVEGAR    ENTER  SELECIONAR", 1.6f, Muted);
            Text(72, 679, "BASE MPHREAD / NONEGIVEN    ADAPTACAO PC EXPERIMENTAL", 1.3f, Muted);
        }

        public static void Line(Vector2 a, Vector2 b, Color4 color, float width = 1)
        {
            GL.LineWidth(width);
            GL.Color4(color);
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex2(a); GL.Vertex2(b);
            GL.End();
            GL.LineWidth(1);
        }

        public static void Panel(string title, string subtitle)
        {
            Rect(-10000, -10000, 20000, 20000, new Color4(0.015f, 0.035f, 0.06f, 0.97f));
            Text(72, 46, "HUNTERS / SISTEMAS DE CAMPO", 1.8f, Cyan);
            Rect(1050, 36, 158, 34, new Color4(0.06f, 0.22f, 0.28f, 1));
            Text(1062, 48, "VOLTAR [ESC]", 1.7f, Cyan);
            Text(72, 89, title, 3.4f, White);
            Text(72, 135, PcText.Ellipsis(subtitle, 100), 1.6f, Muted);
            Rect(72, 682, 1136, 1, new Color4(0.15f, 0.3f, 0.34f, 1));
            Text(72, 696, "SIMULACAO PAUSADA / FECHAR RETOMA WASD + MOUSE", 1.4f, Muted);
        }

        public static void Clip(Vector2i size, int x, int y, int width, int height)
        {
            float scale = Math.Min(size.X / 1280f, size.Y / 720f);
            int left = (int)((size.X - 1280 * scale) / 2 + x * scale);
            int bottom = (int)((size.Y - 720 * scale) / 2 + (720 - y - height) * scale);
            GL.Scissor(left, bottom, (int)(width * scale), (int)(height * scale));
            GL.Enable(EnableCap.ScissorTest);
        }

        public static void Unclip() => GL.Disable(EnableCap.ScissorTest);

        public void Dispose()
        {
            GL.MatrixMode(MatrixMode.Modelview); GL.PopMatrix();
            GL.MatrixMode(MatrixMode.Projection); GL.PopMatrix();
            GL.MatrixMode((MatrixMode)_matrixMode);
            GL.PopAttrib();
            GL.UseProgram(_program);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
        }
    }
}
