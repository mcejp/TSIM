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
        public static void RenderSvg(SimulationCoordinateSpace coordinateSpace,
                                          INetworkDatabase ndb,
                                          List<Unit> units,
                                          string filename)
        {
            Console.WriteLine("RenderFullView start");
            
            var w = 1000;
            var h = 600;
            var scale = 0.04;        // meters/pixel

            var center = new PointD(w/2, h/2);

            SvgSurface surf = new SvgSurface(filename, w, h);
            Context cr = new Context(surf);

            cr.SetSourceRGB(1, 1, 1);
            cr.Rectangle(0, 0, w, h);
            cr.Fill();
            
            cr.SetSourceRGB(0, 0, 0);

            cr.MoveTo(0, h);
            cr.ShowText($"Scale: full width = {(w / scale)} meters");

            foreach (var seg in ndb.IterateSegments())
            {
                Trace.Assert(seg.ControlPoints.Length == 2);

                var start = To(seg.ControlPoints[0], center, scale);
                var end = To(seg.ControlPoints[1], center, scale);
                
                cr.MoveTo(start);
                cr.LineTo(end);
                cr.Stroke();
            }

            DrawCrosshair(cr, new PointD(w / 2, h / 2));

            surf.Flush();
            surf.Finish();
            
            Console.WriteLine("RenderFullView done");
        }

        private static void DrawCrosshair(Context cr, PointD pointD)
        {
            cr.Save();
            cr.SetSourceRGB(1, 0, 0);
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

        private static PointD To(Vector3 segControlPoint, PointD center, double scale)
        {
            return new PointD(center.X + segControlPoint.X * scale, center.Y - segControlPoint.Y * scale);
        }
    }
}
