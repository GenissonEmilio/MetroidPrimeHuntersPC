using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MphRead
{
    public static class PcRom
    {
        public const string UsaRev0Sha256 = "7d0a98ff98e1b7c985d1f3d89b01730af1b2115061a4dfea847612d217a8b855";

        public static string Validate(ReadOnlySpan<byte> data)
        {
            if (data.Length != 64 * 1024 * 1024 || Encoding.ASCII.GetString(data.Slice(12, 4)) != "AMHE" || data[30] != 0)
            {
                throw new InvalidDataException("Esta versao requer Metroid Prime Hunters USA, revisao 0, 64 MiB.");
            }
            string hash = Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
            if (hash != UsaRev0Sha256)
            {
                throw new InvalidDataException("A ROM nao corresponde a revisao USA 0 verificada. O arquivo original foi preservado.");
            }
            return hash;
        }

        public static void Import(string source)
        {
            byte[] data;
            if (Path.GetExtension(source).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                using var archive = ZipFile.OpenRead(source);
                var games = archive.Entries.Where(e => e.FullName.EndsWith(".nds", StringComparison.OrdinalIgnoreCase)).ToArray();
                if (games.Length != 1 || games[0].Length != 64 * 1024 * 1024)
                {
                    throw new InvalidDataException("O ZIP deve conter exatamente uma ROM .nds de 64 MiB.");
                }
                using var input = games[0].Open();
                data = new byte[64 * 1024 * 1024];
                input.ReadExactly(data);
                if (input.ReadByte() != -1) throw new InvalidDataException("Tamanho de ROM invalido.");
            }
            else
            {
                if (new FileInfo(source).Length != 64 * 1024 * 1024)
                    throw new InvalidDataException("A ROM .nds precisa ter 64 MiB.");
                data = File.ReadAllBytes(source);
            }
            string hash = Validate(data);
            if (File.Exists("paths.txt"))
            {
                Paths.UpdatePaths();
                Paths.ChooseMphPath();
                if (Ready) return;
                throw new InvalidDataException("Ja existe uma importacao incompleta nesta pasta. Use uma pasta nova para importar.");
            }
            string temporary = Path.Combine(AppContext.BaseDirectory, ".import.nds");
            try
            {
                File.WriteAllBytes(temporary, data);
                Extract.Setup(temporary);
                File.WriteAllText("rom-import.json", JsonSerializer.Serialize(new { GameCode = "AMHE", Revision = 0, Sha256 = hash }));
                Paths.UpdatePaths();
                Paths.ChooseMphPath();
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }

        public static bool Ready => File.Exists("paths.txt") && Directory.Exists(Paths.FileSystem);
    }
}
