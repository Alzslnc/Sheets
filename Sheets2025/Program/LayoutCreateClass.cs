using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using BaseFunction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml;

namespace Sheets.Program
{
    internal class LayoutCreateClass
    {
        internal static void Create()
        {
            //если не находимся в модели то прерываем
            if (!Support.InModel(true)) return;
                    
            //выбираем блоки
            if (!BaseGetObjectClass.TryGetObjectsIds(out List<ObjectId> ids, typeof(BlockReference), $"Выберите блоки для создания листов.")) return;

            Dictionary<string, List<BlockReference>> blockList = new Dictionary<string, List<BlockReference>> ();

            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ids)
                {
                    //получаем блок
                    BlockReference reference = tr.GetObject(id, OpenMode.ForRead, false, true) as BlockReference;
                    if (reference == null) continue;

                    //считываем данные приложения
                    ResultBuffer values = reference.GetXDataForApplication(Names.LayoutCreateAppName);
                    if (values == null) continue;

                    //распределяем блоки по экранам, к которым они прицеплены
                    foreach (TypedValue typedValue in values)
                    {
                        if (typedValue.TypeCode == Convert.ToInt32(DxfCode.ExtendedDataHandle))
                        {
                            string handle = typedValue.Value.ToString();
                            if (string.IsNullOrEmpty(handle)) break;

                            //Handle h = 
                            if (blockList.ContainsKey(handle)) blockList[handle].Add(reference);
                            else blockList.Add(handle, new List<BlockReference> { reference });

                            break;                           
                        }
                    }
                }

                //проверяем сколько будет листов при отработке программы
                if (blockList.Sum(x => x.Value.Count) < (250 - LayoutManager.Current.LayoutCount))
                {
                    foreach (KeyValuePair<string, List<BlockReference>> keyValuePair in blockList)
                    {
                        //пробуем получить видовой экран
                        if (HostApplicationServices.WorkingDatabase.TryGetObjectId(new Handle(Convert.ToInt64(keyValuePair.Key, 16)), out ObjectId id))
                        { 
                            Viewport viewport = tr.GetObject(id, OpenMode.ForWrite, false, true) as Viewport;
                            if (viewport == null) continue;

                            if (!Create(viewport, keyValuePair.Value, tr)) return;
                        }
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Число видовых экранов превышает максимально допустимое для работы программы - 250");                 
                }              
            
                tr.Commit();
            }

        }

        private static bool Create(Viewport viewport, List<BlockReference> value, Transaction tr)
        {
            //название слоя для видовых экранов на новых листах
            string layerName = $"{Names.ViewportLayerName}_{viewport.Handle}_{DateTime.Now.ToString("HH:mm:ss dd.MM.yyyy")}";

            //открываем слой видового экрана и копируем его если его нет       
            LayerTable layerTable = tr.GetObject(HostApplicationServices.WorkingDatabase.LayerTableId, OpenMode.ForRead, false, true) as LayerTable;
            if (!layerTable.Has(layerName))
            {               
                LayerTableRecord layer = tr.GetObject(viewport.LayerId, OpenMode.ForWrite, false, true).Clone() as LayerTableRecord;
                layer.Name = layerName;
                layerTable.Add(layer);
                tr.AddNewlyCreatedDBObject(layer, true);
            }

            //создаем уникальный идентификатор для текущей команды и добавляем его в xdata
            string unique = Guid.NewGuid().ToString();
            viewport.XDataSet(Names.LayoutCreateAppName, new List<TypedValue> { new TypedValue(Convert.ToInt32(DxfCode.ExtendedDataAsciiString), unique) }, true);
            
            //получаем исходный лист через пространство видового экрана
            BlockTableRecord btr = tr.GetObject(viewport.OwnerId, OpenMode.ForRead, false, true) as BlockTableRecord;
            if (btr == null || !btr.IsLayout) return false;
            Layout layout = tr.GetObject(btr.LayoutId, OpenMode.ForRead, false, true) as Layout;
            if (layout == null) return false;

            //получаем название для листов
            string layoutName = "!_Created_" + layout.LayoutName;
            int i = 1;

            //определяем класс видового экрана
            RXClass viewportClass = RXClass.GetClass(typeof(Viewport));

            //исходные геометрические данные
            Matrix3d ViewportMatrix = viewport.ConvertToViewport();
            double Angle = viewport.TwistAngle;

            //создаем листы
            foreach (BlockReference block in value)
            {
                //создаем новое имя для листа
                string name;
                while (LayoutManager.Current.LayoutExists(name = $"{layoutName}({i++})")) continue;

                //создаем лист как копию основного
                try
                {
                    LayoutManager.Current.CloneLayout(layout.LayoutName, name, LayoutManager.Current.LayoutCount);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                { 
                    System.Windows.MessageBox.Show(ex.Message);
                    return false;
                }

                //октрываем новый лист
                Layout newLayout = tr.GetObject(LayoutManager.Current.GetLayoutId(name), OpenMode.ForRead, false, true) as Layout;

                //открываем пространство блока
                BlockTableRecord newBtr = tr.GetObject(newLayout.BlockTableRecordId, OpenMode.ForRead, false, true) as BlockTableRecord;

                //флаг остановки
                bool changed = false;

                //ищем видовой экран
                foreach (ObjectId id in newBtr)
                {
                    //определяем видовой экран
                    if (id.ObjectClass != viewportClass) continue;
                    Viewport newViewport = tr.GetObject(id, OpenMode.ForWrite, false, true) as Viewport;
                    if (newViewport == null) continue;

                    //определяем записаны ли в него данные
                    ResultBuffer typedValues = newViewport.GetXDataForApplication(Names.LayoutCreateAppName);
                    if (typedValues == null) continue;

                    //проверяем нужный ли видовой экран
                    foreach (TypedValue typedValue in typedValues)
                    {
                        if (typedValue.Value == unique)
                        {
                            //устанавливаем слой
                            newViewport.Layer = layerName;

                            //получаем вектор смещения на новое положение видового экрана
                            Vector3d toNewViewCenter = (cur.Center - StartPoint).RotateBy(Angle, Vector3d.ZAxis);
                            //получаем новое положение видового экрана
                            Point2d newCenter = newVp.ViewCenter + new Vector2d(toNewViewCenter.X, toNewViewCenter.Y);
                            if (cur.Angle != 0)
                            {
                                //получаем новое положение видового экрана с учетом разворота листа
                                newCenter = newCenter.TransformBy(Matrix2d.Rotation(-cur.Angle, Point2d.Origin));
                                //разворачиваем видовой экран по листу
                                newVp.TwistAngle -= cur.Angle;
                            }
                            //устанавливаем вид
                            newVp.ViewCenter = newCenter;

                            changed = true;
                            break;
                        }

                    }

                    if (changed) break;
                }
            }
                        
            return true;
        }

        private static void SetViewportPosition(BlockTableRecord newBtr, Transaction tr, RXClass viewportClass, BlockReference block, string unique, string layoutName, bool plot)
        {
            
        }
    }
}
