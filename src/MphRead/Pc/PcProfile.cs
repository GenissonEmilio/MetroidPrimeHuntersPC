using System;
using System.IO;
using System.Text.Json;
using MphRead.Entities;
using MphRead.Sound;

namespace MphRead
{
    public sealed class PcProfile
    {
        public float MouseSensitivity { get; set; } = 1;
        public bool InvertY { get; set; }
        public bool ShowHints { get; set; } = true;
        public bool Fullscreen { get; set; }
        public float SoundVolume { get; set; } = 0.35f;
        public float MusicVolume { get; set; } = 0.5f;
        public byte LastSaveSlot { get; set; } = 1;

        public void Validate()
        {
            MouseSensitivity = Single.IsFinite(MouseSensitivity) ? Math.Clamp(MouseSensitivity, 0.1f, 4) : 1;
            SoundVolume = Single.IsFinite(SoundVolume) ? Math.Clamp(SoundVolume, 0, 1) : 0.35f;
            MusicVolume = Single.IsFinite(MusicVolume) ? Math.Clamp(MusicVolume, 0, 1) : 0.5f;
            if (LastSaveSlot is < 1 or > 3) LastSaveSlot = 1;
        }

        public static PcProfile Load(string path)
        {
            PcProfile result;
            try
            {
                result = File.Exists(path) ? JsonSerializer.Deserialize<PcProfile>(File.ReadAllText(path)) ?? new() : new();
            }
            catch (Exception e) when (e is JsonException || e is IOException || e is UnauthorizedAccessException)
            {
                // A broken settings file must not prevent reaching the settings screen.
                result = new();
            }
            result.Validate();
            return result;
        }

        public void Save(string path)
        {
            Validate();
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, path, overwrite: true);
        }
    }

    public static class PcRuntime
    {
        public static bool Enabled { get; set; }
        public static PcProfile Profile { get; set; } = new();
        public static string StateRoot { get; set; } = PcStorage.DataDirectory;
        public static string ProfilePath => Path.Combine(StateRoot, "pc-profile.json");
        internal static PcReplayInput? Replay { get; set; }
        internal static bool AccelerateMovieVerification { get; set; }
        internal static Action<Movie, int, int, bool>? MovieEnded { get; set; }

        public static void Apply()
        {
            Profile.Validate();
            Sfx.Volume = Profile.SoundVolume;
            Music.UserVolume = Profile.MusicVolume;
            if (PlayerEntity.Players.Count > PlayerEntity.MainPlayerIndex && PlayerEntity.Main != null)
            {
                PlayerEntity.Main.Controls.InvertAimY = Profile.InvertY;
                PlayerEntity.Main.Controls.ScrollAllWeapons = true;
            }
        }
    }
}
