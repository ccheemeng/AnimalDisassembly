#region Assembly Human, Version=1.7.3.0, Culture=neutral, PublicKeyToken=null
// Decompiled with ICSharpCode.Decompiler 8.1.1.7464
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Human.Properties;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;
using Rhino.Geometry;

namespace Human;

public class DynamicPipeline : GH_Component
{
    private const ObjectType TotalFilter = 61u;

    private const ObjectType BlankFilter = 0u;

    private string m_layerFilter;

    private List<string> m_ListenerLayFilters;

    private List<string> m_ListenerNameFilters;

    private string m_nameFilter;

    private ObjectType m_typeFilter;

    private ObjectType m_listenerTypeFilter;

    private bool m_includeLocked;

    private bool m_includeHidden;

    private bool m_groupLayer;

    private bool m_groupType;

    private bool m_enableFullPath;

    private bool m_expired;

    private bool m_listenToAttributeChanges;

    protected GH_Structure<IGH_Goo> m_data2;

    private List<Guid> m_idCache;

    private BoundingBox m_clip;

    public string LayerFilter
    {
        get
        {
            return m_layerFilter;
        }
        set
        {
            if (value == null)
            {
                value = string.Empty;
            }

            m_layerFilter = value;
        }
    }

    public string NameFilter
    {
        get
        {
            return m_nameFilter;
        }
        set
        {
            if (value == null)
            {
                value = string.Empty;
            }

            m_nameFilter = value;
        }
    }

    public ObjectType TypeFilter
    {
        get
        {
            //IL_0001: Unknown result type (might be due to invalid IL or missing references)
            return m_typeFilter;
        }
        set
        {
            //IL_0001: Unknown result type (might be due to invalid IL or missing references)
            //IL_0004: Unknown result type (might be due to invalid IL or missing references)
            //IL_0005: Unknown result type (might be due to invalid IL or missing references)
            //IL_000b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0012: Unknown result type (might be due to invalid IL or missing references)
            //IL_0015: Invalid comparison between Unknown and I4
            //IL_0028: Unknown result type (might be due to invalid IL or missing references)
            //IL_002f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0030: Unknown result type (might be due to invalid IL or missing references)
            //IL_0019: Unknown result type (might be due to invalid IL or missing references)
            //IL_001f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0020: Unknown result type (might be due to invalid IL or missing references)
            m_typeFilter = (ObjectType)(value & 0x3D);
            if ((m_typeFilter & 0x10) == 16)
            {
                m_typeFilter = (ObjectType)(m_typeFilter | 8);
            }
            else
            {
                m_typeFilter = (ObjectType)(m_typeFilter & -9);
            }
        }
    }

    public bool IncludeLocked
    {
        get
        {
            return m_includeLocked;
        }
        set
        {
            m_includeLocked = value;
        }
    }

    public bool IncludeHidden
    {
        get
        {
            return m_includeHidden;
        }
        set
        {
            m_includeHidden = value;
        }
    }

    public bool GroupByLayer
    {
        get
        {
            return m_groupLayer;
        }
        set
        {
            m_groupLayer = value;
        }
    }

    public bool GroupByType
    {
        get
        {
            return m_groupType;
        }
        set
        {
            m_groupType = value;
        }
    }

    public bool enableFullPath
    {
        get
        {
            return m_enableFullPath;
        }
        set
        {
            m_enableFullPath = value;
        }
    }

    protected override Bitmap Icon => Human.Properties.Resources.dynamic_pipeline;

    public override Guid ComponentGuid => new Guid("{3B896370-6860-4F0D-AD7A-704BB786272C}");

    public DynamicPipeline()
        : base("Dynamic Geometry Pipeline", "DPipeline", "Defines a Geometry Pipeline from Rhino to Grasshopper, with variable filters for name, object type, and layer.", "Human", "Reference")
    {
        //IL_0042: Unknown result type (might be due to invalid IL or missing references)
        //IL_0049: Unknown result type (might be due to invalid IL or missing references)
        m_ListenerLayFilters = new List<string>();
        m_layerFilter = "*";
        m_nameFilter = "*";
        m_typeFilter = (ObjectType)0;
        m_listenerTypeFilter = (ObjectType)0;
        m_includeLocked = true;
        m_includeHidden = true;
        m_groupLayer = false;
        m_groupType = false;
        m_expired = true;
        m_idCache = new List<Guid>();
        m_listenToAttributeChanges = true;
        enableFullPath = false;
        UpdateMenu();
    }

    public override void ExpireSolution(bool recompute)
    {
        m_expired = true;
        ((GH_ActiveObject)this).ExpireSolution(recompute);
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        string text = "Filter is case-sensitive." + "\n" + "The following wildcards are allowed:" + "\n" + "? (any single character)" + "\n" + "* (zero or more characters)" + "\n" + "# (any single digit [0-9])" + "\n" + "[chars] (any single character in chars)" + "\n" + "[!chars] (any single character not in chars)";
        string text2 = "The type is specified with a list of text items, " + "\n" + "including 'Point,','Curve,''Brep,''Mesh,' 'Hatch,'" + "\n" + "'Extrusion,' 'Light', 'Text,' and 'Block.' In addition," + "\n" + "'All' can be used to accept all types, and 'Default' will " + "\n" + "include only those types accepted by the built-in pipeline component.";
        pManager.AddTextParameter("Layer Filter", "lay", text, (GH_ParamAccess)0, "*");
        pManager.AddTextParameter("Type Filter", "type", text2, (GH_ParamAccess)1);
        ((GH_ParamManager)pManager)[1].Optional = true;
        pManager.AddTextParameter("Name Filter", "name", text, (GH_ParamAccess)0, "*");
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Geometry Out", "G", "Collected Geometry", (GH_ParamAccess)1);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        //IL_0027: Unknown result type (might be due to invalid IL or missing references)
        //IL_0020: Unknown result type (might be due to invalid IL or missing references)
        //IL_059d: Unknown result type (might be due to invalid IL or missing references)
        //IL_05a2: Unknown result type (might be due to invalid IL or missing references)
        //IL_05b3: Unknown result type (might be due to invalid IL or missing references)
        //IL_05d9: Unknown result type (might be due to invalid IL or missing references)
        //IL_05e0: Expected O, but got Unknown
        //IL_062d: Unknown result type (might be due to invalid IL or missing references)
        //IL_06d1: Unknown result type (might be due to invalid IL or missing references)
        //IL_0793: Unknown result type (might be due to invalid IL or missing references)
        //IL_079a: Expected O, but got Unknown
        //IL_058b: Unknown result type (might be due to invalid IL or missing references)
        //IL_0591: Unknown result type (might be due to invalid IL or missing references)
        //IL_0596: Unknown result type (might be due to invalid IL or missing references)
        //IL_0597: Unknown result type (might be due to invalid IL or missing references)
        //IL_0404: Unknown result type (might be due to invalid IL or missing references)
        //IL_040e: Unknown result type (might be due to invalid IL or missing references)
        //IL_040f: Unknown result type (might be due to invalid IL or missing references)
        //IL_041b: Unknown result type (might be due to invalid IL or missing references)
        //IL_0425: Unknown result type (might be due to invalid IL or missing references)
        //IL_0426: Unknown result type (might be due to invalid IL or missing references)
        //IL_042d: Unknown result type (might be due to invalid IL or missing references)
        //IL_0437: Unknown result type (might be due to invalid IL or missing references)
        //IL_0438: Unknown result type (might be due to invalid IL or missing references)
        //IL_039c: Unknown result type (might be due to invalid IL or missing references)
        //IL_03a2: Unknown result type (might be due to invalid IL or missing references)
        //IL_03a3: Unknown result type (might be due to invalid IL or missing references)
        //IL_037a: Unknown result type (might be due to invalid IL or missing references)
        //IL_0381: Unknown result type (might be due to invalid IL or missing references)
        //IL_0382: Unknown result type (might be due to invalid IL or missing references)
        //IL_0389: Unknown result type (might be due to invalid IL or missing references)
        //IL_038f: Unknown result type (might be due to invalid IL or missing references)
        //IL_0390: Unknown result type (might be due to invalid IL or missing references)
        //IL_03c3: Unknown result type (might be due to invalid IL or missing references)
        //IL_03cd: Unknown result type (might be due to invalid IL or missing references)
        //IL_03ce: Unknown result type (might be due to invalid IL or missing references)
        //IL_045b: Unknown result type (might be due to invalid IL or missing references)
        //IL_0462: Unknown result type (might be due to invalid IL or missing references)
        //IL_0463: Unknown result type (might be due to invalid IL or missing references)
        //IL_046a: Unknown result type (might be due to invalid IL or missing references)
        //IL_0470: Unknown result type (might be due to invalid IL or missing references)
        //IL_0471: Unknown result type (might be due to invalid IL or missing references)
        //IL_0478: Unknown result type (might be due to invalid IL or missing references)
        //IL_047f: Unknown result type (might be due to invalid IL or missing references)
        //IL_0480: Unknown result type (might be due to invalid IL or missing references)
        //IL_0487: Unknown result type (might be due to invalid IL or missing references)
        //IL_048d: Unknown result type (might be due to invalid IL or missing references)
        //IL_048e: Unknown result type (might be due to invalid IL or missing references)
        //IL_0495: Unknown result type (might be due to invalid IL or missing references)
        //IL_049b: Unknown result type (might be due to invalid IL or missing references)
        //IL_049c: Unknown result type (might be due to invalid IL or missing references)
        //IL_04a3: Unknown result type (might be due to invalid IL or missing references)
        //IL_04ad: Unknown result type (might be due to invalid IL or missing references)
        //IL_04ae: Unknown result type (might be due to invalid IL or missing references)
        //IL_04b5: Unknown result type (might be due to invalid IL or missing references)
        //IL_04bf: Unknown result type (might be due to invalid IL or missing references)
        //IL_04c0: Unknown result type (might be due to invalid IL or missing references)
        //IL_04c7: Unknown result type (might be due to invalid IL or missing references)
        //IL_04d1: Unknown result type (might be due to invalid IL or missing references)
        //IL_04d2: Unknown result type (might be due to invalid IL or missing references)
        //IL_04d9: Unknown result type (might be due to invalid IL or missing references)
        //IL_04e3: Unknown result type (might be due to invalid IL or missing references)
        //IL_04e4: Unknown result type (might be due to invalid IL or missing references)
        //IL_04eb: Unknown result type (might be due to invalid IL or missing references)
        //IL_04f5: Unknown result type (might be due to invalid IL or missing references)
        //IL_04f6: Unknown result type (might be due to invalid IL or missing references)
        //IL_04fd: Unknown result type (might be due to invalid IL or missing references)
        //IL_0507: Unknown result type (might be due to invalid IL or missing references)
        //IL_0508: Unknown result type (might be due to invalid IL or missing references)
        //IL_03da: Unknown result type (might be due to invalid IL or missing references)
        //IL_03e0: Unknown result type (might be due to invalid IL or missing references)
        //IL_03e1: Unknown result type (might be due to invalid IL or missing references)
        //IL_03ed: Unknown result type (might be due to invalid IL or missing references)
        //IL_03f7: Unknown result type (might be due to invalid IL or missing references)
        //IL_03f8: Unknown result type (might be due to invalid IL or missing references)
        //IL_03af: Unknown result type (might be due to invalid IL or missing references)
        //IL_03b6: Unknown result type (might be due to invalid IL or missing references)
        //IL_03b7: Unknown result type (might be due to invalid IL or missing references)
        //IL_0511: Unknown result type (might be due to invalid IL or missing references)
        //IL_0518: Unknown result type (might be due to invalid IL or missing references)
        //IL_0519: Unknown result type (might be due to invalid IL or missing references)
        //IL_0520: Unknown result type (might be due to invalid IL or missing references)
        //IL_0526: Unknown result type (might be due to invalid IL or missing references)
        //IL_0527: Unknown result type (might be due to invalid IL or missing references)
        //IL_052e: Unknown result type (might be due to invalid IL or missing references)
        //IL_0535: Unknown result type (might be due to invalid IL or missing references)
        //IL_0536: Unknown result type (might be due to invalid IL or missing references)
        //IL_053d: Unknown result type (might be due to invalid IL or missing references)
        //IL_0543: Unknown result type (might be due to invalid IL or missing references)
        //IL_0544: Unknown result type (might be due to invalid IL or missing references)
        //IL_054b: Unknown result type (might be due to invalid IL or missing references)
        //IL_0551: Unknown result type (might be due to invalid IL or missing references)
        //IL_0552: Unknown result type (might be due to invalid IL or missing references)
        //IL_0444: Unknown result type (might be due to invalid IL or missing references)
        //IL_044e: Unknown result type (might be due to invalid IL or missing references)
        //IL_044f: Unknown result type (might be due to invalid IL or missing references)
        if (DA.Iteration == 0)
        {
            m_ListenerLayFilters = new List<string>();
            m_ListenerNameFilters = new List<string>();
            m_listenerTypeFilter = (ObjectType)0;
        }

        m_typeFilter = (ObjectType)0;
        string text = "*";
        string text2 = "";
        DA.GetData<string>(0, ref text);
        m_ListenerLayFilters.Add(text);
        m_layerFilter = text;
        DA.GetData<string>(2, ref text2);
        m_ListenerNameFilters.Add(text2);
        m_nameFilter = text2;
        List<string> list = new List<string>();
        if (DA.GetDataList<string>(1, list))
        {
            foreach (string item in list)
            {
                switch (item.ToLower())
                {
                    case "surface":
                    case "polysurface":
                    case "brep":
                        m_typeFilter = (ObjectType)(m_typeFilter | 0x10);
                        m_typeFilter = (ObjectType)(m_typeFilter | 8);
                        break;
                    case "curve":
                    case "crv":
                        m_typeFilter = (ObjectType)(m_typeFilter | 4);
                        break;
                    case "mesh":
                        m_typeFilter = (ObjectType)(m_typeFilter | 0x20);
                        break;
                    case "extrusion":
                        m_typeFilter = (ObjectType)(m_typeFilter | 0x40000000);
                        break;
                    case "point":
                    case "pt":
                        m_typeFilter = (ObjectType)(m_typeFilter | 1);
                        break;
                    case "block":
                    case "instance":
                        m_typeFilter = (ObjectType)(m_typeFilter | 0x1000);
                        break;
                    case "light":
                        m_typeFilter = (ObjectType)(m_typeFilter | 0x100);
                        break;
                    case "annotation":
                    case "text":
                        m_typeFilter = (ObjectType)(m_typeFilter | 0x200);
                        m_typeFilter = (ObjectType)(m_typeFilter | 0x2000);
                        break;
                    case "hatch":
                        m_typeFilter = (ObjectType)(m_typeFilter | 0x10000);
                        break;
                    case "any":
                    case "all":
                        m_typeFilter = (ObjectType)(m_typeFilter | 0x20);
                        m_typeFilter = (ObjectType)(m_typeFilter | 4);
                        m_typeFilter = (ObjectType)(m_typeFilter | 0x10);
                        m_typeFilter = (ObjectType)(m_typeFilter | 8);
                        m_typeFilter = (ObjectType)(m_typeFilter | 1);
                        m_typeFilter = (ObjectType)(m_typeFilter | 0x200);
                        m_typeFilter = (ObjectType)(m_typeFilter | 0x10000);
                        m_typeFilter = (ObjectType)(m_typeFilter | 0x100);
                        m_typeFilter = (ObjectType)(m_typeFilter | 0x1000);
                        m_typeFilter = (ObjectType)(m_typeFilter | 0x40000000);
                        m_typeFilter = (ObjectType)(m_typeFilter | 0x2000);
                        break;
                    case "default":
                        m_typeFilter = (ObjectType)(m_typeFilter | 0x20);
                        m_typeFilter = (ObjectType)(m_typeFilter | 4);
                        m_typeFilter = (ObjectType)(m_typeFilter | 0x10);
                        m_typeFilter = (ObjectType)(m_typeFilter | 8);
                        m_typeFilter = (ObjectType)(m_typeFilter | 1);
                        break;
                    default:
                        ((GH_ActiveObject)this).AddRuntimeMessage((GH_RuntimeMessageLevel)10, $"Your type {item} was not recognized.");
                        break;
                }
            }

            m_listenerTypeFilter |= m_typeFilter;
        }

        m_clip = BoundingBox.Empty;
        m_idCache.Clear();
        if ((int)m_listenerTypeFilter == 0)
        {
            m_expired = false;
            return;
        }

        RhinoDoc activeDoc = RhinoDoc.ActiveDoc;
        if (activeDoc == null)
        {
            ((GH_ActiveObject)this).AddRuntimeMessage((GH_RuntimeMessageLevel)10, "doc is null");
            return;
        }

        ObjectEnumeratorSettings val = new ObjectEnumeratorSettings();
        val.ActiveObjects = true;
        val.LockedObjects = IncludeLocked;
        val.HiddenObjects = IncludeHidden;
        val.IncludeGrips = false;
        val.IncludeLights = true;
        val.IncludePhantoms = false;
        val.ReferenceObjects = true;
        val.NormalObjects = true;
        val.ObjectTypeFilter = TypeFilter;
        IEnumerable<RhinoObject> objectList = activeDoc.Objects.GetObjectList(val);
        List<object> list2 = new List<object>();
        List<ObjectAttributes> list3 = new List<ObjectAttributes>();
        SortedList<int, int> sortedList = new SortedList<int, int>();
        using (null)
        {
            foreach (RhinoObject item2 in objectList)
            {
                if (!IsRelevantObject(item2))
                {
                    continue;
                }

                IGH_GeometricGoo val2 = GH_Convert.ToGeometricGoo((object)((ModelComponent)item2).Id);
                if (val2 != null)
                {
                    list2.Add(val2);
                    list3.Add(item2.Attributes);
                    m_idCache.Add(((ModelComponent)item2).Id);
                    if (val2 != null)
                    {
                        ((BoundingBox)(ref m_clip)).Union(val2.Boundingbox);
                    }

                    if (GroupByLayer)
                    {
                        if (sortedList.ContainsKey(item2.Attributes.LayerIndex))
                        {
                            SortedList<int, int> sortedList2 = sortedList;
                            int layerIndex = item2.Attributes.LayerIndex;
                            sortedList2[layerIndex] += 1;
                        }
                        else
                        {
                            sortedList.Add(item2.Attributes.LayerIndex, 1);
                        }
                    }
                }
                else
                {
                    try
                    {
                        list2.Add(item2);
                    }
                    catch (Exception ex)
                    {
                        ((GH_ActiveObject)this).AddRuntimeMessage((GH_RuntimeMessageLevel)10, ex.ToString());
                    }
                }
            }
        }

        List<object> list4 = new List<object>();
        int num = list2.Count - 1;
        for (int i = 0; i <= num; i++)
        {
            GH_Path val3 = new GH_Path(0);
            if (GroupByLayer)
            {
                int num2 = sortedList.IndexOfKey(list3[i].LayerIndex);
                val3 = val3.AppendElement(num2);
            }

            list4.Add(list2[i]);
        }

        m_expired = false;
        DA.SetDataList(0, (IEnumerable)list4);
    }

    public override void AddedToDocument(GH_Document document)
    {
        if (((GH_ActiveObject)this).Locked)
        {
            RemoveHandlers();
        }
        else
        {
            AddHandlers();
        }
    }

    public override void RemovedFromDocument(GH_Document document)
    {
        RemoveHandlers();
    }

    public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
    {
        //IL_0000: Unknown result type (might be due to invalid IL or missing references)
        //IL_0002: Unknown result type (might be due to invalid IL or missing references)
        //IL_0020: Expected I4, but got Unknown
        switch (context - 2)
        {
            case 0:
            case 2:
            case 5:
                if (((GH_ActiveObject)this).Locked)
                {
                    RemoveHandlers();
                }
                else
                {
                    AddHandlers();
                }

                break;
            case 1:
            case 3:
            case 4:
                RemoveHandlers();
                break;
        }
    }

    private void AddHandlers()
    {
        RemoveHandlers();
        RhinoDoc.EndOpenDocument += OnEndOpenDocument;
        Command.UndoRedo += OnUndoRedo;
        RhinoDoc.AddRhinoObject += OnObjectAdded;
        RhinoDoc.DeleteRhinoObject += OnObjectDeleted;
        RhinoDoc.UndeleteRhinoObject += OnObjectUndeleted;
        RhinoDoc.ModifyObjectAttributes += OnObjectAttributesChanged;
        RhinoDoc.LayerTableEvent += OnLayerChanged;
        RhinoDoc.LightTableEvent += OnLightEvent;
    }

    private void RemoveHandlers()
    {
        RhinoDoc.EndOpenDocument -= OnEndOpenDocument;
        Command.UndoRedo -= OnUndoRedo;
        RhinoDoc.AddRhinoObject -= OnObjectAdded;
        RhinoDoc.DeleteRhinoObject -= OnObjectDeleted;
        RhinoDoc.UndeleteRhinoObject -= OnObjectUndeleted;
        RhinoDoc.ModifyObjectAttributes -= OnObjectAttributesChanged;
        RhinoDoc.LayerTableEvent -= OnLayerChanged;
        RhinoDoc.LightTableEvent -= OnLightEvent;
    }

    private void OnEndOpenDocument(object sender, DocumentOpenEventArgs e)
    {
        if (!e.Reference && !e.Merge)
        {
            ((GH_DocumentObject)this).ExpireSolution(true);
        }
    }

    private void OnUndoRedo(object sender, UndoRedoEventArgs e)
    {
        //IL_0024: Unknown result type (might be due to invalid IL or missing references)
        //IL_002a: Invalid comparison between Unknown and I4
        if ((e.IsEndRecording || e.IsEndRedo) && m_expired)
        {
            GH_Document val = ((GH_DocumentObject)this).OnPingDocument();
            if (val != null && (int)val.SolutionState != 1)
            {
                ((GH_DocumentObject)this).ExpireSolution(true);
            }
        }
    }

    private void OnObjectAttributesChanged(object sender, RhinoModifyObjectAttributesEventArgs e)
    {
        if (!m_listenToAttributeChanges || m_expired)
        {
            return;
        }

        RhinoDoc activeDoc = RhinoDoc.ActiveDoc;
        if (activeDoc != null)
        {
            RhinoObject val = activeDoc.Objects.Find(e.NewAttributes.ObjectId);
            if (val != null && IsRelevantListenObject(val))
            {
                m_expired = true;
            }
        }
    }

    private void OnObjectAdded(object sender, RhinoObjectEventArgs e)
    {
        if (!m_expired && IsRelevantListenObject(e.TheObject))
        {
            m_expired = true;
        }
    }

    private void OnLightEvent(object sender, LightTableEventArgs e)
    {
        if (!m_expired && IsRelevantListenObject((RhinoObject)(object)RhinoDoc.ActiveDoc.Lights[e.LightIndex]))
        {
            m_expired = true;
        }
    }

    private void OnObjectDeleted(object sender, RhinoObjectEventArgs e)
    {
        if (!m_expired && IsRelevantListenObject(e.TheObject))
        {
            m_expired = true;
        }
    }

    private void OnObjectUndeleted(object sender, RhinoObjectEventArgs e)
    {
        if (!m_expired && IsRelevantListenObject(e.TheObject))
        {
            m_expired = true;
        }
    }

    private void OnLayerChanged(object sender, LayerTableEventArgs e)
    {
        //IL_000a: Unknown result type (might be due to invalid IL or missing references)
        //IL_0010: Invalid comparison between Unknown and I4
        if (m_expired || (int)e.EventType != 3 || e.OldState.IsLocked != e.NewState.IsLocked || e.OldState.IsVisible != e.NewState.IsVisible || e.OldState.Color != e.NewState.Color || e.OldState.IsExpanded != e.NewState.IsExpanded || e.OldState.LinetypeIndex != e.NewState.LinetypeIndex || e.OldState.ParentLayerId != e.NewState.ParentLayerId || e.OldState.PlotColor != e.NewState.PlotColor || e.OldState.PlotWeight != e.NewState.PlotWeight || e.OldState.RenderMaterialIndex != e.NewState.RenderMaterialIndex)
        {
            return;
        }

        bool flag = false;
        foreach (string listenerLayFilter in m_ListenerLayFilters)
        {
            LikeOperator.LikeString(((ModelComponent)e.OldState).Name, listenerLayFilter, CompareMethod.Binary);
            if (LikeOperator.LikeString(((ModelComponent)e.NewState).Name, listenerLayFilter, CompareMethod.Binary))
            {
                flag = true;
            }
        }

        if (flag)
        {
            ((GH_DocumentObject)this).ExpireSolution(true);
        }
    }

    private bool IsRelevantObject(RhinoObject obj)
    {
        //IL_0016: Unknown result type (might be due to invalid IL or missing references)
        //IL_001c: Unknown result type (might be due to invalid IL or missing references)
        //IL_0021: Unknown result type (might be due to invalid IL or missing references)
        //IL_0023: Unknown result type (might be due to invalid IL or missing references)
        if (m_idCache.Contains(((ModelComponent)obj).Id))
        {
            return true;
        }

        if ((ObjectType)(obj.ObjectType & m_typeFilter) != obj.ObjectType)
        {
            return false;
        }

        if (!LikeOperator.LikeString(obj.Attributes.Name, m_nameFilter, CompareMethod.Binary))
        {
            return false;
        }

        RhinoDoc document = obj.Document;
        string source = ((ModelComponent)document.Layers[obj.Attributes.LayerIndex]).Name;
        if (enableFullPath)
        {
            source = document.Layers[obj.Attributes.LayerIndex].FullPath;
        }

        if (document != null)
        {
            return LikeOperator.LikeString(source, m_layerFilter, CompareMethod.Binary);
        }

        return false;
    }

    private bool IsRelevantListenObject(RhinoObject obj)
    {
        //IL_0016: Unknown result type (might be due to invalid IL or missing references)
        //IL_001c: Unknown result type (might be due to invalid IL or missing references)
        //IL_0021: Unknown result type (might be due to invalid IL or missing references)
        //IL_0023: Unknown result type (might be due to invalid IL or missing references)
        if (m_idCache.Contains(((ModelComponent)obj).Id))
        {
            return true;
        }

        if ((ObjectType)(obj.ObjectType & m_listenerTypeFilter) != obj.ObjectType)
        {
            return false;
        }

        bool flag = false;
        foreach (string listenerNameFilter in m_ListenerNameFilters)
        {
            if (LikeOperator.LikeString(obj.Attributes.Name, listenerNameFilter, CompareMethod.Binary))
            {
                flag = true;
            }
        }

        if (!flag)
        {
            return false;
        }

        RhinoDoc document = obj.Document;
        bool flag2 = false;
        foreach (string listenerLayFilter in m_ListenerLayFilters)
        {
            bool flag3 = LikeOperator.LikeString(((ModelComponent)document.Layers[obj.Attributes.LayerIndex]).Name, listenerLayFilter, CompareMethod.Binary);
            if (enableFullPath)
            {
                flag3 = LikeOperator.LikeString(document.Layers[obj.Attributes.LayerIndex].FullPath, listenerLayFilter, CompareMethod.Binary);
            }

            if (flag3)
            {
                flag2 = true;
            }
        }

        return document != null && flag2;
    }

    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
        ((ToolStripItem)GH_DocumentObject.Menu_AppendItem((ToolStrip)(object)menu, "Match Full Path", (EventHandler)menu_explodeAll, true, enableFullPath)).ToolTipText = "When checked, the component looks for a layer filter match against the full layer path, instead of just the layer name.";
        ((ToolStripItem)GH_DocumentObject.Menu_AppendItem((ToolStrip)(object)menu, "Listen To Attribute Changes", (EventHandler)menu_listenToAttributeChanges, true, m_listenToAttributeChanges)).ToolTipText = "When checked, the component listens to changes to object attributes - otherwise it will not refresh.";
    }

    private void menu_explodeAll(object sender, EventArgs e)
    {
        ((GH_DocumentObject)this).RecordUndoEvent("Toggle Full Path");
        enableFullPath = !enableFullPath;
        UpdateMenu();
        ((GH_DocumentObject)this).ExpireSolution(true);
    }

    private void menu_listenToAttributeChanges(object sender, EventArgs e)
    {
        ((GH_DocumentObject)this).RecordUndoEvent("Toggle Attributes Listening");
        m_listenToAttributeChanges = !m_listenToAttributeChanges;
        ((GH_DocumentObject)this).ExpireSolution(true);
    }

    private void UpdateMenu()
    {
        if (enableFullPath)
        {
            ((GH_Component)this).Message = "Layer Full Path";
        }
        else
        {
            ((GH_Component)this).Message = "Layer Name Only";
        }
    }

    public override bool Write(GH_IWriter writer)
    {
        //IL_0029: Unknown result type (might be due to invalid IL or missing references)
        //IL_0033: Expected I4, but got Unknown
        writer.SetString("LayerFilter", LayerFilter);
        writer.SetString("NameFilter", NameFilter);
        writer.SetInt32("TypeFilter", Convert.ToInt32((uint)(int)TypeFilter));
        writer.SetBoolean("IncludeLocked", IncludeLocked);
        writer.SetBoolean("IncludeHidden", IncludeHidden);
        writer.SetBoolean("GroupByLayer", GroupByLayer);
        writer.SetBoolean("GroupByType", GroupByType);
        writer.SetBoolean("enableFullPath", enableFullPath);
        writer.SetBoolean("listenToAttributes", m_listenToAttributeChanges);
        return ((GH_Component)this).Write(writer);
    }

    public override bool Read(GH_IReader reader)
    {
        LayerFilter = "*";
        NameFilter = "*";
        TypeFilter = (ObjectType)61;
        IncludeLocked = true;
        IncludeHidden = true;
        GroupByLayer = false;
        GroupByType = false;
        reader.TryGetString("LayerFilter", ref m_layerFilter);
        reader.TryGetString("NameFilter", ref m_nameFilter);
        reader.TryGetBoolean("IncludeLocked", ref m_includeLocked);
        reader.TryGetBoolean("IncludeHidden", ref m_includeHidden);
        reader.TryGetBoolean("GroupByLayer", ref m_groupLayer);
        reader.TryGetBoolean("GroupByType", ref m_groupType);
        reader.TryGetBoolean("listenToAttributes", ref m_listenToAttributeChanges);
        reader.TryGetBoolean("enableFullPath", ref m_enableFullPath);
        int value = Convert.ToInt32(61u);
        if (reader.TryGetInt32("TypeFilter", ref value))
        {
            uint num = Convert.ToUInt32(value);
            TypeFilter = (ObjectType)num;
        }

        UpdateMenu();
        return ((GH_Component)this).Read(reader);
    }
}
#if false // Decompilation log
'164' items in cache
------------------
Resolve: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\mscorlib.dll'
------------------
Resolve: 'Grasshopper, Version=6.2.18065.11031, Culture=neutral, PublicKeyToken=dda4f5ec2cd80803'
Could not find by name: 'Grasshopper, Version=6.2.18065.11031, Culture=neutral, PublicKeyToken=dda4f5ec2cd80803'
------------------
Resolve: 'RhinoCommon, Version=6.2.18065.11031, Culture=neutral, PublicKeyToken=552281e97c755530'
Could not find by name: 'RhinoCommon, Version=6.2.18065.11031, Culture=neutral, PublicKeyToken=552281e97c755530'
------------------
Resolve: 'System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Drawing.dll'
------------------
Resolve: 'System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Could not find by name: 'System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
------------------
Resolve: 'GH_IO, Version=6.2.18065.11031, Culture=neutral, PublicKeyToken=6a29997d2e6b4f97'
Could not find by name: 'GH_IO, Version=6.2.18065.11031, Culture=neutral, PublicKeyToken=6a29997d2e6b4f97'
------------------
Resolve: 'System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Core.dll'
------------------
Resolve: 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.dll'
------------------
Resolve: 'Microsoft.VisualBasic, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'Microsoft.VisualBasic, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\Microsoft.VisualBasic.dll'
------------------
Resolve: 'Microsoft.Win32.Registry, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'Microsoft.Win32.Registry, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\Microsoft.Win32.Registry.dll'
------------------
Resolve: 'System.Runtime, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Runtime.dll'
------------------
Resolve: 'System.Security.Principal.Windows, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Principal.Windows, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Security.Principal.Windows.dll'
------------------
Resolve: 'System.Security.Permissions, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Could not find by name: 'System.Security.Permissions, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
------------------
Resolve: 'System.Collections, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Collections.dll'
------------------
Resolve: 'System.Collections.NonGeneric, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.NonGeneric, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Collections.NonGeneric.dll'
------------------
Resolve: 'System.Collections.Concurrent, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'System.ObjectModel, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ObjectModel, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.ObjectModel.dll'
------------------
Resolve: 'System.Console, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Console.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'System.Diagnostics.Contracts, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Contracts, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Diagnostics.Contracts.dll'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.Diagnostics.Tracing, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Tracing, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Diagnostics.Tracing.dll'
------------------
Resolve: 'System.IO.FileSystem.DriveInfo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.IO.FileSystem.DriveInfo, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.IO.FileSystem.DriveInfo.dll'
------------------
Resolve: 'System.IO.IsolatedStorage, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.IO.IsolatedStorage, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.IO.IsolatedStorage.dll'
------------------
Resolve: 'System.ComponentModel, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.ComponentModel.dll'
------------------
Resolve: 'System.Threading.Thread, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Threading.Thread.dll'
------------------
Resolve: 'System.Reflection.Emit, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Reflection.Emit, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Reflection.Emit.dll'
------------------
Resolve: 'System.Reflection.Emit.ILGeneration, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Reflection.Emit.ILGeneration, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Reflection.Emit.ILGeneration.dll'
------------------
Resolve: 'System.Reflection.Emit.Lightweight, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Reflection.Emit.Lightweight, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Reflection.Emit.Lightweight.dll'
------------------
Resolve: 'System.Reflection.Primitives, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Reflection.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Reflection.Primitives.dll'
------------------
Resolve: 'System.Resources.Writer, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Resources.Writer, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Resources.Writer.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.VisualC, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.CompilerServices.VisualC, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Runtime.CompilerServices.VisualC.dll'
------------------
Resolve: 'System.Runtime.Serialization.Formatters, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.Serialization.Formatters, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Runtime.Serialization.Formatters.dll'
------------------
Resolve: 'System.Security.AccessControl, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.AccessControl, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Security.AccessControl.dll'
------------------
Resolve: 'System.IO.FileSystem.AccessControl, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.IO.FileSystem.AccessControl, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.IO.FileSystem.AccessControl.dll'
------------------
Resolve: 'System.Threading.AccessControl, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Threading.AccessControl, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.Security.Claims, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Claims, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Security.Claims.dll'
------------------
Resolve: 'System.Security.Cryptography, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Cryptography, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Security.Cryptography.dll'
------------------
Resolve: 'System.Text.Encoding.Extensions, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.Encoding.Extensions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Text.Encoding.Extensions.dll'
------------------
Resolve: 'System.Threading, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Threading.dll'
------------------
Resolve: 'System.Threading.Overlapped, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Overlapped, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Threading.Overlapped.dll'
------------------
Resolve: 'System.Threading.ThreadPool, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.ThreadPool, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Threading.ThreadPool.dll'
------------------
Resolve: 'System.Threading.Tasks.Parallel, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Tasks.Parallel, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Threading.Tasks.Parallel.dll'
------------------
Resolve: 'System.Drawing.Common, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Could not find by name: 'System.Drawing.Common, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
------------------
Resolve: 'System.Drawing.Primitives, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.ComponentModel.TypeConverter, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.TypeConverter, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.ComponentModel.TypeConverter.dll'
------------------
Resolve: 'System.Configuration.ConfigurationManager, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Could not find by name: 'System.Configuration.ConfigurationManager, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
------------------
Resolve: 'System.IO.MemoryMappedFiles, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.IO.MemoryMappedFiles, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.IO.MemoryMappedFiles.dll'
------------------
Resolve: 'System.IO.Pipes, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.IO.Pipes, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.IO.Pipes.dll'
------------------
Resolve: 'System.Diagnostics.EventLog, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Could not find by name: 'System.Diagnostics.EventLog, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
------------------
Resolve: 'System.Diagnostics.PerformanceCounter, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Could not find by name: 'System.Diagnostics.PerformanceCounter, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
------------------
Resolve: 'System.Linq.Expressions, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq.Expressions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Linq.Expressions.dll'
------------------
Resolve: 'System.IO.Pipes.AccessControl, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.IO.Pipes.AccessControl, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.IO.Pipes.AccessControl.dll'
------------------
Resolve: 'System.Linq, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Linq.dll'
------------------
Resolve: 'System.Linq.Queryable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq.Queryable, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Linq.Queryable.dll'
------------------
Resolve: 'System.Linq.Parallel, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq.Parallel, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Linq.Parallel.dll'
------------------
Resolve: 'System.CodeDom, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Could not find by name: 'System.CodeDom, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
------------------
Resolve: 'Microsoft.Win32.SystemEvents, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Could not find by name: 'Microsoft.Win32.SystemEvents, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
------------------
Resolve: 'System.Diagnostics.Process, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Process, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Diagnostics.Process.dll'
------------------
Resolve: 'System.Collections.Specialized, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Specialized, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Collections.Specialized.dll'
------------------
Resolve: 'System.ComponentModel.EventBasedAsync, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.EventBasedAsync, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.ComponentModel.EventBasedAsync.dll'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'Microsoft.Win32.Primitives, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'Microsoft.Win32.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\Microsoft.Win32.Primitives.dll'
------------------
Resolve: 'System.Diagnostics.TraceSource, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.TraceSource, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Diagnostics.TraceSource.dll'
------------------
Resolve: 'System.Diagnostics.TextWriterTraceListener, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.TextWriterTraceListener, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Diagnostics.TextWriterTraceListener.dll'
------------------
Resolve: 'System.Diagnostics.FileVersionInfo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.FileVersionInfo, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Diagnostics.FileVersionInfo.dll'
------------------
Resolve: 'System.IO.Compression, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.IO.Compression, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.IO.Compression.dll'
------------------
Resolve: 'System.IO.FileSystem.Watcher, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.IO.FileSystem.Watcher, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.IO.FileSystem.Watcher.dll'
------------------
Resolve: 'System.IO.Ports, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Could not find by name: 'System.IO.Ports, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
------------------
Resolve: 'System.Windows.Extensions, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Could not find by name: 'System.Windows.Extensions, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
------------------
Resolve: 'System.Net.Requests, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Requests, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Net.Requests.dll'
------------------
Resolve: 'System.Net.Primitives, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Net.Primitives.dll'
------------------
Resolve: 'System.Net.HttpListener, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Found single assembly: 'System.Net.HttpListener, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Net.HttpListener.dll'
------------------
Resolve: 'System.Net.ServicePoint, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Found single assembly: 'System.Net.ServicePoint, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Net.ServicePoint.dll'
------------------
Resolve: 'System.Net.NameResolution, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NameResolution, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Net.NameResolution.dll'
------------------
Resolve: 'System.Net.WebClient, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Found single assembly: 'System.Net.WebClient, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Net.WebClient.dll'
------------------
Resolve: 'System.Net.WebHeaderCollection, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.WebHeaderCollection, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Net.WebHeaderCollection.dll'
------------------
Resolve: 'System.Net.WebProxy, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Found single assembly: 'System.Net.WebProxy, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Net.WebProxy.dll'
------------------
Resolve: 'System.Net.Mail, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Found single assembly: 'System.Net.Mail, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Net.Mail.dll'
------------------
Resolve: 'System.Net.NetworkInformation, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NetworkInformation, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Net.NetworkInformation.dll'
------------------
Resolve: 'System.Net.Ping, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Ping, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Net.Ping.dll'
------------------
Resolve: 'System.Net.Security, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Security, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Net.Security.dll'
------------------
Resolve: 'System.Net.Sockets, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Sockets, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Net.Sockets.dll'
------------------
Resolve: 'System.Net.WebSockets.Client, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.WebSockets.Client, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Net.WebSockets.Client.dll'
------------------
Resolve: 'System.Net.WebSockets, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.WebSockets, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Net.WebSockets.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '8.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'Microsoft.VisualBasic.Core, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'Microsoft.VisualBasic.Core, Version=13.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
WARN: Version mismatch. Expected: '0.0.0.0', Got: '13.0.0.0'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\Microsoft.VisualBasic.Core.dll'
------------------
Resolve: 'System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Runtime.dll'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'System.ObjectModel, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ObjectModel, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.ObjectModel.dll'
------------------
Resolve: 'System.Net.WebHeaderCollection, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.WebHeaderCollection, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.5\ref\net8.0\System.Net.WebHeaderCollection.dll'
#endif