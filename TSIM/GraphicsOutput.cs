using System;
using System.Diagnostics;
using System.Numerics;
using Cairo;
using TSIM.Model;
using TSIM.RailroadDatabase;

namespace TSIM
{
    public class GraphicsOutput
    {
        private static readonly Color chameleon1 = FromHex("#8ae234");
        private static readonly Color chameleon3 = FromHex("#4e9a06");
        private static readonly Color scarledRed1 = FromHex("#ef2929");
        private static readonly Color plum1 = FromHex("#ad7fa8");
        private static readonly Color aluminium1 = FromHex("#eeeeec");
        private static readonly Color aluminium6 = FromHex("#2e3436");

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
                                          Context cr,
                                          PointD center,
                                          double scale)
        {
            Console.WriteLine("RenderFullView start");

            cr.SetSourceColor(aluminium1);
            cr.Paint();

//            cr.MoveTo(0, h);
//            cr.ShowText($"Scale: full width = {(w / scale)} meters");

            // Draw quadtree
            DrawQuadTreeNode(GeoJsonNetworkDatabase.StaticInstanceForDebug.GetQuadTreeForDebug().GetRootNodeForDebug(), cr, center, scale);

            // Draw rail links
            DrawLinks(ndb, cr, center, scale);

            // Draw railway
            foreach (var seg in ndb.EnumerateSegments())
            {
                Trace.Assert(seg.ControlPoints.Length == 2);

                var start = SimToCanvasSpace(seg.ControlPoints[0], center, scale);
                var end = SimToCanvasSpace(seg.ControlPoints[1], center, scale);

                cr.LineWidth = 1;
                cr.SetSourceColor(aluminium6);
                cr.MoveTo(start);
                cr.LineTo(end);
                cr.Stroke();
            }

            // Draw trains
            foreach (var unit in units.EnumerateUnits())
            {
                var pos = unit.Pos;
                var head = unit.Pos + Vector3.Transform(new Vector3((float) (5 / scale), 0, 0), unit.Orientation);

                var posCS = SimToCanvasSpace(pos, center, scale);
                var headCS = SimToCanvasSpace(head, center, scale);

                cr.LineWidth = 2;
                cr.SetSourceColor(chameleon1);
                cr.MoveTo(posCS.X * 2 - headCS.X, posCS.Y * 2 - headCS.Y);
                cr.LineTo(headCS);
                cr.Stroke();

                var layout = Pango.CairoHelper.CreateLayout(cr);
                layout.FontDescription = Pango.FontDescription.FromString("Arial Bold 7");
                layout.SetText($"{unit.Class.Name}\n{unit.Velocity.Length() * 3.6:F1} km/h");
                cr.SetSourceColor(chameleon3);
                cr.MoveTo(posCS);
                Pango.CairoHelper.ShowLayout(cr, layout);
            }

            DrawCrosshair(cr, center);

            Console.WriteLine("RenderFullView done");
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

            var center = new PointD(w/2, h/2);

            var surf = new SvgSurface(filename, w, h);
            Context cr = new Context(surf);

            RenderToContext(coordinateSpace, ndb, units, cr, center, scale);

            surf.Flush();
            surf.Finish();
        }

        public static void RenderPng(SimulationCoordinateSpace coordinateSpace,
            INetworkDatabase ndb,
            IUnitDatabase units,
            string filename,
            int w, int h,
            double scale)
        {
            var center = new PointD(w/2, h/2);

            var surf = new ImageSurface(Format.Argb32, w, h);
            Context cr = new Context(surf);

            RenderToContext(coordinateSpace, ndb, units, cr, center, scale);

            surf.WriteToPng(filename);
            surf.Finish();
        }

        private static void DrawCrosshair(Context cr, PointD pointD)
        {
            cr.Save();
            cr.SetSourceColor(scarledRed1);
            cr.LineWidth = 1;

            cr.MoveTo(pointD);
            cr.RelMoveTo(-10, 0);
            cr.RelLineTo(20, 0);
            cr.Stroke();
            cr.MoveTo(pointD);
            cr.RelMoveTo(0, -10);
            cr.RelLineTo(0, 20);
            cr.Stroke();

            cr.Restore();
        }

        private static PointD SimToCanvasSpace(Vector3 segControlPoint, PointD center, double scale)
        {
            return new PointD(center.X + segControlPoint.X * scale, center.Y - segControlPoint.Y * scale);
        }
    }
}
