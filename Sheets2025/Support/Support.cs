using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        
    }
}
