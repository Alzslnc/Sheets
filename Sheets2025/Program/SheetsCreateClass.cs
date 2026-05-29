using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using BaseFunction;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sheets.Program
{
    internal class SheetsCreateClass
    {
        internal static void Create()
        {
            try
            {
                bool? result = Autodesk.AutoCAD.ApplicationServices.Application.ShowModalWindow(new View.SheetsCreateView.SheetsCreateView());
                if (result.HasValue && result.Value)
                {
                    Settings.Save();
                }
                else
                {
                    Settings.Load();
                    return;
                }
                if (Settings.Default.SelectPosition)
                {
                    if (!Support.InLayout(true)) return;
                }
                if (string.IsNullOrEmpty(Settings.Default.ViewportLayerName))
                {
                    System.Windows.MessageBox.Show("Не выбран слой на котором находятся видовые экраны для создания схемы.");
                    return;
                }
                CreateSheets();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private static void CreateLayers(Transaction tr)
        {
            //создаем слой фона
            Support.CreateLayer(Names.BackgroundSheetsLayer, true, tr);
            //создаем слой фона
            Support.CreateLayer(Names.HeaderLayer, true, tr);
            //создаем слой фона
            Support.CreateLayer(Names.BorderLayer, true, tr);
            //создаем слой фона
            Support.CreateLayer(Names.SheetsLayer, true, tr);
            //создаем слой фона
            Support.CreateLayer(Names.CurrentObjectsLayer, true, tr);
        }

        private static void CreateSheets()
        {            
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {

                CreateLayers(tr);
                BlockTable bt = tr.GetObject(HostApplicationServices.WorkingDatabase.BlockTableId, OpenMode.ForWrite) as BlockTable;
                //исходные данные для создания схем
                Point3d? blockPosition = Point3d.Origin;
                Point3d blockOriginPosition = Point3d.Origin;
                double blockScale = 1000 / Settings.Default.Scale ;
                List<ViewportData> viewports = Support.GetViewportDatas(tr, Settings.Default.ViewportLayerName);
                if (viewports.Count == 0)
                {
                    System.Windows.MessageBox.Show("На выбранном слое отсутствую видовые экраны.");
                    return;
                }

                //если строим схему на видовом экране то выбираем его и получаем данные
                if (Settings.Default.SelectPosition && Settings.Default.PositionType == PositionType.viewport)
                {
                    ObjectId viewportId = Support.GetViewportId(tr);
                    if (viewportId == ObjectId.Null) return;

                    Viewport viewport = tr.GetObject(viewportId, OpenMode.ForRead, false, true) as Viewport;
                    if (viewport == null) return;

                    blockPosition = viewport.CenterPoint;
                    blockOriginPosition = viewport.CenterPoint.TransformBy(viewport.ConvertToViewport().Inverse());
                    blockScale = viewport.CustomScale;
                }

                //создаем блоки и возвращаем
                BlockTableRecord btr = CreateBlock(blockOriginPosition, blockScale, tr, bt, viewports, out List<BlockReference> references);

                //получаем положение блока на листе если задано вручную
                if (Settings.Default.SelectPosition && Settings.Default.PositionType == PositionType.point && references.Any(x => x != null))
                {
                    blockPosition = GetPosition(btr, blockScale);
                    if (blockPosition == null)
                    {
                        System.Windows.MessageBox.Show("Не выбрано местоположение схемы.");
                        return;
                    }
                    
                }

                if (Settings.Default.SelectPosition && references.Any(x => x != null))
                {
                    //двигаем блоки
                    foreach (BlockReference reference in references)
                    {
                        if (reference == null) continue;
                        reference.Position = blockPosition.Value;
                    }
                   
                }


                tr.Commit();
            }
        }

       

        private static Point3d? GetPosition(BlockTableRecord btr, double blockScale)
        {
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                using (BlockReference reference = new BlockReference(Point3d.Origin, btr.Id))
                {
                    reference.ScaleFactors = new Scale3d(blockScale);
                    reference.EntityInsert(out ObjectId id);
                    if (id != ObjectId.Null)
                    {                       
                        return reference.Position;
                    }
                }
            }
            return null;
        }

        private static BlockTableRecord CreateBlock(Point3d blockOriginPosition, double blockScale, Transaction tr, BlockTable bt, List<ViewportData> viewports, out List<BlockReference> references)
        {
            references = new List<BlockReference>();
            //высота заголовка
            double headerHeight = Settings.Default.HeaderHeight / blockScale;
            //высота текста
            double fontHeigth = Settings.Default.FontHeight / blockScale;
            

            //область для определения центра блока и границы
            Extents3d extents = new Extents3d();

            //получаем уникальное имя подложки
            string name;
            int i = 1;
            while (bt.Has(name = $"{Names.BackgroundSheetsLayer}({i++})")) continue;

            //создаем блок подложки
            BlockTableRecord btr = new BlockTableRecord() { Name = name };
            bt.Add(btr);
            tr.AddNewlyCreatedDBObject(btr, true);

            //заполняем подложку экранами
            foreach (ViewportData data in viewports)
            {
                data.ModelCurve = data.Viewport.GetViewportCurveInModel(tr, false, out _, out _, out _, out _);

                Curve curve = data.ModelCurve.Clone() as Curve;
                curve.Layer = Names.SheetsLayer;
                curve.ColorIndex = 256;
                curve.LineWeight = LineWeight.ByLayer;
                btr.AppendEntity(curve);
                tr.AddNewlyCreatedDBObject(curve, true);

                extents.AddExtents(curve.GeometricExtents);
            }

            //если не выбирается базовый вид определяем центр подложки и рисуем границу
            if (!Settings.Default.SelectPosition || Settings.Default.PositionType != PositionType.viewport)
            {
                blockOriginPosition = extents.MinPoint + (extents.MaxPoint - extents.MinPoint) / 2;

                extents.AddPoint(extents.MinPoint - Vector3d.YAxis - Vector3d.XAxis);
                extents.AddPoint(extents.MaxPoint + Vector3d.XAxis + Vector3d.YAxis);

                //устанавливаем границу
                Polyline polyline = extents.CreatePolylineFromExtents();
                polyline.ColorIndex = 256;
                polyline.LineWeight = LineWeight.ByLayer;
                polyline.Layer = Names.BorderLayer;
                polyline.LinetypeId = HostApplicationServices.WorkingDatabase.ContinuousLinetype;

                btr.AppendEntity(polyline);
                tr.AddNewlyCreatedDBObject(polyline, true);
            }

            //устанавливаем загололвок
            MText header = new MText() 
            { 
                TextHeight = headerHeight, 
                Attachment = AttachmentPoint.BottomCenter, 
                Layer = Names.HeaderLayer, ColorIndex = 256, 
                Contents = Settings.Default.Header,
                Location = new Point3d((extents.MinPoint.X + extents.MaxPoint.X) / 2, extents.MaxPoint.Y, 0),
            };
            btr.AppendEntity(header);
            tr.AddNewlyCreatedDBObject(header, true);

            //устанавлием центр подложки
            btr.Origin = blockOriginPosition;

            //создаем блоки под каждый видовой экран
            foreach (ViewportData data in viewports)
            {
                references.Add(Support.RecreateSheetBlock(data, btr, bt, tr, fontHeigth, blockScale));             
            }

            return btr;
        }
       
    }
}