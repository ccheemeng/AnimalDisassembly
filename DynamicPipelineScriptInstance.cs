using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Microsoft.VisualBasic;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;

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
    private void RunScript(List<string> lay, List<string> type, List<string> name, ref object G)
    {
        DynamicPipeline dynamicPipeline = new DynamicPipeline(RhinoDocument);
        G = dynamicPipeline.Compute(lay, type, name);
    }

  // <Custom additional code> 
    class DynamicPipeline {
        private RhinoDoc rhinoDocument;
        private List<string> layerFilters;
        private List<string> nameFilters;
        private ObjectType typeFilter;

        public DynamicPipeline(RhinoDoc rhinoDocument) {
            this.rhinoDocument = rhinoDocument;
            this.layerFilters = new List<string>();
            this.nameFilters = new List<string>();
            this.typeFilter = (ObjectType) 0x0;
        }

        public List<object> Compute(List<string> lay, List<string> type, List<string> name) {
            this.layerFilters = lay;
            this.nameFilters = name;
            this.typeFilter = UpdateTypeFilter(type);

            ObjectEnumeratorSettings objectEnumeratorSettings = new ObjectEnumeratorSettings();
            objectEnumeratorSettings.ActiveObjects = true;
            objectEnumeratorSettings.LockedObjects = true;
            objectEnumeratorSettings.HiddenObjects = true;
            objectEnumeratorSettings.IncludeGrips = false;
            objectEnumeratorSettings.IncludeLights = true;
            objectEnumeratorSettings.IncludePhantoms = false;
            objectEnumeratorSettings.ReferenceObjects = true;
            objectEnumeratorSettings.NormalObjects = true;
            objectEnumeratorSettings.ObjectTypeFilter = this.typeFilter;
            IEnumerable<RhinoObject> objectList = this.rhinoDocument.Objects.GetObjectList(objectEnumeratorSettings);
            List<object> objs = new List<object>();
            foreach (RhinoObject rhinoObject in objectList) {
                if (!IsRelevantObject(rhinoObject)) {
                    continue;
                }

                IGH_GeometricGoo geometry = GH_Convert.ToGeometricGoo((object) ((ModelComponent) rhinoObject).Id);
                if (geometry != null) {
                    objs.Add(geometry);
                } else {
                    try {
                        objs.Add(rhinoObject);
                    } catch (Exception e) {
                        RhinoApp.Write(e.ToString() + "\n");
                    }
                }
            }

            return objs;
        }

        private ObjectType UpdateTypeFilter(List<string> type) {
            ObjectType objectType = 0x0;
            if (type.Count == 0) {
                objectType = (ObjectType) ((int) objectType | 0x1); // Point
                objectType = (ObjectType) ((int) objectType | 0x4); // Curve
                objectType = (ObjectType) ((int) objectType | 0x8); // Surface
                objectType = (ObjectType) ((int) objectType | 0x10); // Brep
                objectType = (ObjectType) ((int) objectType | 0x20); // Mesh
                objectType = (ObjectType) ((int) objectType | 0x100); // Light
                objectType = (ObjectType) ((int) objectType | 0x200); // Annotation
                objectType = (ObjectType) ((int) objectType | 0x1000); // InstanceReference
                objectType = (ObjectType) ((int) objectType | 0x2000); // TextDot
                objectType = (ObjectType) ((int) objectType | 0x10000); // Hatch
                objectType = (ObjectType) ((int) objectType | 0x40000000); // Extrusion
            }
            foreach (string typeString in type) {
                switch (typeString.ToLower()) {
                    case "point":
                    case "pt":
                        objectType = (ObjectType) ((int) objectType | 0x1); // Point
                        break;
                    case "curve":
                    case "crv":
                        objectType = (ObjectType) ((int) objectType | 0x4); // Curve
                        break;
                    case "surface":
                    case "polysurface":
                    case "brep":
                        objectType = (ObjectType) ((int) objectType | 0x8); // Surface
                        objectType = (ObjectType) ((int) objectType | 0x10); // Brep
                        break;
                    case "mesh":
                        objectType = (ObjectType) ((int) objectType | 0x20); // Mesh
                        break;
                    case "light":
                        objectType = (ObjectType) ((int) objectType | 0x100); // Light
                        break;
                    case "annotation":
                    case "text":
                        objectType = (ObjectType) ((int) objectType | 0x200); // Annotation
                        objectType = (ObjectType) ((int) objectType | 0x2000); // TextDot
                        break;
                    case "block":
                    case "instance":
                        objectType = (ObjectType) ((int) objectType | 0x1000); // InstanceReference
                        break;
                    case "hatch":
                        objectType = (ObjectType) ((int) objectType | 0x10000); // Hatch
                        break;
                    case "extrusion":
                        objectType = (ObjectType) ((int) objectType | 0x40000000); // Extrusion
                        break;
                    case "default":
                        objectType = (ObjectType) ((int) objectType | 0x1); // Point
                        objectType = (ObjectType) ((int) objectType | 0x4); // Curve
                        objectType = (ObjectType) ((int) objectType | 0x8); // Surface
                        objectType = (ObjectType) ((int) objectType | 0x10); // Brep
                        objectType = (ObjectType) ((int) objectType | 0x20); // Mesh
                        break;
                    case "any":
                    case "all":
                        objectType = (ObjectType) ((int) objectType | 0x1); // Point
                        objectType = (ObjectType) ((int) objectType | 0x4); // Curve
                        objectType = (ObjectType) ((int) objectType | 0x8); // Surface
                        objectType = (ObjectType) ((int) objectType | 0x10); // Brep
                        objectType = (ObjectType) ((int) objectType | 0x20); // Mesh
                        objectType = (ObjectType) ((int) objectType | 0x100); // Light
                        objectType = (ObjectType) ((int) objectType | 0x200); // Annotation
                        objectType = (ObjectType) ((int) objectType | 0x1000); // InstanceReference
                        objectType = (ObjectType) ((int) objectType | 0x2000); // TextDot
                        objectType = (ObjectType) ((int) objectType | 0x10000); // Hatch
                        objectType = (ObjectType) ((int) objectType | 0x40000000); // Extrusion
                        break;
                }
            }
            return objectType;
        }

        private bool IsRelevantObject(RhinoObject obj) {
            if ((ObjectType) (obj.ObjectType & this.typeFilter) != obj.ObjectType) {
                return false;
            }
            foreach (string nameFilter in this.nameFilters) {
                if (!Microsoft.VisualBasic.CompilerServices.LikeOperator.LikeString(
                    obj.Attributes.Name, nameFilter, CompareMethod.Binary)) {
                    return false;
                }
            }
            foreach (string layerFilter in this.layerFilters) {
                if (!Microsoft.VisualBasic.CompilerServices.LikeOperator.LikeString(
                    ((ModelComponent) this.rhinoDocument.Layers[obj.Attributes.LayerIndex]).Name,
                    layerFilter, CompareMethod.Binary)) {
                    return false;
                }
            }
            return true;
        }
    }
  // </Custom additional code> 
}