using System;
using System.Collections.Generic;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace BodyOrientationControlLib
{
    public static class Model3DFactory
    {
        public static GeometryModel3D CreateNormalizedCube(Material material)
        {
            MeshGeometry3D geometry = new MeshGeometry3D();

            var farPoint = new Point3D(-0.5, -0.5, -0.5);
            var nearPoint = new Point3D(0.5, 0.5, 0.5);

            var cube = new Model3DGroup();
            var p0 = new Point3D(farPoint.X, farPoint.Y, farPoint.Z);
            var p1 = new Point3D(nearPoint.X, farPoint.Y, farPoint.Z);
            var p2 = new Point3D(nearPoint.X, farPoint.Y, nearPoint.Z);
            var p3 = new Point3D(farPoint.X, farPoint.Y, nearPoint.Z);
            var p4 = new Point3D(farPoint.X, nearPoint.Y, farPoint.Z);
            var p5 = new Point3D(nearPoint.X, nearPoint.Y, farPoint.Z);
            var p6 = new Point3D(nearPoint.X, nearPoint.Y, nearPoint.Z);
            var p7 = new Point3D(farPoint.X, nearPoint.Y, nearPoint.Z);
            int startIndex = 0;
            startIndex = AddTriangleFace(geometry, p3, p2, p6, startIndex);
            startIndex = AddTriangleFace(geometry, p3, p6, p7, startIndex);
            startIndex = AddTriangleFace(geometry, p2, p1, p5, startIndex);
            startIndex = AddTriangleFace(geometry, p2, p5, p6, startIndex);
            startIndex = AddTriangleFace(geometry, p1, p0, p4, startIndex);
            startIndex = AddTriangleFace(geometry, p1, p4, p5, startIndex);
            startIndex = AddTriangleFace(geometry, p0, p3, p7, startIndex);
            startIndex = AddTriangleFace(geometry, p0, p7, p4, startIndex);
            startIndex = AddTriangleFace(geometry, p7, p6, p5, startIndex);
            startIndex = AddTriangleFace(geometry, p7, p5, p4, startIndex);
            startIndex = AddTriangleFace(geometry, p2, p3, p0, startIndex);
            startIndex = AddTriangleFace(geometry, p2, p0, p1, startIndex);

            return new GeometryModel3D(geometry, material);
        }

        public static GeometryModel3D CreateNormalizedCylinder(Material material, int numSegments)
        {
            MeshGeometry3D geometry = new MeshGeometry3D();

            double radius = 0.5;
            double depth = 1;
            double minusDepthHalf = -depth / 2;
            var nearCircle = new CircleAssitor();
            var farCircle = new CircleAssitor();

            var twoPi = Math.PI * 2;
            var firstPass = true;

            double x;
            double y;

            double increment = twoPi / numSegments;
            int startIndex = 0;
            for (double i = 0; i < twoPi + increment; i = i + increment)
            {
                x = (radius * Math.Cos(i));
                y = (-radius * Math.Sin(i));

                farCircle.CurrentTriangle.P0 = new Point3D(0, 0, minusDepthHalf);
                farCircle.CurrentTriangle.P1 = farCircle.LastPoint;
                farCircle.CurrentTriangle.P2 = new Point3D(x, y, minusDepthHalf);

                nearCircle.CurrentTriangle = farCircle.CurrentTriangle.Clone(depth, true);

                if (!firstPass)
                {
                    startIndex = AddTriangleFace(geometry, farCircle.CurrentTriangle, startIndex);
                    startIndex = AddTriangleFace(geometry, nearCircle.CurrentTriangle, startIndex);

                    startIndex = AddTriangleFace(geometry, farCircle.CurrentTriangle.P2, farCircle.CurrentTriangle.P1, nearCircle.CurrentTriangle.P2, startIndex);
                    startIndex = AddTriangleFace(geometry, nearCircle.CurrentTriangle.P2, nearCircle.CurrentTriangle.P1, farCircle.CurrentTriangle.P2, startIndex);
                }
                else
                {
                    farCircle.FirstPoint = farCircle.CurrentTriangle.P1;
                    nearCircle.FirstPoint = nearCircle.CurrentTriangle.P1;
                    firstPass = false;
                }
                farCircle.LastPoint = farCircle.CurrentTriangle.P2;
                nearCircle.LastPoint = nearCircle.CurrentTriangle.P2;
            }

            return new GeometryModel3D(geometry, material);
        }

        public static GeometryModel3D CreateNormalizedSphere(Material material, int numSegments)
        {
            MeshGeometry3D geometry = new MeshGeometry3D();
            
            if (numSegments < 2)
                return null;
            int u = numSegments, v = numSegments;
            double radius = 0.5;
            Point3D[,] pts = new Point3D[u, v];
            for (int i = 0; i < u; i++)
            {
                for (int j = 0; j < v; j++)
                {
                    pts[i, j] = GetPosition(radius,
                    i * 180 / (u - 1), j * 360 / (v - 1));
                }
            }

            int startIndex = 0;
            Point3D[] p = new Point3D[4];
            for (int i = 0; i < u - 1; i++)
            {
                for (int j = 0; j < v - 1; j++)
                {
                    p[0] = pts[i, j];
                    p[1] = pts[i + 1, j];
                    p[2] = pts[i + 1, j + 1];
                    p[3] = pts[i, j + 1];
                    startIndex = AddTriangleFace(geometry, p[0], p[1], p[2], startIndex);
                    startIndex = AddTriangleFace(geometry, p[2], p[3], p[0], startIndex);
                }
            }

            return new GeometryModel3D(geometry, material);
        }

        public static Material GetSurfaceMaterial(Color colour)
        {
            //var materialGroup = new MaterialGroup();
            //var emmMat = new EmissiveMaterial(new SolidColorBrush(colour));
            //materialGroup.Children.Add(emmMat);
            //materialGroup.Children.Add(new DiffuseMaterial(new SolidColorBrush(colour)));
            //var specMat = new SpecularMaterial(new SolidColorBrush(Colors.White), 30);
            //materialGroup.Children.Add(specMat);
            //return materialGroup;

            // Performance: only diffuse material
            return new DiffuseMaterial(new SolidColorBrush(colour));
        }

        public static void AddCube(this Model3DCollection collection, double x, double y, double z, double size, Color color)
        {
            var cube = Model3DFactory.CreateNormalizedCube(Model3DFactory.GetSurfaceMaterial(color));
            var transform = new Transform3DGroup();
            transform.Children.Add(new ScaleTransform3D(size, size, size));
            transform.Children.Add(new TranslateTransform3D(x, y, z));
            cube.Transform = transform;
            collection.Add(cube);
        }

        private static Point3D GetPosition(double radius, double theta, double phi)
        {
            Point3D pt = new Point3D();
            double snt = Math.Sin(theta * Math.PI / 180);
            double cnt = Math.Cos(theta * Math.PI / 180);
            double snp = Math.Sin(phi * Math.PI / 180);
            double cnp = Math.Cos(phi * Math.PI / 180);
            pt.X = radius * snt * cnp;
            pt.Y = radius * cnt;
            pt.Z = -radius * snt * snp;
            return pt;
        }

        private static int AddTriangleFace(MeshGeometry3D mesh, Triangle triangle, int startIndex)
        {
            return Model3DFactory.AddTriangleFace(mesh, triangle.P0, triangle.P1, triangle.P2, startIndex);
        }

        private static int AddTriangleFace(MeshGeometry3D mesh, Point3D p0, Point3D p1, Point3D p2, int startIndex)
        {
            Vector3D normal = Model3DFactory.CalcNormal(p0, p1, p2);
            mesh.Positions.Add(p0); mesh.Positions.Add(p1); mesh.Positions.Add(p2);
            mesh.TriangleIndices.Add(startIndex); mesh.TriangleIndices.Add(startIndex + 1); mesh.TriangleIndices.Add(startIndex + 2);
            mesh.Normals.Add(normal); mesh.Normals.Add(normal); mesh.Normals.Add(normal);
            return startIndex + 3;
        }

        private static Vector3D CalcNormal(Point3D p0, Point3D p1, Point3D p2)
        {
            Vector3D v0 = new Vector3D(p1.X - p0.X, p1.Y - p0.Y, p1.Z - p0.Z);
            Vector3D v1 = new Vector3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
            return Vector3D.CrossProduct(v0, v1);
        }

        private class CircleAssitor
        {
            public CircleAssitor()
            {
                CurrentTriangle = new Triangle();
            }

            public Point3D FirstPoint { get; set; }
            public Point3D LastPoint { get; set; }
            public Triangle CurrentTriangle { get; set; }
        }

        private class Triangle
        {
            public Point3D P0 { get; set; }
            public Point3D P1 { get; set; }
            public Point3D P2 { get; set; }

            public Triangle Clone(double z, bool switchP1andP2)
            {
                var newTriangle = new Triangle();
                newTriangle.P0 = GetPointAdjustedBy(this.P0, new Point3D(0, 0, z));

                var point1 = GetPointAdjustedBy(this.P1, new Point3D(0, 0, z));
                var point2 = GetPointAdjustedBy(this.P2, new Point3D(0, 0, z));

                if (!switchP1andP2)
                {
                    newTriangle.P1 = point1;
                    newTriangle.P2 = point2;
                }
                else
                {
                    newTriangle.P1 = point2;
                    newTriangle.P2 = point1;
                }
                return newTriangle;
            }

            private Point3D GetPointAdjustedBy(Point3D point, Point3D adjustBy)
            {
                var newPoint = new Point3D { X = point.X, Y = point.Y, Z = point.Z };
                newPoint.Offset(adjustBy.X, adjustBy.Y, adjustBy.Z);
                return newPoint;
            }
        }
    }
}
