using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace MphRead
{
    internal sealed record PcMenuRow(Func<string> Label, string Detail, Action<int> Activate);

    internal sealed class PcMenu
    {
        private readonly Action _resume;
        private readonly Action _exit;
        private readonly Action _fullscreen;
        private readonly bool _pause;
        private readonly List<PcMenuRow> _rows = new();
        private int _selected;
        private bool _settings;
        public string Status { get; set; } = "";

        public PcMenu(bool pause, Action resume, Action exit, Action fullscreen, Action? practice = null, Action? map = null, Action? journal = null)
        {
            _pause = pause; _resume = resume; _exit = exit; _fullscreen = fullscreen;
            void Main()
            {
                _settings = false; _selected = 0; _rows.Clear();
                Add(() => pause ? "VOLTAR AO JOGO" : "INICIAR / CONTINUAR", "Retomar a expedicao. Progresso salvo pela nave.", _ => resume());
                if (!pause && practice != null)
                    Add(() => "TESTAR CONTROLES", "Arena local sem bots. Nao altera o save da campanha.", _ => practice());
                if (pause && map != null) Add(() => "MAPA [M]", "Planta da sala, posicao e salas ja exploradas.", _ => map());
                if (pause && journal != null) Add(() => "DIARIO [J]", "Ler os scans registrados nesta expedicao.", _ => journal());
                Add(() => "CONFIGURACOES", "Mouse, audio e apresentacao. Alteracoes salvas localmente.", _ => Settings());
                Add(() => pause ? "VOLTAR AO MENU" : "SAIR", pause ? "Progresso desde o ultimo save da nave pode ser perdido." : "Fechar Hunters PC.", _ => exit());
            }
            void Settings()
            {
                _settings = true; _selected = 0; _rows.Clear();
                Add(() => $"MOUSE       {PcRuntime.Profile.MouseSensitivity:0.0}X", "Setas esquerda / direita ou clique para ajustar.", d => PcRuntime.Profile.MouseSensitivity = Math.Clamp(MathF.Round(PcRuntime.Profile.MouseSensitivity + d * 0.1f, 1), 0.1f, 4));
                Add(() => $"INVERTER Y  {OnOff(PcRuntime.Profile.InvertY)}", "Inverter apenas a direcao vertical da camera.", _ => PcRuntime.Profile.InvertY = !PcRuntime.Profile.InvertY);
                Add(() => $"DICAS HUD   {OnOff(PcRuntime.Profile.ShowHints)}", "Mostrar os atalhos de teclado na parte inferior da tela.", _ => PcRuntime.Profile.ShowHints = !PcRuntime.Profile.ShowHints);
                Add(() => $"TELA CHEIA  {OnOff(PcRuntime.Profile.Fullscreen)}", "Alternar tambem com F11 durante o jogo.", _ => fullscreen());
                Add(() => $"EFEITOS     {PcRuntime.Profile.SoundVolume:P0}", "Setas esquerda / direita ou clique para ajustar.", d => PcRuntime.Profile.SoundVolume = Math.Clamp(PcRuntime.Profile.SoundVolume + d * 0.05f, 0, 1));
                Add(() => $"MUSICA      {PcRuntime.Profile.MusicVolume:P0}", "Volume independente para a musica do jogo.", d => PcRuntime.Profile.MusicVolume = Math.Clamp(PcRuntime.Profile.MusicVolume + d * 0.05f, 0, 1));
                Add(() => "VOLTAR", "Retornar ao menu anterior.", _ => Main());
            }
            _goBack = () => { if (_settings) Main(); else if (_pause) resume(); };
            Main();
        }

        private readonly Action _goBack;
        private static string OnOff(bool value) => value ? "SIM" : "NAO";
        private void Add(Func<string> label, string detail, Action<int> action) => _rows.Add(new(label, detail, action));
        private void Activate(int direction)
        {
            _rows[_selected].Activate(direction);
            PcRuntime.Apply();
            PcRuntime.Profile.Save(PcRuntime.ProfilePath);
        }

        public void Key(Keys key)
        {
            if (key is Keys.Escape or Keys.Backspace) _goBack();
            else if (key is Keys.W or Keys.Up) _selected = (_selected + _rows.Count - 1) % _rows.Count;
            else if (key is Keys.S or Keys.Down) _selected = (_selected + 1) % _rows.Count;
            else if (key is Keys.Enter or Keys.Space or Keys.Right or Keys.D) Activate(1);
            else if (_settings && key is Keys.Left or Keys.A) Activate(-1);
        }

        public void Pointer(Vector2 pointer, Vector2i size, bool click)
        {
            Vector2 point = PcLayout.ToCanvas(pointer, size);
            int row = PcLayout.HitRow(point, _rows.Count);
            if (row < 0) return;
            _selected = row;
            if (click) Activate(_settings && point.X < 170 ? -1 : 1);
        }

        public void Draw(Vector2i size)
        {
            using var canvas = new PcCanvas(size);
            PcCanvas.Background(_pause);
            // Seven settings rows use the same hit boxes as the four main rows.
            for (int i = 0; i < _rows.Count; i++)
            {
                float y = 226 + i * 58;
                bool selected = i == _selected;
                PcCanvas.Rect(72, y, 604, 50, selected ? new Color4(0.06f, 0.25f, 0.30f, 1) : new Color4(0.035f, 0.095f, 0.13f, 1));
                if (selected) PcCanvas.Rect(72, y, 4, 50, PcCanvas.Cyan);
                PcCanvas.Text(94, y + 17, selected ? ">" : " ", 2, PcCanvas.Cyan);
                PcCanvas.Text(122, y + 17, _rows[i].Label(), 2, selected ? PcCanvas.White : PcCanvas.Muted);
            }
            PcCanvas.Text(820, 290, _settings ? "AJUSTES DE CAMPO" : "OPERADOR / SAMUS", 2, PcCanvas.Cyan);
            PcCanvas.Text(824, 324, "KEYBOARD + MOUSE", 1.6f, PcCanvas.Muted);
            PcCanvas.Text(766, 516, "WASD    MOVIMENTO", 2, PcCanvas.White);
            PcCanvas.Text(766, 542, "MOUSE   CAMERA / MIRA", 2, PcCanvas.White);
            PcCanvas.Text(766, 568, "ESC     PAUSA", 2, PcCanvas.White);
            if (!_settings)
            {
                PcCanvas.Text(74, 548, _rows[_selected].Detail, 1.3f, PcCanvas.Muted);
                PcCanvas.Text(74, 582, Status, 1.3f, PcCanvas.Amber);
            }
        }
    }
}
