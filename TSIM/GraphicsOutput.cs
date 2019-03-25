using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Cairo;
using TSIM.Model;
using TSIM.RailroadDatabase;

namespace TSIM
{
    internal class GraphicsOutput
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

        public static void RenderSvg(SimulationCoordinateSpace coordinateSpace,
                                          INetworkDatabase ndb,
                                          IEnumerable<Unit> units,
                                          string filename)
        {
            Console.WriteLine("RenderFullView start");

            var w = 1000;
            var h = 600;
            var scale = 0.04;        // meters/pixel

            var center = new PointD(w/2, h/2);

            SvgSurface surf = new SvgSurface(filename, w, h);
            Context cr = new Context(surf);

            cr.SetSourceColor(aluminium1);
            cr.Rectangle(0, 0, w, h);
            cr.Fill();

            cr.SetSourceColor(aluminium6);

            cr.MoveTo(0, h);
            cr.ShowText($"Scale: full width = {(w / scale)} meters");

            // Draw railway
            foreach (var seg in ndb.EnumerateSegments())
            {
                Trace.Assert(seg.ControlPoints.Length == 2);

                var start = SimToCanvasSpace(seg.ControlPoints[0], center, scale);
                var end = SimToCanvasSpace(seg.ControlPoints[1], center, scale);

                cr.LineWidth = 1;
                cr.MoveTo(start);
                cr.LineTo(end);
                cr.Stroke();
            }

            // Draw trains
            foreach (var unit in units)
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
                layout.SetText($"{unit.Class.Name}\n{unit.Velocity.Length() * 3.6} km/h");
                cr.SetSourceColor(chameleon3);
                cr.MoveTo(posCS);
                Pango.CairoHelper.ShowLayout(cr, layout);
            }

            DrawCrosshair(cr, new PointD(w / 2, h / 2));

            surf.Flush();
            surf.Finish();

            Console.WriteLine("RenderFullView done");
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
