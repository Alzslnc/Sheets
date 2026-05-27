using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using BaseFunction;
using System;
using System.Collections.Generic;
using System.Linq;
using static BaseFunction.BaseGeometryClass;

namespace Sheets.Program
{
    internal class ExtentsCreateClass
    {
        internal static void Create()
        {
            Settings.Default.ExtentsCreateType = ExtentsCreateType.curve;
            Settings.Default.ExtentsOverlap = 2;
            Settings.Default.ExtentsAlongCurve = true;
            Settings.Default.YOnNorth = false;

            //если не находимся на листе то прерываем
            if (!Support.InLayout(true)) return;
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                ObjectId id = Support.GetViewportId(tr);

                if (id == ObjectId.Null) return;

                //переходим на модель
                LayoutManager.Current.CurrentLayout = "Model";

                //выбираем объекты областей или трасс для создания раскладки экранов
                string message = Settings.Default.ExtentsCreateType == ExtentsCreateType.curve ? "трассы" : "области";

                List<Type> types = new List<Type> { typeof(Polyline), typeof(Circle), typeof(Spline), typeof(Ellipse) };
                if (Settings.Default.ExtentsCreateType == ExtentsCreateType.curve)
                {
                    types.Add(typeof(Line));
                    types.Add(typeof(Arc));
                } 

                if (!BaseGetObjectClass.TryGetObjectsIds(out List<ObjectId> ids, types, $"Выберите {message} для создания областей видимости видовых экранов.")) return;
                            
                //получаем видовой экран
                Viewport viewport = tr.GetObject(id, OpenMode.ForRead, false, true) as Viewport;
                if (viewport == null) return;

              
                //получаем выбранные кривые
                List<Curve> curves = new List<Curve>();
                foreach (ObjectId cid in ids)
                { 
                    Curve curve = tr.GetObject(cid, OpenMode.ForRead, false, true) as Curve;
                    if (Settings.Default.ExtentsCreateType == ExtentsCreateType.area && (!curve.Closed || !curve.StartPoint.IsEqualTo(curve.EndPoint))) continue;
                    curves.Add(curve);
                }
                if (curves.Count == 0)
                {
                    if (Settings.Default.ExtentsCreateType == ExtentsCreateType.area)
                    {
                        System.Windows.MessageBox.Show("Для расстановки внутри областей они должны быть замкнуты.");
                    }
                    return;
                } 
                    
                //создаем блок экрана и возвращаем его Id          
                string name = $"{Names.ExtentName}{viewport.Handle}";
                //создаем слой для блоков
                Support.CreateLayer(name, false, tr);
                               
                //создаем контур соответствующий видовому экрану в модели
                using (Curve polyline = viewport.GetViewportCurveInModel(tr, true, out double height, out double width, out _, out _))
                {
                    //перекрытите
                    double overlap = width * Settings.Default.ExtentsOverlap / 100;

                    //создаем блок
                    ObjectId blockId = GetExtentBlockId(name, tr, width, height, polyline);

                    //создаем блоки
                    List<Entity> toModelSpace = Settings.Default.ExtentsCreateType == ExtentsCreateType.curve 
                        ? CreateExtentsOnCurve(curves, blockId, name, polyline, overlap) 
                        : CreateExtentsInArea(curves, blockId, name, polyline, width, height);

                    //добавляем области в чертеж
                    toModelSpace.AddEntityInCurrentBTR(tr);

                    int i = 1;

                    //добввляем хендл области блокам
                    foreach (Entity entity in toModelSpace)
                    {
                        entity.XDataSet(Names.LayoutCreateAppName, new List<TypedValue> { new TypedValue(Convert.ToInt32(DxfCode.ExtendedDataHandle), viewport.Handle) }, true);
                        BlockReference reference = entity as BlockReference;
                        if (reference != null)
                        {
                            reference.BlockReferenceSetAttribute(tr);
                            reference.BlockReferenceChangeAttribute(tr, new Dictionary<string, string> { { Names.BlockReferenceNumber, i++.ToString() } });
                        }
                    }

                }
                               

                tr.Commit();            
            }
        }

        private static List<Entity> CreateExtentsOnCurve(List<Curve> curves, ObjectId blockId, string name, Curve polyline, double overlap)
        {
            List<Entity> toModelSpace = new List<Entity>();

            foreach (Curve curve in curves)
            {
                //дистанция до последней рамки
                double curDistance = 0;
                //длина полилинии
                double contLongth = curve.GetLength();
                //определение четности рамки для выбора нужной
                bool chet = false;

                //пока рамки в пределах полилинии
                while (curDistance < contLongth)
                {
                    using (Curve clone = polyline.Clone() as Curve)
                    {
                        Point3d newCenter = curve.GetPointAtDist(curDistance);
                        //помещаем на последнее полученное пересечение
                        clone.TransformBy(Matrix3d.Displacement(newCenter - Point3d.Origin));
                        Vector3d fd = curve.GetFirstDerivative(newCenter);
                        double angle = 0;

                        //разворачиваем вдоль трассы если требуется
                        if (Settings.Default.ExtentsAlongCurve)
                        {
                            angle = Vector3d.XAxis.GetAngleTo(fd, Vector3d.ZAxis);

                            //устанавливаем разворот для читаемости если требуется
                            if (Settings.Default.YOnNorth)
                            {
                                if (angle > Math.PI / 2 && angle < Math.PI / 2 * 3) angle += Math.PI;
                                if (angle > Math.PI * 2) angle -= Math.PI * 2;
                            }

                            clone.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, newCenter));
                        }

                        using (Point3dCollection coll = new Point3dCollection())
                        {
                            //ищем пересечения рамки и поилинии
                            clone.IntersectWith(curve, Intersect.OnBothOperands, coll, IntPtr.Zero, IntPtr.Zero);
                            //если пересечений нет то рамка больше полилинии и полилиния внутри, оставляем одну рамку и останавливаем
                            if (coll.Count == 0)
                            {
                                toModelSpace.Add(new BlockReference(newCenter, blockId) { Layer = name, Rotation = angle });
                                break;
                            }

                            //убираем все пересечения до центра рамки
                            for (int i = coll.Count - 1; i >= 0; i--)
                            {
                                coll[i] = curve.GetClosestPointTo(coll[i], false);
                                if (curve.GetDistAtPoint(coll[i]) <= curDistance) coll.RemoveAt(i);
                            }

                            //если после центра рамки пересечений нет то значит рамка последняя, добавляем ее и останавливаем
                            if (coll.Count == 0)
                            {
                                toModelSpace.Add(new BlockReference(newCenter, blockId) { Layer = name, Rotation = angle });
                                break;
                            }
                            //если пересечений несколько то сортируем вдоль полилинии
                            else if (coll.Count > 1) coll.SortOnCurve(curve);

                            //если рамка четная то добавляем ее, если не то пропускаем
                            if (chet)
                            {                               
                                toModelSpace.Add(new BlockReference(newCenter, blockId) { Layer = name, Rotation = angle });
                                chet = false;
                            }
                            else chet = true;

                            //получаем расстояние до ближайшего пересечения после цетра рамки
                            double newDistance = curve.GetDistAtPoint(coll[0]) - overlap;
                            if (newDistance <= curDistance) break;
                            curDistance = newDistance;
                        }
                    }
                }
            }

            return toModelSpace;
        }
       
        private static List<Entity> CreateExtentsInArea(List<Curve> curves, ObjectId blockId, string name, Curve polyline, double width, double height)
        {
            polyline.TransformBy(Matrix3d.Displacement(Point3d.Origin - new Point3d(-width / 2, -height / 2, 0))); 

            double xOverlap = width * Settings.Default.ExtentsOverlap / 100;
            double yOverlap = height * Settings.Default.ExtentsOverlap / 100;

            List<Entity> toModelSpace = new List<Entity>();

            Extents3d extents = new Extents3d();

            foreach (Curve curve in curves) extents.AddExtents(curve.GeometricExtents);

            double areaWidth = extents.MaxPoint.X - extents.MinPoint.X;
            double areaHeight = extents.MaxPoint.Y - extents.MinPoint.Y;

            double curAreaXPosition = extents.MinPoint.X - xOverlap;
            double curAreaYPosition = extents.MinPoint.Y - yOverlap;

            List<object> objects = curves.Cast<object>().ToList();  

            while (curAreaYPosition < extents.MaxPoint.Y)
            {
                while (curAreaXPosition < extents.MaxPoint.X)
                {
                    using (Polyline contour = polyline.Clone() as Polyline)
                    {
                        Point3d point = new Point3d(curAreaXPosition, curAreaYPosition, 0);
                        contour.TransformBy(Matrix3d.Displacement(point - Point3d.Origin));

                        if (curves.Any(x => x.Intersectionts(contour).Count > 0) || point.GetPositionType(objects, null) == PositionAndIntersections.PositionType.inner)
                        {
                            toModelSpace.Add(new BlockReference(new Point3d(curAreaXPosition + width / 2, curAreaYPosition + height / 2, 0), blockId) { Layer = name });
                        }                        
                    }
                    curAreaXPosition += width - xOverlap;
                }

                curAreaYPosition += height - yOverlap;
                curAreaXPosition = extents.MinPoint.X - xOverlap;
            }

            return toModelSpace;
        }       

        private static ObjectId GetExtentBlockId(string name, Transaction tr, double width, double height, Curve? polyline)
        {
            //получаем таблицу блоков
            BlockTable bt = tr.GetObject(HostApplicationServices.WorkingDatabase.BlockTableId, OpenMode.ForWrite, false, true) as BlockTable;

            //формируем блок
            BlockTableRecord btr;
            if (bt.Has(name))
            {
                btr = tr.GetObject(bt[name], OpenMode.ForWrite, false, true) as BlockTableRecord;
                foreach (ObjectId id in btr)
                { 
                    Entity entity = tr.GetObject(id, OpenMode.ForWrite, false, true) as Entity;
                    entity?.Erase();
                }
            }
            else
            {
                btr = new BlockTableRecord();
                btr.Name = name;
                bt.Add(btr);
                tr.AddNewlyCreatedDBObject(btr, true);
            }

            //создаем если надо непечатаемый слой
            Support.CreateLayer(Names.NoPlotLayer, false, tr);

            //получаем половинные габариты
            width /= 2;
            height /= 2;

            //создаем объекты в блоке
            Curve contour = polyline.Clone() as Curve;
            btr.AppendEntity(contour);
            tr.AddNewlyCreatedDBObject(contour, true);

            //направления
            CreateLine(Names.NoPlotLayer, 0, 0, 0, height / 2, btr, tr, 1);
            CreateLine(Names.NoPlotLayer, 0, 0, width / 2, 0, btr, tr, 1);

            AttributeDefinition attribute = new AttributeDefinition()
            {
                Tag = Names.BlockReferenceNumber,
                ColorIndex = 1,
                Height = width / 5,
                Justify = AttachmentPoint.MiddleCenter,
                AlignmentPoint = Point3d.Origin,
                LockPositionInBlock = true,
                Preset = false,
                TextString = "",
                Layer = Names.NoPlotLayer,
                IsMTextAttributeDefinition = true,
            };
            btr.AppendEntity(attribute);
            tr.AddNewlyCreatedDBObject(attribute, true);

            return bt[name];
        }
        private static void CreateLine(string layer, double x1, double y1, double x2, double y2, BlockTableRecord btr, Transaction tr, int colorIndex = 0)
        {
            Line line = new Line()
            {
                ColorIndex = colorIndex,
                LinetypeId = HostApplicationServices.WorkingDatabase.ContinuousLinetype,
                Layer = layer,
                StartPoint = new Point3d(x1, y1, 0),
                EndPoint = new Point3d(x2, y2, 0)
            };
            btr.AppendEntity(line);
            tr.AddNewlyCreatedDBObject(line, true);
        }
    }
}