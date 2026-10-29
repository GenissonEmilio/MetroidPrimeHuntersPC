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
        public const string EuropeRev1Sha256 = "faac3ac98be17375b90916a3c12b5a9b8c220165fbb485cf179033f90839f97e";

        public static string Validate(ReadOnlySpan<byte> data)
        {
            if (data.Length != 64 * 1024 * 1024)
            {
                throw new InvalidDataException("Esta versao requer Metroid Prime Hunters USA revisao 0 ou Europa revisao 1, 64 MiB.");
            }
            string code = Encoding.ASCII.GetString(data.Slice(12, 4));
            string? expected = (code, data[30]) switch
            {
                ("AMHE", 0) => UsaRev0Sha256,
                ("AMHP", 1) => EuropeRev1Sha256,
                _ => null
            };
            string hash = Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
            if (hash != expected)
            {
                throw new InvalidDataException("A ROM nao corresponde a uma revisao verificada (USA 0 / Europa 1). O arquivo original foi preservado.");
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
            string temporary = Path.Combine(PcRuntime.StateRoot, ".import.nds");
            try
            {
                File.WriteAllBytes(temporary, data);
                Extract.Setup(temporary);
                File.WriteAllText("rom-import.json", JsonSerializer.Serialize(new
                {
                    GameCode = Encoding.ASCII.GetString(data, 12, 4), Revision = data[30], Sha256 = hash
                }));
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
