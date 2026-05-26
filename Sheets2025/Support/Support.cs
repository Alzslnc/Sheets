using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Internal;
using BaseFunction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                poly.TransformBy(Matrix3d.Displacement(Point3d.Origin - center));
            }

            //center = curve.GeometricExtents.MinPoint + (curve.GeometricExtents.MaxPoint - curve.GeometricExtents.MinPoint) / 2;

            //переносим контур в начало координат блока
            if (onOriginPoint)
            {                
                curve.TransformBy(Matrix3d.Displacement(Point3d.Origin - center));
            }
            
            return curve;
        }
    }
}
