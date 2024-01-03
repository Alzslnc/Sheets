using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using static BaseFunction.BaseGetObjectClass;
using static BaseFunction.F;

namespace Sheets
{
    public class SheetLinkAcadClass
    {
        public void Start()
        {
            if (!TryGetobjectId(out ObjectId brId, typeof(BlockReference), "Выберите связываемый блок")) return;
            if (!TryGetobjectId(out ObjectId vpId, typeof(Viewport), "Выберите связываемый видовой экран")) return;

            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                using (Viewport vp = tr.GetObject(vpId, OpenMode.ForRead, false, true) as Viewport)
                {
                    XDataSet(brId, "SheetsOnLayouts", new List<TypedValue>
                    {
                        new TypedValue(Convert.ToInt32(DxfCode.ExtendedDataHandle), vp.Handle),
                    }, true);
                    System.Windows.Forms.MessageBox.Show("Блок связан с видовым экраном, что бы обновить блок запустите команду \"Схема листов\"");
                }
                tr.Commit();
            }           
        }

    }
}
