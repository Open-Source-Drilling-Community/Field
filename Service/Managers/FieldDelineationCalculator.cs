using NORCE.Drilling.Field.Model;
using OSDC.DotnetLibraries.General.Math;
using Clipper2Lib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NORCE.Drilling.Field.Service.Managers
{
    internal static class FieldDelineationCalculator
    {
        public static void Calculate(Model.Field field)
        {
            if (field.DelineationLines == null)
            {
                return;
            }

            foreach (FieldDelineationLine line in field.DelineationLines)
            {
                line.CalculatedBoundaryLines = CalculateBoundaryLines(line);
            }
        }

        private static List<FieldDelineationBoundaryLine> CalculateBoundaryLines(FieldDelineationLine line)
        {
            List<FieldDelineationBoundaryLine> boundaries = [];
            if (line.Points == null || line.Points.Count < 2 || line.Margin == null || line.Margin <= 0)
            {
                return boundaries;
            }

            List<Point3DGlobalCoordinates> points = NormalizePoints(line.Points);
            if (points.Count < 2)
            {
                return boundaries;
            }

            double margin = line.Margin.Value;
            bool closed = IsClosed(points);
            if (closed)
            {
                List<Point3DGlobalCoordinates> polygon = points.Take(points.Count - 1).ToList();
                if (polygon.Count < 3)
                {
                    return boundaries;
                }

                double signedArea = SignedArea(polygon);
                if (System.Math.Abs(signedArea) < 1e-9)
                {
                    return boundaries;
                }

                foreach (List<Point3DGlobalCoordinates> offset in OffsetClosedPolygon(polygon, margin))
                {
                    offset.Add(ClonePoint(offset[0]));
                    boundaries.Add(new FieldDelineationBoundaryLine
                    {
                        ID = Guid.NewGuid(),
                        IsInteriorBoundary = true,
                        IsClosed = true,
                        Points = offset
                    });
                }
            }
            else
            {
                List<Point3DGlobalCoordinates> left = OffsetOpenPolyline(points, margin);
                List<Point3DGlobalCoordinates> right = OffsetOpenPolyline(points, -margin);
                AddOpenOffsetBoundaries(boundaries, points, margin, [left, right]);
            }

            return boundaries;
        }

        private static void AddOpenOffsetBoundaries(
            List<FieldDelineationBoundaryLine> boundaries,
            List<Point3DGlobalCoordinates> original,
            double margin,
            List<List<Point3DGlobalCoordinates>> offsets)
        {
            foreach (OffsetFragment fragment in SplitOpenOffsets(original, margin, offsets))
            {
                if (fragment.Points.Count < (fragment.IsClosed ? 4 : 2))
                {
                    continue;
                }

                boundaries.Add(new FieldDelineationBoundaryLine
                    {
                        ID = Guid.NewGuid(),
                        IsInteriorBoundary = false,
                        IsClosed = fragment.IsClosed,
                        Points = fragment.Points
                    });
            }
        }

        private static List<Point3DGlobalCoordinates> NormalizePoints(IEnumerable<Point3DGlobalCoordinates> source)
        {
            List<Point3DGlobalCoordinates> points = [];
            foreach (Point3DGlobalCoordinates point in source)
            {
                if (point.RiemannianNorth == null || point.RiemannianEast == null)
                {
                    continue;
                }

                points.Add(ClonePoint(point));
            }

            return points;
        }

        private static List<Point3DGlobalCoordinates> OffsetOpenPolyline(List<Point3DGlobalCoordinates> points, double distance)
        {
            List<Point3DGlobalCoordinates> offset = [];
            if (points.Count < 2)
            {
                return offset;
            }

            for (int i = 0; i < points.Count; i++)
            {
                if (i == 0)
                {
                    Vector2D normal = SegmentNormal(points[0], points[1], distance);
                    offset.Add(Translated(points[i], normal));
                }
                else if (i == points.Count - 1)
                {
                    Vector2D normal = SegmentNormal(points[i - 1], points[i], distance);
                    offset.Add(Translated(points[i], normal));
                }
                else
                {
                    Vector2D normalBefore = SegmentNormal(points[i - 1], points[i], distance);
                    Vector2D normalAfter = SegmentNormal(points[i], points[i + 1], distance);
                    double turn = TurnCrossProduct(points[i - 1], points[i], points[i + 1]);
                    if (System.Math.Abs(turn) > 1e-12 && turn * distance > 0)
                    {
                        AddArc(offset, points[i], normalBefore, normalAfter, turn);
                    }
                    else if (TryIntersectLines(
                        Translated(points[i - 1], normalBefore), Translated(points[i], normalBefore),
                        Translated(points[i], normalAfter), Translated(points[i + 1], normalAfter),
                        out Point3DGlobalCoordinates? intersection))
                    {
                        intersection!.Z = points[i].Z;
                        offset.Add(intersection);
                    }
                    else
                    {
                        offset.Add(Translated(points[i], Average(normalBefore, normalAfter)));
                    }
                }
            }

            return offset;
        }

        private static List<OffsetFragment> SplitOpenOffsets(
            List<Point3DGlobalCoordinates> original,
            double margin,
            List<List<Point3DGlobalCoordinates>> offsets)
        {
            List<List<Point3DGlobalCoordinates>> walkedOffsets = InsertOffsetIntersections(offsets);
            List<OffsetFragment> fragments = [];
            foreach (List<Point3DGlobalCoordinates> walkedOffset in walkedOffsets)
            {
                foreach (OffsetFragment fragment in ExtractClosedFragments(walkedOffset))
                {
                    fragments.AddRange(FilterValidOffsetFragment(fragment, original, margin));
                }
            }

            return fragments;
        }

        private static List<List<Point3DGlobalCoordinates>> InsertOffsetIntersections(List<List<Point3DGlobalCoordinates>> offsets)
        {
            List<List<Point3DGlobalCoordinates>> cleanedOffsets = offsets
                .Select(RemoveDuplicateConsecutivePoints)
                .Where(offset => offset.Count >= 2)
                .ToList();
            List<List<List<SegmentPoint>>> offsetSegmentPoints = [];
            for (int offsetIndex = 0; offsetIndex < cleanedOffsets.Count; offsetIndex++)
            {
                List<Point3DGlobalCoordinates> points = cleanedOffsets[offsetIndex];
                List<List<SegmentPoint>> segmentPoints = [];
                for (int segmentIndex = 0; segmentIndex < points.Count - 1; segmentIndex++)
                {
                    segmentPoints.Add([
                        new SegmentPoint(0.0, ClonePoint(points[segmentIndex])),
                        new SegmentPoint(1.0, ClonePoint(points[segmentIndex + 1]))
                    ]);
                }

                offsetSegmentPoints.Add(segmentPoints);
            }

            for (int firstOffsetIndex = 0; firstOffsetIndex < cleanedOffsets.Count; firstOffsetIndex++)
            {
                List<Point3DGlobalCoordinates> firstOffset = cleanedOffsets[firstOffsetIndex];
                for (int firstSegmentIndex = 0; firstSegmentIndex < firstOffset.Count - 1; firstSegmentIndex++)
                {
                    for (int secondOffsetIndex = firstOffsetIndex; secondOffsetIndex < cleanedOffsets.Count; secondOffsetIndex++)
                    {
                        List<Point3DGlobalCoordinates> secondOffset = cleanedOffsets[secondOffsetIndex];
                        int secondSegmentStart = secondOffsetIndex == firstOffsetIndex ? firstSegmentIndex + 2 : 0;
                        for (int secondSegmentIndex = secondSegmentStart; secondSegmentIndex < secondOffset.Count - 1; secondSegmentIndex++)
                        {
                            if (TryIntersectSegments(
                                firstOffset[firstSegmentIndex], firstOffset[firstSegmentIndex + 1],
                                secondOffset[secondSegmentIndex], secondOffset[secondSegmentIndex + 1],
                                out Point3DGlobalCoordinates? intersection, out double firstParameter, out double secondParameter))
                            {
                                if (firstParameter > IntersectionTolerance && firstParameter < 1.0 - IntersectionTolerance &&
                                    secondParameter > IntersectionTolerance && secondParameter < 1.0 - IntersectionTolerance)
                                {
                                    offsetSegmentPoints[firstOffsetIndex][firstSegmentIndex].Add(new SegmentPoint(firstParameter, ClonePoint(intersection!)));
                                    offsetSegmentPoints[secondOffsetIndex][secondSegmentIndex].Add(new SegmentPoint(secondParameter, ClonePoint(intersection!)));
                                }
                            }
                        }
                    }
                }
            }

            List<List<Point3DGlobalCoordinates>> walkedOffsets = [];
            foreach (List<List<SegmentPoint>> offsetSegments in offsetSegmentPoints)
            {
                List<Point3DGlobalCoordinates> walked = [];
                foreach (List<SegmentPoint> segment in offsetSegments)
                {
                    foreach (SegmentPoint segmentPoint in segment
                        .OrderBy(point => point.Parameter)
                        .ThenBy(point => point.Point.RiemannianNorth ?? 0)
                        .ThenBy(point => point.Point.RiemannianEast ?? 0))
                    {
                        AddIfDistinct(walked, segmentPoint.Point);
                    }
                }

                if (walked.Count >= 2)
                {
                    walkedOffsets.Add(walked);
                }
            }

            return walkedOffsets;
        }

        private static List<OffsetFragment> FilterValidOffsetFragment(OffsetFragment fragment, List<Point3DGlobalCoordinates> original, double margin)
        {
            if (fragment.IsClosed)
            {
                for (int i = 0; i < fragment.Points.Count - 1; i++)
                {
                    if (!IsValidOffsetSegment(fragment.Points[i], fragment.Points[i + 1], original, margin))
                    {
                        return [];
                    }
                }

                return [fragment];
            }

            List<OffsetFragment> fragments = [];
            List<Point3DGlobalCoordinates> current = [];
            for (int i = 0; i < fragment.Points.Count - 1; i++)
            {
                Point3DGlobalCoordinates start = fragment.Points[i];
                Point3DGlobalCoordinates end = fragment.Points[i + 1];
                if (IsValidOffsetSegment(start, end, original, margin))
                {
                    if (current.Count == 0)
                    {
                        current.Add(ClonePoint(start));
                    }

                    AddIfDistinct(current, ClonePoint(end));
                }
                else if (current.Count >= 2)
                {
                    fragments.Add(new OffsetFragment(current, false));
                    current = [];
                }
            }

            if (current.Count >= 2)
            {
                fragments.Add(new OffsetFragment(current, false));
            }

            return fragments;
        }

        private static bool IsValidOffsetSegment(Point3DGlobalCoordinates start, Point3DGlobalCoordinates end, List<Point3DGlobalCoordinates> original, double margin)
        {
            Point3DGlobalCoordinates midpoint = CreatePoint(
                ((start.RiemannianNorth ?? 0) + (end.RiemannianNorth ?? 0)) * 0.5,
                ((start.RiemannianEast ?? 0) + (end.RiemannianEast ?? 0)) * 0.5);
            return MinimumDistanceToPolyline(midpoint, original) >= margin - DistanceTolerance;
        }

        private static double MinimumDistanceToPolyline(Point3DGlobalCoordinates point, List<Point3DGlobalCoordinates> polyline)
        {
            double minimumDistance = double.PositiveInfinity;
            for (int i = 0; i < polyline.Count - 1; i++)
            {
                minimumDistance = System.Math.Min(minimumDistance, DistanceToSegment(point, polyline[i], polyline[i + 1]));
            }

            return minimumDistance;
        }

        private static double DistanceToSegment(Point3DGlobalCoordinates point, Point3DGlobalCoordinates start, Point3DGlobalCoordinates end)
        {
            double px = point.RiemannianEast ?? 0;
            double py = point.RiemannianNorth ?? 0;
            double ax = start.RiemannianEast ?? 0;
            double ay = start.RiemannianNorth ?? 0;
            double bx = end.RiemannianEast ?? 0;
            double by = end.RiemannianNorth ?? 0;
            double dx = bx - ax;
            double dy = by - ay;
            double lengthSquared = dx * dx + dy * dy;
            if (lengthSquared <= 1e-24)
            {
                return System.Math.Sqrt((px - ax) * (px - ax) + (py - ay) * (py - ay));
            }

            double parameter = ((px - ax) * dx + (py - ay) * dy) / lengthSquared;
            parameter = System.Math.Max(0.0, System.Math.Min(1.0, parameter));
            double closestX = ax + parameter * dx;
            double closestY = ay + parameter * dy;
            return System.Math.Sqrt((px - closestX) * (px - closestX) + (py - closestY) * (py - closestY));
        }

        private static List<OffsetFragment> ExtractClosedFragments(List<Point3DGlobalCoordinates> walked)
        {
            List<OffsetFragment> fragments = [];
            List<Point3DGlobalCoordinates> current = [];

            foreach (Point3DGlobalCoordinates point in walked)
            {
                int existingIndex = FindSameHorizontalPoint(current, point);
                if (existingIndex >= 0)
                {
                    List<Point3DGlobalCoordinates> loop = current
                        .Skip(existingIndex)
                        .Select(ClonePoint)
                        .ToList();
                    AddIfDistinct(loop, ClonePoint(point));
                    if (!SameHorizontalPoint(loop[0], loop[^1]))
                    {
                        loop.Add(ClonePoint(loop[0]));
                    }

                    List<Point3DGlobalCoordinates> polygon = loop.Take(loop.Count - 1).ToList();
                    if (polygon.Count >= 3 && System.Math.Abs(SignedArea(polygon)) > 1e-9)
                    {
                        fragments.Add(new OffsetFragment(loop, true));
                    }

                    current = current
                        .Take(existingIndex + 1)
                        .Select(ClonePoint)
                        .ToList();
                }
                else
                {
                    current.Add(ClonePoint(point));
                }
            }

            current = RemoveDuplicateConsecutivePoints(current);
            if (current.Count >= 2)
            {
                fragments.Add(new OffsetFragment(current, false));
            }

            return fragments;
        }

        private static int FindSameHorizontalPoint(List<Point3DGlobalCoordinates> points, Point3DGlobalCoordinates point)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (SameHorizontalPoint(points[i], point))
                {
                    return i;
                }
            }

            return -1;
        }

        private static double TurnCrossProduct(Point3DGlobalCoordinates previous, Point3DGlobalCoordinates current, Point3DGlobalCoordinates next)
        {
            double x1 = (current.RiemannianEast ?? 0) - (previous.RiemannianEast ?? 0);
            double y1 = (current.RiemannianNorth ?? 0) - (previous.RiemannianNorth ?? 0);
            double x2 = (next.RiemannianEast ?? 0) - (current.RiemannianEast ?? 0);
            double y2 = (next.RiemannianNorth ?? 0) - (current.RiemannianNorth ?? 0);
            return x1 * y2 - y1 * x2;
        }

        private static void AddArc(List<Point3DGlobalCoordinates> target, Point3DGlobalCoordinates center, Vector2D startOffset, Vector2D endOffset, double turn)
        {
            double radius = System.Math.Sqrt((startOffset.X ?? 0) * (startOffset.X ?? 0) + (startOffset.Y ?? 0) * (startOffset.Y ?? 0));
            if (radius <= 1e-12)
            {
                AddIfDistinct(target, Translated(center, endOffset));
                return;
            }

            double startAngle = System.Math.Atan2(startOffset.X ?? 0, startOffset.Y ?? 0);
            double endAngle = System.Math.Atan2(endOffset.X ?? 0, endOffset.Y ?? 0);
            if (turn > 0)
            {
                while (endAngle < startAngle)
                {
                    endAngle += 2.0 * System.Math.PI;
                }
            }
            else
            {
                while (endAngle > startAngle)
                {
                    endAngle -= 2.0 * System.Math.PI;
                }
            }

            double sweep = endAngle - startAngle;
            int segmentCount = System.Math.Max(1, (int)System.Math.Ceiling(System.Math.Abs(sweep) / MaximumArcAngleStep));
            for (int i = 0; i <= segmentCount; i++)
            {
                double angle = startAngle + sweep * i / segmentCount;
                Point3DGlobalCoordinates point = CreatePoint(
                    (center.RiemannianNorth ?? 0) + radius * System.Math.Sin(angle),
                    (center.RiemannianEast ?? 0) + radius * System.Math.Cos(angle));
                AddIfDistinct(target, point);
            }
        }

        private static void AddIfDistinct(List<Point3DGlobalCoordinates> target, Point3DGlobalCoordinates point)
        {
            if (target.Count == 0 || !SameHorizontalPoint(target[^1], point))
            {
                target.Add(point);
            }
        }

        private static List<List<Point3DGlobalCoordinates>> OffsetClosedPolygon(List<Point3DGlobalCoordinates> points, double margin)
        {
            List<List<Point3DGlobalCoordinates>> polygons = [];
            PathD path = new(points.Select(point => new PointD(point.RiemannianEast ?? 0, point.RiemannianNorth ?? 0)));
            PathsD offsetPaths = Clipper.InflatePaths(
                new PathsD { path },
                -margin,
                JoinType.Round,
                EndType.Polygon,
                2.0,
                6,
                0.25);

            foreach (PathD offsetPath in offsetPaths)
            {
                List<Point3DGlobalCoordinates> polygon = offsetPath
                    .Select(point => CreatePoint(point.y, point.x))
                    .ToList();
                polygon = RemoveDuplicateConsecutivePoints(polygon);
                if (polygon.Count < 3 || System.Math.Abs(SignedArea(polygon)) < 1e-9)
                {
                    continue;
                }

                RotateToStableStart(polygon);
                polygons.Add(polygon);
            }

            return polygons;
        }

        private static bool IsClosed(List<Point3DGlobalCoordinates> points)
        {
            if (points.Count < 3)
            {
                return false;
            }

            Point3DGlobalCoordinates first = points[0];
            Point3DGlobalCoordinates last = points[^1];
            double dn = (last.RiemannianNorth ?? 0) - (first.RiemannianNorth ?? 0);
            double de = (last.RiemannianEast ?? 0) - (first.RiemannianEast ?? 0);
            return dn * dn + de * de <= 1e-6;
        }

        private static double SignedArea(List<Point3DGlobalCoordinates> points)
        {
            double area = 0;
            for (int i = 0; i < points.Count; i++)
            {
                Point3DGlobalCoordinates current = points[i];
                Point3DGlobalCoordinates next = points[(i + 1) % points.Count];
                area += (current.RiemannianEast ?? 0) * (next.RiemannianNorth ?? 0) -
                    (next.RiemannianEast ?? 0) * (current.RiemannianNorth ?? 0);
            }

            return 0.5 * area;
        }

        private static Vector2D SegmentNormal(Point3DGlobalCoordinates start, Point3DGlobalCoordinates end, double distance)
        {
            double dn = (end.RiemannianNorth ?? 0) - (start.RiemannianNorth ?? 0);
            double de = (end.RiemannianEast ?? 0) - (start.RiemannianEast ?? 0);
            double length = System.Math.Sqrt(dn * dn + de * de);
            if (length <= 0)
            {
                return new Vector2D();
            }

            return new Vector2D
            {
                X = -de / length * distance,
                Y = dn / length * distance
            };
        }

        private static Vector2D Average(Vector2D first, Vector2D second)
        {
            return new Vector2D
            {
                X = ((first.X ?? 0) + (second.X ?? 0)) * 0.5,
                Y = ((first.Y ?? 0) + (second.Y ?? 0)) * 0.5
            };
        }

        private static Point3DGlobalCoordinates Translated(Point3DGlobalCoordinates point, Vector2D offset)
        {
            Point3DGlobalCoordinates translated = ClonePoint(point);
            translated.RiemannianNorth = (point.RiemannianNorth ?? 0) + (offset.X ?? 0);
            translated.RiemannianEast = (point.RiemannianEast ?? 0) + (offset.Y ?? 0);
            if (translated.RiemannianNorth != null && translated.RiemannianEast != null)
            {
                translated.SetLatitudeLongitude(translated.RiemannianNorth.Value, translated.RiemannianEast.Value);
            }

            return translated;
        }

        private static bool TryIntersectLines(
            Point3DGlobalCoordinates a1,
            Point3DGlobalCoordinates a2,
            Point3DGlobalCoordinates b1,
            Point3DGlobalCoordinates b2,
            out Point3DGlobalCoordinates? intersection)
        {
            intersection = null;
            double x1 = a1.RiemannianEast ?? 0;
            double y1 = a1.RiemannianNorth ?? 0;
            double x2 = a2.RiemannianEast ?? 0;
            double y2 = a2.RiemannianNorth ?? 0;
            double x3 = b1.RiemannianEast ?? 0;
            double y3 = b1.RiemannianNorth ?? 0;
            double x4 = b2.RiemannianEast ?? 0;
            double y4 = b2.RiemannianNorth ?? 0;

            double denominator = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
            if (System.Math.Abs(denominator) < 1e-12)
            {
                return false;
            }

            double px = ((x1 * y2 - y1 * x2) * (x3 - x4) - (x1 - x2) * (x3 * y4 - y3 * x4)) / denominator;
            double py = ((x1 * y2 - y1 * x2) * (y3 - y4) - (y1 - y2) * (x3 * y4 - y3 * x4)) / denominator;
            intersection = new Point3DGlobalCoordinates
            {
                RiemannianNorth = py,
                RiemannianEast = px,
                Z = a2.Z
            };
            intersection.SetLatitudeLongitude(py, px);
            return true;
        }

        private static bool TryIntersectSegments(
            Point3DGlobalCoordinates a1,
            Point3DGlobalCoordinates a2,
            Point3DGlobalCoordinates b1,
            Point3DGlobalCoordinates b2,
            out Point3DGlobalCoordinates? intersection,
            out double firstParameter,
            out double secondParameter)
        {
            intersection = null;
            firstParameter = double.NaN;
            secondParameter = double.NaN;

            double x1 = a1.RiemannianEast ?? 0;
            double y1 = a1.RiemannianNorth ?? 0;
            double x2 = a2.RiemannianEast ?? 0;
            double y2 = a2.RiemannianNorth ?? 0;
            double x3 = b1.RiemannianEast ?? 0;
            double y3 = b1.RiemannianNorth ?? 0;
            double x4 = b2.RiemannianEast ?? 0;
            double y4 = b2.RiemannianNorth ?? 0;

            double dx1 = x2 - x1;
            double dy1 = y2 - y1;
            double dx2 = x4 - x3;
            double dy2 = y4 - y3;
            double denominator = dx1 * dy2 - dy1 * dx2;
            if (System.Math.Abs(denominator) < 1e-12)
            {
                return false;
            }

            double dx3 = x3 - x1;
            double dy3 = y3 - y1;
            firstParameter = (dx3 * dy2 - dy3 * dx2) / denominator;
            secondParameter = (dx3 * dy1 - dy3 * dx1) / denominator;
            if (firstParameter < -IntersectionTolerance || firstParameter > 1.0 + IntersectionTolerance ||
                secondParameter < -IntersectionTolerance || secondParameter > 1.0 + IntersectionTolerance)
            {
                return false;
            }

            double east = x1 + firstParameter * dx1;
            double north = y1 + firstParameter * dy1;
            intersection = CreatePoint(north, east);
            return true;
        }

        private static Point3DGlobalCoordinates ClonePoint(Point3DGlobalCoordinates point)
        {
            return new Point3DGlobalCoordinates
            {
                RiemannianNorth = point.RiemannianNorth,
                RiemannianEast = point.RiemannianEast,
                Z = point.Z,
                Latitude = point.Latitude,
                Longitude = point.Longitude,
                TVD = point.TVD
            };
        }

        private static Point3DGlobalCoordinates CreatePoint(double north, double east)
        {
            Point3DGlobalCoordinates point = new()
            {
                RiemannianNorth = north,
                RiemannianEast = east,
                Z = 0,
                TVD = 0
            };
            point.SetLatitudeLongitude(north, east);
            return point;
        }

        private static List<Point3DGlobalCoordinates> RemoveDuplicateConsecutivePoints(List<Point3DGlobalCoordinates> points)
        {
            List<Point3DGlobalCoordinates> result = [];
            foreach (Point3DGlobalCoordinates point in points)
            {
                if (result.Count == 0 || !SameHorizontalPoint(result[^1], point))
                {
                    result.Add(point);
                }
            }

            if (result.Count > 1 && SameHorizontalPoint(result[0], result[^1]))
            {
                result.RemoveAt(result.Count - 1);
            }

            return result;
        }

        private static bool SameHorizontalPoint(Point3DGlobalCoordinates first, Point3DGlobalCoordinates second)
        {
            double dn = (first.RiemannianNorth ?? 0) - (second.RiemannianNorth ?? 0);
            double de = (first.RiemannianEast ?? 0) - (second.RiemannianEast ?? 0);
            return dn * dn + de * de <= 1e-12;
        }

        private static void RotateToStableStart(List<Point3DGlobalCoordinates> points)
        {
            if (points.Count < 2)
            {
                return;
            }

            int index = 0;
            for (int i = 1; i < points.Count; i++)
            {
                double north = points[i].RiemannianNorth ?? 0;
                double east = points[i].RiemannianEast ?? 0;
                double bestNorth = points[index].RiemannianNorth ?? 0;
                double bestEast = points[index].RiemannianEast ?? 0;
                if (north < bestNorth - 1e-9 || (System.Math.Abs(north - bestNorth) <= 1e-9 && east < bestEast))
                {
                    index = i;
                }
            }

            if (index == 0)
            {
                return;
            }

            List<Point3DGlobalCoordinates> rotated = points.Skip(index).Concat(points.Take(index)).ToList();
            points.Clear();
            points.AddRange(rotated);
        }

        private const double MaximumArcAngleStep = System.Math.PI / 18.0;
        private const double IntersectionTolerance = 1e-9;
        private const double DistanceTolerance = 0.25;

        private readonly record struct SegmentPoint(double Parameter, Point3DGlobalCoordinates Point);
        private readonly record struct OffsetFragment(List<Point3DGlobalCoordinates> Points, bool IsClosed);
    }
}
