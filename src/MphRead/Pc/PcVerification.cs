using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MphRead.Entities;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace MphRead
{
    internal static class PcVerification
    {
        public static bool Active { get; private set; }
        private static string _output = "";
        private static string _scenario = "";
        private static int _frame;
        private static bool _arena;
        private static readonly List<string> _checks = new();
        private static readonly List<object> _captures = new();
        private static Vector3 _startPosition;
        private static Vector3 _startFacing;
        private static Vector3 _pausedPosition;
        private static Vector3 _pausedFacing;
        private static ulong _pausedFrame;

        public static void Run(string output)
        {
            string previousDirectory = Environment.CurrentDirectory;
            string previousRoot = PcRuntime.StateRoot;
            _output = output;
            _checks.Clear();
            _captures.Clear();
            Directory.CreateDirectory(output);
            // All save/profile writes are isolated from the player's portable installation.
            Environment.CurrentDirectory = output;
            PcRuntime.StateRoot = output;
            PcRuntime.Profile = new PcProfile();
            PcRuntime.Replay = new PcReplayInput();
            Active = true;
            string? failure = null;
            try
            {
                foreach (int room in new[] { 95, 45 })
                {
                    _arena = room == 95;
                    _scenario = _arena ? "arena" : "adventure";
                    _frame = 0;
                    Menu.SaveSlot = 0;
                    Menu.SaveFromExit = SaveWhen.Never;
                    Menu.SaveFromShip = SaveWhen.Never;
                    Cheats.FreeWeaponSelect = false;
                    Cheats.SkipPlanetIntros = true;
                    using var game = new RenderWindow();
                    game.AddPlayer(Hunter.Samus);
                    game.AddRoom(room, _arena ? GameMode.Battle : GameMode.SinglePlayer);
                    PcRuntime.Apply();
                    game.Run();
                }
            }
            catch (Exception error) { failure = error.ToString(); throw; }
            finally
            {
                File.WriteAllText(Path.Combine(output, "verification.json"), JsonSerializer.Serialize(new
                {
                    Success = failure == null,
                    Failure = failure,
                    UsesDesktopInput = false,
                    VisibleWindows = false,
                    Checks = _checks,
                    Captures = _captures
                }, new JsonSerializerOptions { WriteIndented = true }));
                Active = false;
                PcRuntime.Replay = null;
                PcRuntime.StateRoot = previousRoot;
                Environment.CurrentDirectory = previousDirectory;
            }
        }

        private static void Check(bool condition, string text)
        {
            if (!condition) throw new InvalidOperationException($"{_scenario} frame {_frame}: {text}");
            _checks.Add($"{_scenario}: {text}");
        }

        public static void BeforeFrame(RenderWindow game)
        {
            if (!Active) return;
            var replay = PcRuntime.Replay!;
            replay.Set();
            if (_arena && _frame is >= 20 and < 50) replay.PressMouse(MouseButton.Left);
            if (_arena && _frame is >= 80 and < 104) replay.Set(new Vector2(2, 0), Keys.W);
            if (_frame is >= 120 and < 150) replay.Set(new Vector2(4000, 4000), Keys.W);
            if (_frame is 150 or 151) replay.Set(new Vector2(9000, 0));
            if (_arena && _frame is 205 or 300) replay.Set(default, Keys.C);
            switch (_frame)
            {
                case 79: _startPosition = PlayerEntity.Main.Position; _startFacing = PlayerEntity.Main.FacingVector; break;
                case 120: game.HandlePcKey(Keys.M); break;
                case 130: game.HandlePcKey(Keys.W); game.HandlePcKey(Keys.Equal); break;
                case 133: game.HandlePcKey(Keys.PageUp); break;
                case 135: game.HandlePcKey(Keys.PageDown); break;
                case 150: game.HandlePcKey(Keys.M); break;
                case 160: game.HandlePcKey(Keys.J); break;
                case 164: game.HandlePcKey(Keys.D); break;
                case 168: game.HandlePcKey(Keys.A); break;
                case 170: game.HandlePcPointer(new Vector2(750, 400), false, -1); break;
                case 175: game.HandlePcKey(Keys.Tab); break;
                case 180: game.HandlePcKey(Keys.Tab); break;
                case 185: game.HandlePcKey(Keys.J); break;
                case 190: game.HandlePcKey(Keys.Escape); break;
                case 192: game.HandlePcKey(Keys.M); break;
                case 194: game.HandlePcKey(Keys.Escape); break;
                case 196: game.HandlePcKey(Keys.Escape); break;
                case 370: game.ClientSize = new Vector2i(1024, 768); game.HandlePcKey(Keys.M); break;
                case 379: game.HandlePcKey(Keys.M); break;
                case 382: game.ClientSize = new Vector2i(1920, 1080); game.HandlePcKey(Keys.J); break;
                case 390:
                    // Click at the rendered button after resizing to 1920x1080.
                    game.HandlePcPointer(new Vector2(1120, 52) * 1.5f, true);
                    break;
            }
        }

        public static void AfterFrame(RenderWindow game)
        {
            if (!Active) return;
            if (game.IsVisible) throw new InvalidOperationException("Verification window became visible.");
            var player = PlayerEntity.Main;
            switch (_frame)
            {
                case 60: Capture(game, "hud-1280x720"); break;
                case 110:
                    if (_arena)
                    {
                        Check((player.Position - _startPosition).Length > 0.05f, "WASD moves the player through native input bindings");
                        Check((player.FacingVector - _startFacing).Length > 0.01f, "Mouse deltas turn the native first-person camera");
                    }
                    break;
                case 121:
                    Check(game.PcView == PcFieldView.Map, "M opens the map in the gameplay window");
                    Check(game.PcMap?.PolygonCount > 0, "Map contains collision geometry from the ROM");
                    _pausedPosition = player.Position; _pausedFacing = player.FacingVector; _pausedFrame = game.Scene.FrameCount;
                    break;
                case 125: Capture(game, "map"); break;
                case 140:
                    Check(game.Scene.FrameCount == _pausedFrame && player.Position == _pausedPosition && player.FacingVector == _pausedFacing,
                        "Map pauses simulation and blocks movement/large mouse deltas");
                    Capture(game, "map-zoom-pan");
                    break;
                case 151:
                    Check(game.PcView == PcFieldView.None, "M closes the map");
                    Check((player.FacingVector - _pausedFacing).Length < 0.01f, "Returning from the map discards recapture mouse jumps");
                    break;
                case 161:
                    Check(game.PcView == PcFieldView.Journal && game.PcJournal?.UnlockedCount >= 9, "J shows unlocked ROM scan entries");
                    Capture(game, "journal-equipment");
                    _pausedFrame = game.Scene.FrameCount;
                    break;
                case 165: Capture(game, "journal-bioforms"); break;
                case 171:
                    Check(game.Scene.FrameCount == _pausedFrame, "Reading and scrolling the journal keeps simulation paused");
                    Capture(game, "journal-reading");
                    break;
                case 176: Check(game.PcView == PcFieldView.Map, "Tab switches journal to map without resuming gameplay"); break;
                case 181: Check(game.PcView == PcFieldView.Journal, "Tab switches map to journal"); break;
                case 191: Capture(game, "pause-menu"); break;
                case 193: Check(game.PcView == PcFieldView.Map, "Map can be opened from pause"); break;
                case 195: Check(game.PcView == PcFieldView.Pause, "Esc returns a pause-opened map to its parent menu"); break;
                case 197: Check(game.PcView == PcFieldView.None, "Esc resumes from pause"); break;
                case 206:
                    if (_arena) Check(player.IsMorphing || player.IsAltForm, $"C starts the native morph animation (flags {player.Flags1}/{player.Flags2})");
                    break;
                case 280:
                    if (_arena) { Check(player.IsAltForm, "C activates Morph Ball through native bindings"); Capture(game, "hud-morph"); }
                    break;
                case 360:
                    if (_arena) Check(!player.IsAltForm, "C returns to first-person gameplay");
                    Capture(game, "hud-after-panels");
                    break;
                case 375: Capture(game, "map-1024x768"); break;
                case 386: Capture(game, "journal-1920x1080"); break;
                case 392:
                    Check(game.PcView == PcFieldView.None, "Mouse close button follows the scaled journal layout");
                    Check(!game.IsVisible && game.CursorState != OpenTK.Windowing.Common.CursorState.Grabbed,
                        "Verification stayed hidden and never captured the desktop cursor");
                    game.Close();
                    break;
            }
            _frame++;
        }

        private static void Capture(RenderWindow game, string name)
        {
            string image = PcCapture.Save(game.ClientSize, _output, $"{_scenario}-{name}.png");
            string error = GL.GetError().ToString();
            // PcCapture records GL.GetError in gpu.txt before clearing it.
            string gpu = File.ReadAllText(Path.Combine(_output, "gpu.txt"));
            Check(gpu.Contains("GL error: NoError") && error == "NoError", $"OpenGL render passes: {name}");
            var player = PlayerEntity.Main;
            _captures.Add(new
            {
                File = image, Scenario = _scenario, Frame = _frame, SimulatedFrames = game.Scene.FrameCount,
                View = game.PcView.ToString(), Room = game.Scene.RoomId,
                Position = new[] { player.Position.X, player.Position.Y, player.Position.Z },
                Facing = new[] { player.FacingVector.X, player.FacingVector.Y, player.FacingVector.Z },
                IsAltForm = player.IsAltForm, Health = player.Health
            });
        }
    }
}
