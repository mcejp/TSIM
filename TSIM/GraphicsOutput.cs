using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Cairo;
using TSIM.Model;
using TSIM.RailroadDatabase;

namespace TSIM
{
    public class GraphicsOutput
    {
        // http://tango.freedesktop.org/Tango_Icon_Theme_Guidelines#Color_Palette
        private static readonly Color chameleon1 = FromHex("#8ae234");
        private static readonly Color chameleon3 = FromHex("#4e9a06");
        private static readonly Color scarledRed1 = FromHex("#ef2929");
        private static readonly Color plum1 = FromHex("#ad7fa8");
        private static readonly Color aluminium1 = FromHex("#eeeeec");
        private static readonly Color aluminium2 = FromHex("#d3d7ef");
        private static readonly Color aluminium5 = FromHex("#555753");
        private static readonly Color aluminium6 = FromHex("#2e3436");
        private static readonly Color skyBlue1 = FromHex("#729fcf");
        private static readonly Color skyBlue2 = FromHex("#3465a4");
        private static readonly Color skyBlue3 = FromHex("#204a87");

        private static readonly double railLineWidth = 1.5;

        // https://stackoverflow.com/a/24213444
        private static Color FromHex(string hex)
        {
            if (hex.StartsWith("#"))
            {
                hex = hex.Substring(1);
            }

            if (hex.Length != 6)
            {
                throw new ArgumentException("Color not valid");
            }

            return new Color(
                int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber) * (1.0 /255.0),
                int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber) * (1.0 /255.0),
                int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) * (1.0 /255.0));
        }

        private static void RenderToContext(SimulationCoordinateSpace coordinateSpace,
            INetworkDatabase ndb,
            IUnitDatabase units,
            IDictionary<int, TrainControlStateSummary>? controllerMap,
            Context cr,
            PointD center,
            double scale, int fontSize)
        {
            //Console.WriteLine("RenderFullView start");

            cr.SetSourceColor(aluminium1);
            cr.Paint();

//            cr.MoveTo(0, h);
//            cr.ShowText($"Scale: full width = {(w / scale)} meters");

            // Draw quadtree
            var maybeQuadTree = ndb.GetQuadTreeIfYouHaveOne();
            if (maybeQuadTree is {} quadTree)
            {
                DrawQuadTreeNode(quadTree.Root, cr, center, scale);
            }

            // Draw rail links
            DrawLinks(ndb, cr, center, scale);

            // Draw railway
            foreach (var seg in ndb.EnumerateSegments())
            {
                Trace.Assert(seg.ControlPoints.Length == 2);

                var start = SimToCanvasSpace(seg.ControlPoints[0], center, scale);
                var end = SimToCanvasSpace(seg.ControlPoints[1], center, scale);

                cr.LineWidth = railLineWidth;
                cr.SetSourceColor(seg.Oneway == false ? aluminium6 : aluminium5);
                cr.MoveTo(start);
                cr.LineTo(end);
                cr.Stroke();

//                DrawTextRegular(cr, $"{seg.Id}", new PointD((start.X + end.X) / 2, (start.Y + end.Y) / 2), 9);
            }

            // Draw stations
            foreach (var station in ndb.EnumerateStations())
            {
                PointD pos0 = new PointD();
                cr.SetSourceColor(skyBlue2);

                foreach (var stop in station.Stops)
                {
                    var pos = ndb.GetSegmentById(stop.SegmentId).GetPoint(stop.T);

                    var posCS = SimToCanvasSpace(pos, center, scale);
                    pos0 = posCS;

                    cr.LineWidth = 0.5;
                    DrawCrosshair(cr, posCS, 5);
                }

                cr.SetSourceColor(aluminium2);
                DrawTextRegular(cr, station.Name, pos0, fontSize);
            }

            // Draw trains
            int unitIndex = 0;
            foreach (var unit in units.EnumerateUnits())
            {
                var pos = unit.Pos;
                var head = unit.Pos + Vector3.Transform(new Vector3((float) (5 / scale), 0, 0), unit.Orientation);

                var posCS = SimToCanvasSpace(pos, center, scale);
                var headCS = SimToCanvasSpace(head, center, scale);

                var info = $"{unit.Class.Name}\n{unit.Velocity.Length() * 3.6:F1} km/h";
                if (controllerMap != null && controllerMap.ContainsKey(unitIndex)) {
                    info += "\n" + controllerMap[unitIndex].SchedulerState;

                    var route = controllerMap[unitIndex].SegmentsToFollow;

                    if (route != null) {
                        foreach (var entry in route) {
                            // try {
                            // Console.WriteLine($"Segment => {entry.SegmentId}");
                            var seg = ndb.GetSegmentById(entry.SegmentId);
                            Trace.Assert(seg.ControlPoints.Length == 2);

                            var startXyz = seg.GetEndpoint(entry.EntryEp);
                            var endXyz = entry.GoalT >= 0 ? seg.GetPoint(entry.GoalT) : seg.GetEndpoint(entry.EntryEp.Other());

                            var start = SimToCanvasSpace(startXyz, center, scale);
                            var end = SimToCanvasSpace(endXyz, center, scale);

                            cr.LineWidth = railLineWidth * 2;
                            cr.SetSourceColor(plum1);
                            cr.MoveTo(start);
                            cr.LineTo(end);
                            cr.Stroke();
                            // }
                            // catch (System.InvalidOperationException ex) {
                            // }
                        }
                    }
                }

                cr.LineWidth = railLineWidth * 2;
                cr.SetSourceColor(chameleon1);
                cr.MoveTo(posCS.X * 2 - headCS.X, posCS.Y * 2 - headCS.Y);
                cr.LineTo(headCS);
                cr.Stroke();

                cr.SetSourceColor(chameleon3);
                DrawTextBold(cr, info, posCS, fontSize);

                unitIndex++;
            }

            // Draw info about agents
            // if (agents != null)
            // {
            //     int i = 0;

            //     foreach (var agent in agents)
            //     {
            //         DrawTextRegular(cr, agent.ToString(), new PointD(0, i * 10), 9);
            //         i++;
            //     }
            // }

            cr.LineWidth = 1;
            cr.SetSourceColor(scarledRed1);
            DrawCrosshair(cr, center, 10);

            //Console.WriteLine("RenderFullView done");
        }

        private static void DrawTextBold(Context cr, string text, PointD pos, int fontSize)
        {
            var layout = Pango.CairoHelper.CreateLayout(cr);
            layout.FontDescription = Pango.FontDescription.FromString("DejaVu Sans Bold " + fontSize);
            layout.SetText(text);
            cr.MoveTo(pos);
            Pango.CairoHelper.ShowLayout(cr, layout);
        }

        private static void DrawTextRegular(Context cr, string text, PointD pos, int fontSize)
        {
            var layout = Pango.CairoHelper.CreateLayout(cr);
            layout.FontDescription = Pango.FontDescription.FromString("DejaVu Sans " + fontSize);
            layout.SetText(text);
            cr.MoveTo(pos);
            Pango.CairoHelper.ShowLayout(cr, layout);
        }

        private static void DrawLinks(INetworkDatabase ndb, Context cr, PointD center, double scale)
        {
            foreach (var link in ndb.EnumerateSegmentLinks())
            {
                var pos = ndb.GetSegmentById(link.Segment1).GetEndpoint(link.Ep1);

                var c = aluminium6;
                c.A = 0.1;
                var pt = SimToCanvasSpace(new Vector3(pos.X, pos.Y, 0), center, scale);

                cr.SetSourceColor(c);
                cr.Arc(pt.X, pt.Y, 1, 0, 2*Math.PI);
                cr.Fill();
            }
        }

        private static void DrawQuadTreeNode(QuadTreeNode node, Context cr, PointD center, double scale)
        {
            var c = scarledRed1;

            if (node.SegmentIds == null)
            {
                c.A = 0.1;
            }

            cr.SetSourceColor(c);
            cr.LineWidth = 0.2;
            cr.MoveTo(SimToCanvasSpace(new Vector3(node.BoundingMin.X, node.BoundingMin.Y, 0), center, scale));
            cr.LineTo(SimToCanvasSpace(new Vector3(node.BoundingMax.X, node.BoundingMin.Y, 0), center, scale));
            cr.LineTo(SimToCanvasSpace(new Vector3(node.BoundingMax.X, node.BoundingMax.Y, 0), center, scale));
            cr.LineTo(SimToCanvasSpace(new Vector3(node.BoundingMin.X, node.BoundingMax.Y, 0), center, scale));
            cr.LineTo(SimToCanvasSpace(new Vector3(node.BoundingMin.X, node.BoundingMin.Y, 0), center, scale));
            cr.Stroke();

            if (node.Quadrants != null)
            {
                foreach (var q in node.Quadrants)
                {
                    DrawQuadTreeNode(q, cr, center, scale);
                }
            }
        }

        public static void RenderSvg(SimulationCoordinateSpace coordinateSpace,
            INetworkDatabase ndb,
            IUnitDatabase units,
            string filename)
        {
            var w = 1000;
            var h = 600;
            var scale = 0.04;        // meters/pixel
            var fontSize = 8;

            var center = new PointD(w/2, h/2);

            var surf = new SvgSurface(filename, w, h);
            Context cr = new Context(surf);

            RenderToContext(coordinateSpace, ndb, units, null, cr, center, scale, fontSize);

            cr.Dispose();
            surf.Flush();
            surf.Finish();
            surf.Dispose();
        }

        public static void RenderPng(SimulationCoordinateSpace coordinateSpace,
            INetworkDatabase ndb,
            IUnitDatabase units,
            IDictionary<int, TrainControlStateSummary>? controllerMap,
            string filename,
            int w, int h,
            double scale, int fontSize)
        {
            var center = new PointD(w/2, h/2);

            var surf = new ImageSurface(Format.Argb32, w, h);
            Context cr = new Context(surf);

            RenderToContext(coordinateSpace, ndb, units, controllerMap, cr, center, scale, fontSize);

            cr.Dispose();
            surf.WriteToPng(filename);
            surf.Finish();
            surf.Dispose();
        }

        private static void DrawCrosshair(Context cr, PointD pointD, int radius)
        {
            cr.Save();

            cr.MoveTo(pointD);
            cr.RelMoveTo(-radius, 0);
            cr.RelLineTo(2 * radius, 0);
            cr.Stroke();
            cr.MoveTo(pointD);
            cr.RelMoveTo(0, -radius);
            cr.RelLineTo(0, 2 * radius);
            cr.Stroke();

            cr.Restore();
        }

        private static PointD SimToCanvasSpace(Vector3 segControlPoint, PointD center, double scale)
        {
            return new PointD(center.X + segControlPoint.X * scale, center.Y - segControlPoint.Y * scale);
        }
    }
}
