using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using BaseFunction;
using Sheets.App;
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
                    ResultBuffer values = reference.GetXDataForApplication(AppConstants.LayoutCreateAppName);
                    if (values == null) continue;

                    //распределяем блоки по экранам, к которым они прицеплены
                    foreach (TypedValue typedValue in values)
                    {
                        if (typedValue.TypeCode == Convert.ToInt32(DxfCode.ExtendedDataHandle))
                        {
                            string handle = typedValue.Value.ToString();
                            if (string.IsNullOrEmpty(handle)) break;

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
            List<(double, BlockReference)> order = new List<(double, BlockReference)>();

            foreach (BlockReference blockReference in value)
            {
                if (blockReference.BlockReferenceGetAttribute(AppConstants.BlockReferenceNumber, out string result) && double.TryParse(result.Replace(",", "."), out double doubleResult))
                {
                    order.Add((doubleResult, blockReference));
                }
            }

            order = order.OrderBy(x => x.Item1).ToList();

            //название слоя для видовых экранов на новых листах
            string layerName = $"{AppConstants.ViewportLayerName}_{viewport.Handle}_{DateTime.Now.ToString("HH.mm.ss dd.MM.yyyy")}";

            //открываем слой видового экрана и копируем его если его нет       
            LayerTable layerTable = tr.GetObject(HostApplicationServices.WorkingDatabase.LayerTableId, OpenMode.ForWrite, false, true) as LayerTable;
            if (!layerTable.Has(layerName))
            {
                LayerTableRecord layer = tr.GetObject(viewport.LayerId, OpenMode.ForWrite, false, true).Clone() as LayerTableRecord;
                layer.Name = layerName;
                layerTable.Add(layer);
                tr.AddNewlyCreatedDBObject(layer, true);
            }

            //создаем уникальный идентификатор для текущей команды и добавляем его в xdata
            string unique = Guid.NewGuid().ToString();
            viewport.XDataSet(AppConstants.LayoutCreateAppName, new List<TypedValue> { new TypedValue(Convert.ToInt32(DxfCode.ExtendedDataAsciiString), unique) }, true);

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


            using (Curve curve = viewport.GetViewportCurveInModel(tr, false, out _, out _, out Point3d center, out Matrix3d matrix))
            {
                Point3d viewOrigin = viewport.ViewCenter.GetPoint3d(0).TransformBy(matrix.Inverse());

                //создаем листы
                foreach ((double, BlockReference) val in order)
                {
                    BlockReference block = val.Item2;
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
                        ResultBuffer typedValues = newViewport.GetXDataForApplication(AppConstants.LayoutCreateAppName);
                        if (typedValues == null) continue;


                        //проверяем нужный ли видовой экран
                        foreach (TypedValue typedValue in typedValues)
                        {
                            if (typedValue.Value.ToString() == unique)
                            {
                                //устанавливаем слой
                                newViewport.Layer = layerName;

                                //разворачиваем
                                if (block.Rotation != 0)
                                {                                                                    
                                    //разворачиваем видовой экран по блоку
                                    newViewport.TwistAngle -= block.Rotation;
                                }

                                //устанавливаем вид
                                newViewport.ViewTarget = block.Position;
                                newViewport.ViewCenter = Point2d.Origin;
                                                                 
                                changed = true;

                                break;
                            }

                        }

                        if (changed) break;
                    }
                }

                return true;
            }
        }
    }
}
