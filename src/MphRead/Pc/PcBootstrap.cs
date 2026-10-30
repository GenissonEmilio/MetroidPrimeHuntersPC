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
                if (args.Length == 2 && args[0] == "--pc-verify-startup")
                {
                    if (!PcRom.Ready) throw new InvalidDataException("Importe a ROM antes de verificar a abertura.");
                    PcStartupVerification.Run(Path.GetFullPath(args[1]));
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
                    throw new ArgumentException("Use --pc, --pc-import <rom.nds ou zip>, --pc-verify <pasta>, --pc-verify-startup <pasta>, --pc-smoke <pasta>, --pc-room <id> ou --legacy.");
                bool playOpening = true;
                while (true)
                {
                    bool start;
                    bool practice;
                    byte saveSlot;
                    Menu.SaveSlot = 0;
                    using (var menu = new PcFrontEnd(playOpening: playOpening))
                    {
                        menu.Run();
                        start = menu.StartGame;
                        practice = menu.Practice;
                        saveSlot = menu.SaveSlot;
                    }
                    playOpening = false;
                    if (!start) break;
                    RunGame(practice ? 95 : 45, practice, saveSlot);
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

        internal static void RunGame(int roomId, bool practice, byte saveSlot = 1)
        {
            Cheats.FreeWeaponSelect = false;
            Cheats.SkipPlanetIntros = practice;
            if (!practice && saveSlot is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(saveSlot));
            bool newCampaign = !practice && !PcSaveCatalog.Read(PcRuntime.StateRoot, saveSlot).HasProgress;
            Menu.SaveSlot = practice ? (byte)0 : saveSlot;
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
            if (newCampaign)
                game.Scene.StartMovies(Movie.StoryIntro, Movie.CALanding, FadeType.FadeOutInBlack, 0,
                    FadeType.FadeOutWhite, 5 / 30f, AfterMovie.StartGame);
            PcRuntime.Apply();
            game.Run();
            if (!practice && Menu.NeededSave == SaveWhen.Always) GameState.CommitSave();
        }
    }
}
