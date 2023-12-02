using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Sheets
{
    public class ViewportLayersClass
    {
        public ViewportLayersClass() 
        { 
            GetViewportLayers();
        }
        private void GetViewportLayers()
        {
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                using (BlockTable bt = tr.GetObject(HostApplicationServices.WorkingDatabase.BlockTableId, OpenMode.ForRead) as BlockTable)
                {
                    foreach (ObjectId btrId in bt)
                    {
                        using (BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord)
                        {
                            if (!btr.IsLayout) continue;
                            using (Layout lay = tr.GetObject(btr.LayoutId, OpenMode.ForRead) as Layout)
                            {
                                if (lay.LayoutName == "Model") continue;
                            }

                            bool first = true;
                            foreach (ObjectId id in btr)
                            {
                                if (!id.ObjectClass.Equals(RXClass.GetClass(typeof(Viewport)))) continue;
                                if (first)
                                {
                                    first = false;
                                    continue;
                                }
                                using (Viewport v = tr.GetObject(id, OpenMode.ForRead, false, true) as Viewport)
                                {
                                    ViewportLayer viewportLayer = GetViewportLayer(v.Layer);
                                    if (viewportLayer == null)
                                    {
                                        viewportLayer = new ViewportLayer(v.Layer);
                                        ViewportLayers.Add(viewportLayer);
                                    }
                                    viewportLayer.Ids.Add(id);
                                }
                            }
                        }
                    }                
                }  
                tr.Commit();
            }
        }
        public ViewportLayer GetViewportLayer(string name)
        { 
            foreach(ViewportLayer layer in ViewportLayers)
            {
                if (layer.Name.Equals(name)) return layer;
            }        
            return null;
        }
        public List<ViewportLayer> ViewportLayers { get; private set; } = new List<ViewportLayer>();        
    }

    public class ViewportLayer
    {
        public ViewportLayer(string name)
        { 
            Name = name;
        }
        public string Name { get; set; } = "";
        public List<ObjectId> Ids { get; set; } = new List<ObjectId>();
    }
}
