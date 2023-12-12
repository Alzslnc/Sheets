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
using static BaseFunction.BaseLayerClass;
using Autodesk.AutoCAD.Colors;

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

            //создаем слои и параметры
            LayerNew("!_sheets_bound");
            LayerNew("!_sheets_number");
            LayerNew("!_sheets_hutch");
            LayerChangeParametrs("!_sheets_hutch", 1);

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

                    using (BlockTableRecord btr = GetClearBlock(btrName, tr, bt))
                    {
                        //граница всех объектов
                        Extents3d fullex = new Extents3d();

                        //список Id всех блоков
                        List<ObjectId> btrIds = new List<ObjectId>();

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
                                            
                                    //центр видового экрана
                                    Point3d center = viewport.CenterPoint;

                                    Vector3d s1 = viewport.ViewCenter.GetPoint3d(0) - viewport.CenterPoint;
                                    c.TransformBy(Matrix3d.Displacement(s1));
                                    center = center.TransformBy(Matrix3d.Displacement(s1));

                                    if (viewport.TwistAngle != 0)
                                    {
                                        c.TransformBy(Matrix3d.Rotation(-viewport.TwistAngle, Vector3d.ZAxis, Point3d.Origin));
                                        center = center.TransformBy(Matrix3d.Rotation(-viewport.TwistAngle, Vector3d.ZAxis, Point3d.Origin));
                                    }

                                    Vector3d s2 = Point3d.Origin - viewport.ViewTarget;
                                    c.TransformBy(Matrix3d.Displacement(s2));
                                    center = center.TransformBy(Matrix3d.Displacement(s2));
                                         
                                    //добавляем в границу

                                    if (c.Bounds.HasValue) fullex.AddExtents(c.Bounds.Value);

                                    //добавляем контур в основной блок
                                    c.Layer = "!_sheets_bound";
                                    c.ColorIndex = 256;
                                    c.LinetypeId = HostApplicationServices.WorkingDatabase.ByLayerLinetype;
                                    c.LineWeight = LineWeight.ByLayer;                                    

                                    ObjectId cId = btr.AppendEntity(c);
                                    tr.AddNewlyCreatedDBObject(c, true); 

                                    //получаем название видового экрана
                                    string name = GetName(viewport, tr);
                                    //создаем переменную для ограничивающего штриховку круга
                                    double radius = 1;
                                    //создаем текст с названиеи и добавляем в бло
                                    using (MText mText = new MText())
                                    {
                                        mText.Location = center;
                                        mText.Contents = name;
                                        mText.TextHeight = textheight;
                                        mText.Attachment = AttachmentPoint.MiddleCenter;

                                        mText.Layer = "!_sheets_number";
                                        mText.ColorIndex = 256;
                                        mText.LinetypeId = HostApplicationServices.WorkingDatabase.ByLayerLinetype;
                                        mText.LineWeight = LineWeight.ByLayer;

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
                                        using (BlockTableRecord referenceBtr = GetClearBlock(referenceName, tr, bt))
                                        {
                                            if (!btrIds.Contains(referenceBtr.Id)) btrIds.Add(referenceBtr.Id);

                                            using (BlockReference bref = new BlockReference(Point3d.Origin, btr.Id))
                                            {
                                                referenceBtr.AppendEntity(bref);
                                                tr.AddNewlyCreatedDBObject(bref, true);
                                            }

                                            //получаем точку вставки блока, контур штриховки и границу текста                                         
                                            Curve curve = c.Clone() as Curve;
                                            curve.Layer = "!_sheets_hutch";
                                            curve.ColorIndex = 256;
                                            curve.LinetypeId = HostApplicationServices.WorkingDatabase.ByLayerLinetype;
                                            curve.LineWeight = LineWeight.ByLayer;

                                            //копия текста
                                            MText mtClone = mText.Clone() as MText;
                                            if (Settings.Default.SelfNumberColor) mtClone.Layer = "!_sheets_hutch";                                       

                                            Circle circle = new Circle(mText.Location, Vector3d.ZAxis, radius);
                                            circle.Layer = "!_sheets_hutch";
                                            circle.ColorIndex = 256;
                                            circle.LinetypeId = HostApplicationServices.WorkingDatabase.ByLayerLinetype;
                                            circle.LineWeight = LineWeight.ByLayer;
                                            //referenceBtr.Origin = mText.Location;

                                            referenceBtr.AppendEntity(mtClone);
                                            tr.AddNewlyCreatedDBObject(mtClone, true);
                                            ObjectId curveId = referenceBtr.AppendEntity(curve);
                                            tr.AddNewlyCreatedDBObject(curve, true);
                                            ObjectId circleId = referenceBtr.AppendEntity(circle);
                                            tr.AddNewlyCreatedDBObject(circle, true);
                                            using (Hatch h = new Hatch())
                                            {                                               
                                                h.PatternScale = hatchScale;
                                                h.SetHatchPattern(HatchPatternType.PreDefined, "ANSI32");
                                                h.Layer = "!_sheets_hutch";
                                                h.ColorIndex = 256;                                                
                                                h.LineWeight = LineWeight.ByLayer;

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

                                            curve?.Dispose();
                                            mtClone?.Dispose();
                                            circle?.Dispose();

                                           

                                            using (BlockTableRecord ltr = tr.GetObject(viewport.OwnerId, OpenMode.ForWrite) as BlockTableRecord)
                                            {
                                                Point3d location = viewport.CenterPoint;
                                                Scale3d scale = blockScale;
                                                Color color = null;
                                                string layer = null;
                                                ObjectId linetypeId = ObjectId.Null;
                                                LineWeight lineWeight = LineWeight.ByLineWeightDefault;
                                                bool exist = false;
                                                List<ObjectId> created = new List<ObjectId>();
                                                foreach (ObjectId rid in ltr)
                                                {
                                                    ObjectId bId = ObjectId.Null;
                                                    if (rid.IsErased || created.Contains(rid)) continue;
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
                                                        exist = true;
                                                        location = viewport.CenterPoint;
                                                        scale = blockScale;
                                                        color = null;
                                                        layer = null;
                                                        linetypeId = ObjectId.Null;
                                                        lineWeight = LineWeight.ByLineWeightDefault;
                                                        using (BlockReference br = tr.GetObject(bId, OpenMode.ForWrite, false, true) as BlockReference)
                                                        {
                                                            if (!Settings.Default.ScaleExist)
                                                            {
                                                                scale = br.ScaleFactors;
                                                            }
                                                            location = br.Position;
                                                            color = br.Color;
                                                            layer = br.Layer;
                                                            linetypeId = br.LinetypeId;
                                                            lineWeight = br.LineWeight;                                                           
                                                            br?.Erase();
                                                        }
                                                        using (BlockReference blockReference = new BlockReference(location, referenceBtr.Id))
                                                        {
                                                            ObjectId nbrId = ltr.AppendEntity(blockReference);
                                                            created.Add(nbrId);
                                                            tr.AddNewlyCreatedDBObject(blockReference, true);
                                                            XDataSet(nbrId, "SheetsOnLayouts", new List<TypedValue>
                                                            {
                                                                new TypedValue(Convert.ToInt32(DxfCode.ExtendedDataHandle), viewport.Handle),
                                                            }, true);
                                                            blockReference.ScaleFactors = scale;
                                                            if (layer != null) blockReference.Layer = layer;
                                                            if (color != null) blockReference.Color = color;
                                                            if (lineWeight != LineWeight.ByLineWeightDefault) blockReference.LineWeight = lineWeight;
                                                            if (linetypeId != ObjectId.Null) blockReference.LinetypeId = linetypeId;
                                                            
                                                        }
                                                    } 
                                                }
                                                if (!exist)
                                                {
                                                    using (BlockReference blockReference = new BlockReference(location, referenceBtr.Id))
                                                    {
                                                        ObjectId nbrId = ltr.AppendEntity(blockReference);
                                                        created.Add(nbrId);
                                                        tr.AddNewlyCreatedDBObject(blockReference, true);
                                                        XDataSet(nbrId, "SheetsOnLayouts", new List<TypedValue>
                                                            {
                                                                new TypedValue(Convert.ToInt32(DxfCode.ExtendedDataHandle), viewport.Handle),
                                                            }, true);
                                                        blockReference.ScaleFactors = scale;                                                 
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                c?.Dispose();
                            }
                        }

                        //получаем центр всех объектов
                        Point3d vcenter = fullex.MinPoint + (fullex.MaxPoint - fullex.MinPoint) * 0.5;
                                               
                        foreach (ObjectId id in btrIds)
                        {
                            using (BlockTableRecord referenceBtr = tr.GetObject(id, OpenMode.ForWrite) as BlockTableRecord)
                            {
                                if (!referenceBtr.Origin.IsEqualTo(Point3d.Origin))
                                { 
                                    vcenter = referenceBtr.Origin;
                                    break;
                                }                                  
                            }
                        }
                        foreach (ObjectId id in btrIds)
                        {
                            using (BlockTableRecord referenceBtr = tr.GetObject(id, OpenMode.ForWrite) as BlockTableRecord)
                            {                                
                                referenceBtr.Origin = vcenter;                            
                            }                        
                        }
                    }  
                }        
                tr.Commit();
            }
        }
        /// <summary>
        /// пробует получить экземпляр BlockTableRecord и очищает его, если не получен создает новый
        /// </summary>
        private BlockTableRecord GetClearBlock(string name, Transaction tr, BlockTable bt)
        {
            BlockTableRecord btr;
            if (bt.Has(name))
            {
                btr = tr.GetObject(bt[name], OpenMode.ForWrite) as BlockTableRecord;
                foreach (ObjectId id in btr) using (Entity e2 = tr.GetObject(id, OpenMode.ForWrite) as Entity) e2?.Erase();
            }                
            else
            {
                btr = new BlockTableRecord
                {
                    Name = name
                };
                bt.Add(btr);
                tr.AddNewlyCreatedDBObject(btr, true);
            }
            return btr;   
        }

        #region GetName
        /// <summary>
        /// формирует имя листа
        /// </summary>
        private string GetName(Viewport viewport, Transaction tr)
        {
            //префикс
            string prefix = "";
            //примемять ли нумерацию к первому значению
            bool first = false;
            //получение префикса в зависимости от выбранного типа
            switch (IniData.PrefixType)
            {                
                case PrefixType.Layer:
                    {
                        //записываем в префикс выбранный слой
                        prefix = IniData.Layer;
                        break;
                    }
                case PrefixType.List:
                    {
                        //ищем название листа на котором находится видовой экран
                        //и записываем в префикс его название
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
                        //записываем в префикс введенный вручную префикс
                        prefix = Settings.Default.Prefix;
                        first = true;
                        break;
                    }
                case PrefixType.Attribute:
                    {      
                        //тут префикс составной, если используем ручной записываем его
                        if (Settings.Default.UsePrefix) prefix = Settings.Default.Prefix;                       

                        //ищем значение выбранного атрибута и прибвялем к префиксу
                        using (BlockTableRecord btr = tr.GetObject(viewport.OwnerId, OpenMode.ForWrite) as BlockTableRecord)
                        {
                            //проходим по листу
                            foreach (ObjectId id in btr)
                            {
                                //ищем блок, с выбранным названием
                                if (!id.ObjectClass.Equals(RXClass.GetClass(typeof(BlockReference)))) continue;

                                //создаем переменную с данными блока
                                BrefData brefData = null;
                                foreach (BrefData bref in BrefDatas)
                                {
                                    //если блок с таким Id уже проверялся загружаем в переменную его данные
                                    if (bref.Id == id)
                                    { 
                                        brefData = bref;
                                        break;
                                    }
                                }
                                //если такого блока еще не было
                                if (brefData == null)
                                {
                                    //получаем пространства блока
                                    using (BlockReference bref = tr.GetObject(id, OpenMode.ForRead, false, true) as BlockReference)
                                    {
                                        using (BlockTableRecord btr2 = tr.GetObject(bref.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord)
                                        {
                                            //переменная для имени динамического блока
                                            string dName = "";
                                            if (bref.DynamicBlockTableRecord != null && bref.DynamicBlockTableRecord != ObjectId.Null)
                                            {
                                                using (BlockTableRecord btr3 = tr.GetObject(bref.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord)
                                                { 
                                                    dName = btr3.Name;
                                                }
                                            }
                                            //если хотя бы один из вариантов имени блока совпадает с выбранным блоком то продолжаем
                                            if (!btr2.Name.Equals(IniData.Block) && !bref.Name.Equals(IniData.Block) && !dName.Equals(IniData.Block)) continue;

                                            //создаем переменную для границ
                                            Extents3d ex = new Extents3d();
                                            //если границы определяются то записываем их
                                            if (bref.Bounds.HasValue) ex = bref.Bounds.Value;
                                            else
                                            {
                                                //если нет то взрываем блок и получаем границы из объектов внутри блока
                                                using (DBObjectCollection collection = new DBObjectCollection())
                                                {
                                                    bref.Explode(collection);
                                                    foreach (DBObject dBObject in collection)
                                                    {
                                                        if (dBObject is Curve curve && curve.Bounds.HasValue) ex.AddExtents(curve.Bounds.Value);
                                                        dBObject?.Dispose();
                                                    }
                                                }
                                            }                                        
                                            //создаем контур блока
                                            Polyline poly = CreatePolyline(ex);
                                            //если по выбранному тагу находится значение то создаем данные блока
                                            if (BlockReferenceGetAttribute(bref, IniData.AttributeTag, out string attResult))
                                            {
                                                brefData = new BrefData(id, poly, attResult);
                                                BrefDatas.Add(brefData);
                                            }
                                            else poly?.Dispose();
                                        }
                                    }
                                }
                                //если блок не относится к нужным то продолжаем
                                if (brefData == null) continue;
                                //если блок нужный то проверяем находится ли видовой экран в области блока
                                //если да то к префиксу добавляем значение выбранного атрибута
                                PositionType position = viewport.CenterPoint.GetPositionType(brefData.Bound);
                                if (position != PositionType.inner) continue;
                                prefix += brefData.AttResult;
                                break;
                            }
                        }
                        break;
                    }
            }

            //начано нумерации
            int i = 1;
            string name = prefix;            

            //если префикс пустой или нумеровать надо независимо от префикса то прибавляем номер к префиксу
            if (string.IsNullOrEmpty(prefix)) name = i.ToString();
            else if (first) name += i.ToString();

            //если такое имя уже используется то составляем имя со стелующим номером
            while (Names.Contains(name))
            { 
                name = prefix + i++.ToString();
            }
            
            //додбавляем имя в список
            Names.Add(name);
            //возвращаем новое имя
            return name.Trim();            
        }
        /// <summary>
        /// список используемых имен
        /// </summary>
        private List<string> Names { get; set; } = new List<string>();
        /// <summary>
        /// список данных блоков
        /// </summary>
        private List<BrefData> BrefDatas { get; set; } = new List<BrefData>();
        /// <summary>
        /// данные блока
        /// </summary>
        private class BrefData
        {
            public BrefData(ObjectId id, Polyline bound, string attResult) 
            { 
                Bound = bound;
                Id = id;
                AttResult = attResult;           
            }
            //границы
            public Polyline Bound { get; private set; }
            //ObjectId
            public ObjectId Id { get; private set; }    
            //значение выбранного атрибута
            public string AttResult { get; private set; }
        }
        #endregion

        private IniData IniData { get; set; }
    }
}
