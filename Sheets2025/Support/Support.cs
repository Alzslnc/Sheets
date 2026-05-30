using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.Runtime;
using BaseFunction;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Windows.Media.Media3D;

namespace Sheets
{
    internal static class Support
    {
        public static bool InModel(bool message)
        {
            if (AcAeUtilities.IsInBlockEditor())
            {
                if (message)
                {
                    System.Windows.MessageBox.Show("Для работы команды нельзя находиться в редакторе блоков");
                }

                return false;
            }

            if (LayoutManager.Current.CurrentLayout != "Model")
            {
                if (message)
                {
                    System.Windows.MessageBox.Show("Для работы команды нужно находиться в модели");
                }

                return false;
            }

            return true;                      
        }
        public static bool InLayout(bool message)
        {
            if (AcAeUtilities.IsInBlockEditor())
            {
                if (message)
                {
                    System.Windows.MessageBox.Show("Для работы команды нельзя находиться в редакторе блоков");
                }

                return false;
            }

            if (LayoutManager.Current.CurrentLayout == "Model")
            {
                if (message)
                {
                    System.Windows.MessageBox.Show("Для работы команды нужно находиться на листе");
                }

                return false;
            }

            return true;
        }
        public static void CreateLayer(string name, bool plot, Transaction tr)
        {
            using (LayerTable layerTable = tr.GetObject(HostApplicationServices.WorkingDatabase.LayerTableId, OpenMode.ForWrite, false, true) as LayerTable)
            {
                LayerTableRecord layerTableRecord = null;
                if (layerTable.Has(name))
                {
                    layerTableRecord = tr.GetObject(layerTable[name], OpenMode.ForWrite, false, true) as LayerTableRecord;
                }
                else
                { 
                    layerTableRecord = new LayerTableRecord();
                    layerTableRecord.Name = name;
                    layerTableRecord.IsPlottable = plot;
                    layerTable.Add(layerTableRecord);
                    tr.AddNewlyCreatedDBObject(layerTableRecord, true);
                }

                if (layerTableRecord == null) return;

                using (layerTableRecord)
                { 
                    layerTableRecord.IsOff = false;
                    layerTableRecord.IsFrozen = false;                    
                }
            }
        }
        public static ObjectId GetViewportId(Transaction tr)
        {
            RXClass viewportClass = RXClass.GetClass(typeof(Viewport));
            //выбираем видовой экран для получения границы и размера           
            while (true)
            {
                if (!BaseGetObjectClass.TryGetobjectId(out ObjectId id, new List<Type> { typeof(Curve), typeof(Viewport) }, "Выберите видовой экран для последующего создания областей в модели: ", true)) return ObjectId.Null;

                if (id.ObjectClass == viewportClass) return id;

                Curve curve = tr.GetObject(id, OpenMode.ForRead, false, true) as Curve;

                if (curve == null) continue;

                foreach (ObjectId vid in (tr.GetObject(HostApplicationServices.WorkingDatabase.CurrentSpaceId, OpenMode.ForRead) as BlockTableRecord))
                {
                    if (vid.ObjectClass != viewportClass) continue;

                    Viewport vp = tr.GetObject(vid, OpenMode.ForRead, false, true) as Viewport;

                    if (vp == null) continue;

                    if (vp.NonRectClipOn && vp.NonRectClipEntityId == id)
                    {
                        return vid;
                    }
                }
            }
        }
        public static Curve GetViewportCurveInModel(this Viewport viewport, Transaction tr, bool onOriginPoint, out double height, out double width, out Point3d center, out Matrix3d viewportMatrix)
        {
            //исходные геометрические данные
            viewportMatrix = viewport.ConvertToViewport();

            Curve curve;

            //получаем его габариты в модели
            width = viewport.Width / viewport.CustomScale;
            height = viewport.Height / viewport.CustomScale;
            
            //центр контура         
            center = viewport.CenterPoint.TransformBy(viewportMatrix.Inverse()).Z0();

            //получаем контур видового экрана
            if (viewport.NonRectClipEntityId != null && viewport.NonRectClipEntityId != ObjectId.Null)
            {
                //получаем контур если он имеет нестандартную границу
                curve = tr.GetObject(viewport.NonRectClipEntityId, OpenMode.ForRead).Clone() as Curve;
                //трансформируем в габариты модели
                curve.TransformBy(viewportMatrix.Inverse());
                curve = curve.GetOrthoProjectedCurve(new Plane());              
                //получаем актуальные размеры
                height = curve.GeometricExtents.MaxPoint.X - curve.GeometricExtents.MinPoint.X;
                width = curve.GeometricExtents.MaxPoint.Y - curve.GeometricExtents.MinPoint.Y;
            }
            else
            {
                Polyline poly = new Polyline();
                int j = 0;
                poly.AddVertexAt(j++, new Point2d(-width / 2, - height / 2), 0, 0, 0);
                poly.AddVertexAt(j++, new Point2d(-width / 2, height / 2), 0, 0, 0);
                poly.AddVertexAt(j++, new Point2d(width / 2, height / 2), 0, 0, 0);
                poly.AddVertexAt(j++, new Point2d(width / 2, - height / 2), 0, 0, 0);
                poly.Closed = true;
                curve = poly;
                poly.TransformBy(Matrix3d.Displacement(center - Point3d.Origin));
            }
            
            //переносим контур в начало координат блока
            if (onOriginPoint)
            {                
                curve.TransformBy(Matrix3d.Displacement(Point3d.Origin - center));
            }
            
            return curve;
        }

        internal static IEnumerable<string> GetViewportLayers()
        {
            List<string> result = new List<string>() { "" };

            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            { 
                BlockTable blockTable = tr.GetObject(HostApplicationServices.WorkingDatabase.BlockTableId, OpenMode.ForRead) as BlockTable;

                RXClass viewportClass = RXClass.GetClass(typeof(Viewport));

                foreach (ObjectId btrId in blockTable)
                { 
                    BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;

                    if (!btr.IsLayout) continue;

                    foreach (ObjectId id in btr)
                    { 
                        if (id.ObjectClass != viewportClass) continue;

                        Entity entity = tr.GetObject(id, OpenMode.ForRead, false, true) as Entity;

                        if (!result.Contains(entity.Layer)) result.Add(entity.Layer);
                    }
                }

                tr.Commit();
            }   

            return result.OrderBy(x => x);
        }
        public static List<ViewportData> GetViewportDatas(Transaction tr, string layer)
        {
            List<ViewportData> result = new List<ViewportData>();
                      
            BlockTable blockTable = tr.GetObject(HostApplicationServices.WorkingDatabase.BlockTableId, OpenMode.ForRead) as BlockTable;

            RXClass viewportClass = RXClass.GetClass(typeof(Viewport));
            RXClass blockReferenceClass = RXClass.GetClass(typeof(BlockReference));

            int i = 1;

            foreach (ObjectId btrId in blockTable)
            {
                BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForWrite) as BlockTableRecord;

                if (!btr.IsLayout) continue;

                Layout layout = tr.GetObject(btr.LayoutId, OpenMode.ForRead) as Layout;

                string[] strings = layout.LayoutName.Split(new string[] { "(", ")" }, StringSplitOptions.RemoveEmptyEntries);

                string layoutNum = strings.Length < 2 ? string.Empty : strings[1];

                bool numExist = int.TryParse(layoutNum, out int layoutIntNum);

                List<BlockReference> blockReferences = new List<BlockReference>();
                List<string> tags = new List<string>() { Settings.Default.AttributeName };

                if (Settings.Default.NumType == NumType.byReferenceAtteibute)
                {
                    foreach (ObjectId id in btr)
                    {
                        if (id.ObjectClass != blockReferenceClass) continue;

                        BlockReference reference = tr.GetObject(id, OpenMode.ForRead, false, true) as BlockReference;

                        if (reference.GetName() == Settings.Default.BlockName) blockReferences.Add(reference);
                    }
                }

                List<string> layers = new List<string>();

                foreach (ObjectId id in btr)
                {
                    if (id.ObjectClass != viewportClass) continue;

                    Viewport viewport = tr.GetObject(id, OpenMode.ForRead, false, true) as Viewport;

                    if (viewport != null) layers.Add(viewport.Layer);

                    if (viewport == null || viewport.Layer != layer) continue;

                    ViewportData viewportData = new ViewportData { Name = string.Empty, Viewport = viewport, Owner = btr };

                    result.Add(viewportData);

                    if (Settings.Default.NumType == NumType.order)
                    {
                        viewportData.Name = i++.ToString();
                    }
                    else if (Settings.Default.NumType == NumType.byList)
                    {
                        if (numExist) viewportData.Name = (layoutIntNum + Settings.Default.NumListShift).ToString();
                    }
                    else if (Settings.Default.NumType == NumType.byReferenceAtteibute)
                    {
                        foreach (BlockReference reference in blockReferences)
                        {
                            if (viewport.CenterPoint.X < reference.GeometricExtents.MinPoint.X ||
                                viewport.CenterPoint.X > reference.GeometricExtents.MaxPoint.X ||
                                viewport.CenterPoint.Y < reference.GeometricExtents.MinPoint.Y ||
                                viewport.CenterPoint.Y > reference.GeometricExtents.MaxPoint.Y) continue;

                            reference.BlockReferenceGetAttribute(out Dictionary<string, string> keyValuePairs, tr, tags, false);

                            if (keyValuePairs.TryGetValue(Settings.Default.AttributeName, out string? value)) viewportData.Name = value;

                            break;
                        }                        
                    }
                }
            }

            return result;
        }
     
        public static IEnumerable<BlockRefData> GetBlockRefDatas()
        {
            List<BlockRefData> result = new List<BlockRefData>();

            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                BlockTable blockTable = tr.GetObject(HostApplicationServices.WorkingDatabase.BlockTableId, OpenMode.ForRead) as BlockTable;

                RXClass attribute = RXClass.GetClass(typeof(AttributeDefinition));

                foreach (ObjectId btrId in blockTable)
                {
                    BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;

                    if (btr.IsLayout) continue;

                    BlockRefData blockRefData = new BlockRefData() { Name = btr.Name };

                    foreach (ObjectId id in btr)
                    {
                        if (id.ObjectClass != attribute) continue;

                        AttributeDefinition entity = tr.GetObject(id, OpenMode.ForRead, false, true) as AttributeDefinition;

                        blockRefData.Attributes.Add(entity.Tag);
                    }

                    if (blockRefData.Attributes.Count > 0) result.Add(blockRefData);
                }

                tr.Commit();
            }

            return result.OrderBy(x => x.Name);
        }

      
        internal static BlockReference? RecreateSheetBlock(ViewportData data, BlockTableRecord btr, BlockTable bt, Transaction tr, double textHeight, double scale)
        {
            //название блока
            string viewportBlockName = $"{Names.SheetsLayer}_{data.Viewport.Handle}";
            bool newBlock = false;
            //создаем новый блок
            BlockTableRecord newBtr;
            if (bt.Has(viewportBlockName))
            {
                newBtr = tr.GetObject(bt[viewportBlockName], OpenMode.ForWrite) as BlockTableRecord;
                foreach (ObjectId id in newBtr)
                {
                    Entity entity = tr.GetObject(id, OpenMode.ForWrite, false, true) as Entity;
                    entity.Erase();
                }
            }
            else
            {     
                newBlock = true;
                newBtr = new BlockTableRecord() { Name = viewportBlockName };
                bt.Add(newBtr);
                tr.AddNewlyCreatedDBObject(newBtr, true);
            }

            //если блок новый то устанавливаем центр
            newBtr.Origin = btr.Origin;

            //добавляем подложку
            BlockReference background = new BlockReference(btr.Origin, btr.Id) { Layer = Names.SheetsLayer, ColorIndex = 256, LineWeight = LineWeight.ByLayer };
            newBtr.AppendEntity(background);
            tr.AddNewlyCreatedDBObject(background, true);

            //добавляем контур
            Curve curve = data.ModelCurve.Clone() as Curve;
            curve.LineWeight = LineWeight.ByLayer;
            curve.ColorIndex = 256;
            curve.Layer = Names.CurrentObjectsLayer;
            newBtr.AppendEntity(curve);
            tr.AddNewlyCreatedDBObject(curve, true);

            //определяем центр контура
            Point3d center = curve.GeometricExtents.MinPoint + (curve.GeometricExtents.MaxPoint - curve.GeometricExtents.MinPoint) / 2;

            //добавляем номер
            MText number = new MText()
            {
                TextHeight = textHeight,
                Attachment = AttachmentPoint.MiddleCenter,
                Layer = Names.BackgroundSheetsLayer,
                ColorIndex = 256,
                LineWeight = LineWeight.ByLayer,
                Contents = Settings.Default.Prefix + data.Name,
                Location = center,
            };
            Circle circle = null;
            if (!string.IsNullOrEmpty(number.Text))
            {
                btr.AppendEntity(number);
                tr.AddNewlyCreatedDBObject(number, true);

                MText nClone = number.Clone() as MText;
                nClone.Layer = Names.CurrentObjectsLayer;
                newBtr.AppendEntity(nClone);
                tr.AddNewlyCreatedDBObject(nClone, true);               

                double radius = number.GeometricExtents.MaxPoint.DistanceTo(number.GeometricExtents.MinPoint) / 2;

                //добавляем круг
                circle = new Circle(center, Vector3d.ZAxis, radius > textHeight ? radius : textHeight) { Layer = Names.CurrentObjectsLayer, ColorIndex = 256, LineWeight = LineWeight.ByLayer };
                newBtr.AppendEntity(circle);
                tr.AddNewlyCreatedDBObject(circle, true);
            }
            //добавляем штриховку
            Hatch hatch = new Hatch();
            hatch.ColorIndex = 256;
            hatch.LineWeight = LineWeight.ByLayer;
            hatch.Layer = Names.CurrentObjectsLayer;
            hatch.PatternScale = Settings.Default.HatchScale / scale;
            try
            {
                hatch.PatternAngle = Settings.Default.HatchAngle / 180 * Math.PI;    
            }
            catch
            { 
            }
            hatch.SetHatchPattern(HatchPatternType.PreDefined, Settings.Default.HatchPattern);
            newBtr.AppendEntity(hatch);
            tr.AddNewlyCreatedDBObject(hatch, true);
            hatch.AppendLoop(HatchLoopTypes.External, new ObjectIdCollection { curve.Id });
            if (circle != null) hatch.AppendLoop(HatchLoopTypes.Outermost, new ObjectIdCollection { circle.Id });

            BlockReference reference = null;
            if (newBlock)
            {
                reference = new BlockReference(data.Viewport.CenterPoint, newBtr.Id);
                data.Owner.AppendEntity(reference);
                tr.AddNewlyCreatedDBObject(reference, true);
                return reference;
            }
            else
            {
                foreach (ObjectId id in newBtr.GetBlockReferenceIds(true, false))
                { 
                    reference = tr.GetObject(id, OpenMode.ForWrite, false, true) as BlockReference;
                    break;
                }
            }

            if (reference != null)
            {
                reference.ScaleFactors = new Scale3d(scale);                
            }

            return reference;

        }
    }
    public class BlockRefData
    {
        public string Name { get; set; } = string.Empty;
        public ObservableCollection<string> Attributes { get; } = new ObservableCollection<string>();
        public override string ToString()
        {
            return Name;
        }
    }
    public class ViewportData
    {
        public string Name { get; set; } = string.Empty;
        public Viewport Viewport { get; set; } = null;
        public BlockTableRecord Owner { get; set; } = null;
        public Curve ModelCurve { get; set; } = null;
    }
}
