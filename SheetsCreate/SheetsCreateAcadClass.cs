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
using static BaseFunction.F;
using BaseFunction;
using System.Security.Cryptography;


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
                    GetViewports(tr, btr, bt);

                    btr?.Dispose();
                }        
                tr.Commit();
            }


               
        }
        /// <summary>
        /// получаем видовые экраны и добавляем их в блок
        /// </summary>
        private void GetViewports(Transaction tr, BlockTableRecord btr, BlockTable bt)
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
                    int num = 1;
                    List<string> names = new List<string>();
                    bool first = true;
                    foreach (ObjectId id in lay.GetViewports())
                    {
                        if (first)
                        { 
                            first = false;
                            continue;
                        }
                        if (id.IsErased || !id.ObjectClass.Equals(RXClass.GetClass(typeof(Viewport)))) continue;
                        using (Viewport viewport = tr.GetObject(id, OpenMode.ForRead, false, true) as Viewport)
                        {
                            if (viewport == null|| viewport.Visible == false || !viewport.On || viewport.Layer != IniData.Layer) continue;
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
                                c.TransformBy(Matrix3d.Scaling(1 / viewport.CustomScale, viewport.CenterPoint));
                                Vector3d s1 = Point3d.Origin - viewport.ViewTarget;
                                Vector3d s2 = viewport.ViewCenter.GetPoint3d(0) - viewport.CenterPoint;
                                s2 -= s1;
                                c.TransformBy(Matrix3d.Displacement(s2));
                                ObjectId cId = btr.AppendEntity(c);
                                tr.AddNewlyCreatedDBObject(c, true);
                                XDataSet(cId, "SheetsOnLayouts", new List<TypedValue> 
                                { 
                                    new TypedValue(Convert.ToInt32(DxfCode.ExtendedDataHandle), viewport.Handle), 
                                }, true);
                                string name = lay.LayoutName;
                                string nname = name;
                                while (names.Contains(nname))
                                {
                                    nname = name + "_" + num++;                                  
                                }
                                names.Add(nname);
                                MText text;
                                using (MText mText = new MText())
                                {
                                    mText.Location = viewport.CenterPoint.TransformBy(Matrix3d.Displacement(s2));
                                    mText.Contents = nname;
                                    mText.TextHeight = 20;
                                    mText.Attachment = AttachmentPoint.MiddleCenter;
                                    ObjectId tId = btr.AppendEntity(mText);
                                    tr.AddNewlyCreatedDBObject(mText, true);
                                    XDataSet(tId, "SheetsOnLayouts", new List<TypedValue>
                                    {
                                        new TypedValue(Convert.ToInt32(DxfCode.ExtendedDataHandle), viewport.Handle),
                                    }, true);
                                    text = mText.Clone() as MText;        
                                }
                                CreateSingleReference(bt, tr, text, lay, viewport, btr);
                                text?.Dispose();
                            }
                            c?.Dispose();
                        }
                    }
                }
            }

        }

        private void CreateSingleReference(BlockTable bt, Transaction tr, MText mText, Layout lay, Viewport viewport, BlockTableRecord allViewportsBtr)
        {
            string blockName = IniData.Layer + "_" + mText.Text;
           
            BlockTableRecord btr;   
            if (bt.Has(blockName)) btr = tr.GetObject(bt[blockName], OpenMode.ForWrite) as BlockTableRecord;
            else
            {
                btr = new BlockTableRecord();
                btr.Name = blockName;
                bt.Add(btr);
                tr.AddNewlyCreatedDBObject(btr, true);
            }

            foreach (ObjectId id in btr) using (Entity e2 = tr.GetObject(id, OpenMode.ForWrite) as Entity) e2?.Erase();

            using (BlockReference bref = new BlockReference(Point3d.Origin, allViewportsBtr.Id))
            {
                btr.AppendEntity(bref);
                tr.AddNewlyCreatedDBObject(bref, true);                
            }

            Point3d origin = Point3d.Origin;
            Curve curve = null;
            Polyline polyline = null;

            foreach (ObjectId eId in allViewportsBtr)
            {
                ResultBuffer typedValues = XDataGet(eId, "SheetsOnLayouts");
                if (typedValues == null) continue;
                foreach (TypedValue tv in typedValues)
                {
                    if (tv.TypeCode == Convert.ToInt32(DxfCode.ExtendedDataHandle))
                    {
                        Handle h = new Handle(Convert.ToInt64(tv.Value.ToString(), 16));
                        if (!viewport.Handle.Equals(h)) break;
                        using (Entity e = tr.GetObject(eId, OpenMode.ForRead).Clone() as Entity)
                        {
                            if (e is MText m)
                            {
                                origin = m.Location;
                                polyline = CreatePolyline(mText);
                            }
                            else if (e is Curve c)
                            {
                                curve = c.Clone() as Curve;
                            }
                        }
                    }
                }
            }

            btr.Origin = origin;

            if (curve != null && polyline != null)
            {
                ObjectId curveId = btr.AppendEntity(curve);
                tr.AddNewlyCreatedDBObject(curve, true);
                ObjectId polyId = btr.AppendEntity(polyline);
                tr.AddNewlyCreatedDBObject(polyline, true);
                using (Hatch h = new Hatch())
                {
                    h.ColorIndex = 1;
                    h.PatternScale = 1;
                    h.SetHatchPattern(HatchPatternType.PreDefined, "ANSI32");
                    btr.AppendEntity(h);
                    tr.AddNewlyCreatedDBObject(h, true);
                    using (ObjectIdCollection coll = new ObjectIdCollection { curveId })
                    {
                        h.AppendLoop(HatchLoopTypes.External, coll);
                    }
                    using (ObjectIdCollection coll = new ObjectIdCollection { polyId })
                    {
                        h.AppendLoop(HatchLoopTypes.Outermost, coll);
                    }
                } 
            }

            ObjectId bId = ObjectId.Null;
            using (BlockTableRecord ltr = tr.GetObject(lay.BlockTableRecordId, OpenMode.ForWrite) as BlockTableRecord)
            {
                foreach (ObjectId id in ltr)
                {
                    if (id.ObjectClass.Equals(RXClass.GetClass(typeof(BlockReference))))
                    {
                        ResultBuffer typedValues = XDataGet(id, "SheetsOnLayouts");
                        if (typedValues == null) continue;
                        foreach (TypedValue tv in typedValues)
                        {
                            if (tv.TypeCode == Convert.ToInt32(DxfCode.ExtendedDataHandle))
                            {
                                if (!viewport.Handle.Equals(new Handle(Convert.ToInt64(tv.Value.ToString(), 16)))) break;
                                bId = id;
                            }
                        }
                    }
                    if (bId != ObjectId.Null) break;
                }
                if (bId == ObjectId.Null)
                {
                    using (BlockReference blockReference = new BlockReference(viewport.CenterPoint, btr.Id))
                    {
                        ObjectId nbrId = ltr.AppendEntity(blockReference);
                        tr.AddNewlyCreatedDBObject(blockReference, true);
                        XDataSet(nbrId, "SheetsOnLayouts", new List<TypedValue>
                                    {
                                        new TypedValue(Convert.ToInt32(DxfCode.ExtendedDataHandle), viewport.Handle),
                                    }, true);
                    }

                }
            }

          
            btr.Dispose();
        }
        
        private IniData IniData { get; set; }
    }
}
