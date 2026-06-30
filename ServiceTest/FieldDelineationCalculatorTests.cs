using System.Reflection;

namespace ServiceTest
{
    public class FieldDelineationCalculatorTests
    {
        [Test]
        public void ClosedSquareMargin_IsAppliedInside()
        {
            Assembly serviceAssembly = Assembly.LoadFrom(GetServiceAssemblyPath("Service.dll"));
            Assembly modelAssembly = Assembly.LoadFrom(GetServiceAssemblyPath("Model.dll"));
            Assembly mathAssembly = Assembly.LoadFrom(GetServiceAssemblyPath("OSDC.DotnetLibraries.General.Math.dll"));

            Type fieldType = RequireType(modelAssembly, "NORCE.Drilling.Field.Model.Field");
            Type lineType = RequireType(modelAssembly, "NORCE.Drilling.Field.Model.FieldDelineationLine");
            Type pointType = RequireType(mathAssembly, "OSDC.DotnetLibraries.General.Math.Point3DGlobalCoordinates");
            Type boundaryType = RequireType(modelAssembly, "NORCE.Drilling.Field.Model.FieldDelineationBoundaryLine");

            object field = Activator.CreateInstance(fieldType)!;
            object line = Activator.CreateInstance(lineType)!;
            Set(line, "ID", Guid.NewGuid());
            Set(line, "Name", "Lease");
            Set(line, "Margin", 10.0);
            Set(line, "Points", CreateList(pointType, [
                CreatePoint(pointType, 0, 0),
                CreatePoint(pointType, 100, 0),
                CreatePoint(pointType, 100, 100),
                CreatePoint(pointType, 0, 100),
                CreatePoint(pointType, 0, 0)
            ]));
            Set(field, "DelineationLines", CreateList(lineType, [line]));

            Type calculator = RequireType(serviceAssembly, "NORCE.Drilling.Field.Service.Managers.FieldDelineationCalculator");
            MethodInfo calculate = calculator.GetMethod("Calculate", BindingFlags.Static | BindingFlags.Public)
                ?? throw new InvalidOperationException("Calculate method not found.");

            calculate.Invoke(null, [field]);

            object firstLine = ((System.Collections.IEnumerable)Get(field, "DelineationLines")!).Cast<object>().Single();
            object boundary = ((System.Collections.IEnumerable)Get(firstLine, "CalculatedBoundaryLines")!).Cast<object>().Single();
            List<object> boundaryPoints = ((System.Collections.IEnumerable)Get(boundary, "Points")!).Cast<object>().ToList();

            Assert.That(boundaryPoints, Has.Count.EqualTo(5));
            Assert.That(Get(boundaryPoints[0], "RiemannianNorth"), Is.EqualTo(10.0).Within(1e-9));
            Assert.That(Get(boundaryPoints[0], "RiemannianEast"), Is.EqualTo(10.0).Within(1e-9));
            Assert.That(Get(boundaryPoints[1], "RiemannianNorth"), Is.EqualTo(90.0).Within(1e-9));
            Assert.That(Get(boundaryPoints[1], "RiemannianEast"), Is.EqualTo(10.0).Within(1e-9));

            _ = boundaryType;
        }

        [Test]
        public void ClosedSquareMargin_IsAppliedInsideForReversedOrientation()
        {
            Assembly serviceAssembly = Assembly.LoadFrom(GetServiceAssemblyPath("Service.dll"));
            Assembly modelAssembly = Assembly.LoadFrom(GetServiceAssemblyPath("Model.dll"));
            Assembly mathAssembly = Assembly.LoadFrom(GetServiceAssemblyPath("OSDC.DotnetLibraries.General.Math.dll"));

            object field = CreateFieldWithLine(modelAssembly, mathAssembly, 10.0, [
                (0.0, 0.0),
                (0.0, 100.0),
                (100.0, 100.0),
                (100.0, 0.0),
                (0.0, 0.0)
            ]);

            Calculate(serviceAssembly, field);

            object firstLine = ((System.Collections.IEnumerable)Get(field, "DelineationLines")!).Cast<object>().Single();
            object boundary = ((System.Collections.IEnumerable)Get(firstLine, "CalculatedBoundaryLines")!).Cast<object>().Single();
            List<object> boundaryPoints = ((System.Collections.IEnumerable)Get(boundary, "Points")!).Cast<object>().ToList();

            Assert.That(boundaryPoints, Has.Count.EqualTo(5));
            Assert.That(boundaryPoints.Select(point => (double)Get(point, "RiemannianNorth")!).Min(), Is.EqualTo(10.0).Within(1e-9));
            Assert.That(boundaryPoints.Select(point => (double)Get(point, "RiemannianNorth")!).Max(), Is.EqualTo(90.0).Within(1e-9));
            Assert.That(boundaryPoints.Select(point => (double)Get(point, "RiemannianEast")!).Min(), Is.EqualTo(10.0).Within(1e-9));
            Assert.That(boundaryPoints.Select(point => (double)Get(point, "RiemannianEast")!).Max(), Is.EqualTo(90.0).Within(1e-9));
        }

        [Test]
        public void ClosedSquareMarginLargerThanHalfWidth_ReturnsNoBoundary()
        {
            Assembly serviceAssembly = Assembly.LoadFrom(GetServiceAssemblyPath("Service.dll"));
            Assembly modelAssembly = Assembly.LoadFrom(GetServiceAssemblyPath("Model.dll"));
            Assembly mathAssembly = Assembly.LoadFrom(GetServiceAssemblyPath("OSDC.DotnetLibraries.General.Math.dll"));

            object field = CreateFieldWithLine(modelAssembly, mathAssembly, 55.0, [
                (0.0, 0.0),
                (100.0, 0.0),
                (100.0, 100.0),
                (0.0, 100.0),
                (0.0, 0.0)
            ]);

            Calculate(serviceAssembly, field);

            object firstLine = ((System.Collections.IEnumerable)Get(field, "DelineationLines")!).Cast<object>().Single();
            List<object> boundaries = ((System.Collections.IEnumerable)Get(firstLine, "CalculatedBoundaryLines")!).Cast<object>().ToList();

            Assert.That(boundaries, Is.Empty);
        }

        [Test]
        public void ClosedNonConvexMarginCanReturnMultipleBoundaries()
        {
            Assembly serviceAssembly = Assembly.LoadFrom(GetServiceAssemblyPath("Service.dll"));
            Assembly modelAssembly = Assembly.LoadFrom(GetServiceAssemblyPath("Model.dll"));
            Assembly mathAssembly = Assembly.LoadFrom(GetServiceAssemblyPath("OSDC.DotnetLibraries.General.Math.dll"));

            object field = CreateFieldWithLine(modelAssembly, mathAssembly, 11.0, [
                (0.0, 0.0),
                (0.0, 40.0),
                (40.0, 40.0),
                (40.0, 60.0),
                (0.0, 60.0),
                (0.0, 100.0),
                (100.0, 100.0),
                (100.0, 60.0),
                (60.0, 60.0),
                (60.0, 40.0),
                (100.0, 40.0),
                (100.0, 0.0),
                (0.0, 0.0)
            ]);

            Calculate(serviceAssembly, field);

            object firstLine = ((System.Collections.IEnumerable)Get(field, "DelineationLines")!).Cast<object>().Single();
            List<object> boundaries = ((System.Collections.IEnumerable)Get(firstLine, "CalculatedBoundaryLines")!).Cast<object>().ToList();

            Assert.That(boundaries, Has.Count.EqualTo(2));
            Assert.That(boundaries.Select(boundary => ((System.Collections.IEnumerable)Get(boundary, "Points")!).Cast<object>().Count()).ToList(), Is.All.GreaterThan(5));
        }

        [Test]
        public void ClosedNonConvexMargin_UsesRoundArcsAtReentrantCorners()
        {
            Assembly serviceAssembly = Assembly.LoadFrom(GetServiceAssemblyPath("Service.dll"));
            Assembly modelAssembly = Assembly.LoadFrom(GetServiceAssemblyPath("Model.dll"));
            Assembly mathAssembly = Assembly.LoadFrom(GetServiceAssemblyPath("OSDC.DotnetLibraries.General.Math.dll"));

            object field = CreateFieldWithLine(modelAssembly, mathAssembly, 5.0, [
                (0.0, 0.0),
                (40.0, 0.0),
                (40.0, 40.0),
                (60.0, 40.0),
                (60.0, 0.0),
                (100.0, 0.0),
                (100.0, 100.0),
                (60.0, 100.0),
                (60.0, 60.0),
                (40.0, 60.0),
                (40.0, 100.0),
                (0.0, 100.0),
                (0.0, 0.0)
            ]);

            Calculate(serviceAssembly, field);

            object firstLine = ((System.Collections.IEnumerable)Get(field, "DelineationLines")!).Cast<object>().Single();
            object boundary = ((System.Collections.IEnumerable)Get(firstLine, "CalculatedBoundaryLines")!).Cast<object>().Single();
            List<object> points = ((System.Collections.IEnumerable)Get(boundary, "Points")!).Cast<object>().ToList();

            Assert.That(points.Count, Is.GreaterThan(13));
            Assert.That(HasArcInteriorPoint(points, 40.0, 40.0, 5.0, 35.0, 40.0, 40.0, 45.0), Is.True);
            Assert.That(HasArcInteriorPoint(points, 60.0, 40.0, 5.0, 60.0, 65.0, 40.0, 45.0), Is.True);
            Assert.That(HasArcInteriorPoint(points, 60.0, 60.0, 5.0, 60.0, 65.0, 55.0, 60.0), Is.True);
            Assert.That(HasArcInteriorPoint(points, 40.0, 60.0, 5.0, 35.0, 40.0, 55.0, 60.0), Is.True);
        }

        [Test]
        public void OpenPolylineExteriorJoin_UsesDiscretizedCircularArc()
        {
            Assembly serviceAssembly = Assembly.LoadFrom(GetServiceAssemblyPath("Service.dll"));
            Assembly modelAssembly = Assembly.LoadFrom(GetServiceAssemblyPath("Model.dll"));
            Assembly mathAssembly = Assembly.LoadFrom(GetServiceAssemblyPath("OSDC.DotnetLibraries.General.Math.dll"));

            object field = CreateFieldWithLine(modelAssembly, mathAssembly, 10.0, [
                (40.0, 40.0),
                (40.0, 60.0),
                (60.0, 60.0)
            ]);

            Calculate(serviceAssembly, field);

            object firstLine = ((System.Collections.IEnumerable)Get(field, "DelineationLines")!).Cast<object>().Single();
            List<object> boundaries = ((System.Collections.IEnumerable)Get(firstLine, "CalculatedBoundaryLines")!).Cast<object>().ToList();
            List<object> rightBoundaryPoints = boundaries
                .Select(boundary => ((System.Collections.IEnumerable)Get(boundary, "Points")!).Cast<object>().ToList())
                .Single(points => ContainsPoint(points, 30.0, 60.0) && ContainsPoint(points, 40.0, 70.0));

            Assert.That(rightBoundaryPoints.Count, Is.GreaterThan(4));
            Assert.That(ContainsPoint(rightBoundaryPoints, 30.0, 40.0), Is.True);
            Assert.That(ContainsPoint(rightBoundaryPoints, 30.0, 60.0), Is.True);
            Assert.That(ContainsPoint(rightBoundaryPoints, 40.0, 70.0), Is.True);
            Assert.That(ContainsPoint(rightBoundaryPoints, 60.0, 70.0), Is.True);
            Assert.That(rightBoundaryPoints.Any(point =>
            {
                double north = (double)Get(point, "RiemannianNorth")!;
                double east = (double)Get(point, "RiemannianEast")!;
                double dn = north - 40.0;
                double de = east - 60.0;
                return north > 30.0 && north < 40.0 &&
                    east > 60.0 && east < 70.0 &&
                    System.Math.Abs(System.Math.Sqrt(dn * dn + de * de) - 10.0) < 1e-9;
            }), Is.True);
        }

        [Test]
        public void OpenPolylineMixedTurns_OnlyExteriorJoinsUseArc()
        {
            Assembly serviceAssembly = Assembly.LoadFrom(GetServiceAssemblyPath("Service.dll"));
            Assembly modelAssembly = Assembly.LoadFrom(GetServiceAssemblyPath("Model.dll"));
            Assembly mathAssembly = Assembly.LoadFrom(GetServiceAssemblyPath("OSDC.DotnetLibraries.General.Math.dll"));

            object field = CreateFieldWithLine(modelAssembly, mathAssembly, 10.0, [
                (0.0, 0.0),
                (0.0, 100.0),
                (100.0, 100.0),
                (100.0, 200.0)
            ]);

            Calculate(serviceAssembly, field);

            object firstLine = ((System.Collections.IEnumerable)Get(field, "DelineationLines")!).Cast<object>().Single();
            List<object> boundaries = ((System.Collections.IEnumerable)Get(firstLine, "CalculatedBoundaryLines")!).Cast<object>().ToList();
            List<object> firstTurnArcBoundary = boundaries
                .Select(boundary => ((System.Collections.IEnumerable)Get(boundary, "Points")!).Cast<object>().ToList())
                .Single(points => ContainsPoint(points, -10.0, 100.0) && ContainsPoint(points, 0.0, 110.0));
            List<object> secondTurnArcBoundary = boundaries
                .Select(boundary => ((System.Collections.IEnumerable)Get(boundary, "Points")!).Cast<object>().ToList())
                .Single(points => ContainsPoint(points, 100.0, 90.0) && ContainsPoint(points, 110.0, 100.0));

            Assert.That(firstTurnArcBoundary.Count, Is.GreaterThan(4));
            Assert.That(secondTurnArcBoundary.Count, Is.GreaterThan(4));
            Assert.That(ContainsPoint(firstTurnArcBoundary, 90.0, 110.0), Is.True);
            Assert.That(ContainsPoint(secondTurnArcBoundary, 10.0, 90.0), Is.True);
            Assert.That(firstTurnArcBoundary.Count(point =>
            {
                double north = (double)Get(point, "RiemannianNorth")!;
                double east = (double)Get(point, "RiemannianEast")!;
                double dn = north - 0.0;
                double de = east - 100.0;
                return north > -10.0 && north < 0.0 &&
                    east > 100.0 && east < 110.0 &&
                    System.Math.Abs(System.Math.Sqrt(dn * dn + de * de) - 10.0) < 1e-9;
            }), Is.GreaterThanOrEqualTo(1));
            Assert.That(secondTurnArcBoundary.Count(point =>
            {
                double north = (double)Get(point, "RiemannianNorth")!;
                double east = (double)Get(point, "RiemannianEast")!;
                double dn = north - 100.0;
                double de = east - 100.0;
                return north > 100.0 && north < 110.0 &&
                    east > 90.0 && east < 100.0 &&
                    System.Math.Abs(System.Math.Sqrt(dn * dn + de * de) - 10.0) < 1e-9;
            }), Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void OpenPolylineSelfIntersectingOffset_SplitsClosedAndOpenFragments()
        {
            Assembly serviceAssembly = Assembly.LoadFrom(GetServiceAssemblyPath("Service.dll"));
            Assembly modelAssembly = Assembly.LoadFrom(GetServiceAssemblyPath("Model.dll"));
            Assembly mathAssembly = Assembly.LoadFrom(GetServiceAssemblyPath("OSDC.DotnetLibraries.General.Math.dll"));

            object field = CreateFieldWithLine(modelAssembly, mathAssembly, 10.0, [
                (0.0, 0.0),
                (50.0, 20.0),
                (70.0, 30.0),
                (100.0, 25.0),
                (100.0, 40.0),
                (80.0, 60.0),
                (50.0, 30.0),
                (20.0, 40.0),
                (0.0, 35.0)
            ]);

            Calculate(serviceAssembly, field);

            object firstLine = ((System.Collections.IEnumerable)Get(field, "DelineationLines")!).Cast<object>().Single();
            List<object> boundaries = ((System.Collections.IEnumerable)Get(firstLine, "CalculatedBoundaryLines")!).Cast<object>().ToList();
            List<object> closedBoundaries = boundaries
                .Where(boundary => (bool)Get(boundary, "IsClosed")!)
                .ToList();
            List<object> openBoundaries = boundaries
                .Where(boundary => !(bool)Get(boundary, "IsClosed")!)
                .ToList();

            Assert.That(boundaries, Has.Count.EqualTo(3));
            Assert.That(closedBoundaries, Has.Count.EqualTo(1));
            Assert.That(openBoundaries, Has.Count.EqualTo(2));
            Assert.That(closedBoundaries.Select(boundary => ((System.Collections.IEnumerable)Get(boundary, "Points")!).Cast<object>().Count()), Is.All.GreaterThanOrEqualTo(4));
            Assert.That(closedBoundaries.Any(boundary =>
            {
                List<object> points = ((System.Collections.IEnumerable)Get(boundary, "Points")!).Cast<object>().ToList();
                return SameTestPoint(points[0], points[^1]) && System.Math.Abs(TestSignedArea(points.Take(points.Count - 1))) > 1e-9;
            }), Is.True);
        }

        private static object CreateFieldWithLine(Assembly modelAssembly, Assembly mathAssembly, double margin, IEnumerable<(double North, double East)> coordinates)
        {
            Type fieldType = RequireType(modelAssembly, "NORCE.Drilling.Field.Model.Field");
            Type lineType = RequireType(modelAssembly, "NORCE.Drilling.Field.Model.FieldDelineationLine");
            Type pointType = RequireType(mathAssembly, "OSDC.DotnetLibraries.General.Math.Point3DGlobalCoordinates");

            object field = Activator.CreateInstance(fieldType)!;
            object line = Activator.CreateInstance(lineType)!;
            Set(line, "ID", Guid.NewGuid());
            Set(line, "Name", "Lease");
            Set(line, "Margin", margin);
            Set(line, "Points", CreateList(pointType, coordinates.Select(coordinate => CreatePoint(pointType, coordinate.North, coordinate.East))));
            Set(field, "DelineationLines", CreateList(lineType, [line]));
            return field;
        }

        private static void Calculate(Assembly serviceAssembly, object field)
        {
            Type calculator = RequireType(serviceAssembly, "NORCE.Drilling.Field.Service.Managers.FieldDelineationCalculator");
            MethodInfo calculate = calculator.GetMethod("Calculate", BindingFlags.Static | BindingFlags.Public)
                ?? throw new InvalidOperationException("Calculate method not found.");

            calculate.Invoke(null, [field]);
        }

        private static object CreatePoint(Type pointType, double north, double east)
        {
            object point = Activator.CreateInstance(pointType)!;
            Set(point, "RiemannianNorth", north);
            Set(point, "RiemannianEast", east);
            Set(point, "X", north);
            Set(point, "Y", east);
            Set(point, "Z", 0.0);
            Set(point, "TVD", 0.0);
            pointType.GetMethod("SetLatitudeLongitude")!.Invoke(point, [north, east]);
            return point;
        }

        private static bool ContainsPoint(IEnumerable<object> points, double north, double east)
        {
            return points.Any(point =>
                System.Math.Abs((double)Get(point, "RiemannianNorth")! - north) < 1e-9 &&
                System.Math.Abs((double)Get(point, "RiemannianEast")! - east) < 1e-9);
        }

        private static bool SameTestPoint(object first, object second)
        {
            return System.Math.Abs((double)Get(first, "RiemannianNorth")! - (double)Get(second, "RiemannianNorth")!) < 1e-9 &&
                System.Math.Abs((double)Get(first, "RiemannianEast")! - (double)Get(second, "RiemannianEast")!) < 1e-9;
        }

        private static double TestSignedArea(IEnumerable<object> points)
        {
            List<object> pointList = points.ToList();
            double area = 0;
            for (int i = 0; i < pointList.Count; i++)
            {
                object current = pointList[i];
                object next = pointList[(i + 1) % pointList.Count];
                area += (double)Get(current, "RiemannianEast")! * (double)Get(next, "RiemannianNorth")! -
                    (double)Get(next, "RiemannianEast")! * (double)Get(current, "RiemannianNorth")!;
            }

            return 0.5 * area;
        }

        private static bool HasArcInteriorPoint(IEnumerable<object> points, double centerNorth, double centerEast, double radius, double minNorth, double maxNorth, double minEast, double maxEast)
        {
            return points.Any(point =>
            {
                double north = (double)Get(point, "RiemannianNorth")!;
                double east = (double)Get(point, "RiemannianEast")!;
                double dn = north - centerNorth;
                double de = east - centerEast;
                return north > minNorth && north < maxNorth &&
                    east > minEast && east < maxEast &&
                    System.Math.Abs(System.Math.Sqrt(dn * dn + de * de) - radius) < 1e-6;
            });
        }

        private static object CreateList(Type itemType, IEnumerable<object> items)
        {
            Type listType = typeof(List<>).MakeGenericType(itemType);
            object list = Activator.CreateInstance(listType)!;
            System.Collections.IList asList = (System.Collections.IList)list;
            foreach (object item in items)
            {
                asList.Add(item);
            }

            return list;
        }

        private static Type RequireType(Assembly assembly, string typeName)
        {
            return assembly.GetType(typeName) ?? throw new InvalidOperationException($"Type not found: {typeName}");
        }

        private static object? Get(object target, string propertyName)
        {
            return target.GetType().GetProperty(propertyName)!.GetValue(target);
        }

        private static void Set(object target, string propertyName, object? value)
        {
            target.GetType().GetProperty(propertyName)!.SetValue(target, value);
        }

        private static string GetServiceAssemblyPath(string assemblyName)
        {
            string solutionRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
            return Path.Combine(solutionRoot, "Service", "bin", "Debug", "net8.0", assemblyName);
        }
    }
}
