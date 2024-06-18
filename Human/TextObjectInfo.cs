using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Rhino.DocObjects;

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
    private void RunScript(object txtObj, ref object L, ref object T, ref object S, ref object A, ref object VA)
    {
        GH_ObjectWrapper objectWrapper = new GH_ObjectWrapper(txtObj);
        if (objectWrapper == null) {
            return;
        }

        object ghGoo1 = ((GH_Goo<object>) (object) objectWrapper).Value;
        TextDotObject textDotObject = (TextDotObject) ((ghGoo1 is TextDotObject) ? ghGoo1 : null);
        TextDot textDot = null;
        if (textDotObject != null) {
            GeometryBase geometry = ((RhinoObject) textDotObject).Geometry;
            textDot = (TextDot) (object) ((geometry is TextDot) ? geometry : null);
        }

        object ghGoo2 = ((GH_Goo<object>) (object) objectWrapper).Value;
        TextEntity textEntity = (TextEntity) ((ghGoo2 is TextEntity) ? ghGoo2 : null);
        if (textEntity == null) {
            object ghGoo3 = ((GH_Goo<object>) (object) objectWrapper).Value;
            TextObject textObject = (TextObject) ((ghGoo3 is TextObject) ? ghGoo3 : null);
            if (textObject != null) {
                textEntity = textObject.TextGeometry;
            }
        }

        if (textEntity != null) {
            L = (object) ((AnnotationBase) textEntity).Plane;
            T = (object) ((AnnotationBase) textEntity).PlainText;
            S = (object) ((AnnotationBase) textEntity).TextHeight;
            int justification = (int) textEntity.Justification;
            A = (justification << 16) >> 17;
            VA = justification >> 17;
        } else if (textDot != null) {
            L = (object) new Plane(textDot.Point, Vector3d.ZAxis);
            T = (object) textDot.Text;
            S = (object) textDot.FontHeight;
            A = (object) null;
            VA = (object) null;
        }
    }

    // <Custom additional code> 

    // </Custom additional code>
