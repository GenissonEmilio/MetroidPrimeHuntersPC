using System;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace MphRead
{
    internal sealed class PcFrontEnd : GameWindow
    {
        private readonly PcMenu _menu;
        public bool StartGame { get; private set; }
        public bool Practice { get; private set; }
        private bool _capture;

        public PcFrontEnd() : base(new GameWindowSettings { UpdateFrequency = 60 }, new NativeWindowSettings
        {
            ClientSize = new Vector2i(1280, 720), Title = "Hunters PC - Menu",
            Profile = ContextProfile.Compatability, Flags = ContextFlags.Default, APIVersion = new Version(3, 2)
        })
        {
            VSync = VSyncMode.On;
            if (PcRuntime.Profile.Fullscreen) WindowState = WindowState.Fullscreen;
            _menu = new PcMenu(false, () => Start(false), Close, ToggleFullscreen, () => Start(true));
            _menu.Status = PcRom.Ready ? "ROM LOCAL VERIFICADA / TELA UNICA" : "ARRASTE SEU ZIP OU ROM .NDS PARA ESTA JANELA";
        }

        private void Start(bool practice)
        {
            if (!PcRom.Ready)
            {
                _menu.Status = "IMPORTE SUA ROM USA REV 0 ARRASTANDO O ARQUIVO AQUI";
                return;
            }
            Practice = practice; StartGame = true; Close();
        }

        private void ToggleFullscreen()
        {
            PcRuntime.Profile.Fullscreen = WindowState != WindowState.Fullscreen;
            WindowState = PcRuntime.Profile.Fullscreen ? WindowState.Fullscreen : WindowState.Normal;
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
            _menu.Draw(ClientSize);
            if (_capture)
            {
                PcCapture.Save(ClientSize);
                _capture = false;
            }
            SwapBuffers();
            base.OnRenderFrame(args);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (e.IsRepeat) return;
            if (e.Key == Keys.F11) { ToggleFullscreen(); PcRuntime.Profile.Save(PcRuntime.ProfilePath); }
            else if (e.Key == Keys.F12) _capture = true;
            else _menu.Key(e.Key);
            base.OnKeyDown(e);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            _menu.Pointer(MouseState.Position, ClientSize, false);
            base.OnMouseMove(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Left) _menu.Pointer(MouseState.Position, ClientSize, true);
            base.OnMouseDown(e);
        }

        protected override void OnFileDrop(FileDropEventArgs e)
        {
            if (e.FileNames.Length == 1)
            {
                try { PcRom.Import(e.FileNames[0]); _menu.Status = "ROM IMPORTADA / PRONTO PARA INICIAR"; }
                catch (Exception error) { _menu.Status = error.Message; }
            }
            base.OnFileDrop(e);
        }
    }
}
