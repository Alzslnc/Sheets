using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using BaseFunction;
using System;
using System.Collections.Generic;
using static BaseFunction.BaseGeometryClass;
using static BaseFunction.F;
using static BaseFunction.TextBounds;
using System.Windows.Forms;
using static BaseFunction.PositionAndIntersections;
using static BaseFunction.BaseBlockReferenceClass;

namespace Sheets
{
    public class SheetsCreateAcadClass
    {    
        /// <summary>
        /// создаем блок с видовыми экранами
        /// </summary>  
        public void CreateBlock(IniData iniData)
        {
            //получаем данные от формы
            IniData = iniData;

            double textheight = Settings.Default.TextHeight;
            double hatchScale = Settings.Default.Scale;
            Scale3d blockScale = new Scale3d(Settings.Default.BlockScale);

            if (string.IsNullOrEmpty(IniData.Layer)) return;

            if (IniData.PrefixType == PrefixType.Manual || (IniData.PrefixType == PrefixType.Attribute && Settings.Default.UsePrefix))
            {
                using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
                {
                    try
                    {
                        using (BlockTableRecord ms = tr.GetObject(HostApplicationServices.WorkingDatabase.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord)
                        {
                            using (MText m = new MText())
                            {
                                m.Contents = Settings.Default.Prefix;
                                ms.AppendEntity(m);
                                tr.AddNewlyCreatedDBObject(m, true);
                            }
                        }
                    }
                    catch 
                    {
                        MessageBox.Show("В префиксе присутствуют некорректные символы, префикс заменен на \"Лист \"");
                        Settings.Default.Prefix = "Лист ";
                        Settings.Default.Save();
                    }
                    tr.Abort();
                }
            }

            //получаем данные по видовым экранам в выбранном слое
            ViewportLayer vl = IniData.ViewportLayersClass.GetViewportLayer(iniData.Layer);
            if (vl == null)
            {
                MessageBox.Show("Очень редкая ошибка, по идее ее не должно быть");
                return;
            }             

            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                //получаем таблицу блоков
                using (BlockTable bt = tr.GetObject(HostApplicationServices.WorkingDatabase.BlockTableId, OpenMode.ForWrite) as BlockTable)
                {
                    //создаем имя основного блока
                    string btrName = "_sheets_" + vl.Name;

                    using (BlockTableRecord btr = GetBlock(btrName, tr, bt))
                    {
                        //проходим по всем видовым экранам
                        foreach (ObjectId id in vl.Ids)
                        {
                            using (Viewport viewport = tr.GetObject(id, OpenMode.ForRead, false, true) as Viewport)
                            {
                                //создаем переменную для контура видового экрана и получаем кривую контура
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
                                    //получаем контур трансформированный в модель
                                    c.TransformBy(Matrix3d.Scaling(1 / viewport.CustomScale, viewport.CenterPoint));
                                    Vector3d s1 = Point3d.Origin - viewport.ViewTarget;
                                    Vector3d s2 = viewport.ViewCenter.GetPoint3d(0) - viewport.CenterPoint;
                                    s2 -= s1;
                                    c.TransformBy(Matrix3d.Displacement(s2));
                                    //добавляем контур в основной блок
                                    ObjectId cId = btr.AppendEntity(c);
                                    tr.AddNewlyCreatedDBObject(c, true); 

                                    //получаем название видового экрана
                                    string name = GetName(viewport, tr, bt);
                                    //создаем переменную для ограничивающего штриховку круга
                                    double radius = 1;
                                    //создаем текст с названиеи и добавляем в бло
                                    using (MText mText = new MText())
                                    {
                                        mText.Location = viewport.CenterPoint.TransformBy(Matrix3d.Displacement(s2));
                                        mText.Contents = name;
                                        mText.TextHeight = textheight;
                                        mText.Attachment = AttachmentPoint.MiddleCenter;
                                        ObjectId tId = btr.AppendEntity(mText);
                                        tr.AddNewlyCreatedDBObject(mText, true);
                                        
                                        //получаем диаметр круга
                                        using (Polyline polyline = CreatePolyline(mText))
                                        {
                                            radius = mText.Location.Z0().DistanceTo(polyline.GetPoint3dAt(0).Z0()) * 1.1;
                                        }

                                        //создаем название блока 
                                        string referenceName = btrName + "_" + name;

                                        //создаем блок для видового экрана
                                        using (BlockTableRecord referenceBtr = GetBlock(referenceName, tr, bt))
                                        {
                                            using (BlockReference bref = new BlockReference(Point3d.Origin, btr.Id))
                                            {
                                                referenceBtr.AppendEntity(bref);
                                                tr.AddNewlyCreatedDBObject(bref, true);
                                            }

                                            //получаем точку вставки блока, контур штриховки и границу текста                                         
                                            Curve curve = c.Clone() as Curve;
                                            Circle circle = new Circle(mText.Location, Vector3d.ZAxis, radius);

                                            referenceBtr.Origin = mText.Location;

                                            ObjectId curveId = referenceBtr.AppendEntity(curve);
                                            tr.AddNewlyCreatedDBObject(curve, true);
                                            ObjectId circleId = referenceBtr.AppendEntity(circle);
                                            tr.AddNewlyCreatedDBObject(circle, true);
                                            using (Hatch h = new Hatch())
                                            {
                                                h.ColorIndex = 1;
                                                h.PatternScale = hatchScale;
                                                h.SetHatchPattern(HatchPatternType.PreDefined, "ANSI32");
                                                referenceBtr.AppendEntity(h);
                                                tr.AddNewlyCreatedDBObject(h, true);
                                                using (ObjectIdCollection coll = new ObjectIdCollection { curveId })
                                                {
                                                    h.AppendLoop(HatchLoopTypes.External, coll);
                                                }
                                                using (ObjectIdCollection coll = new ObjectIdCollection { circleId })
                                                {
                                                    h.AppendLoop(HatchLoopTypes.Outermost, coll);
                                                }
                                            }

                                            ObjectId bId = ObjectId.Null;

                                            using (BlockTableRecord ltr = tr.GetObject(viewport.OwnerId, OpenMode.ForWrite) as BlockTableRecord)
                                            {
                                                foreach (ObjectId rid in ltr)
                                                {
                                                    if (rid.ObjectClass.Equals(RXClass.GetClass(typeof(BlockReference))))
                                                    {
                                                        ResultBuffer typedValues = XDataGet(rid, "SheetsOnLayouts");
                                                        if (typedValues == null) continue;
                                                        foreach (TypedValue tv in typedValues)
                                                        {
                                                            if (tv.TypeCode == Convert.ToInt32(DxfCode.ExtendedDataHandle))
                                                            {
                                                                if (!viewport.Handle.Equals(new Handle(Convert.ToInt64(tv.Value.ToString(), 16)))) break;
                                                                bId = rid;
                                                            }
                                                        }
                                                    }
                                                    if (bId != ObjectId.Null)
                                                    {
                                                        if (Settings.Default.ScaleExist)
                                                        {
                                                            using (BlockReference br = tr.GetObject(bId, OpenMode.ForWrite, false, true) as BlockReference)
                                                            {
                                                                br.ScaleFactors = blockScale;
                                                            }                                                        
                                                        }
                                                        break;
                                                    }                                                       
                                                    
                                                }
                                                if (bId == ObjectId.Null)
                                                {
                                                    using (BlockReference blockReference = new BlockReference(viewport.CenterPoint, referenceBtr.Id))
                                                    {
                                                        ObjectId nbrId = ltr.AppendEntity(blockReference);
                                                        tr.AddNewlyCreatedDBObject(blockReference, true);
                                                        XDataSet(nbrId, "SheetsOnLayouts", new List<TypedValue>
                                                        {
                                                            new TypedValue(Convert.ToInt32(DxfCode.ExtendedDataHandle), viewport.Handle),
                                                        }, true);
                                                        blockReference.ScaleFactors = blockScale;
                                                    }

                                                }
                                            }
                                        }
                                    }
                                }
                                c?.Dispose();
                            }
                        }
                    }  
                }        
                tr.Commit();
            }
        }

        private BlockTableRecord GetBlock(string name, Transaction tr, BlockTable bt)
        {
            BlockTableRecord btr;
            if (bt.Has(name))
            {
                btr = tr.GetObject(bt[name], OpenMode.ForWrite) as BlockTableRecord;
                foreach (ObjectId id in btr) using (Entity e2 = tr.GetObject(id, OpenMode.ForWrite) as Entity) e2?.Erase();
            }                
            else
            {
                btr = new BlockTableRecord();
                btr.Name = name;
                bt.Add(btr);
                tr.AddNewlyCreatedDBObject(btr, true);
            }
            return btr;   
        }


        #region GetName
        private string GetName(Viewport viewport, Transaction tr, BlockTable bt)
        {
            string prefix = "";
            bool first = false;
            switch (IniData.PrefixType)
            {
                case PrefixType.Layer:
                    {
                        prefix = IniData.Layer;
                        break;
                    }
                case PrefixType.List:
                    {
                        using (BlockTableRecord btr = tr.GetObject(viewport.OwnerId, OpenMode.ForRead) as BlockTableRecord)
                        {
                            if (btr.IsLayout)
                            {
                                using (Layout lo = tr.GetObject(btr.LayoutId, OpenMode.ForRead) as Layout)
                                {
                                    prefix = lo.LayoutName;
                                }
                            }
                        }                          
                        break;
                    }
                case PrefixType.Manual:
                    {
                        prefix = Settings.Default.Prefix;
                        first = true;
                        break;
                    }
                case PrefixType.Attribute:
                    {                      
                        if (Settings.Default.UsePrefix) prefix = Settings.Default.Prefix;

                        if (!bt.Has(IniData.Block)) break;

                        using (BlockTableRecord btr = tr.GetObject(viewport.OwnerId, OpenMode.ForRead) as BlockTableRecord)
                        {
                            foreach (ObjectId id in btr)
                            {
                                if (!id.ObjectClass.Equals(RXClass.GetClass(typeof(BlockReference)))) continue;

                                BrefData brefData = null;
                                foreach (BrefData bref in BrefDatas)
                                {
                                    if (bref.OwnerId == viewport.OwnerId && bref.Id == id)
                                    { 
                                        brefData = bref;
                                        break;
                                    }
                                }

                                if (brefData == null)
                                {
                                    using (BlockReference bref = tr.GetObject(id, OpenMode.ForRead, false, true) as BlockReference)
                                    {
                                        if (!bref.BlockTableRecord.Equals(bt[IniData.Block])) continue;
                                        using (BlockTableRecord btr2 = tr.GetObject(bref.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord)
                                        {
                                           

                                            Extents3d ex = new Extents3d();

                                            using (DBObjectCollection collection = new DBObjectCollection())
                                            {
                                                bref.Explode(collection);
                                                foreach (DBObject dBObject in collection)
                                                {
                                                    if (dBObject is Curve curve && curve.Bounds.HasValue) ex.AddExtents(curve.Bounds.Value);
                                                    dBObject?.Dispose();
                                                }
                                            }

                                            using (Polyline poly = CreatePolyline(ex))
                                            {
                                                if (BlockReferenceGetAttribute(bref, IniData.AttributeTag, out string attResult))
                                                {
                                                    brefData = new BrefData(id, poly, attResult, bref.OwnerId);
                                                    BrefDatas.Add(brefData);
                                                }
                                            }
                                        }
                                    }
                                }

                                if (brefData == null) continue;

                                PositionType position = viewport.CenterPoint.GetPositionType(brefData.Bound);
                                if (position != PositionType.inner) continue;
                                prefix += brefData.AttResult;
                                break;
                            }
                        }
                        break;
                    }
            }

            int i = 1;
            string name = prefix;            

            if (string.IsNullOrEmpty(prefix)) name = i.ToString();
            else if (first) name += i.ToString();

            while (Names.Contains(name))
            { 
                name = prefix + i++.ToString();
            }
            
            Names.Add(name);
            return name.Trim();            
        }
        private List<string> Names { get; set; } = new List<string>();
        private List<BrefData> BrefDatas { get; set; } = new List<BrefData>();
        private class BrefData
        {
            public BrefData(ObjectId id, Polyline bound, string attResult, ObjectId ownerId) 
            { 
                Bound = bound;
                Id = id;
                AttResult = attResult;
                OwnerId = ownerId;
            }
            public Polyline Bound { get; private set; }
            public ObjectId Id { get; private set; }
            public ObjectId OwnerId { get; private set; }
            public string AttResult { get; private set; }
        }
        #endregion
        private IniData IniData { get; set; }
    }
}
