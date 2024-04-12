using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using static BaseFunction.BaseGeometryClass;
using static BaseFunction.BaseGetObjectClass;
using static BaseFunction.BaseLayerClass;
using static BaseFunction.F;
using static BaseFunction.PositionAndIntersections;
using static BaseFunction.TextBounds;



namespace Sheets
{
    public class LayoutCreateAcadClass
    {
        public void Start()
        {
            
            if (!TryGetobjectId(out ObjectId PolyId, typeof(Polyline), "Выберите отображаемую область / направляющую")) return;

            GetLayouts();
            
            if (LayoutClasses.Count == 0) return;

            ActionClass = new ActionClass(LayoutNames);

            Thread thread = new Thread(ThreadForm);
            thread.Start();

            while (ActionClass.Action == Action.none || ActionClass.Action == Action.NLC)
            {
                if (ActionClass.Action == Action.NLC)
                {
                    ActionClass.CheckLoName();
                    ActionClass.Action = Action.none;
                }
                else Thread.Sleep(200);
            }

            if (ActionClass.Action != Action.Ok) return;

            #region


            bool fail = false;

            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                using (Polyline poly = tr.GetObject(PolyId, OpenMode.ForRead, false, true) as Polyline)
                {
                    if (!Settings.Default.LC_OnLine)
                    {
                        if (!poly.Closed)
                        {
                            fail = true;
                            MessageBox.Show("Область не замкнута");
                        }
                        if (!fail && (poly.Area == 0))
                        {
                            fail = true;
                            MessageBox.Show("Область не корректна");
                        }
                        if (!fail)
                        {
                            try
                            {
                                using (MPolygon polygon = new MPolygon())
                                {
                                    polygon.AppendLoopFromBoundary(poly, true, Tolerance.Global.EqualPoint);
                                }
                            }
                            catch
                            {
                                fail = true;
                                MessageBox.Show("Область не корректна");
                            }
                        }
                    }
                    if (!fail)
                    {
                        Contour = poly.Clone() as Polyline;
                        Contour.Elevation = 0;
                    }
                }
                tr.Commit();
            }

            if (fail) return;
            

            double overlap = Settings.Default.LC_Overlap / 100;

            if (LayoutManager.Current.CurrentLayout != ActionClass.Result) LayoutManager.Current.CurrentLayout = ActionClass.Result;

            if (!TryGetobjectId(out ObjectId vpId, typeof(Viewport), "Выберите базовый видовой экран")) return;

            List<string> LayerNames = GetLayerNames();

            string name = "!_New_Viewport_set_";

            int i = 1;
            while (LayerNames.Contains(name + i)) i++;

            name += i;

            LayerNew(name);

            if (Settings.Default.LC_OnLine) CreateOnLine(name, overlap, vpId);
            else CreateOnArea(name, overlap, vpId);

            #endregion

        }
        private void ThreadForm()
        {
            LayoutCreateForm form = new LayoutCreateForm(ActionClass);
            form.ShowDialog();
            if (form.DialogResult == DialogResult.OK) ActionClass.Action = Action.Ok;
            else ActionClass.Action = Action.Cancel;
        }

        private void GetLayouts()
        {
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                using (DBDictionary lm = tr.GetObject(HostApplicationServices.WorkingDatabase.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary)
                {
                    foreach (DBDictionaryEntry dde in lm)
                    {
                        using (Layout layout = tr.GetObject(dde.Value, OpenMode.ForRead) as Layout)
                        {
                            if (layout.LayoutName == "Model") continue;
                            LayoutNames.Add(layout.LayoutName);
                            LayoutClasses.Add(new LayoutClass(layout));
                        }
                    }
                }
                tr.Commit();
            }
        }
        private void CreateOnArea(string name, double overlap, ObjectId vpId)
        {
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                LOCreateData lO = new LOCreateData(tr, vpId);

                if (lO.c == null || lO.baseViewportId == ObjectId.Null || !lO.c.Bounds.HasValue)
                {
                    tr.Abort();
                    return;
                }

                Vector3d vx = Vector3d.XAxis.TransformBy(Matrix3d.Rotation(-lO.angle, Vector3d.ZAxis, Point3d.Origin));
                Vector3d vy = Vector3d.YAxis.TransformBy(Matrix3d.Rotation(-lO.angle, Vector3d.ZAxis, Point3d.Origin));

                if (Settings.Default.LC_LOCreate)
                {
                    XDataSet(lO.baseViewportId, "LayoutCreate", new List<TypedValue>
                            {
                                new TypedValue(Convert.ToInt32(DxfCode.ExtendedDataAsciiString), "1"),
                            }, true);
                }

                Point3d Start;

                if (lO.angle != 0)
                {
                    Contour.TransformBy(Matrix3d.Rotation(lO.angle, Vector3d.ZAxis, Point3d.Origin));
                    Start = Contour.Bounds.Value.MinPoint;
                    lO.maxx = Contour.Bounds.Value.MaxPoint.X - Contour.Bounds.Value.MinPoint.X;
                    lO.maxy = Contour.Bounds.Value.MaxPoint.Y - Contour.Bounds.Value.MinPoint.Y;
                    Contour.TransformBy(Matrix3d.Rotation(-lO.angle, Vector3d.ZAxis, Point3d.Origin));
                    Start = Start.TransformBy(Matrix3d.Rotation(-lO.angle, Vector3d.ZAxis, Point3d.Origin));

                    lO.c.TransformBy(Matrix3d.Rotation(lO.angle, Vector3d.ZAxis, Point3d.Origin));
                    lO.cStart = lO.c.Bounds.Value.MinPoint;
                    lO.c.TransformBy(Matrix3d.Rotation(-lO.angle, Vector3d.ZAxis, Point3d.Origin));
                    lO.cStart = lO.cStart.TransformBy(Matrix3d.Rotation(-lO.angle, Vector3d.ZAxis, Point3d.Origin));
                }
                else
                {
                    lO.cStart = lO.c.Bounds.Value.MinPoint;
                    Start = Contour.Bounds.Value.MinPoint;
                    lO.maxx = Contour.Bounds.Value.MaxPoint.X - Contour.Bounds.Value.MinPoint.X;
                    lO.maxy = Contour.Bounds.Value.MaxPoint.Y - Contour.Bounds.Value.MinPoint.Y;
                }

                List<Curve> futureViewports = new List<Curve>();

                double currx = 0;
                double curry = 0;

                while (currx < lO.maxx && curry < lO.maxy)
                {
                    Curve vc = lO.c.Clone() as Curve;

                    vc.TransformBy(Matrix3d.Displacement(Start - lO.cStart));
                    vc.TransformBy(Matrix3d.Displacement(vx * currx));
                    vc.TransformBy(Matrix3d.Displacement(vy * curry));

                    using (Point3dCollection coll = new Point3dCollection())
                    {
                        vc.IntersectWith(Contour, Intersect.OnBothOperands, coll, IntPtr.Zero, IntPtr.Zero);
                        if (coll.Count == 0)
                        {
                            PositionType position = vc.StartPoint.GetPositionType(Contour);
                            if (position != PositionType.inner)
                            {
                                vc?.Dispose();
                            }
                        }
                        if (!vc.IsDisposed)
                        {
                            futureViewports.Add(vc);
                        }                       
                        curry += lO.vpy * (1 - overlap);
                        if (curry > lO.maxy)
                        {
                            curry = 0;
                            currx += lO.vpx * (1 - overlap);
                        }
                    }
                }


                CreateLayoutsAndContours(futureViewports, tr, name, lO.t1transform, lO.angle);

                if (Settings.Default.LC_LOCreate) XDataClear(lO.baseViewportId, "LayoutCreate", null);

                lO.c?.Dispose();
                tr.Commit();
            }

        }
        private void CreateOnLine(string name, double overlap, ObjectId vpId)
        {
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                LOCreateData lO = new LOCreateData(tr, vpId);

                if (lO.c == null || lO.baseViewportId == ObjectId.Null || !lO.c.Bounds.HasValue)
                {
                    tr.Abort();
                    return;
                }

                if (Settings.Default.LC_LOCreate)
                {
                    XDataSet(lO.baseViewportId, "LayoutCreate", new List<TypedValue>
                            {
                                new TypedValue(Convert.ToInt32(DxfCode.ExtendedDataAsciiString), "1"),
                            }, true);
                }
           
                Point3d cCenter = lO.c.Bounds.Value.MinPoint + (lO.c.Bounds.Value.MaxPoint - lO.c.Bounds.Value.MinPoint) / 2;


                //список новых рамок
                List<Curve> futureViewports = new List<Curve>();

                //дистанция до последней рамки
                double curDistance = 0;
                //длина полилинии
                double contLongth = Contour.GetLength();
                      
                //определяем нахлест как процент от размера рамки
                overlap *= lO.c.Bounds.Value.MaxPoint.X - lO.c.Bounds.Value.MinPoint.X;

                //определение четности рамки для выбора нужной
                bool chet = false;

                //пока рамки в пределах полилинии
                while (curDistance < contLongth)
                { 
                    //получаем клон рамки
                    Curve curve = lO.c.Clone() as Curve;

                    //помещаем на последнее полученное пересечение
                    curve.TransformBy(Matrix3d.Displacement(Contour.GetPointAtDist(curDistance) - cCenter));
                                        
                    using (Point3dCollection coll = new Point3dCollection())
                    {
                        //ищем пересечения рамки и поилинии
                        curve.IntersectWith(Contour, Intersect.OnBothOperands, coll, IntPtr.Zero, IntPtr.Zero);
                        //если пересечений нет то рамка больше полилинии и полилиния внутри, оставляем одну рамку и останавливаем
                        if (coll.Count == 0)
                        {
                            futureViewports.Add(curve);
                            break;
                        }   
                            
                        //убираем все пересечения до центра рамки
                        for (int i = coll.Count - 1; i >= 0; i--)
                        {
                            Point3d p = coll[i];
                            if (Contour.GetDistAtPoint(p) <= curDistance) coll.RemoveAt(i);
                        }

                        //если после центра рамки пересечений нет то значит рамка последняя, добавляем ее и останавливаем
                        if (coll.Count == 0)
                        {
                            futureViewports.Add(curve);
                            break;
                        }
                        //если пересечений несколько то сортируем вдоль полилинии
                        else if (coll.Count > 1) coll.SortOnCurve(Contour); 

                        //получаем расстояние до ближайшего пересечения после цетра рамки    
                        curDistance = Contour.GetDistAtPoint(coll[0]) - overlap; ;

                        //если рамка четная то добавляем ее, если не то пропускаем
                        if (chet)
                        {
                            futureViewports.Add(curve);
                            chet = false;
                        }
                        else
                        {       
                            curve?.Dispose();
                            chet = true;
                        }       
                    }
                }            

                CreateLayoutsAndContours(futureViewports, tr, name, lO.t1transform, lO.angle);

                if (Settings.Default.LC_LOCreate) XDataClear(lO.baseViewportId, "LayoutCreate", null);

                lO.c?.Dispose();
                tr.Commit();
            }
        }

        private void CreateLayoutsAndContours(List<Curve> futureViewports, Transaction tr, string name, Vector3d t1transform, double angle)
        {
            if (Settings.Default.LC_LOCreate)
            {
                string loIName = Settings.Default.LC_LayoutName;
                string loname = loIName;
                int k = 0;
                while (LayoutNames.Contains(loname + "(1)") || LayoutNames.Contains(loname + "(2)"))
                {
                    k++;
                    loname = loIName + "_" + k;
                }

                k = 1;
                string cloname = loname + "(" + k + ")";

                using (LayoutManager lm = LayoutManager.Current)
                {
                    foreach (Curve cur in futureViewports)
                    {
                        while (LayoutNames.Contains(cloname))
                        {
                            k++;
                            cloname = loname + "(" + k + ")";
                        }
                        LayoutNames.Add(cloname);

                        lm.CloneLayout(ActionClass.Result, cloname, lm.LayoutCount);

                        using (Layout newLo = tr.GetObject(lm.GetLayoutId(cloname), OpenMode.ForRead) as Layout)
                        {
                            using (BlockTableRecord newLobtr = tr.GetObject(newLo.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord)
                            {
                                foreach (ObjectId id in newLobtr)
                                {
                                    ResultBuffer rb = XDataGet(id, "LayoutCreate");
                                    if (rb != null)
                                    {
                                        XDataClear(id, "LayoutCreate", null);

                                        using (Viewport newVp = tr.GetObject(id, OpenMode.ForWrite) as Viewport)
                                        {
                                            newVp.Layer = name;
                                            newVp.ViewCenter = (cur.StartPoint + t1transform).GetPoint2d();

                                            newVp.ViewCenter = (newVp.ViewCenter.GetPoint3d(0) + (Point3d.Origin - newVp.ViewTarget)).GetPoint2d();
                                            newVp.ViewCenter = newVp.ViewCenter.TransformBy(Matrix2d.Rotation(angle, Point2d.Origin));
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            using (BlockTableRecord ms = tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(HostApplicationServices.WorkingDatabase), OpenMode.ForWrite) as BlockTableRecord)
            {
                foreach (Curve cur in futureViewports)
                {
                    cur.Layer = name;
                    ms.AppendEntity(cur);
                    tr.AddNewlyCreatedDBObject(cur, true);
                }
            }         
        }
        private class LOCreateData
        {
            public LOCreateData(Transaction tr, ObjectId vpId)
            {
                using (Viewport viewport = tr.GetObject(vpId, OpenMode.ForRead, false, true) as Viewport)
                {
                    vclone = viewport.Clone() as Viewport;

                    //создаем переменную для контура видового экрана и получаем кривую контура

                    baseViewportId = viewport.Id;

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
                        //точка центра
                        Point3d centrPoint = viewport.ViewCenter.GetPoint3d(0);

                        //размеры видового экрана в модели
                        if (c.Bounds.HasValue)
                        {
                            vpx = c.Bounds.Value.MaxPoint.X - c.Bounds.Value.MinPoint.X;
                            vpy = c.Bounds.Value.MaxPoint.Y - c.Bounds.Value.MinPoint.Y;
                        }

                        Vector3d s1 = viewport.ViewCenter.GetPoint3d(0) - viewport.CenterPoint;
                        c.TransformBy(Matrix3d.Displacement(s1));
                        cStart.TransformBy(Matrix3d.Displacement(s1));

                        if (viewport.TwistAngle != 0)
                        {
                            angle = viewport.TwistAngle;
                            c.TransformBy(Matrix3d.Rotation(-angle, Vector3d.ZAxis, Point3d.Origin));
                            centrPoint = centrPoint.TransformBy(Matrix3d.Rotation(-angle, Vector3d.ZAxis, Point3d.Origin));
                            cStart.TransformBy(Matrix3d.Rotation(-angle, Vector3d.ZAxis, Point3d.Origin));
                        }

                        Vector3d s2 = Point3d.Origin - viewport.ViewTarget;

                        c.TransformBy(Matrix3d.Displacement(s2));
                        cStart.TransformBy(Matrix3d.Displacement(s2));
                        centrPoint = centrPoint.TransformBy(Matrix3d.Displacement(s2));

                        t1transform = centrPoint - c.StartPoint;
                    }
                }
            }


            public Curve c { get; set; } = null;
            public Point3d cStart { get; set; } = new Point3d();
            public Vector3d t1transform { get; set; } = new Vector3d();
            public ObjectId baseViewportId { get; set; } = ObjectId.Null;
            public double angle { get; set; } = 0;
            public double vpx { get; set; } = 100;
            public double vpy { get; set; } = 100;

            public double maxx { get; set; } = 0;
            public double maxy { get; set; } = 0;

            public Viewport vclone { get; set; } = null;
        }

   
        public ActionClass ActionClass;
        private Polyline Contour { get; set; } = null;
        private List<string> LayoutNames { get; set; } = new List<string>();
        private List<LayoutClass> LayoutClasses { get; set; } = new List<LayoutClass>();
        public class LayoutClass
        {        
            public LayoutClass(Layout layout)
            {
                Id = layout.Id;
                Name = layout.LayoutName;
            }         
            public ObjectId Id { get; set; }
            public string Name { get; set; }
        }       
    }
    public class ActionClass
    {     
        protected readonly object Lock = new object();
        public ActionClass(List<string> layoutNames)
        {
            LayoutNames = layoutNames;         
        }
        public void CheckLoName()
        {
            if (string.IsNullOrEmpty(LNCName)) LNC = false;
            int i = 0;
            string nname = LNCName;
            while (LayoutNames.Contains(nname))
            {
                i++;
                nname = LNCName + "_" + i;
            }
            try
            {
                using (LayoutManager lm = LayoutManager.Current)
                {
                    lm.CreateLayout(nname);
                    lm.DeleteLayout(nname);
                    LNC = true;
                }
            }
            catch
            {
                LNC = false;
            }
        }
        public List<string> LayoutNames { get; set; } = new List<string>();
        public string LNCName { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public bool LNC { get; set; } = false;
        public Action Action
        {
            get
            {
                lock (Lock)
                {
                    return _Action;
                }
            }
            set
            {
                lock (Lock)
                { 
                    _Action = value;
                }
            }
        }
        private Action _Action = Action.none;
    }
}
