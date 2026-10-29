using System;
using System.Collections.Generic;
using System.Linq;
using MphRead.Text;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace MphRead
{
    public sealed record PcJournalEntry(int ScanId, char Category, string Title, string Body);

    public static class PcJournalCatalog
    {
        public static readonly char[] Categories = { 'E', 'B', 'O', 'L' };

        public static IReadOnlyList<PcJournalEntry> Read(IReadOnlyList<StringTableEntry> table, Func<int, bool> unlocked)
        {
            var result = new List<PcJournalEntry>();
            for (int i = 0; i < table.Count; i++)
            {
                StringTableEntry entry = table[i];
                if (Categories.Contains(entry.Category) && unlocked(i))
                    result.Add(new(i, entry.Category, entry.String1, entry.String2));
            }
            return result;
        }
    }

    internal sealed class PcJournal
    {
        private static readonly string[] _names = { "EQUIPAMENTO", "BIOFORMAS", "OBJETOS", "HISTORIA" };
        private readonly IReadOnlyList<PcJournalEntry> _entries;
        private readonly int[] _totals;
        private int _category;
        private int _selected;
        private int _scroll;
        public int SelectedScanId => Current.Count == 0 ? -1 : Current[_selected].ScanId;
        public int UnlockedCount => _entries.Count;
        public int Scroll => _scroll;
        private const int VisibleLines = 12;
        private IReadOnlyList<PcJournalEntry> Current => _entries.Where(e => e.Category == PcJournalCatalog.Categories[_category]).ToArray();
        private IReadOnlyList<string> BodyLines => Current.Count == 0 ? Array.Empty<string>() : PcText.Wrap(Current[_selected].Body, 64);

        public PcJournal(StorySave save)
        {
            var table = Strings.ReadStringTable(StringTables.ScanLog);
            _entries = PcJournalCatalog.Read(table, id => id < save.Logbook.Length * 8 && save.CheckLogbook(id));
            _totals = PcJournalCatalog.Categories.Select(c => table.Count(e => e.Category == c)).ToArray();
        }

        private void Category(int value) { _category = (value + 4) % 4; _selected = 0; _scroll = 0; }
        private void Select(int value) { _selected = Math.Clamp(value, 0, Math.Max(0, Current.Count - 1)); _scroll = 0; }
        private void ScrollBy(int amount) => _scroll = Math.Clamp(_scroll + amount, 0, Math.Max(0, BodyLines.Count - VisibleLines));

        private static string PcTip(int scanId) => scanId switch
        {
            0 => "NO PC: E ALTERNA O VISOR. SEGURE Q PARA ESCANEAR.",
            1 => "NO PC: M ABRE O MAPA DA SALA E AS AREAS EXPLORADAS.",
            2 or 3 => "NO PC: CLIQUE ESQUERDO DISPARA. MOUSE CONTROLA A MIRA.",
            4 => "NO PC: 2 SELECIONA MISSEIS. CLIQUE ESQUERDO DISPARA.",
            5 => "NO PC: C ALTERNA MORPH BALL. ESPACO ATIVA O BOOST.",
            6 => "NO PC: Q USA BOMBAS NA FORMA ALTERNATIVA.",
            26 => "NO PC: ESPACO PULA. WASD CONTROLA O MOVIMENTO.",
            28 => "NO PC: SEGURE O CLIQUE ESQUERDO PARA CARREGAR.",
            _ => "REGISTRO ORIGINAL / DESCOBERTA DA SUA EXPEDICAO"
        };

        public void Key(Keys key)
        {
            if (key is Keys.W or Keys.Up) Select(_selected - 1);
            else if (key is Keys.S or Keys.Down) Select(_selected + 1);
            else if (key is Keys.A or Keys.Left or Keys.Q) Category(_category - 1);
            else if (key is Keys.D or Keys.Right or Keys.E) Category(_category + 1);
            else if (key == Keys.PageDown) ScrollBy(12);
            else if (key == Keys.PageUp) ScrollBy(-12);
            else if (key == Keys.Home) _scroll = 0;
            else if (key == Keys.End) ScrollBy(Int32.MaxValue / 2);
        }

        public void Pointer(Vector2 point, bool click, float wheel = 0)
        {
            if (click && point.Y is >= 159 and < 199 && point.X is >= 72 and < 1208)
                Category((int)((point.X - 72) / 284));
            else if (click && point.X is >= 72 and < 420 && point.Y is >= 216 and < 558)
            {
                int first = Math.Clamp(_selected - 4, 0, Math.Max(0, Current.Count - 9));
                Select(first + (int)((point.Y - 216) / 38));
            }
            if (wheel != 0)
            {
                if (point.X >= 446) ScrollBy(wheel > 0 ? -3 : 3);
                else Select(_selected + (wheel > 0 ? -1 : 1));
            }
        }

        public void Draw(Vector2i size)
        {
            using var canvas = new PcCanvas(size);
            PcCanvas.Panel("DIARIO DE SCAN", "Registros da sua expedicao / texto original da ROM");
            for (int i = 0; i < 4; i++)
            {
                int count = _entries.Count(e => e.Category == PcJournalCatalog.Categories[i]);
                PcCanvas.Rect(72 + i * 284, 159, 276, 40, i == _category ? new Color4(0.06f, 0.25f, 0.30f, 1) : new Color4(0.03f, 0.09f, 0.12f, 1));
                PcCanvas.Text(84 + i * 284, 173, $"{_names[i]} {count}/{_totals[i]}", 1.5f, i == _category ? PcCanvas.Cyan : PcCanvas.Muted);
            }
            PcCanvas.Rect(72, 216, 348, 366, new Color4(0.025f, 0.07f, 0.10f, 0.95f));
            PcCanvas.Rect(446, 216, 762, 366, new Color4(0.025f, 0.07f, 0.10f, 0.95f));
            var entries = Current;
            if (entries.Count == 0)
            {
                PcCanvas.Text(90, 239, "NENHUM REGISTRO", 2, PcCanvas.Muted);
                PcCanvas.Text(468, 242, "EXPLORE E ESCANEIE COM O VISOR.", 2, PcCanvas.Cyan);
                PcCanvas.Text(468, 280, "E ATIVA O VISOR. SEGURE Q PARA SCAN.", 1.8f, PcCanvas.White);
                PcCanvas.Text(468, 316, "APENAS DESCOBERTAS REGISTRADAS APARECEM AQUI.", 1.7f, PcCanvas.Muted);
            }
            else
            {
                int first = Math.Clamp(_selected - 4, 0, Math.Max(0, entries.Count - 9));
                for (int i = first; i < Math.Min(entries.Count, first + 9); i++)
                {
                    float y = 216 + (i - first) * 38;
                    if (i == _selected) PcCanvas.Rect(72, y, 348, 36, new Color4(0.06f, 0.22f, 0.28f, 1));
                    PcCanvas.Text(86, y + 12, PcText.Ellipsis(entries[i].Title, 30), 1.7f, i == _selected ? PcCanvas.Cyan : PcCanvas.White);
                }
                int titleRow = 0;
                foreach (string line in PcText.Wrap(entries[_selected].Title, 52).Take(2))
                    PcCanvas.Text(468, 237 + titleRow++ * 19, line, 2, PcCanvas.Amber);
                PcCanvas.Rect(468, 282, 716, 1, PcCanvas.Muted);
                PcCanvas.Text(468, 300, PcTip(entries[_selected].ScanId), 1.45f, PcCanvas.Cyan);
                var body = BodyLines;
                for (int i = _scroll; i < Math.Min(body.Count, _scroll + VisibleLines); i++)
                    PcCanvas.Text(468, 334 + (i - _scroll) * 18, body[i], 1.8f, PcCanvas.White);
                PcCanvas.Text(468, 562, $"{_selected + 1}/{entries.Count} REGISTROS     LINHAS {_scroll + 1}-{Math.Min(body.Count, _scroll + VisibleLines)}/{body.Count}", 1.3f, PcCanvas.Muted);
            }
            PcCanvas.Text(72, 608, "W/S OU SETAS  REGISTROS   A/D OU Q/E  CATEGORIAS   RODA / PGUP / PGDN  LER", 1.6f, PcCanvas.White);
            PcCanvas.Text(72, 646, "J / ESC  VOLTAR     M  MAPA     TAB  ALTERNAR PAINEIS", 1.8f, PcCanvas.Cyan);
        }
    }
}
