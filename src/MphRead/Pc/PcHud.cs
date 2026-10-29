using MphRead.Entities;
using MphRead.Formats;
using OpenTK.Mathematics;

namespace MphRead
{
    internal static class PcHud
    {
        private static readonly (BeamType Beam, string Name)[] _slots =
        {
            (BeamType.PowerBeam, "PWR"), (BeamType.Missile, "MSL"), (BeamType.VoltDriver, "VLT"),
            (BeamType.Battlehammer, "BHM"), (BeamType.Imperialist, "IMP"), (BeamType.Judicator, "JUD"),
            (BeamType.Magmaul, "MAG"), (BeamType.ShockCoil, "SHK"), (BeamType.OmegaCannon, "OMG")
        };

        public static void Draw(Vector2i size)
        {
            PlayerEntity player = PlayerEntity.Main;
            if (player.Health <= 0 || player.ScanVisor) return;
            using var canvas = new PcCanvas(size);
            if (!player.IsAltForm)
            {
                for (int i = 0; i < _slots.Length; i++)
                {
                    var slot = _slots[i];
                    bool available = player.AvailableWeapons[slot.Beam];
                    bool selected = slot.Beam == player.CurrentWeapon;
                    float x = 352 + i * 54;
                    PcCanvas.Rect(x, 643, 48, 36, new Color4(0.01f, 0.04f, 0.06f, 0.82f));
                    if (selected) PcCanvas.Rect(x, 643, 48, 2, PcCanvas.Amber);
                    var color = selected ? PcCanvas.Amber : available ? PcCanvas.Cyan : new Color4(0.22f, 0.29f, 0.31f, 1);
                    PcCanvas.Text(x + 7, 651, (i + 1).ToString(), 1.5f, color);
                    PcCanvas.Text(x + 7, 666, slot.Name, 1, color);
                }
            }
            PcCanvas.Rect(352, 684, 796, 28, new Color4(0.01f, 0.04f, 0.06f, 0.8f));
            string hint = player.IsAltForm ? "C VOLTAR  WASD MOVER  ESPACO BOOST  Q BOMBA  ESC PAUSA"
                : "C MORPH  E VISOR  Q SCAN  1-9 ARMAS  M MAPA  J DIARIO  ESC PAUSA";
            PcCanvas.Text(364, 692, hint, 1.4f, PcCanvas.Cyan);
        }
    }
}
