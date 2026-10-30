using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using MphRead.Entities;
using MphRead.Formats;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace MphRead
{
    internal static class PcStartupVerification
    {
        public static bool Active { get; private set; }
        private static string _output = "";
        private static readonly List<string> _checks = new();
        private static readonly List<object> _movies = new();
        private static readonly HashSet<string> _captures = new();
        private static int _phase, _titleFrames, _gameFrames, _menuFrames, _postMovieFrames;
        private static int _openingCount, _storyCount, _landingCount;
        private static bool _resume;
        private static bool _arrivalCameraSeen;
        private static string _otherSaveHash = "";
        private static readonly Stopwatch _movieTimer = new();
        private static Movie _timedMovie = Movie.None;
        private static int _resizeStage;
        private static bool _qualityChecked;
        private static object? _graphics;

        public static void Run(string output)
        {
            string previousDirectory = Environment.CurrentDirectory, previousRoot = PcRuntime.StateRoot;
            PcProfile previousProfile = PcRuntime.Profile;
            _output = output; Directory.CreateDirectory(output);
            string state = Path.Combine(output, "state-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(state);
            string paths = "0.35.0.0\n" + String.Join("\n", Paths.AllPaths.Select(p => p.Key + "=" + (p.Value.Length == 0 ? "" : Path.GetFullPath(p.Value, previousDirectory))));
            File.WriteAllText(Path.Combine(state, "paths.txt"), paths);
            Environment.CurrentDirectory = PcRuntime.StateRoot = state;
            PcRuntime.Profile = new PcProfile { MusicVolume = 0, SoundVolume = 0 };
            PcRuntime.Replay = new PcReplayInput();
            _checks.Clear(); _movies.Clear(); _captures.Clear();
            _phase = _titleFrames = _gameFrames = _menuFrames = _openingCount = _storyCount = _landingCount = 0;
            bool accelerated = Environment.GetEnvironmentVariable("HUNTERSPC_VERIFY_REALTIME") != "1";
            _resume = false; Active = true; PcRuntime.AccelerateMovieVerification = accelerated;
            _timedMovie = Movie.None;
            _resizeStage = 0; _qualityChecked = false;
            _graphics = null;
            _postMovieFrames = 0; _arrivalCameraSeen = false;
            PcRuntime.MovieEnded = (movie, expected, played, skipped) =>
            {
                Check(!skipped && expected == played, $"Complete {movie}: {played}/{expected} frames, no skip");
                double seconds = _movieTimer.Elapsed.TotalSeconds;
                if (!accelerated) Check(seconds >= expected / 15.0 - 0.15, $"Encoded duration respected: {movie}");
                _movies.Add(new { Movie = movie.ToString(), ExpectedFrames = expected, PresentedFrames = played, Skipped = skipped, Seconds = seconds });
                _timedMovie = Movie.None;
                if (movie == Movie.Opening) _openingCount++;
                if (movie == Movie.StoryIntro) _storyCount++;
                if (movie == Movie.CALanding) _landingCount++;
            };
            string? failure = null;
            try
            {
                Menu.SaveSlot = 3; GameState.Reset();
                GameState.StorySave.CheckpointRoomId = 45;
                GameState.StorySave.SetVisitedRoom(45);
                GameState.CommitSave(); Menu.SaveSlot = 0;
                _otherSaveHash = SaveHash(3);
                using (var menu = new PcFrontEnd())
                {
                    menu.Run();
                    Check(menu.StartGame && !menu.Practice && menu.SaveSlot == 1, "New game receives the chosen slot, not a hard-coded save");
                }
                Check(!File.Exists(PcSaveCatalog.PathFor(state, 1)), "Title, file selection and settings never create a campaign save");
                PcBootstrap.RunGame(45, practice: false, saveSlot: 1);
                Check(_storyCount == 1 && _landingCount == 1, "New campaign plays the prologue followed by the landing movie");
                Check(SaveHash(3) == _otherSaveHash, "New game in slot 1 preserves the existing slot 3 byte for byte");
                _resume = true; _gameFrames = _postMovieFrames = 0;
                PcBootstrap.RunGame(45, practice: false, saveSlot: 3);
                Check(_storyCount == 1 && _landingCount == 2, "Continue loads its own slot without replaying the new-game prologue");
                _phase = 7;
                Menu.SaveSlot = 0;
                using (var menu = new PcFrontEnd(playOpening: false)) menu.Run();
                Check(_openingCount == 1 && SaveHash(3) == _otherSaveHash, "Returning to title preserves saves and does not replay the opening");
            }
            catch (Exception error) { failure = error.ToString(); throw; }
            finally
            {
                File.WriteAllText(Path.Combine(output, "startup-verification.json"), JsonSerializer.Serialize(new
                {
                    Success = failure == null, Failure = failure, UsesDesktopInput = false, VisibleWindows = false,
                    AcceleratedMovieClock = accelerated, Checks = _checks, Movies = _movies, Graphics = _graphics, Captures = _captures, StateDirectory = state
                }, new JsonSerializerOptions { WriteIndented = true }));
                Active = false; PcRuntime.AccelerateMovieVerification = false; PcRuntime.MovieEnded = null;
                PcRuntime.Replay = null; PcRuntime.StateRoot = previousRoot; PcRuntime.Profile = previousProfile;
                Environment.CurrentDirectory = previousDirectory;
            }
        }
        private static string SaveHash(byte slot) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(PcSaveCatalog.PathFor(PcRuntime.StateRoot, slot))));
        private static void Check(bool condition, string description)
        {
            if (!condition) throw new InvalidOperationException(description);
            _checks.Add(description); System.Console.WriteLine("PASS " + description);
        }
        private static void Capture(OpenTK.Windowing.Desktop.GameWindow window, string name)
        {
            if (!_captures.Add(name)) return;
            PcCapture.Save(window.FramebufferSize, _output, name + ".png");
            Check(File.ReadAllText(Path.Combine(_output, "gpu.txt")).Contains("GL error: NoError"), "OpenGL: " + name);
        }
        private static Vector2 WindowPoint(Vector2 point, Vector2i size)
        {
            float scale = Math.Min(size.X / 1280f, size.Y / 720f);
            return point * scale + new Vector2((size.X - 1280 * scale) / 2, (size.Y - 720 * scale) / 2);
        }
        private static void ObserveMovie(Scene? scene)
        {
            if (scene?.MoviePlaying == true && scene.CurrentMovie != _timedMovie)
            {
                _timedMovie = scene.CurrentMovie;
                _movieTimer.Restart();
            }
        }
        private static void CheckMovieViewport(PcFrontEnd menu, string name)
        {
            int[] viewport = new int[4]; GL.GetInteger(GetPName.Viewport, viewport);
            var size = menu.FramebufferSize;
            Check(menu.OpeningScene!.Size == size && viewport[0] == 0 && viewport[1] == 0
                && viewport[2] == size.X && viewport[3] == size.Y, "Movie covers the complete drawable: " + name);
            byte[] pixel = new byte[4];
            foreach (var point in new[] { new Vector2i(2, 2), new Vector2i(size.X - 3, size.Y - 3) })
            {
                GL.ReadPixels(point.X, point.Y, 1, 1, PixelFormat.Rgba, PixelType.UnsignedByte, pixel);
                Check(pixel[0] < 3 && pixel[1] < 3 && pixel[2] < 3, "No white frame at movie boundary: " + name);
            }
            Capture(menu, name);
        }
        private static void ResizeMovie(PcFrontEnd menu)
        {
            if (menu.OpeningScene?.CurrentMovie != Movie.Opening || menu.OpeningScene.MovieFrameIndex < 320 || _resizeStage >= 4) return;
            if (_resizeStage > 0) CheckMovieViewport(menu, new[] { "", "opening-1920x1080", "opening-2560x1080", "opening-1024x768" }[_resizeStage]);
            var size = _resizeStage switch { 0 => new Vector2i(1920,1080), 1 => new Vector2i(2560,1080), 2 => new Vector2i(1024,768), _ => new Vector2i(1280,720) };
            menu.ClientSize = size;
            // Reproduce an overlay/fullscreen transition restoring a stale GL viewport.
            GL.Viewport(137, 166, 1280, 720);
            _resizeStage++;
        }
        public static void AfterMenuFrame(PcFrontEnd menu)
        {
            if (!Active) return;
            if (++_menuFrames > 30000) throw new TimeoutException("Startup replay did not finish.");
            if (menu.IsVisible || menu.CursorState == CursorState.Grabbed) throw new InvalidOperationException("Startup test touched the desktop.");
            ObserveMovie(menu.OpeningScene);
            if (menu.OpeningScene?.CurrentMovie == Movie.Opening && menu.OpeningScene.MovieFrameIndex >= 320) Capture(menu, "opening-original");
            ResizeMovie(menu);
            if (_phase == 0)
            {
                if (menu.Flow.Page != PcStartupPage.Title || ++_titleFrames < 2) return;
                Check(_openingCount == 1, "Full opening returns to the title screen"); Capture(menu, "title");
                menu.HandleMenuKey(Keys.F2);
                Check(menu.Flow.Page == PcStartupPage.Files && !menu.StartGame, "Any title key opens file selection without starting gameplay");
                _phase = 1;
            }
            else if (_phase == 1) { Capture(menu, "files-1280x720"); menu.ClientSize = new(1024, 768); _phase = 2; }
            else if (_phase == 2)
            {
                Capture(menu, "files-1024x768"); menu.HandleMenuPointer(WindowPoint(new(400, 505), menu.ClientSize), true);
                Check(menu.Flow.Page == PcStartupPage.Menu && menu.SaveSlot == 3 && !menu.StartGame, "Scaled mouse click selects the correct existing file");
                _phase = 3;
            }
            else if (_phase == 3)
            {
                Capture(menu, "slot3-menu"); menu.HandleMenuKey(Keys.Down); menu.HandleMenuKey(Keys.Down);
                menu.HandleMenuKey(Keys.Enter); menu.HandleMenuKey(Keys.Right); _phase = 4;
            }
            else if (_phase == 4)
            {
                Capture(menu, "settings"); Check(PcRuntime.Profile.MouseSensitivity == 1.1f, "Settings remain accessible from the selected file");
                for (int i = 0; i < 6; i++) menu.HandleMenuKey(Keys.Down);
                menu.HandleMenuKey(Keys.Enter);
                Check(!PcRuntime.Profile.EnhancedGraphics && !PcProfile.Load(PcRuntime.ProfilePath).EnhancedGraphics, "Original graphics can be selected and persisted from the menu");
                menu.HandleMenuPointer(WindowPoint(new(400, 520), menu.ClientSize), true);
                Check(PcRuntime.Profile.EnhancedGraphics && PcProfile.Load(PcRuntime.ProfilePath).EnhancedGraphics, "Scaled mouse click restores enhanced graphics");
                menu.HandleMenuKey(Keys.Escape); menu.HandleMenuKey(Keys.Escape); menu.HandleMenuKey(Keys.Down); menu.HandleMenuKey(Keys.Enter); _phase = 5;
            }
            else if (_phase == 5) { Capture(menu, "new-game-menu"); menu.HandleMenuKey(Keys.Enter); _phase = 6; }
            else if (_phase == 7)
            {
                Check(menu.Flow.Page == PcStartupPage.Title && !menu.StartGame, "Exiting a session returns to the title screen");
                Capture(menu, "return-to-title"); menu.Close(); _phase = 8;
            }
        }
        public static void BeforeGameFrame(RenderWindow game)
        {
            if (!Active) return;
            PcRuntime.Replay!.Set();
            // A held launch click must not skip either of the new-game movies.
            if (!_resume && _gameFrames < 120) PcRuntime.Replay.PressMouse(MouseButton.Left);
        }
        public static void AfterGameFrame(RenderWindow game)
        {
            if (!Active) return;
            if (++_gameFrames > 18000) throw new TimeoutException("Campaign intro did not return to gameplay.");
            if (game.IsVisible || game.CursorState == CursorState.Grabbed) throw new InvalidOperationException("Game test touched the desktop.");
            ObserveMovie(game.Scene);
            if (game.Scene.MoviePlaying && game.Scene.MovieFrameIndex >= 15)
                Capture(game, (_resume ? "continue-" : "new-") + game.Scene.CurrentMovie);
            bool complete = _resume ? _landingCount == 2 : _storyCount == 1 && _landingCount == 1;
            if (complete && !game.Scene.MoviePlaying)
            {
                _postMovieFrames++;
                if (!_resume && CameraSequence.Current?.Name == "unit2_land_intro")
                {
                    _arrivalCameraSeen = true;
                    if (_postMovieFrames >= 120) Capture(game, "room-arrival-camera");
                }
            }
            if (complete && !game.Scene.MoviePlaying && _postMovieFrames >= 240 && game.Scene.ActiveCutscene == -1
                && CameraSequence.Current?.Name != "unit2_land_intro" && game.Scene.FadeType == FadeType.None)
            {
                if (!_resume) Check(_arrivalCameraSeen, "The native room-arrival camera ran to completion after the videos");
                if (!_resume && !_qualityChecked)
                {
                    PcRuntime.Profile.EnhancedGraphics = false; game.Scene.OnRenderFrame(); Capture(game, "graphics-original");
                    PcRuntime.Profile.EnhancedGraphics = true; game.Scene.OnRenderFrame(); Capture(game, "graphics-enhanced");
                    Check(game.Scene.TextureFiltering.MipmappedTextures > 0, "World textures have mipmap chains");
                    Check(game.Scene.TextureFiltering.MaxAnisotropy is >= 1 and <= 16, "Anisotropy respects GPU support and the 16X limit");
                    int oldTexture = GL.GetInteger(GetPName.TextureBinding2D);
                    GL.BindTexture(TextureTarget.Texture2D, game.Scene.TextureFiltering.LastModelTexture);
                    GL.GetTexParameter(TextureTarget.Texture2D, GetTextureParameter.TextureMinFilter, out int minFilter);
                    GL.GetTexParameter(TextureTarget.Texture2D, GetTextureParameter.TextureMagFilter, out int magFilter);
                    GL.GetTexLevelParameter(TextureTarget.Texture2D, 1, GetTextureParameter.TextureWidth, out int mipWidth);
                    float anisotropy = 1;
                    if (game.Scene.TextureFiltering.MaxAnisotropy > 1)
                        GL.GetTexParameter(TextureTarget.Texture2D, (GetTextureParameter)0x84FE, out anisotropy);
                    GL.BindTexture(TextureTarget.Texture2D, oldTexture);
                    Check(minFilter == (int)TextureMinFilter.LinearMipmapLinear && magFilter == (int)TextureMagFilter.Linear && mipWidth > 0, "GPU model sampler uses trilinear mipmaps and linear magnification");
                    Check(anisotropy == game.Scene.TextureFiltering.MaxAnisotropy, "GPU sampler applies the supported anisotropy");
                    _graphics = new { Anisotropy = anisotropy, GeneratedMipmaps = game.Scene.TextureFiltering.MipmappedTextures,
                        MinFilter = "LinearMipmapLinear", MagFilter = "Linear", EdgeAntialiasing = true };
                    Check(!File.ReadAllBytes(Path.Combine(_output, "graphics-original.png")).SequenceEqual(File.ReadAllBytes(Path.Combine(_output, "graphics-enhanced.png"))), "Graphics toggle changes the same frozen scene");
                    _qualityChecked = true;
                }
                Check(Menu.SaveSlot == (_resume ? 3 : 1) && game.Scene.RoomId == 45, "Campaign reaches the selected save's room");
                Capture(game, _resume ? "continue-room" : "new-game-room"); game.Close();
            }
        }
    }
}
