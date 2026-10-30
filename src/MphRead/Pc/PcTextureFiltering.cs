using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;

namespace MphRead
{
    internal sealed class PcTextureFiltering
    {
        private const TextureParameterName AnisotropyParameter = (TextureParameterName)0x84FE;
        private readonly HashSet<int> _mipmapped = new();
        public float MaxAnisotropy { get; }
        public int MipmappedTextures => _mipmapped.Count;
        public int LastModelTexture { get; private set; }

        public PcTextureFiltering()
        {
            bool supported = false;
            int extensions = GL.GetInteger(GetPName.NumExtensions);
            for (int i = 0; i < extensions; i++)
            {
                string name = GL.GetString(StringNameIndexed.Extensions, i);
                supported |= name is "GL_EXT_texture_filter_anisotropic" or "GL_ARB_texture_filter_anisotropic";
            }
            MaxAnisotropy = supported ? Math.Clamp(GL.GetFloat((GetPName)0x84FF), 1, 16) : 1;
        }

        // Called with a model texture bound. HUD atlases and movie frames never
        // receive mipmaps, so their glyphs and independently updated frames stay intact.
        public void Model(int texture, bool legacyLinear)
        {
            bool enhanced = PcRuntime.Enabled && PcRuntime.Profile.EnhancedGraphics;
            LastModelTexture = texture;
            if (enhanced && _mipmapped.Add(texture)) GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)(enhanced ? TextureMinFilter.LinearMipmapLinear : legacyLinear ? TextureMinFilter.Linear : TextureMinFilter.Nearest));
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)(enhanced || legacyLinear ? TextureMagFilter.Linear : TextureMagFilter.Nearest));
            if (MaxAnisotropy > 1)
                GL.TexParameter(TextureTarget.Texture2D, AnisotropyParameter, enhanced ? MaxAnisotropy : 1f);
        }

        public void Invalidate(int texture) => _mipmapped.Remove(texture);
    }
}
