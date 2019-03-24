using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TSIM.Model;

namespace TSIM.RailroadDatabase
{
    public class SqliteSimDatabase : IDisposable, INetworkDatabase
    {
        private MyContext db_;

        private class MyContext : DbContext
        {
            private readonly string _filename;

            public DbSet<Entity.SegmentModel> Segments { get; set; }
            public DbSet<Entity.SimulationCoordinateSpace> SimulationCoordinateSpaces { get; set; }
            public DbSet<Entity.UnitModel> Units { get; set; }

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
            return new SqliteSimDatabase(dbPath);
        }

        public void AddSegments(IEnumerable<Segment> segments)
        {
            db_.Segments.AddRange(segments.Select(segment => new Entity.SegmentModel(segment)));
            db_.SaveChanges();
        }

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
            return from segment in db_.Segments.Include(s => s.ControlPoints) select segment.ToModel();
        }

        public IEnumerable<Unit> EnumerateUnits()
        {
            return db_.Units.Include(u => u.Class).Select(unit => unit.ToModel());
        }

        public SimulationCoordinateSpace GetCoordinateSpace()
        {
            return db_.SimulationCoordinateSpaces.First().ToModel();
        }
    }
}
