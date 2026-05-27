using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using BaseFunction;
using System;
using System.Collections.Generic;

namespace Sheets.Program
{
    internal class SheetsCreateClass
    {
        internal static void Create()
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
                System.Windows.MessageBox.Show("Для создания листов с возможностью выбора положения надо находиться в пространстве листа.");
                return;
            }
            if (string.IsNullOrEmpty(Settings.Default.ViewportLayerName))
            {
                System.Windows.MessageBox.Show("Не выбран слой на котором находятся видовые экраны для создания схемы.");
                return;
            }
            CreateSheets();
        }

        private static void CreateSheets()
        {
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(HostApplicationServices.WorkingDatabase.BlockTableId, OpenMode.ForWrite) as BlockTable;
                //исходные данные для создания схем
                Point3d? blockPosition = Point3d.Origin;
                Point3d blockOriginPosition = Point3d.Origin;
                double blockScale = Settings.Default.Scale;
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
                if (Settings.Default.SelectPosition && Settings.Default.PositionType == PositionType.point)
                {
                    blockPosition = GetPosition(btr, blockScale);
                    if (blockPosition == null)
                    {
                        System.Windows.MessageBox.Show("Не выбрано местоположение схемы.");
                        return;
                    }
                    //двигаем блоки
                    foreach (BlockReference reference in references) reference.Position = blockPosition.Value;
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
            //создаем слой фона
            Support.CreateLayer(Names.BackgroundSheetsLayer, true, tr);

            //область для определения центра блока и границы
            Extents3d extents = new Extents3d();

            //получаем уникальное имя подложки
            string name;
            int i = 1;
            while (bt.Has(name = $"{Names.BackgroundSheetsLayer}({i})")) continue;

            BlockTableRecord btr = new BlockTableRecord() { Name = name };

            bt.Add(btr);
            tr.AddNewlyCreatedDBObject(btr, true);

            foreach (ViewportData data in viewports)
            {
                data.ModelCurve = data.Viewport.GetViewportCurveInModel(tr, false, out _, out _, out _, out _);

                Curve curve = data.ModelCurve.Clone() as Curve;

                btr.AppendEntity(curve);
                tr.AddNewlyCreatedDBObject(curve, true);

                extents.AddExtents(curve.GeometricExtents);
            }

            if (!Settings.Default.SelectPosition || Settings.Default.PositionType != PositionType.viewport)
            {
                blockOriginPosition = extents.MinPoint + (extents.MaxPoint - extents.MinPoint) / 2;

                extents.AddPoint(extents.MinPoint - Vector3d.YAxis - Vector3d.XAxis);
                extents.AddPoint(extents.MaxPoint + Vector3d.XAxis + Vector3d.YAxis);

                Polyline polyline = extents.CreatePolylineFromExtents();
                polyline.ColorIndex = 256;
                polyline.Layer = Names.BorderLayer;
                polyline.LinetypeId = HostApplicationServices.WorkingDatabase.ContinuousLinetype;

                btr.AppendEntity(polyline);
                tr.AddNewlyCreatedDBObject(polyline, true);
            }

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

            btr.Origin = blockOriginPosition;

            foreach (ViewportData data in viewports)
            {
                string viewportBlockName = ;

            }


            return btr;
        }
       
    }
}