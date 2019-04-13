using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TSIM.Model;

namespace TSIM.RailroadDatabase
{
    public class SqliteSimDatabase : IDisposable, INetworkDatabase, IUnitDatabase
    {
        private readonly MyContext db_;
        private Unit[] _units;
        private QuadTree? _quadTree;

        private class MyContext : DbContext
        {
            private readonly string _filename;

            public DbSet<Entity.SegmentModel> Segments { get; set; }
            public DbSet<SegmentLink> SegmentLinks { get; set; }
            public DbSet<Entity.SimulationCoordinateSpace> SimulationCoordinateSpaces { get; set; }
            public DbSet<Entity.UnitModel> Units { get; set; }
            public DbSet<Entity.QuadTreeNodeEntity> QuadTreeNodes { get; set; }
            public DbSet<Entity.QuadTreeReferencedSegment> QuadTreeSegments { get; set; }

            public MyContext(string filename)
            {
                _filename = filename;
            }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseSqlite("Data Source=" + _filename);
            }
        }

        private SqliteSimDatabase(string dbPath)
        {
            db_ = new MyContext(dbPath);
        }

        private SqliteSimDatabase(string dbPath, SimulationCoordinateSpace coordinateSpace)
        {
            db_ = new MyContext(dbPath);
            db_.Database.EnsureCreated();

            db_.SimulationCoordinateSpaces.Add(new Entity.SimulationCoordinateSpace(coordinateSpace));
            db_.SaveChanges();
        }

        public static SqliteSimDatabase New(string dbPath, SimulationCoordinateSpace coordinateSpace)
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }

            return new SqliteSimDatabase(dbPath, coordinateSpace);
        }

        public static SqliteSimDatabase Open(string dbPath)
        {
            if (!File.Exists(dbPath))
            {
                throw new FileNotFoundException("Database must exist: " + dbPath);
            }

            return new SqliteSimDatabase(dbPath);
        }

        public void AddSegments(IEnumerable<Segment> segments)
        {
            db_.Segments.AddRange(segments.Select(segment => new Entity.SegmentModel(segment)));
            db_.SaveChanges();
        }

        public void AddSegmentLinks(IEnumerable<SegmentLink> segmentLinks)
        {
            db_.SegmentLinks.AddRange(segmentLinks);
            db_.SaveChanges();
        }

        // FIXME: this is hella wrong and out of sync with _units
        public void AddUnits(IEnumerable<Unit> units)
        {
            db_.Units.AddRange(units.Select(unit => new Entity.UnitModel(unit)));
            db_.SaveChanges();
        }

        public void Dispose()
        {
            db_.Dispose();
        }

        public IEnumerable<Segment> EnumerateSegments()
        {
            return from segment in db_.Segments select segment.ToModel();
        }

        public IEnumerable<SegmentLink> EnumerateSegmentLinks()
        {
            return db_.SegmentLinks;
        }

        public IEnumerable<Unit> EnumerateUnits()
        {
            EnsureUnitsLoaded();

            return _units;
        }

        public int GetNumUnits()
        {
            EnsureUnitsLoaded();

            return _units.Length;
        }

        public SimulationCoordinateSpace GetCoordinateSpace()
        {
            return db_.SimulationCoordinateSpaces.First().ToModel();
        }

        public void SetUnitSpeed(int id, float speed)
        {
            EnsureUnitsLoaded();

            // aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
            var u = _units[id];
            u.Velocity = Utility.QuaternionToDirectionVector(u.Orientation) * speed;
            _units[id] = u;
        }

        private void EnsureUnitsLoaded()
        {
            if (_units == null)
            {
                _units = db_.Units.Include(u => u.Class).Select(unit => unit.ToModel()).ToArray();
            }
        }

        public Segment GetSegmentById(int id)
        {
            return db_.Segments.First(s => s.Id == id).ToModel();
        }

        public SegmentLink[] FindConnectingSegments(int segmentId, SegmentEndpoint ep)
        {
            return db_.SegmentLinks.Where(l =>
                (l.Segment1 == segmentId && l.Ep1 == ep) || (l.Segment2 == segmentId && l.Ep2 == ep)).ToArray();
        }

        public QuadTree GetQuadTree()
        {
            if (_quadTree == null)
            {
                // FIXME: super hack for finding the root while automagically wiring relations
                var root = db_.QuadTreeNodes.Include(n => n.Segments).ToList().First();
                _quadTree = new QuadTree(this, root.ToQuadTreeNode());
            }

            return _quadTree;
        }

        public ref Unit GetUnitByIndex(int unitIndex)
        {
            EnsureUnitsLoaded();

            return ref _units[unitIndex];
        }

        public void UpdateUnitByIndex(int unitIndex, Unit unit)
        {
            EnsureUnitsLoaded();

            _units[unitIndex] = unit;
        }

        public void PutQuadTree(QuadTree quadTree)
        {
            // TODO: validate existence of all referenced segments
            // TODO: ensure table is empty

            db_.QuadTreeNodes.Add(new Entity.QuadTreeNodeEntity(quadTree.Root));
            db_.SaveChanges();
        }
    }
}
