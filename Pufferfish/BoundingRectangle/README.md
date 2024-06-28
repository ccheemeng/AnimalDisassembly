# Pufferfish BoundingRectangle Reimplementation

Rhino version: Rhino 7  
Plugin version: [Pufferfish V3.0](https://www.food4rhino.com/en/app/pufferfish)  
Component reference: ```Pufferfish.Components.Components_Curve._5_Curve.BoundingRectangle```

## Missing References

**BoundingRectangleDecompiled.cs**
```C#
 49        //IL_0012: Unknown result type (might be due to invalid IL or missing references)
 50        //IL_0080: Unknown result type (might be due to invalid IL or missing references)
 51        //IL_0085: Unknown result type (might be due to invalid IL or missing references)
...
136        //IL_04c6: Unknown result type (might be due to invalid IL or missing references)
```

These comments are usually thrown when the decompiler encounters a reference to Rhino or Grasshopper classes and should not pose a problem. For peace of mind (at the cost of sanity), the reference of each code label can be manually checked with IL DASM. For example, the code labels above correspond to the following references:  
- ```IL_0012 -> [RhinoCommon]Rhino.Geometry.Plane```  
- ```IL_0080 -> [RhinoCommon]Rhino.Geometry.BoundingBox```  
- ```IL_0085 -> [RhinoCommon]Rhino.Geometry.BoundingBox```  
- ```IL_04c6 -> [RhinoCommon]Rhino.Geometry.Interval```  

## Unavailable Input Data Types

**BoundingRectangleDecompiled.cs**
```C#
137        List<IGH_GeometricGoo> list = new List<IGH_GeometricGoo>();
```

**BoundingRectangle.cs**
```C#
57   List<IGH_GeometricGoo> geometricGoo = new List<IGH_GeometricGoo>();
58    foreach (GeometryBase geometryBase in G) {
59      geometricGoo.Add(GH_Convert.ToGeometricGoo(geometryBase));
60    }
```

Because ```IGH_GeometricGoo``` is not a valid input type for the provided C# Scripting Component, the next best type ```GeometryBase``` is used.  

```Grasshopper.Kernel.GH_Convert::ToGeometricGoo(Object)``` is used to convert ```GeometryBase``` to ```IGH_GeometricGoo```.

## Constructor Confusion ```.ctor```

**BoundingRecangleDecompiled.cs**
```C#
154       List<BoundingBox> list3 = new List<BoundingBox>();
...
175        Box val3 = default(Box);
...
191            ((Box)(ref val3))._002Ector(list3[j]);
192            ((Box)(ref val3)).Plane = val;
```

**BoundingRectangle.cs**
```C#
63    List<BoundingBox> boundingBoxes = new List<BoundingBox>();
...
79    Box box = default(Box);
...
94      box = new Box(boundingBoxes[i]);
95      box.Plane = Pl;
```

Sometimes decompilers confuse an IL constructor ```.ctor``` for another method, and thus represent it as such. In this case, the constructor has been represented as ```._002Ector```.  

With some reference to the [RhinoCommon API](https://developer.rhino3d.com/api/rhinocommon/rhino.geometry.box/box#(boundingbox)) and some more inference, we can see that the ```val3``` is being set as a new ```Box``` via the ```Box(BoundingBox)``` constructor.

## Passing by Reference

**BoundingRectangleDecompiled.cs**
```C#
176        Plane val11 = default(Plane);
...
197                        ((Plane)(ref val11))._002Ector(((Box)(ref val3)).PointAt(0.5, 0.5, num2), ((Plane)(ref val)).XAxis, ((Plane)(ref val)).YAxis);
```

**BoundingRectangle.cs**
```C#
81    Plane plane0 = default(Plane);
...
98          plane0 = new Plane(box.PointAt(0.5, 0.5, S), Pl.XAxis, Pl.YAxis);
```

All arguments should not be passed by reference. Before variable renaming (and the aforementioned constructor issue), the following replacements were made:
- ```((Plane)(ref val11)) -> val11```
- ```((Box)(ref val3)) -> val3```
- ```((Plane)(ref val)) -> val```