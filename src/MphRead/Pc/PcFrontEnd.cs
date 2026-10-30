using System;
using MphRead.Sound;
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
        private readonly PcStartupArt _art = new();
        internal PcStartupFlow Flow { get; }
        public bool StartGame { get; private set; }
        public bool Practice { get; private set; }
        public byte SaveSlot => Flow.SaveSlot;
        private Scene? _opening;
        private bool _openingStarted, _artLoaded, _capture;
        private double _time;
        private int _file;
        private readonly PcSaveSlot[] _slots = new PcSaveSlot[3];
        private string _status = "";
        private readonly string? _smokeOutput;
        private int _smokeFrame;
        internal Scene? OpeningScene => _opening;

        public PcFrontEnd(string? smokeOutput = null, bool playOpening = true) : base(new GameWindowSettings { UpdateFrequency = 60 }, new NativeWindowSettings
        {
            ClientSize = new Vector2i(1280, 720), Title = "Metroid Prime Hunters PC",
            Profile = ContextProfile.Compatability, Flags = ContextFlags.Default, APIVersion = new Version(3, 2),
            StartVisible = smokeOutput == null && !PcStartupVerification.Active
        })
        {
            _smokeOutput = smokeOutput;
            Flow = new PcStartupFlow(PcRom.Ready && playOpening && smokeOutput == null);
            _file = PcRuntime.Profile.LastSaveSlot - 1;
            RefreshSlots();
            VSync = PcRuntime.AccelerateMovieVerification ? VSyncMode.Off : VSyncMode.On;
            if (PcRuntime.AccelerateMovieVerification) UpdateFrequency = 0;
            if (PcRuntime.Profile.Fullscreen && smokeOutput == null && !PcStartupVerification.Active) WindowState = WindowState.Fullscreen;
            _menu = new PcMenu(false, () => Start(false), () => Flow.Back(), ToggleFullscreen, () => Start(true),
                startLabel: () => SelectedSave().HasProgress ? "CONTINUAR AVENTURA" : "NOVA AVENTURA");
        }
        private void RefreshSlots() { for (byte slot = 1; slot <= 3; slot++) _slots[slot - 1] = PcSaveCatalog.Read(PcRuntime.StateRoot, slot); }
        private PcSaveSlot SelectedSave() => _slots[Flow.SaveSlot == 0 ? _file : Flow.SaveSlot - 1];

        protected override void OnLoad()
        {
            PcPresentation.Sync(this, null);
            if (PcRom.Ready && _smokeOutput == null) StartPresentation();
            base.OnLoad();
        }
        private void LoadArt() { if (!_artLoaded) { _art.Load(); _artLoaded = true; } }
        private void StartPresentation()
        {
            Menu.SaveSlot = 0;
            _opening = new Scene(FramebufferSize, KeyboardState, MouseState, _ => { }, Close);
            Sfx.Load(_opening); GameState.Mode = GameMode.Unknown15;
            if (Flow.Page == PcStartupPage.Opening)
                _opening.StartMovie(Movie.Opening, FadeType.FadeOutInBlack, 0, FadeType.FadeOutBlack, 0, afterMovieAction: AfterMovie.StartGame);
            _opening.OnLoad(); PcRuntime.Apply();
            if (Flow.Page != PcStartupPage.Opening) { LoadArt(); PlayTitleMusic(); }
        }
        private static void PlayTitleMusic() => Sfx.Instance.PlayFreeStream((int)VoiceId.STRM_TITLE_SCREEN);
        private void Start(bool practice)
        {
            RefreshSlots();
            if (!PcRom.Ready) { _status = "IMPORTE SUA ROM USA REV 0 OU EUROPA REV 1 AQUI"; return; }
            if (!practice && SelectedSave().Damaged) { _status = "ARQUIVO ILEGIVEL. O ORIGINAL FOI PRESERVADO."; return; }
            Practice = practice; StartGame = true; Close();
        }
        private void ToggleFullscreen()
        {
            PcRuntime.Profile.Fullscreen = WindowState != WindowState.Fullscreen;
            WindowState = PcRuntime.Profile.Fullscreen ? WindowState.Fullscreen : WindowState.Normal;
        }
        protected override void OnRenderFrame(FrameEventArgs args)
        {
            if (!PcPresentation.Sync(this, _opening)) return;
            _time += args.Time;
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            if (Flow.Page == PcStartupPage.Opening && _opening != null)
            {
                GameState.ApplyPause(); _opening.OnUpdateFrame(); _opening.OnRenderFrame(); _opening.AfterRenderFrame();
                _openingStarted |= _opening.MoviePlaying;
                if (_openingStarted && !_opening.MoviePlaying)
                {
                    LoadArt();
                    Flow.FinishOpening();
                    PlayTitleMusic();
                }
                if (Flow.Page == PcStartupPage.Opening) PcStartupArt.SkipHint(FramebufferSize, "ESC  PULAR ABERTURA");
            }
            else { if (_opening != null) Sfx.Update((float)args.Time); DrawPage(); }
            if (_smokeOutput != null) SmokeFrame();
            PcStartupVerification.AfterMenuFrame(this);
            if (_capture) { PcCapture.Save(FramebufferSize); _capture = false; }
            SwapBuffers(); base.OnRenderFrame(args);
        }
        private void DrawPage()
        {
            if (Flow.Page == PcStartupPage.Menu && _menu.IsSettings) { _menu.Draw(FramebufferSize); return; }
            using (var canvas = new PcCanvas(FramebufferSize))
            {
                PcStartupArt.Background(_time);
                if (Flow.Page == PcStartupPage.Title)
                {
                    _art.Logo(240, 120, 800);
                    float light = 0.65f + 0.25f * (float)Math.Sin(_time * 2);
                    PcCanvas.Text(310, 536, "CLIQUE OU PRESSIONE QUALQUER TECLA", 3.2f, new Color4(light, light, light, 1));
                    PcCanvas.Text(500, 654, "(C) 2006 NINTENDO", 2, PcCanvas.Muted);
                    if (!PcRom.Ready) PcCanvas.Text(220, 602, "ARRASTE SUA ROM .NDS OU ZIP PARA ESTA JANELA", 2.1f, PcCanvas.Amber);
                }
                else
                {
                    _art.Logo(884, 20, 340);
                    PcCanvas.Text(72, 85, Flow.Page == PcStartupPage.Files ? "SELECIONE UM ARQUIVO" : $"ARQUIVO {Flow.SaveSlot:00}", 3.4f, PcCanvas.White);
                    PcCanvas.Rect(72, 158, 1136, 2, PcCanvas.Muted);
                    if (Flow.Page == PcStartupPage.Files) DrawFiles();
                    else
                    {
                        var save = SelectedSave();
                        PcCanvas.Text(760, 340, PcText.Ellipsis(save.Location, 31), 2.2f, PcCanvas.White);
                        PcCanvas.Text(760, 386, $"OCTOLITOS  {save.Octoliths} / 8", 2.1f, PcCanvas.Amber);
                        PcCanvas.Text(760, 420, $"SCANS      {save.Scans}", 2.1f, PcCanvas.Muted);
                    }
                    PcCanvas.Rect(72, 626, 1136, 1, PcCanvas.Muted);
                    PcCanvas.Text(72, 654, "WASD / SETAS  NAVEGAR    ENTER  SELECIONAR    ESC  VOLTAR", 1.9f, PcCanvas.Muted);
                }
                if (_status.Length != 0) PcCanvas.Text(72, 686, PcText.Ellipsis(_status, 110), 1.6f, PcCanvas.Amber);
            }
            if (Flow.Page == PcStartupPage.Menu) _menu.Draw(FramebufferSize, backdrop: false);
        }
        private void DrawFiles()
        {
            for (byte slot = 1; slot <= 3; slot++)
            {
                var save = _slots[slot - 1];
                float y = 222 + (slot - 1) * 118; bool selected = _file == slot - 1;
                PcCanvas.Rect(72, y, 1136, 104, selected ? new Color4(0.16f, 0.23f, 0.27f, 1) : new Color4(0.06f, 0.09f, 0.12f, 1));
                PcCanvas.Rect(72, y, 5, 104, selected ? PcCanvas.Amber : PcCanvas.Muted);
                PcCanvas.Text(106, y + 24, $"ARQUIVO {slot:00}", 2.8f, selected ? PcCanvas.White : PcCanvas.Muted);
                PcCanvas.Text(382, y + 22, PcText.Ellipsis(save.Location, 47), 2.2f, save.Damaged ? PcCanvas.Amber : PcCanvas.White);
                PcCanvas.Text(382, y + 60, !save.Exists ? "NOVO JOGO" : $"OCTOLITOS {save.Octoliths}/8     SCANS {save.Scans}", 1.9f, PcCanvas.Muted);
            }
        }
        internal void HandleMenuKey(Keys key)
        {
            if (Flow.Page == PcStartupPage.Opening) { if (key == Keys.Escape) _opening?.SkipMovie(); return; }
            if (Flow.Page == PcStartupPage.Title)
            {
                if (PcRom.Ready || _smokeOutput != null) { RefreshSlots(); Flow.PressTitle(); _status = ""; }
                else _status = "IMPORTE SUA ROM PARA ABRIR OS ARQUIVOS";
                return;
            }
            if (Flow.Page == PcStartupPage.Menu) { _menu.Key(key); return; }
            if (key is Keys.Escape or Keys.Backspace) Flow.Back();
            else if (key is Keys.W or Keys.Up) _file = (_file + 2) % 3;
            else if (key is Keys.S or Keys.Down) _file = (_file + 1) % 3;
            else if (key is Keys.Enter or Keys.Space) SelectFile();
        }
        private void SelectFile()
        {
            byte slot = (byte)(_file + 1);
            RefreshSlots();
            if (_slots[slot - 1].Damaged) { _status = "ARQUIVO ILEGIVEL. O ORIGINAL FOI PRESERVADO."; return; }
            Flow.SelectFile(slot); _menu.ResetMain(); PcRuntime.Profile.LastSaveSlot = slot; PcRuntime.Profile.Save(PcRuntime.ProfilePath); _status = "";
        }
        internal void HandleMenuPointer(Vector2 pointer, bool click)
        {
            if (Flow.Page == PcStartupPage.Title) { if (click) HandleMenuKey(Keys.Enter); return; }
            if (Flow.Page == PcStartupPage.Menu) { _menu.Pointer(pointer, ClientSize, click); return; }
            if (Flow.Page != PcStartupPage.Files) return;
            int row = PcStartupLayout.HitFile(PcLayout.ToCanvas(pointer, ClientSize));
            if (row < 0) return; _file = row; if (click) SelectFile();
        }
        private void SmokeFrame()
        {
            if (IsVisible || CursorState == CursorState.Grabbed) throw new InvalidOperationException("Teste deve ficar oculto, com cursor livre.");
            if (_smokeFrame == 2)
            {
                PcSmokeCheck.CheckPresentationShader();
                PcCapture.Save(FramebufferSize, _smokeOutput, "menu.png"); HandleMenuKey(Keys.Enter); HandleMenuKey(Keys.Enter);
                HandleMenuKey(Keys.Down); HandleMenuKey(Keys.Down); HandleMenuKey(Keys.Enter); HandleMenuKey(Keys.Right);
            }
            if (_smokeFrame == 5) { PcCapture.Save(FramebufferSize, _smokeOutput, "settings.png"); HandleMenuKey(Keys.Escape); ClientSize = new(1024, 768); }
            if (_smokeFrame == 8) { PcCapture.Save(FramebufferSize, _smokeOutput, "menu-1024x768.png"); Close(); }
            _smokeFrame++;
        }
        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (e.IsRepeat) return;
            if (e.Key == Keys.F11) { ToggleFullscreen(); PcRuntime.Profile.Save(PcRuntime.ProfilePath); }
            else if (e.Key == Keys.F12) _capture = true; else HandleMenuKey(e.Key);
            base.OnKeyDown(e);
        }
        protected override void OnMouseMove(MouseMoveEventArgs e) { HandleMenuPointer(MouseState.Position, false); base.OnMouseMove(e); }
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (Flow.Page == PcStartupPage.Title || e.Button == MouseButton.Left) HandleMenuPointer(MouseState.Position, true);
            base.OnMouseDown(e);
        }
        protected override void OnResize(ResizeEventArgs e)
        {
            PcPresentation.Sync(this, _opening);
            base.OnResize(e);
        }
        protected override void OnUnload() { _opening?.DoCleanup(); _art.Dispose(); base.OnUnload(); }
        protected override void OnFileDrop(FileDropEventArgs e)
        {
            if (e.FileNames.Length == 1)
            {
                try { PcRom.Import(e.FileNames[0]); LoadArt(); _status = "ROM IMPORTADA / CLIQUE PARA INICIAR"; }
                catch (Exception error) { _status = error.Message; }
            }
            base.OnFileDrop(e);
        }
    }
    public static class PcStartupLayout
    {
        public static int HitFile(Vector2 point)
        {
            if (point.X < 72 || point.X >= 1208 || point.Y < 222) return -1;
            int row = (int)((point.Y - 222) / 118);
            return row < 3 && point.Y < 222 + row * 118 + 104 ? row : -1;
        }
    }
}
