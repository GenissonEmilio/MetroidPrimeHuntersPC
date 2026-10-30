using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Desktop;

namespace MphRead
{
    internal static class PcPresentation
    {
        // GLFW client coordinates and drawable pixels can differ on HiDPI displays.
        // Reconcile every frame: fullscreen callbacks can arrive before scene creation.
        public static bool Sync(GameWindow window, Scene? scene)
        {
            var pixels = window.FramebufferSize;
            if (pixels.X <= 0 || pixels.Y <= 0) return false;
            if (scene != null && scene.Size != pixels)
            {
                scene.Size = pixels;
                scene.OnResize();
            }
            GL.Viewport(0, 0, pixels.X, pixels.Y);
            return true;
        }
    }
}
