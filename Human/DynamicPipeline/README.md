# Human DynamicPipeline Reimplementation

Rhino version: Rhino 7  
Plugin version: [Human for Rhino 6](https://www.food4rhino.com/en/app/human)  
Component reference: ```Human.DynamicPipeline```

## Missing References

**DynamicPipelineDecompiled.cs**
```C#
238         //IL_0027: Unknown result type (might be due to invalid IL or missing references)
239        //IL_0020: Unknown result type (might be due to invalid IL or missing references)
240        //IL_059d: Unknown result type (might be due to invalid IL or missing references)
...
333        //IL_044f: Unknown result type (might be due to invalid IL or missing references)
```

These comments are usually thrown when the decompiler encounters a reference to Rhino or Grasshopper classes and should not pose a problem. For peace of mind (at the cost of sanity), the reference of each code label can be manually checked with IL DASM. For example, the code labels above correspond to the following references:  
- ```IL_0027 -> [RhinoCommon]Rhino.DocObjects.ObjectType```  
- ```IL_0020 -> [RhinoCommon]Rhino.DocObjects.ObjectType```  
- ```IL_059d -> [RhinoCommon]Rhino.Geometry.BoundingBox```  
- ```IL_044f -> [RhinoCommon]Rhino.DocObjects.ObjectType```  