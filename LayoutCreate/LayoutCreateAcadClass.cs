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
            //проверяем что мы находимся в модели
            if (LayoutManager.Current.CurrentLayout != "Model")
            {
                MessageBox.Show("Выбрать область или направляющую линию можно только в модели");
                return;            
            }

            //пробуем получить область/кривую
            if (!TryGetObjectsIds(out List<ObjectId> PolyIds, typeof(Polyline), "Выберите отображаемые области / направляющие")) return;

            //получаем список листов
            GetLayouts();

            if (LayoutClasses.Count == 0) return;

            //создаем класс передачи данных
            ActionClass = new ActionClass(LayoutNames);

            //во втором потоке открываем форму и получаем параметры от пользователя
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
                        
            if (ActionClass.Action != Action.Ok || string.IsNullOrEmpty(ActionClass.Result)) return;

            #region первичная обработка исходных данных и получение дополнительных

            //переменная валидности исходной области, используется если раскладка идет по области
            bool correct = true;
            //считываем метод обработки
            bool onLine = Settings.Default.LC_OnLine;

            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                foreach (ObjectId PolyId in PolyIds)
                {
                    //получаем исходный контур
                    Polyline poly = tr.GetObject(PolyId, OpenMode.ForRead, false, true) as Polyline;
                    //если раскладка по области проверяем контур
                    if (!onLine)
                    {
                        correct = CheckPoly(poly, true);                        
                    }
                    //если контур корректен то получаем его клон и дальше будем работать с ним
                    if (correct)
                    {
                        Polyline Contour = poly.Clone() as Polyline;
                        Contour.Elevation = 0;
                        Contours.Add(Contour);
                    }
                    else
                    {
                        //MessageBox.Show("Выбраны некорректные контуры (контуры должны быть замкнуты и не должны иметь самопересечения)");
                        tr.Commit();
                        return;
                    }
                }
                tr.Commit();
            }            

            //если контур не корректен прекращщаем работу
            if (!correct) return;

            //даем возможность выбрать внетренние контура
            if (!Settings.Default.LC_OnLine && TryGetObjectsIds(out List<ObjectId> innerContourIds, typeof(Polyline), "Можете выбрать внутренние контуры области"))
            {
                using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId PolyId in innerContourIds)
                    {
                        if (PolyIds.Contains(PolyId)) continue;
                        Polyline poly = tr.GetObject(PolyId, OpenMode.ForRead, false, true) as Polyline;
                        if (!CheckPoly(poly, true))
                        {
                            MessageBox.Show("Выбраны некорректные внутренние контуры (контуры должны быть замкнуты и не должны иметь самопересечения)");
                            tr.Commit();
                            return;
                        }
                        Polyline Contour = poly.Clone() as Polyline;
                        Contour.Elevation = 0;
                        InnerContours.Add(Contour);
                    }
                    tr.Commit();
                }
            }

            //переносим объекты к началу координат что бы точнее работали методы 
            ReplaceToOriginPoint();

            if (!onLine)
            {
                if (!CreateContourDatas())
                {
                    MessageBox.Show("Внешние контура не должны пересекаться");                
                    return;
                }
            }                

            //получаем значение перехлеста
            Overlap = Settings.Default.LC_Overlap / 100;
           
            //переходим на выбранный лист
            if (LayoutManager.Current.CurrentLayout != ActionClass.Result) LayoutManager.Current.CurrentLayout = ActionClass.Result;

            //пробуем получить видовой экран            
            if (!TryGetobjectId(out ObjectId vpId, typeof(Viewport), "Выберите базовый видовой экран"))
            {
                //возвращаемся обратно на модель
                if (LayoutManager.Current.CurrentLayout != "Model") LayoutManager.Current.CurrentLayout = "Model";           
                return;
            }

            ViewportId = vpId;

            //получаем список слоев и создаем название под наши новые видовые экраны если требуелся 
            List<string> LayerNames = GetLayerNames();

            Name = "!_New_Viewport_set_";

            int i = 1;
            while (LayerNames.Contains(Name + i)) i++;

            Name += i;

            LayerNew(Name);

            

            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                bool create = true;
                //получаем данные видового экрана и заполняем требуемые поля
                LayoutData ld = new LayoutData(tr, this);
                if (ld.Exist)
                {
                    //определяем нахлест как процент от размера рамки
                    if (onLine) Overlap *= ld.ViewportContour.Bounds.Value.MaxPoint.X - ld.ViewportContour.Bounds.Value.MinPoint.X;


                    //помечаем выбранный видовой экран
                    if (Settings.Default.LC_LOCreate)
                    {
                        XDataSet(ViewportId, "LayoutCreate", new List<TypedValue>
                            {
                                new TypedValue(Convert.ToInt32(DxfCode.ExtendedDataAsciiString), "1"),
                            }, true);
                    }

                    //расходимся на ветки в зависимости от метода расстановки экранов

                    if (onLine)
                    {
                        foreach (Polyline contour in Contours) CreateOnLine(ld, contour);
                    }
                    else
                    { 
                        foreach (ContourData contourData in ContourDatas) CreateOnArea(ld, contourData);
                    }

                    create = CreateLayoutsAndContours(tr);

                    //снимаем отметку с выбранного видового экрана
                    if (Settings.Default.LC_LOCreate) XDataClear(vpId, "LayoutCreate", null);
                }
                ld.ViewportContour?.Dispose();
                if (create) tr.Commit();
                else
                {
                    MessageBox.Show("Число листов в одном файле не может превышать 255 штук, не удалось создать листы");
                    tr.Abort();
                }                    
            }
            #endregion

            //возвращаемся обратно на модель
            if (LayoutManager.Current.CurrentLayout != "Model") LayoutManager.Current.CurrentLayout = "Model";
        }
        private void ThreadForm()
        {
            LayoutCreateForm form = new LayoutCreateForm(ActionClass);
            form.ShowDialog();
            if (form.DialogResult == DialogResult.OK) ActionClass.Action = Action.Ok;
            else ActionClass.Action = Action.Cancel;
        }
        private bool CreateContourDatas()
        {
            foreach (Polyline contour in Contours)
            {
                foreach (Polyline contour2 in Contours)
                {
                    if (contour2.Equals(contour)) continue;
                    using (Point3dCollection coll = new Point3dCollection())
                    {
                        contour.IntersectWith(contour2, Intersect.OnBothOperands, coll, IntPtr.Zero, IntPtr.Zero);
                        if (coll.Count > 0) return false;   
                    }
                }
            }

            foreach (Polyline contour in Contours)
            {
                ContourData data = new ContourData { Contour = contour };
                foreach (Polyline contour2 in InnerContours)
                {                  
                    using (Point3dCollection coll = new Point3dCollection())
                    {
                        contour.IntersectWith(contour2, Intersect.OnBothOperands, coll, IntPtr.Zero, IntPtr.Zero);
                        if (coll.Count > 0)
                        {
                            data.InnerContours.Add(contour2);
                            continue;
                        }
                        PositionType innerPosition = contour2.StartPoint.GetPositionType(contour);
                        if (innerPosition == PositionType.inner)
                        {
                            data.InnerContours.Add(contour2);
                        }
                    }
                }
                ContourDatas.Add(data);
            }

            return true;
        }
        private void ReplaceToOriginPoint()
        { 
            Extents3d extents = new Extents3d();

            foreach (Polyline polyline in Contours)
            {
                if (polyline.Bounds.HasValue) extents.AddExtents(polyline.Bounds.Value);
            }

            foreach (Polyline polyline in InnerContours)
            {
                if (polyline.Bounds.HasValue) extents.AddExtents(polyline.Bounds.Value);
            }

            ToOridginPoint = Point3d.Origin - extents.MinPoint.Z0();

            foreach (Polyline polyline in Contours) polyline.TransformBy(Matrix3d.Displacement(ToOridginPoint));
            foreach (Polyline polyline in InnerContours) polyline.TransformBy(Matrix3d.Displacement(ToOridginPoint));
        }
        private bool CheckPoly(Polyline poly, bool message)
        {
            if (!poly.Closed)
            {
                if (message) MessageBox.Show("Область не замкнута");
                return false;            
            }
            if (poly.Area == 0)
            {
                if (message) MessageBox.Show("Область не корректна");
                return false;
            }
            try
            {
                using (MPolygon polygon = new MPolygon())
                {
                    polygon.AppendLoopFromBoundary(poly, true, Tolerance.Global.EqualPoint);
                    return true;
                }
            }
            catch
            {
                if (message) MessageBox.Show("Область не корректна");
                return false;
            }
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
        private void CreateOnArea(LayoutData ld, ContourData contourData)
        {
            if (contourData.Contour == null) return;
            Polyline contour = contourData.Contour;
            //получаем вектора раскладки экранов
            Vector3d vx = Vector3d.XAxis.TransformBy(Matrix3d.Rotation(-Angle, Vector3d.ZAxis, Point3d.Origin));
            Vector3d vy = Vector3d.YAxis.TransformBy(Matrix3d.Rotation(-Angle, Vector3d.ZAxis, Point3d.Origin));

            if (Settings.Default.LC_LOCreate)
            {
                XDataSet(ViewportId, "LayoutCreate", new List<TypedValue>
                        {
                            new TypedValue(Convert.ToInt32(DxfCode.ExtendedDataAsciiString), "1"),
                        }, true);
            }

            //получаем начальную точку раскладки и габариты контура 
            Point3d Start;
            double maxx;
            double maxy;                
            if (Angle != 0)
            {
                contour.TransformBy(Matrix3d.Rotation(Angle, Vector3d.ZAxis, Point3d.Origin));
                Start = contour.Bounds.Value.MinPoint;
                maxx = contour.Bounds.Value.MaxPoint.X - contour.Bounds.Value.MinPoint.X;
                maxy = contour.Bounds.Value.MaxPoint.Y - contour.Bounds.Value.MinPoint.Y;
                contour.TransformBy(Matrix3d.Rotation(-Angle, Vector3d.ZAxis, Point3d.Origin));
                Start = Start.TransformBy(Matrix3d.Rotation(-Angle, Vector3d.ZAxis, Point3d.Origin));

                ld.ViewportContour.TransformBy(Matrix3d.Rotation(Angle, Vector3d.ZAxis, Point3d.Origin));
                ld.CStart = ld.ViewportContour.Bounds.Value.MinPoint;
                ld.ViewportContour.TransformBy(Matrix3d.Rotation(-Angle, Vector3d.ZAxis, Point3d.Origin));
                ld.CStart = ld.CStart.TransformBy(Matrix3d.Rotation(-Angle, Vector3d.ZAxis, Point3d.Origin));
            }
            else
            {
                ld.CStart = ld.ViewportContour.Bounds.Value.MinPoint;
                Start = contour.Bounds.Value.MinPoint;
                maxx = contour.Bounds.Value.MaxPoint.X - contour.Bounds.Value.MinPoint.X;
                maxy = contour.Bounds.Value.MaxPoint.Y - contour.Bounds.Value.MinPoint.Y;
            }

            double currx = 0;
            double curry = 0;

            while (currx < maxx && curry < maxy)
            {
                Curve vc = ld.ViewportContour.Clone() as Curve;

                vc.TransformBy(Matrix3d.Displacement(Start - ld.CStart));
                vc.TransformBy(Matrix3d.Displacement(vx * currx));
                vc.TransformBy(Matrix3d.Displacement(vy * curry));

                using (Point3dCollection coll = new Point3dCollection())
                {
                    vc.IntersectWith(contour, Intersect.OnBothOperands, coll, IntPtr.Zero, IntPtr.Zero);
                    if (coll.Count == 0)
                    {
                        PositionType position = vc.StartPoint.GetPositionType(contour);
                        if (position != PositionType.inner) vc?.Dispose();
                        else
                        {
                            int inner = 0;
                            bool intersect = false;
                            foreach (Polyline innerContour in contourData.InnerContours)
                            {                                   
                                using (Point3dCollection innerColl = new Point3dCollection())
                                {
                                    vc.IntersectWith(innerContour, Intersect.OnBothOperands, innerColl, IntPtr.Zero, IntPtr.Zero);
                                    if (innerColl.Count > 0)
                                    {
                                        intersect = true;
                                        break;
                                    }

                                    PositionType innerPosition = vc.StartPoint.GetPositionType(innerContour);
                                    if (innerPosition == PositionType.inner) inner++;
                                }
                            }  
                            if (!intersect && inner %2 > 0.1) vc?.Dispose();
                        }
                    }
                    if (!vc.IsDisposed)
                    {
                        FutureViewports.Add(vc);
                    }                       
                    curry += ld.ViewportHeight * (1 - Overlap);
                    if (curry > maxy)
                    {
                        curry = 0;
                        currx += ld.ViewportWidght * (1 - Overlap);
                    }
                }
            }               
            
        }
        private void CreateOnLine(LayoutData ld, Polyline contour)
        {                        
            Point3d cCenter = ld.ViewportContour.Bounds.Value.MinPoint + (ld.ViewportContour.Bounds.Value.MaxPoint - ld.ViewportContour.Bounds.Value.MinPoint) / 2;

            //дистанция до последней рамки
            double curDistance = 0;
            //длина полилинии
            double contLongth = contour.GetLength();
                                     
            //определение четности рамки для выбора нужной
            bool chet = false;

            //пока рамки в пределах полилинии
            while (curDistance < contLongth)
            { 
                //получаем клон рамки
                Curve curve = ld.ViewportContour.Clone() as Curve;

                //помещаем на последнее полученное пересечение
                curve.TransformBy(Matrix3d.Displacement(contour.GetPointAtDist(curDistance) - cCenter));
                                        
                using (Point3dCollection coll = new Point3dCollection())
                {
                    //ищем пересечения рамки и поилинии
                    curve.IntersectWith(contour, Intersect.OnBothOperands, coll, IntPtr.Zero, IntPtr.Zero);
                    //если пересечений нет то рамка больше полилинии и полилиния внутри, оставляем одну рамку и останавливаем
                    if (coll.Count == 0)
                    {
                        FutureViewports.Add(curve);
                        break;
                    }   
                            
                    //убираем все пересечения до центра рамки
                    for (int i = coll.Count - 1; i >= 0; i--)
                    {
                        coll[i] = contour.GetClosestPointTo(coll[i], false);
                        Point3d p = coll[i];
                        if (contour.GetDistAtPoint(p) <= curDistance) coll.RemoveAt(i);
                    }

                    //если после центра рамки пересечений нет то значит рамка последняя, добавляем ее и останавливаем
                    if (coll.Count == 0)
                    {
                        FutureViewports.Add(curve);
                        break;
                    }
                    //если пересечений несколько то сортируем вдоль полилинии
                    else if (coll.Count > 1) coll.SortOnCurve(contour); 

                    //получаем расстояние до ближайшего пересечения после цетра рамки    
                    curDistance = contour.GetDistAtPoint(coll[0]) - Overlap; ;

                    //если рамка четная то добавляем ее, если не то пропускаем
                    if (chet)
                    {
                        FutureViewports.Add(curve);
                        chet = false;
                    }
                    else
                    {       
                        curve?.Dispose();
                        chet = true;
                    }       
                }
            }   
            
        }

        private bool CreateLayoutsAndContours(Transaction tr)
        {
            if (Settings.Default.LC_LOCreate)
            {
                if (FutureViewports.Count > (255 - LayoutManager.Current.LayoutCount))
                {                    
                    return false;
                }

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
                    foreach (Curve cur in FutureViewports)
                    {
                        while (LayoutNames.Contains(cloname))
                        {
                            k++;
                            cloname = loname + "(" + k + ")";
                        }
                        LayoutNames.Add(cloname);

                        try
                        {
                            lm.CloneLayout(ActionClass.Result, cloname, lm.LayoutCount);
                        }
                        catch 
                        {                           
                            return false;
                        }

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
                                            newVp.Layer = Name;
                                            Vector3d toNewViewCenter = (cur.StartPoint - StartPoint).RotateBy(Angle, Vector3d.ZAxis);
                                            newVp.ViewCenter += new Vector2d(toNewViewCenter.X, toNewViewCenter.Y);
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
                foreach (Curve cur in FutureViewports)
                {
                    cur.TransformBy(Matrix3d.Displacement(-ToOridginPoint));
                    cur.Layer = Name;
                    ms.AppendEntity(cur);
                    tr.AddNewlyCreatedDBObject(cur, true);
                }
            }

            return true;
        }
        

        private Vector3d ToOridginPoint { get; set; } = new Vector3d(0, 0, 0);
        private double Angle { get; set; } = 0;
        private Point3d StartPoint { get; set; } = Point3d.Origin;
        private Point3d ViewCenter { get; set; } = Point3d.Origin;
        private Matrix3d ViewportMatrix { get; set; } = new Matrix3d();
        private ActionClass ActionClass { get; set; }
        private List<Polyline> Contours { get; set; } = new List<Polyline>();
        private List<Polyline> InnerContours { get; set; } = new List<Polyline>();
        private List<ContourData> ContourDatas { get; set; } = new List<ContourData>();
        private List<Curve> FutureViewports { get; set; } = new List<Curve>();
        private List<string> LayoutNames { get; set; } = new List<string>();
        private string Name { get; set; } = "";
        private ObjectId ViewportId { get; set; } = ObjectId.Null;
        private double Overlap { get; set; } = 0;
        private List<LayoutClass> LayoutClasses { get; set; } = new List<LayoutClass>();
        private class LayoutData
        {
            public LayoutData(Transaction tr, LayoutCreateAcadClass layoutCreateAcad)
            {
                //получаем данные видового экрана
                using (Viewport viewport = tr.GetObject(layoutCreateAcad.ViewportId, OpenMode.ForRead, false, true) as Viewport)
                {
                    layoutCreateAcad.ViewportMatrix = viewport.ConvertToViewport();

                    layoutCreateAcad.Angle = viewport.TwistAngle;

                    if (viewport.NonRectClipEntityId != null && viewport.NonRectClipEntityId != ObjectId.Null)
                    {
                        ViewportContour = tr.GetObject(viewport.NonRectClipEntityId, OpenMode.ForRead).Clone() as Curve;
                    }
                    else
                    {
                        Extents3d ex = new Extents3d();
                        ex.AddPoint(viewport.CenterPoint - Vector3d.XAxis * 0.5 * viewport.Width - Vector3d.YAxis * 0.5 * viewport.Height);
                        ex.AddPoint(viewport.CenterPoint + Vector3d.XAxis * 0.5 * viewport.Width + Vector3d.YAxis * 0.5 * viewport.Height);
                        ViewportContour = CreatePolyline(ex);
                    }

                    if (ViewportContour == null || !ViewportContour.Bounds.HasValue) return;

                    //размеры видового экрана в модели
                    ViewportWidght = (ViewportContour.Bounds.Value.MaxPoint.X - ViewportContour.Bounds.Value.MinPoint.X) / viewport.CustomScale;
                    ViewportHeight = (ViewportContour.Bounds.Value.MaxPoint.Y - ViewportContour.Bounds.Value.MinPoint.Y) / viewport.CustomScale;

                    ViewportContour.TransformBy(layoutCreateAcad.ViewportMatrix.Inverse());
                    ViewportContour.TransformBy(Matrix3d.Displacement(layoutCreateAcad.ToOridginPoint));


                    layoutCreateAcad.StartPoint = ViewportContour.StartPoint;
                    layoutCreateAcad.ViewCenter = viewport.ViewCenter.GetPoint3d(0).TransformBy(layoutCreateAcad.ViewportMatrix.Inverse()).TransformBy(Matrix3d.Displacement(layoutCreateAcad.ToOridginPoint));

                    T1transform = layoutCreateAcad.ViewCenter - ViewportContour.StartPoint;

                    Exist = true;
                }
            }
            public Curve ViewportContour { get; set; } = null;
            public Point3d CStart { get; set; } = new Point3d();
            public Vector3d T1transform { get; set; } = new Vector3d();
            public ObjectId BaseViewportId { get; set; } = ObjectId.Null;
            public double ViewportWidght { get; set; } = 100;
            public double ViewportHeight { get; set; } = 100;
            public bool Exist { get; } = false;
        }
        private class ContourData
        {
            public Polyline Contour { get; set; } = null;
            public List<Polyline> InnerContours { get; set; } = new List<Polyline> ();        
        }
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
