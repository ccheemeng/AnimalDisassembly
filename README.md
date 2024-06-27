# Animal Disassembly

Sample reimplementations of third-party Grasshopper .NET plugin components using Grasshopper's native script components.

## Instructions

In general, disassembling a plugin involves the following steps:
1. Decompile the relevant grasshopper assembly ```.gha``` file(s), which are effectively .NET DLLs.
2. In the class definition of the desired component, reimplement the ```SolveInstance(IGH_DataAccess)``` method of the class in Grasshopper's native C# script component.

What follows is a general walkthrough of the above steps using Visual Studio 2022 and [Pufferfish](https://www.food4rhino.com/en/app/pufferfish)'s ```BoundingRectangle``` component.

### 1. Decompilation

1. Obtain a copy of the plugin ```.gha``` file (most plugins are available on [Food4Rhino](https://www.food4rhino.com/en)). This example will use the ```Pufferfish3-0.gha```.
2. Rename the ```.gha``` extension to ```.dll```.
3. Open **Developer Powershell for VS 2022** and navigate to the directory where the ```.dll``` file is located.
4. Enter ```ildasm.exe <filename>.dll```.  
```ildasm.exe Pufferfish3-0.dll```  
If successful, the *IL DASM* UI should appear with a tree of the plugin's definitions.
5. Identify the reference path of the desired component. In the case of Pufferfish's ```BoundingRectangle```, its reference is ```Pufferfish.Components.Components_Curve._5_Curve.BoundingRectangle```.
6. Open **Visual Studio 2022** and create a new C# project (whether it is a Console App or otherwie does not matter).
7. [Add a project reference](https://learn.microsoft.com/en-us/visualstudio/ide/how-to-add-or-remove-references-by-using-the-reference-manager?view=vs-2022#add-a-reference) using the *Solution Explorer*. Add the ```.dll``` file as a reference.
8. In the project editor window, add an assembly reference using ```using```.  
```using Pufferfish;```
9. Enter code that references the desired class.  
```Pufferfish.Components.Components_Curve._5_Curve.BoundingRectangle ...```
10. Open the context menu for the class (```BoundingRectangle```) and select ```Go To Implementation```. If successful, Visual Studio 2022 will open another editor window with a decompiled definition of of the class.

### 2. Reimplementation

1. Find the ```SolveInstance(IGH_DataAccess)``` method defined within the class (with a ```void``` return type). This is the method that is called when Grasshopper executes a component.  
    > The IGH_DataAccess interface provides access to three main methods for getting component input data:  
    > ```IGH_DataAccess::GetData<T>(Int32, T)```  
    > ```IGH_DataAccess::GetDataList<T>(Int32, List<T>)```  
    > ```IGH_DataAccess::GetDataTree<T>(Int32, GH_Structure<T>)```  
    > Note that methods for parameter access by name (replacing ```Int32``` with ```String```) exist too, but are rarely used.

    > The IGH_DataAccess interface provides access to four main methods for setting component output data:  
    > ```IGH_DataAccess::SetData(Int32, Object)```  
    > ```IGH_DataAccess::SetDataList<T>(Int32, IEnumerable)```  
    > ```IGH_DataAccess::SetDataTree<T>(Int32, IGH_DataTree)```  
    > ```IGH_DataAccess::SetDataTree<T>(Int32, IGH_Structure)```  
    > Note that methods for parameter access by name (replacing ```Int32``` with ```String```) exist too, but are rarely used.
2. Find all ```GetDataX<T>(Int32,Y<T>)``` calls in the method.
    - ```X``` corresponds to the type of access each input parameter has:  
    ```X``` is nothing: Item Access  
    ```X``` is ```List```: List Access  
    ```X``` is ```Tree```: Tree Access
    - ```Int32``` corresponds to the index of the input parameter. In ```BoundingRectangle```:  
    ```0```: G  
    ```1```: Pl  
    ```2```: A  
    ```3```: S  
    ```4```: U  
3. Find all ```SetDataX(Int32, R)``` calls in the method.
    - ```Int32``` corresponds to the index of the output parameter. In ```BoundingRectangle```:  
    ```0```: B  
    ```1```: X  
    ```2```: Y
4. Edit the input and output parameters of Rhino's C# Script Component to match each parameter referenced by the ```GetData``` and ```SetData``` methods respectively of ```SolveInstance(IGH_DataAccess)```.
5. Reimplement the class's ```SolveInstance(IGH_DataAccess)``` method within the ```RunScript(...)``` method of the Script Component.  

Each subfolder contains documentation with specifics of the reimplementation process for that particular plugin and component.