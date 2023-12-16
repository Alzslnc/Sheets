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
using static BaseFunction.TextBounds;
using static BaseFunction.PositionAndIntersections;
using System.Runtime.ConstrainedExecution;


namespace Sheets
{
    public class LayoutCreateAcadClass
    {
        public void Start()
        {
            double pp = 0;

            if (!TryGetobjectId(out ObjectId PolyId, typeof(Polyline), "Выберите отображаемую область")) return;

            bool fail = false;

            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                using (Polyline poly = tr.GetObject(PolyId, OpenMode.ForRead, false, true) as Polyline)
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
                    if (!fail)
                    {
                        Contour = poly.Clone() as Polyline;
                        Contour.Elevation = 0;
                    }
                }
                tr.Commit();
            }

            if (fail) return;

            GetLayouts();
            
            if (LayoutClasses.Count == 0) return;

            ActionClass = new ActionClass(LayoutNames);

            Thread thread = new Thread(ThreadForm);
            thread.Start(); 

            while(ActionClass.Action == Action.none) Thread.Sleep(200);

            if (ActionClass.Action != Action.Ok) return;

            LayoutManager.Current.CurrentLayout = ActionClass.Result;

            if (!TryGetobjectId(out ObjectId vpId, typeof(Viewport), "Выберите базовый видовой экран")) return;

            List<string> LayerNames = GetLayerNames();

            string name = "!_New_Viewport_set_";

            int i = 1;
            while (LayerNames.Contains(name + i)) i++;

            name += i;

            LayerNew(name);

            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                Curve c = null;
                Point3d cStart = new Point3d();
                Vector3d t1transform = new Vector3d();
                ObjectId baseViewportId = ObjectId.Null;
                double angle = 0;
                double vpx = 100;
                double vpy = 100;

                double maxx = 0;
                double maxy = 0;

                Viewport vclone = null;

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

                        t1transform = viewport.ViewCenter.GetPoint3d(0) - c.StartPoint;

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
                            //t1transform = t1transform.TransformBy(Matrix3d.Rotation(-angle, Vector3d.ZAxis, viewport.ViewCenter.GetPoint3d(0)));
                            c.TransformBy(Matrix3d.Rotation(-viewport.TwistAngle, Vector3d.ZAxis, Point3d.Origin));
                            cStart.TransformBy(Matrix3d.Rotation(-viewport.TwistAngle, Vector3d.ZAxis, Point3d.Origin));
                        }

                        Vector3d s2 = Point3d.Origin - viewport.ViewTarget;
                        c.TransformBy(Matrix3d.Displacement(s2));
                        cStart.TransformBy(Matrix3d.Displacement(s2));

                       
                    }     
                }
                if (c == null || baseViewportId == ObjectId.Null || !c.Bounds.HasValue)
                {
                    tr.Commit();
                    return;
                }

                Vector3d vx = Vector3d.XAxis.TransformBy(Matrix3d.Rotation(-angle, Vector3d.ZAxis, Point3d.Origin));
                Vector3d vy = Vector3d.YAxis.TransformBy(Matrix3d.Rotation(-angle, Vector3d.ZAxis, Point3d.Origin));

                XDataSet(baseViewportId, "LayoutCreate", new List<TypedValue>
                            {
                                new TypedValue(Convert.ToInt32(DxfCode.ExtendedDataAsciiString), "1"),
                            }, true);

                Point3d Start;                

                if (angle != 0)
                {
                    Contour.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin));                   
                    Start = Contour.Bounds.Value.MinPoint;
                    maxx = Contour.Bounds.Value.MaxPoint.X - Contour.Bounds.Value.MinPoint.X;
                    maxy = Contour.Bounds.Value.MaxPoint.Y - Contour.Bounds.Value.MinPoint.Y;
                    Contour.TransformBy(Matrix3d.Rotation(-angle, Vector3d.ZAxis, Point3d.Origin));
                    Start = Start.TransformBy(Matrix3d.Rotation(-angle, Vector3d.ZAxis, Point3d.Origin));

                    c.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin));
                    cStart = c.Bounds.Value.MinPoint;
                    c.TransformBy(Matrix3d.Rotation(-angle, Vector3d.ZAxis, Point3d.Origin));
                    cStart = cStart.TransformBy(Matrix3d.Rotation(-angle, Vector3d.ZAxis, Point3d.Origin));
                }
                else
                {
                    cStart = c.Bounds.Value.MinPoint;
                    Start = Contour.Bounds.Value.MinPoint;
                    maxx = Contour.Bounds.Value.MaxPoint.X - Contour.Bounds.Value.MinPoint.X;
                    maxy = Contour.Bounds.Value.MaxPoint.Y - Contour.Bounds.Value.MinPoint.Y;                   
                }

                List<Curve> futureViewports = new List<Curve>();
                
                double currx = 0;
                double curry = 0;               

                while (currx < maxx && curry < maxy)
                {
                    Curve vc = c.Clone() as Curve;

                    vc.TransformBy(Matrix3d.Displacement(Start - cStart));
                    vc.TransformBy(Matrix3d.Displacement(vx * currx));
                    vc.TransformBy(Matrix3d.Displacement(vy * curry));

                    using (Point3dCollection coll = new Point3dCollection())
                    {
                        vc.IntersectWith(Contour, Intersect.OnBothOperands, coll, IntPtr.Zero, IntPtr.Zero);
                        if (coll.Count == 0 )
                        {
                            PositionType position = vc.StartPoint.GetPositionType(Contour);
                            if (position != PositionType.inner)
                            {
                                vc?.Dispose();                               
                            }                               
                        }
                        if (vc.IsDisposed)
                        {
                            curry += vpy * (1 - pp) * 0.2;
                            if (curry > maxy)
                            {
                                curry = 0;
                                currx += vpx * (1 - pp);
                            }
                        }
                        else
                        {
                            futureViewports.Add(vc);
                            curry += vpy * (1 - pp);
                            if (curry > maxy)
                            {
                                curry = 0;
                                currx += vpx * (1 - pp);
                            }
                        }
                    }
                }

                string loname = "NVS" + i;
                int k = 1;
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
                                            Point2d point = vclone.ViewCenter;
                                            newVp.Layer = name;
                                            newVp.ViewCenter = (cur.StartPoint + t1transform).GetPoint2d();
                                        }
                                        break;
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
                        ms.AppendEntity(cur);
                        tr.AddNewlyCreatedDBObject(cur, true);
                    
                    }                
                }

                    XDataClear(baseViewportId, "LayoutCreate", null);

                c?.Dispose();
                tr.Commit();
            }          
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
        public List<string> LayoutNames { get; set; } = new List<string>();
        public string Result { get; set; } = string.Empty;
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
