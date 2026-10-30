using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;

namespace MphRead
{
    public sealed record PcSaveSlot(byte Number, bool Exists, bool Damaged, int RoomId, int Octoliths, int Scans)
    {
        public bool HasProgress => Exists && !Damaged && RoomId >= 27;
        public string Location => Damaged ? "ARQUIVO ILEGIVEL / PRESERVADO"
            : !HasProgress ? "NOVA AVENTURA" : Metadata.GetRoomById(RoomId, noThrow: true)?.InGameName ?? "EXPEDICAO";
    }

    public static class PcSaveCatalog
    {
        public static string PathFor(string root, byte slot)
        {
            if (slot is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(slot));
            return Path.Combine(root, "Savedata", $"save{slot:000}.json");
        }
        public static PcSaveSlot Read(string root, byte slot)
        {
            string path = PathFor(root, slot);
            if (!File.Exists(path)) return new(slot, false, false, -1, 0, 0);
            try
            {
                if (new FileInfo(path).Length > 1024 * 1024) throw new InvalidDataException("Save too large.");
                using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
                JsonElement save = doc.RootElement;
                RequireArray(save, "RoomState", 66);
                foreach (var room in save.GetProperty("RoomState").EnumerateArray())
                    if (room.ValueKind != JsonValueKind.Array || room.GetArrayLength() != 60) throw new JsonException();
                RequireArray(save, "VisitedRooms", 9); RequireArray(save, "Logbook", 68);
                RequireArray(save, "Ammo", 2); RequireArray(save, "AmmoMax", 2); RequireArray(save, "WeaponSlots", 3);
                int roomId = save.GetProperty("CheckpointRoomId").GetInt32();
                if (roomId != -1 && (roomId is < 27 or > 92)) throw new JsonException();
                int octoliths = BitOperations.PopCount((uint)save.GetProperty("FoundOctoliths").GetUInt16() & 0xff);
                int scans = save.GetProperty("ScanCount").GetInt32();
                return new(slot, true, false, roomId, octoliths, scans);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidDataException or InvalidOperationException or FormatException or OverflowException or KeyNotFoundException)
            {
                // Inspection never rewrites or repairs a player's existing file.
                return new(slot, true, true, -1, 0, 0);
            }
        }
        private static void RequireArray(JsonElement save, string key, int length)
        {
            JsonElement value = save.GetProperty(key);
            if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != length) throw new JsonException();
        }
    }
}
