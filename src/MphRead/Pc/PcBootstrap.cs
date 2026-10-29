using System;
using System.IO;
using System.Linq;
using MphRead.Entities;

namespace MphRead
{
    internal static class PcBootstrap
    {
        public static bool TryRun(string[] args)
        {
            if (args.Length != 0 && !args[0].StartsWith("--pc", StringComparison.Ordinal)) return false;
            PcRuntime.Enabled = true;
            PcRuntime.Profile = PcProfile.Load(PcRuntime.ProfilePath);
            try
            {
                if (args.Length == 2 && args[0] == "--pc-import")
                {
                    PcRom.Import(args[1]);
                    Console.WriteLine("ROM validada e importada localmente.");
                    return true;
                }
                Paths.UpdatePaths();
                Paths.ChooseMphPath();
                if (OperatingSystem.IsLinux())
                    OpenTK.Windowing.GraphicsLibraryFramework.GLFW.InitHint(
                        OpenTK.Windowing.GraphicsLibraryFramework.InitHintPlatform.Platform,
                        OpenTK.Windowing.GraphicsLibraryFramework.Platform.X11);
                if (args.Length == 2 && args[0] == "--pc-smoke")
                {
                    PcSmokeCheck.Run(args[1]);
                    return true;
                }
                if (args.Length == 2 && args[0] == "--pc-verify")
                {
                    if (!PcRom.Ready) throw new InvalidDataException("Importe a ROM antes de verificar o jogo.");
                    PcVerification.Run(Path.GetFullPath(args[1]));
                    return true;
                }
                // Direct room launch is useful for repeatable gameplay validation.
                if (args.Length == 2 && args[0] == "--pc-room")
                {
                    if (!PcRom.Ready) throw new InvalidDataException("Importe a ROM antes de iniciar uma sala.");
                    RunGame(Int32.Parse(args[1]), practice: true);
                    return true;
                }
                if (args.Length != 0 && !args.SequenceEqual(new[] { "--pc" }))
                    throw new ArgumentException("Use --pc, --pc-import <rom.nds ou zip>, --pc-verify <pasta>, --pc-smoke <pasta>, --pc-room <id> ou --legacy.");
                while (true)
                {
                    bool start;
                    bool practice;
                    using (var menu = new PcFrontEnd())
                    {
                        menu.Run();
                        start = menu.StartGame;
                        practice = menu.Practice;
                    }
                    if (!start) break;
                    RunGame(practice ? 95 : 45, practice);
                }
            }
            catch (Exception e)
            {
                File.WriteAllText("hunters-error.log", e.ToString());
                Console.Error.WriteLine(e.Message);
                Environment.ExitCode = 1;
            }
            return true;
        }

        private static void RunGame(int roomId, bool practice)
        {
            Cheats.FreeWeaponSelect = false;
            Menu.SaveSlot = practice ? (byte)0 : (byte)1;
            Menu.SaveFromExit = SaveWhen.Never;
            Menu.SaveFromShip = SaveWhen.Always;
            Menu.NeededSave = SaveWhen.Never;
            using var game = new RenderWindow();
            if (!practice)
            {
                int checkpoint = GameState.StorySave.CheckpointRoomId;
                var metadata = Metadata.GetRoomById(checkpoint, noThrow: true);
                if (metadata != null && !metadata.Multiplayer && !metadata.FirstHunt) roomId = checkpoint;
            }
            game.AddPlayer(Hunter.Samus);
            game.AddRoom(roomId, practice ? GameMode.Battle : GameMode.SinglePlayer);
            PcRuntime.Apply();
            game.Run();
            if (!practice && Menu.NeededSave == SaveWhen.Always) GameState.CommitSave();
        }
    }
}
