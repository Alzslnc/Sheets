using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BaseFunction.TextBounds;
using static BaseFunction.BaseGeometryClass;

namespace Sheets
{
    public class SheetsCreateAcadClass
    {    
        /// <summary>
        /// создаем блок с видовыми экранами
        /// </summary>  
        public void CreateBlock(IniData iniData)
        {
            IniData = iniData;
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                using (BlockTable bt = tr.GetObject(HostApplicationServices.WorkingDatabase.BlockTableId, OpenMode.ForWrite) as BlockTable)
                {
                    BlockTableRecord btr;
                    ObjectId btrId = ObjectId.Null;
                    if (bt.Has("_sheets_" + IniData.Layer)) btr = tr.GetObject(bt["_sheets_" + IniData.Layer], OpenMode.ForWrite) as BlockTableRecord;
                    else
                    { 
                        btr = new BlockTableRecord();
                        btr.Name = "_sheets_" + IniData.Layer;
                        bt.Add(btr);
                        tr.AddNewlyCreatedDBObject(btr, true);
                    }
                    GetViewports(tr, btr);

                    btr?.Dispose();
                }        
                tr.Commit();
            }


               
        }
        /// <summary>
        /// получаем видовые экраны и добавляем их в блок
        /// </summary>
        private void GetViewports(Transaction tr, BlockTableRecord btr)
        {
            foreach (ObjectId id in btr)
            {
                using (Entity e = tr.GetObject(id, OpenMode.ForWrite, false, true) as Entity)
                {
                    e?.Erase();                
                }            
            }
            foreach (DBDictionaryEntry lId in tr.GetObject(HostApplicationServices.WorkingDatabase.LayoutDictionaryId, OpenMode.ForRead, false, true) as DBDictionary)
            {             
                using (Layout lay = tr.GetObject(lId.Value, OpenMode.ForRead, false, true) as Layout)
                {
                    if (lay.LayoutName == "Model") continue;
                    foreach (ObjectId id in lay.GetViewports())
                    {
                        if (id.IsErased || !id.ObjectClass.Equals(RXClass.GetClass(typeof(Viewport)))) continue;
                        using (Viewport viewport = tr.GetObject(id, OpenMode.ForRead, false, true) as Viewport)
                        {
                            if (viewport == null || !viewport.On || viewport.Layer != IniData.Layer) continue;
                            Curve c;
                            if (viewport.NonRectClipEntityId != null && viewport.NonRectClipEntityId != ObjectId.Null)
                            {
                                c = tr.GetObject(viewport.NonRectClipEntityId, OpenMode.ForRead).Clone() as Curve;
                            }
                            else
                            {
                                Extents3d ex = new Extents3d();
                                ex.AddPoint(viewport.CenterPoint - Vector3d.XAxis * 0.5 * viewport.Width - Vector3d.YAxis * 0.5 * viewport.Height);
                                ex.AddPoint(viewport.CenterPoint + Vector3d.XAxis * 0.5 * viewport.Width + Vector3d.YAxis * 0.5 * viewport.Height);
                                c = CreatePolyline(ex);
                            }
                            if (c != null)
                            {
                                c.TransformBy(Matrix3d.Scaling(viewport.ViewHeight / viewport.Height, viewport.CenterPoint));
                                Vector3d s1 = Point3d.Origin - viewport.ViewTarget;
                                Vector3d s2 = viewport.ViewCenter.GetPoint3d(0) - viewport.CenterPoint;
                                s2 -= s1;
                                c.TransformBy(Matrix3d.Displacement(s2));
                                btr.AppendEntity(c);
                                tr.AddNewlyCreatedDBObject(c, true);
                            }

                        }
                    }
                }
            }

        }
        
        private IniData IniData { get; set; }
    }
}
