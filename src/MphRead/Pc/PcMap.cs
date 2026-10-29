using System;
using System.Collections.Generic;
using System.Linq;
using MphRead.Entities;
using MphRead.Formats.Collision;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace MphRead
{
    internal sealed class PcMap
    {
        private static readonly string[] _areas = { "ALINOS", "CELESTIAL ARCHIVES", "VESPER DEFENSE OUTPOST", "ARCTERRA", "OUBLIETTE" };
        private readonly Scene _scene;
        private readonly RoomEntity _room;
        private readonly List<RoomMetadata> _rooms = new();
        private readonly Dictionary<int, PcMapGeometry> _cache = new();
        private List<RoomMetadata> _visible = new();
        private int _selected;
        private int _area;
        private float _height;
        private PcMapView _view = null!;
        public int PolygonCount => _view.Geometry.Polygons.Count;
        public int SelectedRoomId => _visible[_selected].Id;
        public int ExploredCount => _rooms.Count;
        private RoomMetadata Selected => _visible[_selected];
        private string AreaName => _scene.RoomId >= 93 ? "ARENA LOCAL" : _areas[_area];

        public PcMap(Scene scene)
        {
            _scene = scene;
            _room = scene.Room ?? throw new InvalidOperationException("O mapa precisa de uma sala carregada.");
            if (scene.RoomId >= 93) _rooms.Add(_room.Meta);
            else
            {
                for (int id = 27; id <= 92; id++)
                    if (id == scene.RoomId || GameState.StorySave.CheckVisitedRoom(id))
                    {
                        var meta = Metadata.GetRoomById(id);
                        if (meta != null) _rooms.Add(meta);
                    }
            }
            _area = scene.RoomId >= 93 ? 0 : Metadata.GetAreaInfo(scene.RoomId) / 2;
            UpdateArea();
        }

        private void UpdateArea()
        {
            _visible = _rooms.Where(m => _scene.RoomId >= 93 || Metadata.GetAreaInfo(m.Id) / 2 == _area).ToList();
            int current = _visible.FindIndex(m => m.Id == _scene.RoomId);
            Select(Math.Max(0, current));
        }

        private void Select(int index)
        {
            _selected = Math.Clamp(index, 0, _visible.Count - 1);
            if (!_cache.TryGetValue(Selected.Id, out PcMapGeometry? geometry))
            {
                geometry = Selected.Id == _scene.RoomId ? PcMapGeometry.Read(_room.RoomCollision)
                    : PcMapGeometry.Read(new[] { Collision.GetCollision(Selected) });
                _cache.Add(Selected.Id, geometry);
            }
            _view = new(geometry);
            _height = Selected.Id == _scene.RoomId ? PlayerEntity.Main.Position.Y : geometry.MinHeight + 4;
        }

        private void Area(int direction)
        {
            if (_scene.RoomId >= 93) return;
            for (int i = 0; i < 5; i++)
            {
                _area = (_area + direction + 5) % 5;
                if (_rooms.Any(m => Metadata.GetAreaInfo(m.Id) / 2 == _area)) { UpdateArea(); return; }
            }
        }

        public void Key(Keys key)
        {
            if (key == Keys.W) _view.Pan(new Vector2(0, -24));
            else if (key == Keys.S) _view.Pan(new Vector2(0, 24));
            else if (key == Keys.A) _view.Pan(new Vector2(-24, 0));
            else if (key == Keys.D) _view.Pan(new Vector2(24, 0));
            else if (key == Keys.Up) Select(_selected - 1);
            else if (key == Keys.Down) Select(_selected + 1);
            else if (key == Keys.Q) Area(-1);
            else if (key == Keys.E) Area(1);
            else if (key is Keys.Equal or Keys.KeyPadAdd) _view.ZoomBy(1.25f);
            else if (key is Keys.Minus or Keys.KeyPadSubtract) _view.ZoomBy(0.8f);
            else if (key == Keys.PageUp) _height = Math.Min(_height + 8, _view.Geometry.MaxHeight + 7);
            else if (key == Keys.PageDown) _height = Math.Max(_height - 8, _view.Geometry.MinHeight);
            else if (key is Keys.Home or Keys.Space) Select(_selected);
        }

        public void Pointer(Vector2 point, bool click, float wheel = 0)
        {
            if (click && point.X is >= 72 and < 400 && point.Y is >= 216 and < 558)
            {
                int first = Math.Clamp(_selected - 4, 0, Math.Max(0, _visible.Count - 9));
                Select(first + (int)((point.Y - 216) / 38));
            }
            if (wheel != 0)
            {
                if (point.X >= 424) _view.ZoomBy(wheel > 0 ? 1.25f : 0.8f);
                else Select(_selected + (wheel > 0 ? -1 : 1));
            }
        }

        public void Draw(Vector2i size)
        {
            using var canvas = new PcCanvas(size);
            PcCanvas.Panel("MAPA LOCAL", $"{AreaName} / {Selected.InGameName ?? Selected.Name}");
            PcCanvas.Text(72, 177, $"SALAS EXPLORADAS  {_visible.Count}", 1.7f, PcCanvas.Cyan);
            PcCanvas.Text(424, 177, $"PLANTA DA SALA / ALTURA {_height:0.0} / ZOOM {_view.Zoom:0.0}X", 1.6f, PcCanvas.Muted);
            PcCanvas.Rect(72, 208, 328, 370, new Color4(0.025f, 0.07f, 0.10f, 1));
            PcCanvas.Rect(424, 208, 784, 370, new Color4(0.025f, 0.07f, 0.10f, 1));
            int first = Math.Clamp(_selected - 4, 0, Math.Max(0, _visible.Count - 9));
            for (int i = first; i < Math.Min(_visible.Count, first + 9); i++)
            {
                float y = 216 + (i - first) * 38;
                if (i == _selected) PcCanvas.Rect(72, y, 328, 36, new Color4(0.06f, 0.22f, 0.28f, 1));
                string prefix = _visible[i].Id == _scene.RoomId ? "> " : "  ";
                PcCanvas.Text(84, y + 12, PcText.Ellipsis(prefix + (_visible[i].InGameName ?? _visible[i].Name), 29), 1.7f, i == _selected ? PcCanvas.Cyan : PcCanvas.White);
            }
            PcCanvas.Clip(size, 424, 208, 784, 370);
            for (int x = 432; x < 1208; x += 32) PcCanvas.Line(new(x, 208), new(x, 578), new Color4(0.06f, 0.14f, 0.18f, 1));
            for (int y = 218; y < 578; y += 32) PcCanvas.Line(new(424, y), new(1208, y), new Color4(0.06f, 0.14f, 0.18f, 1));
            foreach (var polygon in _view.Geometry.Polygons)
            {
                if (!polygon.OnLevel(_height)) continue;
                if (polygon.Floor)
                {
                    GL.Color4(new Color4(0.12f, 0.42f, 0.46f, 0.3f));
                    GL.Begin(PrimitiveType.TriangleFan);
                    foreach (var point in polygon.Points) GL.Vertex2(_view.Project(point));
                    GL.End();
                }
                else
                {
                    for (int i = 0; i < polygon.Points.Length; i++)
                        PcCanvas.Line(_view.Project(polygon.Points[i]), _view.Project(polygon.Points[(i + 1) % polygon.Points.Length]), new Color4(0.27f, 0.66f, 0.7f, 0.65f));
                }
            }
            if (Selected.Id == _scene.RoomId)
            {
                foreach (EntityBase entity in _scene.Entities)
                {
                    if (entity is DoorEntity door && Math.Abs(door.Position.Y - _height) < 10)
                    {
                        Vector2 point = _view.Project(door.Position);
                        Color4 color = door.Flags.TestFlag(DoorFlags.Locked) ? PcCanvas.Amber : PcCanvas.Cyan;
                        PcCanvas.Rect(point.X - 4, point.Y - 4, 8, 8, color);
                    }
                }
                var player = PlayerEntity.Main;
                if (Math.Abs(player.Position.Y - _height) < 9)
                {
                    Vector2 point = _view.Project(player.Position);
                    Vector2 facing = new(player.FacingVector.X, player.FacingVector.Z);
                    if (facing.LengthSquared > 0.001f) facing.Normalize();
                    Vector2 right = new(-facing.Y, facing.X);
                    GL.Color4(PcCanvas.Amber);
                    GL.Begin(PrimitiveType.Triangles);
                    GL.Vertex2(point + facing * 11); GL.Vertex2(point - facing * 7 + right * 7); GL.Vertex2(point - facing * 7 - right * 7);
                    GL.End();
                }
            }
            PcCanvas.Unclip();
            if (PolygonCount == 0) PcCanvas.Text(460, 360, "GEOMETRIA INDISPONIVEL NESTA SALA", 2, PcCanvas.Muted);
            PcCanvas.Text(72, 596, "WASD  MOVER MAPA   RODA / +/-  ZOOM   PGUP/PGDN  ALTURA   HOME  CENTRALIZAR", 1.6f, PcCanvas.White);
            PcCanvas.Text(72, 623, "SETAS / CLIQUE  SALA   Q/E  REGIAO   AMBAR  VOCE/PORTA TRANCADA   CIANO  PORTA", 1.5f, PcCanvas.Muted);
            PcCanvas.Text(72, 652, "M / ESC  VOLTAR     J  DIARIO     TAB  ALTERNAR PAINEIS", 1.7f, PcCanvas.Cyan);
        }
    }
}
