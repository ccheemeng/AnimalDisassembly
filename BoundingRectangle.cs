using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;



/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public class Script_Instance : GH_ScriptInstance
{
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
  private void RunScript(List<GeometryBase> G, Plane Pl, int A, double S, bool U, ref object B, ref object X, ref object Y)
  {
    List<IGH_GeometricGoo> geometricGoo = new List<IGH_GeometricGoo>();
    foreach (GeometryBase geometryBase in G) {
      geometricGoo.Add(GH_Convert.ToGeometricGoo(geometryBase));
    }

    List<Rectangle3d> rectangles = new List<Rectangle3d>();
    List<BoundingBox> boundingBoxes = new List<BoundingBox>();
    BoundingBox unset = BoundingBox.Unset;
    Transform transform = Transform.ChangeBasis(Plane.WorldXY, Pl);
    for (int i = 0; i < geometricGoo.Count; i++) {
      BoundingBox boundingBox = geometricGoo[i].GetBoundingBox(transform);
      if (U) {
        ((BoundingBox) unset).Union(boundingBox);
      } else {
        boundingBoxes.Add(boundingBox);
      }
    }

    if (U) {
      boundingBoxes.Add(unset);
    }

    Box box = default(Box);
    Interval interval;
    Plane plane0 = default(Plane);
    Interval interval00 = default(Interval);
    Interval interval01 = default(Interval);
    Rectangle3d rectangle0 = default(Rectangle3d);
    Plane plane1 = default(Plane);
    Interval interval10 = default(Interval);
    Interval interval11 = default(Interval);
    Rectangle3d rectangle1 = default(Rectangle3d);
    Plane plane2 = default(Plane);
    Interval interval20 = default(Interval);
    Interval interval21 = default(Interval);
    Rectangle3d rectangle2 = default(Rectangle3d);
    for (int i = 0; i < boundingBoxes.Count; i++) {
      box = new Box(boundingBoxes[i]);
      box.Plane = Pl;
      switch(A) {
        case 0: {
          plane0 = new Plane(box.PointAt(0.5, 0.5, S), Pl.XAxis, Pl.YAxis);
          interval = box.X;
          double double0 = 0.0 - Math.Abs(interval.Length * 0.5);
          interval = box.X;
          interval00 = new Interval(double0, Math.Abs(interval.Length * 0.5));
          interval = box.Y;
          double double1 = 0.0 - Math.Abs(interval.Length * 0.5);
          interval = box.Y;
          interval01 = new Interval(double1, Math.Abs(interval.Length * 0.5));
          rectangle0 = new Rectangle3d(plane0, interval00, interval01);
          rectangle0.Plane = new Plane(plane0.Origin, plane0.XAxis, plane0.YAxis);
          rectangles.Add(rectangle0);
          break;
        }
        case 1: {
          plane1 = new Plane(box.PointAt(0.5, S, 0.5), Pl.XAxis, Pl.ZAxis);
          interval = box.X;
          double double0 = 0.0 - Math.Abs(interval.Length * 0.5);
          interval = box.X;
          interval10 = new Interval(double0, Math.Abs(interval.Length * 0.5));
          interval = box.Z;
          double double1 = 0.0 - Math.Abs(interval.Length * 0.5);
          interval = box.Z;
          interval11 = new Interval(double1, Math.Abs(interval.Length * 0.5));
          rectangle1 = new Rectangle3d(plane1, interval10, interval11);
          rectangle1.Plane = new Plane(plane1.Origin, plane1.XAxis, plane1.YAxis);
          rectangles.Add(rectangle1);
          break;
        }
        default: {
          plane1 = new Plane(box.PointAt(S, 0.5, 0.5), Pl.YAxis, Pl.ZAxis);
          interval = box.Y;
          double double0 = 0.0 - Math.Abs(interval.Length * 0.5);
          interval = box.Y;
          interval20 = new Interval(double0, Math.Abs(interval.Length * 0.5));
          interval = box.Z;
          double double1 = 0.0 - Math.Abs(interval.Length * 0.5);
          interval = box.Z;
          interval21 = new Interval(double1, Math.Abs(interval.Length * 0.5));
          rectangle2 = new Rectangle3d(plane2, interval20, interval21);
          rectangle2.Plane = new Plane(plane2.Origin, plane2.XAxis, plane2.YAxis);
          rectangles.Add(rectangle2);
          break;
        }
      }
    }

    List<double> dimX = new List<double>();
    List<double> dimY = new List<double>();
    for (int i = 0; i < rectangles.Count; i++) {
      Rectangle3d rectangle = rectangles[i];
      interval = rectangle.X;
      double double0 = interval[1];
      rectangle = rectangles[i];
      interval = rectangle.X;
      dimX.Add(double0 - interval[0]);
      rectangle = rectangles[i];
      interval = rectangle.Y;
      double double1 = interval[1];
      rectangle = rectangles[i];
      interval = rectangle.Y;
      dimY.Add(double1 - interval[0]);
    }

    B = rectangles;
    X = dimX;
    Y = dimY;
  }

  // <Custom additional code> 

  // </Custom additional code> 
}