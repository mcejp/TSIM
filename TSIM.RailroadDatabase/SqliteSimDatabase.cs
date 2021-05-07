using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TSIM.Model;

namespace TSIM.RailroadDatabase
{
    // This generalizes to other EF back-ends, not just sqlite.
    public class SqliteSimDatabase : IDisposable, INetworkDatabase, IUnitDatabase
    {
        private readonly MyContext db_;
        private Unit[] _units;
        private QuadTree? _quadTree;

        //
        private readonly Dictionary<int, Segment> _segmentCache =
            new Dictionary<int, Segment>();
        private readonly Dictionary<(int, SegmentEndpoint), SegmentLink[]> _segmentLinkCache =
            new Dictionary<(int, SegmentEndpoint), SegmentLink[]>();

        private class MyContext : DbContext
        {
            private readonly string _filename;

            public DbSet<Entity.SegmentModel> Segments { get; set; }
            public DbSet<SegmentLink> SegmentLinks { get; set; }
            public DbSet<Entity.SimulationCoordinateSpace> SimulationCoordinateSpaces { get; set; }
            public DbSet<Entity.UnitModel> Units { get; set; }
            public DbSet<Entity.StationEntity> Stations { get; set; }
            public DbSet<Entity.StationStopEntity> StationStops { get; set; }
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

        public void AddStations(IEnumerable<Station> stations)
        {
            db_.Stations.AddRange(from station in stations select new Entity.StationEntity(station));
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

        public IEnumerable<Station> EnumerateStations()
        {
            return from station in db_.Stations.Include(s => s.Stops) select station.ToModel();
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

        public Segment GetSegmentById(int id)
        {
            if (_segmentCache.TryGetValue(id, out var seg))
            {
                return seg;
            }

            return _segmentCache[id] = db_.Segments.First(s => s.Id == id).ToModel();
        }

        public Station GetStationById(int id) {
            return db_.Stations.First(s => s.Id == id).ToModel();
        }

        public SegmentLink[] FindConnectingSegments(int segmentId, SegmentEndpoint ep)
        {
            if (_segmentLinkCache.TryGetValue((segmentId, ep), out var links))
            {
                return links;
            }

            return _segmentLinkCache[(segmentId, ep)] = db_.SegmentLinks.Where(l =>
                (l.Segment1 == segmentId && l.Ep1 == ep) || (l.Segment2 == segmentId && l.Ep2 == ep)).ToArray();
        }

        public (Station station, StationStop stop, float distance, TrajectorySegment[] plan)? FindNearestStationAlongTrack(
            int segmentId, float t, SegmentEndpoint dir, int? excludedStationId, bool verbose)
        {
            // Queue of tuples (segmentId, t(entry), dir) for breath-first search
            var backlog = new Queue<(int, float, SegmentEndpoint, float, TrajectorySegment)>();
            backlog.Enqueue((segmentId, t, dir, 0, null));
            //new TrajectorySegment(null, segmentId, dir, 0, 0)

            (Station station, StationStop stop, float distance, TrajectorySegment plan)? best = null;

            while (backlog.Count > 0)
            {
                float distance;
                TrajectorySegment predecessor;
                (segmentId, t, dir, distance, predecessor) = backlog.Dequeue();

                var found = SearchNearestStationAlongTrack(segmentId, t, dir, distance, best?.distance, backlog, predecessor, excludedStationId, verbose);

                if (found != null)
                {
                    best = found;
                }
            }

            if (best == null)
            {
                return null;
            }

            // Compute the plan
            List<TrajectorySegment> plan = new List<TrajectorySegment>();

            var (station, stop, distance1, head) = best.Value;

            while (head != null)
            {
                head.DistToGoalAtEntry = distance1 - head.DistToGoalAtEntry;
                head.DistToGoalAtExit = distance1 - head.DistToGoalAtExit;
                plan.Insert(0, head);
                head = head.Prev;
            }

            if (verbose)
            {
                Console.WriteLine("PRINTING HET PLAAN:");

                foreach (var node in plan)
                {
                    Console.WriteLine($"  - {node}");
                }
            }

            return (station, stop, distance1, plan.ToArray());
        }

        private (Station station, StationStop stop, float distance, TrajectorySegment plan)? SearchNearestStationAlongTrack(int segmentId, float t,
            SegmentEndpoint dir, float distance, float? currentBest, Queue<(int, float, SegmentEndpoint, float, TrajectorySegment)> backlog,
            TrajectorySegment predecessor, int? excludedStationId, bool verbose)
        {
            var seg = GetSegmentById(segmentId);

            // Look for stops in this segment
            var q = db_.StationStops.Where(s => s.SegmentId == segmentId);
            if (excludedStationId.HasValue) {
                q = q.Where(s => s.Station.Id != excludedStationId.Value);
            }
            var stops = q.Include(s => s.Station);

            (Station station, StationStop stop, float distance, TrajectorySegment plan)? best = null;

            foreach (var stop in stops)
            {
                // Is the stop downstream from here?

                float stopDistance;

                if (dir == SegmentEndpoint.End)
                {
                    if (stop.T > t)
                    {
                        stopDistance = distance + seg.GetLength() * (stop.T - t);
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    if (stop.T < t)
                    {
                        stopDistance = distance + seg.GetLength() * (t - stop.T);
                    }
                    else
                    {
                        continue;
                    }
                }

                if (currentBest == null || stopDistance < currentBest)
                {
                    currentBest = stopDistance;
                    // FIXME: shite shite shite aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
                    // this will return a different Station instance every time, which breaks equality comparison in StationToStationAgent
                    best = (stop.Station.ToModel(), stop.ToModel(), stopDistance,
                        new TrajectorySegment(predecessor, segmentId, dir, distance, stopDistance));
                }
            }

            if (best != null)
            {
                return best;
            }

            // Reached end of segment. Look forward
            var distanceAtEnd = distance + seg.DistanceToEndpoint(t, dir);

            if (currentBest.HasValue && currentBest.Value < distanceAtEnd)
            {
                return null;
            }

            var candidates = FindConnectingSegments(segmentId, dir);

            foreach (var candidate in candidates)
            {
                if (candidate.Segment1 == segmentId && candidate.Ep1 == dir)
                {
                    if (verbose)
                    {
                        Console.WriteLine(
                            $"SearchNearestStationAlongTrack: candidate {segmentId},{dir} -> {candidate.Segment2},{candidate.Ep2} ({distanceAtEnd})");
                    }

                    backlog.Enqueue((candidate.Segment2,
                            candidate.Ep2 == SegmentEndpoint.Start ? 0 : 1,
                            candidate.Ep2.Other(),
                            distanceAtEnd,
                            new TrajectorySegment(predecessor, segmentId, dir, distance, distanceAtEnd)
                        ));
                }
                else if (candidate.Segment2 == segmentId && candidate.Ep2 == dir)
                {
                    if (verbose)
                    {
                        Console.WriteLine(
                            $"SearchNearestStationAlongTrack: candidate {segmentId},{dir} -> {candidate.Segment1},{candidate.Ep1} ({distanceAtEnd})");
                    }

                    backlog.Enqueue((candidate.Segment1,
                            candidate.Ep1 == SegmentEndpoint.Start ? 0 : 1,
                            candidate.Ep1.Other(),
                            distanceAtEnd,
                            new TrajectorySegment(predecessor, segmentId, dir, distance, distanceAtEnd)
                        ));
                }
                else
                {
                    Trace.Assert(false);
                }
            }

            return null;
        }

        public QuadTree? GetQuadTreeIfYouHaveOne()
        {
            EnsureQuadTreeLoaded();

            return _quadTree;
        }

        public (int segmentId, SegmentEndpoint dir, float t)? FindSegmentAt(Vector3 position, Quaternion orientation,
            float radius, float maxAngle)
        {
            EnsureQuadTreeLoaded();

            // TODO: this algorithm is not Sqlite-specific and thus has no business sitting here.
            //       It should be in like QuadTreeUtility.

            var candidates = _quadTree.FindSegmentsNear(position, radius);

            var minCosine = Math.Cos(maxAngle);
            var expectedDirection = Utility.QuaternionToDirectionVector(orientation);

            foreach (var (seg, t) in candidates)
            {
                // Get the tangent for start->end and make use of the fact that for the opposite direction it will be simply negated
                var (_, tangent) = seg.GetPointAndTangent(t, SegmentEndpoint.End);

                // Check if the track tangent aligns with the expected orientation. And just return the first matching result.
                // (We could look for the closest match or something, but why bother?)
                if (Vector3.Dot(tangent, expectedDirection) >= minCosine)
                {
                    return (seg.Id, SegmentEndpoint.End, t);
                }
                else if (Vector3.Dot(-tangent, expectedDirection) >= minCosine)
                {
                    return (seg.Id, SegmentEndpoint.Start, t);
                }
            }

            return null;
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

        public byte[] SnapshotFullMake()
        {
            EnsureUnitsLoaded();

            var options = new JsonSerializerOptions { IncludeFields = true };
            return JsonSerializer.SerializeToUtf8Bytes(_units, options);
        }

        public void SnapshotFullRestore(byte[] snapshot)
        {
            var options = new JsonSerializerOptions { IncludeFields = true };
            _units = JsonSerializer.Deserialize<Unit[]>(snapshot, options);
        }

        private void EnsureQuadTreeLoaded()
        {
            if (_quadTree == null)
            {
                // FIXME: super hack for finding the root while automagically wiring relations
                var root = db_.QuadTreeNodes.Include(n => n.Segments).ToList().First();
                _quadTree = new QuadTree(this, root.ToQuadTreeNode());
            }
        }

        private void EnsureUnitsLoaded()
        {
            if (_units == null)
            {
                _units = db_.Units.Include(u => u.Class).Select(unit => unit.ToModel()).ToArray();
            }
        }
    }
}
