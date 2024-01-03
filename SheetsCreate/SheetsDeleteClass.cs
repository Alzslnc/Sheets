using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using static BaseFunction.BaseGetObjectClass;

namespace Sheets
{
    public class SheetsDeleteClass
    {
        public void Start()
        {
            if (!TryGetKeywords(out string result, new List<string> { "ДA", "НET" }, "Удалить все вставленные схемы?")) return;

            bool deleteAll = false;
            
            if (result.Equals("ДA")) deleteAll = true;

            int deleted;
            int deletedReference;

            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                using (BlockTable bt = tr.GetObject(HostApplicationServices.WorkingDatabase.BlockTableId, OpenMode.ForRead) as BlockTable)
                {
                    Delete(tr, bt, deleteAll, "_sheets_", out deleted, out deletedReference);
                    Delete(tr, bt, deleteAll, "_sheets_", out int deleted2, out int deletedReference2);
                    deleted += deleted2;
                    deletedReference += deletedReference2;
                }
                tr.Commit();
            }

            System.Windows.Forms.MessageBox.Show("Удалено записей - " + deleted + "\n" + "Удалено блоков - " + deletedReference);
        }
        public void DeleteBlock(IniData data)
        {
            int deleted;
            int deletedReference;
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                using (BlockTable bt = tr.GetObject(HostApplicationServices.WorkingDatabase.BlockTableId, OpenMode.ForRead) as BlockTable)
                {
                    Delete(tr, bt, true, "_sheets_" + data.Layer, out deleted, out deletedReference);                     
                }
                tr.Commit();
            }
            System.Windows.Forms.MessageBox.Show("Удалено записей - " + deleted + "\n" + "Удалено блоков - " + deletedReference);
        }
        private void Delete(Transaction tr, BlockTable bt, bool deleteAll, string name, out int deletedObject, out int deletedReference)
        {
            deletedObject = 0;
            deletedReference = 0;
            foreach (ObjectId id in bt)
            {
                if (id.IsErased) continue;
                using (BlockTableRecord btr = tr.GetObject(id, OpenMode.ForRead) as BlockTableRecord)
                {
                    if (btr.Name.Length >= name.Length && btr.Name.Substring(0, name.Length).Equals(name))
                    {
                        using (ObjectIdCollection collection = btr.GetBlockReferenceIds(true, true))
                        {
                            if (collection.Count > 0)
                            {
                                if (deleteAll)
                                {
                                    foreach (ObjectId refId in collection)
                                    {
                                        if (refId.IsErased) continue;
                                        using (BlockReference bref = tr.GetObject(refId, OpenMode.ForWrite) as BlockReference)
                                        {
                                            bref?.Erase();
                                            deletedReference++;
                                        }
                                    }
                                }
                                else continue;
                            }
                            btr.UpgradeOpen();
                            btr.Erase();
                            deletedObject++;
                        }
                    }
                }
            }
        }
    }
}
