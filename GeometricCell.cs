using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.Linq;
using Rhino.Geometry.Intersect;

/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public class Script_Instance : GH_ScriptInstance {
#region Utility functions
  /// <summary>Print a String to the [Out] Parameter of the Script component.</summary>
  /// <param name="text">String to print.</param>
  private void Print(string text) { /* Implementation hidden. */ }
  /// <summary>Print a formatted String to the [Out] Parameter of the Script component.</summary>
  /// <param name="format">String format.</param>
  /// <param name="args">Formatting parameters.</param>
  private void Print(string format, params object[] args) { /* Implementation hidden. */ }
  /// <summary>Print useful information about an object instance to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj) { /* Implementation hidden. */ }
  /// <summary>Print the signatures of all the overloads of a specific method to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj, string method_name) { /* Implementation hidden. */ }
#endregion

#region Members
  /// <summary>Gets the current Rhino document.</summary>
  private readonly RhinoDoc RhinoDocument;
  /// <summary>Gets the Grasshopper document that owns this script.</summary>
  private readonly GH_Document GrasshopperDocument;
  /// <summary>Gets the Grasshopper script component that owns this script.</summary>
  private readonly IGH_Component Component;
  /// <summary>
  /// Gets the current iteration count. The first call to RunScript() is associated with Iteration==0.
  /// Any subsequent call within the same solution will increment the Iteration count.
  /// </summary>
  private readonly int Iteration;
#endregion

  /// <summary>
  /// This procedure contains the user code. Input parameters are provided as regular arguments,
  /// Output parameters as ref arguments. You don't have to assign output parameters,
  /// they will have a default value.
  /// </summary>
    private void RunScript(List<Curve> c, ref object r) {
        GeometricRegion geometricRegion = new GeometricRegion(c);
        geometricRegion.Compute(RhinoDocument.ModelAbsoluteTolerance);
        r = (IEnumerable) geometricRegion.regionCurves;
    }

  // <Custom additional code> 
    class GeometricRegion : List<Curve> {
        private List<Curve> reDuCrs = new List<Curve>();

        private double tol;

        public List<Curve> regionCurves {
        get;
        set;
        }

        public List<List<int>> regionTopologies {
        get;
        set;
        }

        public GeometricRegion(IEnumerable<Curve> m) : base(m) {}

        public void Compute(double tolerance) {
            tol = tolerance;
            reDuCrs = ReDupCrv();
            List<Point3d> reDuPts = new List<Point3d>();
            reDuPts.AddRange(UnqPtLsFrCr(reDuCrs));
            List<string> list1 = reDuPts.Select(delegate (Point3d p) {
                    Point3d val5 = RndPt(p);
                    return ((object) (Point3d) val5).ToString();
                }).ToList();
            List<List<int>> ptInLn = (from x in reDuCrs.Select(delegate (Curve t) { // maps all Curve t in this.reDuCrs to an anonymous type with t and str1
                    Point3d val4 = RndPt(t.PointAtStart);
                    return new {
                            t = t, // Curve
                            str1 = ((object) (Point3d) val4).ToString() // string
                        };
                    }).Select(y => { // maps all anonymous type with t and str1 to an anonymous type with y and str2
                    Point3d val3 = RndPt(y.t.PointAtEnd);
                    return new {
                            y = y,
                            str2 = ((object) (Point3d) val3).ToString()
                        };
                    }) select new List<int> {
                            list1.IndexOf(x.y.str1),
                            list1.IndexOf(x.str2)
                        }).ToList();

            List<List<int>> list2 = reDuPts.Select((Point3d m) => new List<int>()).ToList();
            for (int i = 0; i < reDuCrs.Count; i++) {
                list2[ptInLn[i][0]].Add(ptInLn[i][1]);
                list2[ptInLn[i][1]].Add(ptInLn[i][0]);
            }

            Vector3d vx = new Vector3d(1.0, 0.0, 0.0);
            Vector3d val = default(Vector3d); // null
            val = new Vector3d(0.0, 1.0, 0.0);
            Plane wxy = new Plane(new Point3d(0.0, 0.0, 0.0), vx, val);
            List<List<int>> list3 = (from source in reDuPts.Select(
                    (Point3d t, int si) => list2[si].OrderBy(
                    (int a) => Vector3d.VectorAngle(vx, reDuPts[a] - t, wxy)))
                select source.Distinct().ToList()).ToList();
            List<List<int>> list4 = new List<List<int>>();
            for (int i = 0; i < list3.Count; i++) {
                for (int j = 1; j <= list3[i].Count; j++) {
                list4.Add(new List<int> {
                        list3[i][(j - 1) % list3[i].Count],
                        i,
                        list3[i][j % list3[i].Count]
                    });
                }
            }

            List<List<int>> list5 = (from a in list4 orderby a[0], a[1] select a).ToList();
            List<List<int>> list6 = new List<List<int>>();
            List<List<int>> list7 = new List<List<int>>();
            while (list5.Count > 0) {
                List<List<int>> list8 = new List<List<int>>();
                List<int> currentWedge = list5[0];
                list5.RemoveAt(0);
                while (true) {
                    list8.Add(currentWedge);
                    if (list8[0][0] == list8[list8.Count - 1][1] &&
                        list8[0][1] == list8[list8.Count - 1][2]) {
                        break;
                    }
                    int index = list5.FindIndex((List<int> c) =>
                        c[0] == currentWedge[1] && c[1] == currentWedge[2]);
                    currentWedge = list5[index];
                    list5.RemoveAt(index);
                }

                List<int> list9 = list8.Select((List<int> s) => s[1]).ToList();
                list9.Reverse();
                list6.Add(new List<int>(list9));
                list9.Clear();
            }

            List<Curve> list10 = new List<Curve>();
            foreach (List<int> item in list6) {
                List<Point3d> list11 = new List<Point3d>();
                double num = 0.0;
                for (int i = 0; i < item.Count; i++) {
                    list11.Add(reDuPts[item[i]]);
                    double num2 = num;
                    Point3d val2 = reDuPts[item[(i + 1) % item.Count]];
                    double x = ((Point3d) val2).X;
                    val2 = reDuPts[item[i]];
                    double x2 = ((Point3d) val2).X;
                    double num3 = x - x2;
                    val2 = reDuPts[item[(i + 1) % item.Count]];
                    double y = ((Point3d) val2).Y;
                    val2 = reDuPts[item[i]];
                    double y2 = ((Point3d) val2).Y;
                    double num4 = y + y2;
                    double num5 = num3 * num4;
                    num = num2 + num5;
                }

                list11.Add(list11[0]);
                if (num < 0.0) {
                    list10.Add((Curve) (object) new Polyline((IEnumerable<Point3d>) list11).ToNurbsCurve());
                    list7.Add(item);
                }
            }

            List<Curve> list12 = new List<Curve>();
            if (HaveCurve(reDuCrs)) {
                foreach (List<int> item2 in list7.Select((List<int> a) => a.Select(
                (int b) => ptInLn.FindIndex(delegate (List<int> c) {
                    if (c[0] == b && c[1] == a[(a.IndexOf(b) + 1) % a.Count]) {
                        return true;
                    }
                    return c[0] == a[(a.IndexOf(b) + 1) % a.Count] && c[1] == b;
                })).ToList()).ToList()) {
                    list12.AddRange(Curve.JoinCurves(item2.Select((int a) => reDuCrs[a]), this.tol));
                }
            } else {
                list12.AddRange(list10);
            }

            list6.Clear();
            regionTopologies = list7;
            regionCurves = list12;
        }

        public List<Point3d> UnqPtLsFrCr(List<Curve> reDuCrs) {
            HashSet<string> hashSet = new HashSet<string>();
            List<Point3d> list = new List<Point3d>();
            foreach (Curve reDuCr in reDuCrs) {
                Point3d val = RndPt(reDuCr.PointAtStart);
                string item = ((object) (Point3d) val).ToString();
                if (hashSet.Add(item)) {
                    list.Add(reDuCr.PointAtStart);
                }
                val = RndPt(reDuCr.PointAtEnd);
                string item2 = ((object) (Point3d) val).ToString();
                if (hashSet.Add(item2)) {
                    list.Add(reDuCr.PointAtEnd);
                }
            }
            return list;
        }

        private List<Curve> ReDupCrv() {
            List<Curve> list = new List<Curve>();
            using (Enumerator enumerator = GetEnumerator()) {
                while (enumerator.MoveNext()) {
                    Curve current = enumerator.Current;
                    if (!current.IsLinear()) {
                        list.AddRange(current.DuplicateSegments());
                    } else {
                        list.Add(current);
                    }
                }
            }

            List<Curve> list2 = new List<Curve>();
            foreach (Curve item2 in list) {
                Point3d[] array = (Point3d[]) (object) new Point3d[2] {item2.PointAtStart, item2.PointAtEnd};
                // Not sure if this reverses the curve multiple times?
                // Not sure if the pass by ref affects it.
                // Not going to try and be smart and combine if the if statements (for now)
                if (((Point3d) array[0]).X > ((Point3d) array[1]).X) {
                    item2.Reverse();
                }
                if (((Point3d) array[0]).Y > ((Point3d) array[1]).Y) {
                    item2.Reverse();
                }
                if (((Point3d) array[0]).Z > ((Point3d) array[1]).Z) {
                    item2.Reverse();
                }
                HashSet<string> hashSet = new HashSet<string>();
                Point3d val = item2.PointAtNormalizedLength(0.25);
                string text = ((object) (Point3d) val).ToString();
                val = item2.PointAtNormalizedLength(0.75);
                string text2 = ((object) (Point3d) val).ToString();
                string item = text + "-" + text2;
                if (hashSet.Add(item)) {
                    list2.Add(item2);
                }
            }

            List<Curve> list3 = new List<Curve>();
            list3.AddRange(HaveCurve(list2) ?
                RemoveShortSegments(SplitCurves(list2)) :
                RemoveShortSegments(SplitLines(list2)));
            return list3;
        }

        private static bool HaveCurve(List<Curve> curves) {
            return curves.Any((Curve c) => !c.IsLinear());
        }

        private List<Curve> RemoveShortSegments(List<Curve> curves) {
            return curves.Where((Curve c) => c.GetLength() > this.tol).ToList();
        }

        private List<Curve> SplitCurves(List<Curve> curves) {
            List<Curve> list = new List<Curve>();
            for (int i = 0; i < curves.Count; i++) {
                List<double> list2 = new List<double>();
                for (int j = 0; j < curves.Count; j++) {
                    if (i != j) {
                        list2.AddRange(
                            ((IEnumerable<IntersectionEvent>) Intersection
                            .CurveCurve(curves[i], curves[j], 0.01, 0.01))
                            .Select((IntersectionEvent current) => current.ParameterA));
                    }
                }
                if (list2.Count > 0) {
                    list.AddRange(curves[i].Split((IEnumerable<double>) list2));
                } else {
                    list.Add(curves[i]);
                }
            }
            return list;
        }

        private List<Curve> SplitLines(List<Curve> curves) {
            List<Curve> list = new List<Curve>();
            Line val = default(Line); // null
            double item = default(double); // 0.0, for out of Intersection::LineLine
            double num = default(double); // 0.0, for out of Intersection::LineLine
            foreach (Curve curve in curves) {
                List<double> list2 = new List<double>();
                // Original line below was ((Line)(ref val))._002Ector(AllCurf.PointAtStart, AllCurf.PointAtEnd);
                val = new Line(curve.PointAtStart, curve.PointAtEnd);
                foreach (Line item2 in ((IEnumerable<Curve>) curves)
                    .Select((Func<Curve, Line>)
                    ((Curve c) => new Line(c.PointAtStart, c.PointAtEnd)))) {
                    if(Intersection.LineLine(val, item2, out item, out num, this.tol, true)) {
                        list2.Add(item);
                    }
                    }

                if (list2.Count > 0) {
                    List<double> t2 = (from t1 in list2.Distinct() select (t1)).ToList();
                    list.AddRange(SplitLine(val, t2));
                } else {
                    list.Add(curve);
                }
            }
            return list;
        }

        private static List<Curve> SplitLine(Line l, List<double> t) {
            List<double> list = t.OrderBy((double d) => d).ToList();
            if (list[0] != 0.0) {
                list.Insert(0, 0.0);
            }
            if (list[list.Count - 1] != 1.0) {
                list.Add(1.0);
            }
            List<Curve> list2 = new List<Curve>();
            Line val = default(Line); // null
            for (int i = 0; i < list.Count - 1; i++) {
                val = new Line(((Line) l).PointAt(list[i]),
                ((Line) l).PointAt(list[i + 1]));
                NurbsCurve item = ((Line) val).ToNurbsCurve();
                list2.Add((Curve) (object) item);
            }
            return list2;
        }

        public double Round(double num) {
            return tol * Math.Round(num / this.tol);
        }

        private Point3d RndPt(Point3d pt) {
            return new Point3d(Round(((Point3d) pt).X),
                Round(((Point3d) pt).Y),
                Round(((Point3d) pt).Z));
        }
    }
  // </Custom additional code> 
}