# Heteroptera GeometricCell Reimplementation

Rhino version: Rhino 7  
Plugin version: [Heteroptera 7.9.3](https://www.food4rhino.com/en/app/heteroptera)  
Component reference: ```Heteroptera.Components.Geometry.C.Geometric_Cell```

## Dependencies  

The ```GeometricCell``` class has a dependency to the ```GeometricRegion``` class; its decompiled output is also included in this repository as ```GeometricRegionDecompiled.cs```.

## Missing References

**GeometricRegionDecompiled.cs**
```C#
29        //IL_001f: Unknown result type (might be due to invalid IL or missing references)
30        //IL_0024: Unknown result type (might be due to invalid IL or missing references)
31        //IL_0029: Unknown result type (might be due to invalid IL or missing references)
32        //IL_0052: Unknown result type (might be due to invalid IL or missing references)
```

These comments are usually thrown when the decompiler encounters a reference to Rhino or Grasshopper classes and should not pose a problem. For peace of mind (at the cost of sanity), the reference of each code label can be manually checked with IL DASM. For example, the code labels above correspond to the following references:  
- ```IL_001f -> [RhinoCommon]Rhino.Geometry.Point3d```  
- ```IL_0024 -> [RhinoCommon]Rhino.Geometry.Point3d```  
- ```IL_0029 -> [RhinoCommon]Rhino.Geometry.Point3d```  
- ```IL_0052 -> [RhinoCommon]Rhino.Geometry.Point3d```  

## Notes

Time to disassemble: 8 hours  
This was the first component disassembled.