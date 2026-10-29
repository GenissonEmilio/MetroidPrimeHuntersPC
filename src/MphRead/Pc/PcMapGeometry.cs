using System;
using System.Collections.Generic;
using System.Linq;
using MphRead.Formats.Collision;
using OpenTK.Mathematics;

namespace MphRead
{
    public sealed record PcMapPolygon(Vector3[] Points, bool Floor)
    {
        public float MinY => Points.Min(p => p.Y);
        public float MaxY => Points.Max(p => p.Y);
        public bool OnLevel(float height) => MinY <= height + 3 && MaxY >= height - 7;
    }

    public sealed class PcMapGeometry
    {
        public IReadOnlyList<PcMapPolygon> Polygons { get; }
        public Vector2 Min { get; }
        public Vector2 Max { get; }
        public float MinHeight { get; }
        public float MaxHeight { get; }

        public PcMapGeometry(IReadOnlyList<PcMapPolygon> polygons)
        {
            Polygons = polygons;
            Vector3[] points = polygons.SelectMany(p => p.Points).ToArray();
            if (points.Length == 0) { Min = new(-1); Max = new(1); return; }
            Min = new(points.Min(p => p.X), points.Min(p => p.Z));
            Max = new(points.Max(p => p.X), points.Max(p => p.Z));
            MinHeight = points.Min(p => p.Y); MaxHeight = points.Max(p => p.Y);
        }

        public static PcMapGeometry Read(IEnumerable<CollisionInstance> collisions)
        {
            var polygons = new List<PcMapPolygon>();
            foreach (var collision in collisions)
            {
                if (!collision.Active || collision.Info is not MphCollisionInfo info) continue;
                foreach (var face in info.Data)
                {
                    if (face.IgnorePlayers || face.PointIndexCount < 3) continue;
                    float normalY = info.Planes[face.PlaneIndex].Y;
                    if (normalY < -0.5f) continue; // A ceiling would hide the floor beneath it.
                    var points = new Vector3[face.PointIndexCount];
                    for (int i = 0; i < points.Length; i++)
                        points[i] = info.Points[info.PointIndices[face.PointStartIndex + i]] + collision.Translation;
                    polygons.Add(new(points, normalY > 0.5f));
                }
            }
            return new(polygons);
        }
    }

    public sealed class PcMapView
    {
        public Vector2 Center { get; set; }
        public float Zoom { get; private set; } = 1;
        public PcMapGeometry Geometry { get; }
        public PcMapView(PcMapGeometry geometry) { Geometry = geometry; Center = (geometry.Min + geometry.Max) / 2; }
        public void ZoomBy(float amount) => Zoom = Math.Clamp(Zoom * amount, 0.5f, 8);
        public float Scale => Math.Min(720 / Math.Max(Geometry.Max.X - Geometry.Min.X, 1), 320 / Math.Max(Geometry.Max.Y - Geometry.Min.Y, 1)) * Zoom;
        public Vector2 Project(Vector3 point) => new Vector2(816, 382) + (new Vector2(point.X, point.Z) - Center) * Scale;
        public Vector2 Unproject(Vector2 point) => Center + (point - new Vector2(816, 382)) / Scale;
        public void Pan(Vector2 pixels) => Center += pixels / Scale;
    }
}
