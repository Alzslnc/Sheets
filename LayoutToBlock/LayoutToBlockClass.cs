using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Windows.Forms;
using static BaseFunction.BaseGetObjectClass;
using static BaseFunction.BaseBlockReferenceClass;
using Autodesk.AutoCAD.EditorInput;
using System.Collections.Generic;

namespace Sheets
{
    public class LayoutToBlockClass
    {
        public void Start()
        {
            string layoutName = LayoutManager.Current.CurrentLayout;

            if (layoutName == "Model")
            {
                MessageBox.Show("Надо находиться в пространстве листа");
                return;
            }


            if (!GetNumberFromName(layoutName, out int number))
            {
                MessageBox.Show("Не удается считать нумерацию листа" + Environment.NewLine +
                    "Нумерация должна быть в виде числа в конце названия или числа в скобках в конце названия");
                return;
            }

            if (!TryGetobjectId(out ObjectId id, typeof(BlockReference), "Выберите блок с текущим номером листа")) return;

            Editor ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
            PromptStringOptions promptOptions = new PromptStringOptions("Введите название атрибута, в котором находится номер листа")
            {
                AllowSpaces = true
            };
            PromptResult pResult = ed.GetString(promptOptions);

            if (pResult.Status != PromptStatus.OK) return;

            string attName = pResult.StringResult;

            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                BlockReference blockReference = tr.GetObject(id, OpenMode.ForRead, false, true) as BlockReference;

                if (blockReference != null)
                {
                    if (!blockReference.BlockReferenceGetAttribute(attName, out string blockNumberString) || !GetNumberFromName(blockNumberString, out int blockNumber))
                    {
                        MessageBox.Show("Не удалось считать норме страницы из блока");
                        tr.Commit();
                        return;
                    }

                    int difference = blockNumber - number;

                    ObjectId btrId;
                    if (blockReference.IsDynamicBlock) btrId = blockReference.DynamicBlockTableRecord;
                    else btrId = blockReference.BlockTableRecord;

                    BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead, false, true) as BlockTableRecord;

                    if (btr != null)
                    {
                        List<ObjectId> refIds = new List<ObjectId>();
                        foreach (ObjectId refId in btr.GetAnonymousBlockIds()) if (!refIds.Contains(refId)) refIds.Add(refId);
                        foreach (ObjectId refId in btr.GetBlockReferenceIds(false, true)) if (!refIds.Contains(refId)) refIds.Add(refId);

                        foreach (ObjectId refId in refIds)
                        {
                            BlockReference checkedRef = tr.GetObject(refId, OpenMode.ForRead, false, true) as BlockReference;
                            if (checkedRef == null) continue;
                            BlockTableRecord refBtr = tr.GetObject(checkedRef.OwnerId, OpenMode.ForRead, false, true) as BlockTableRecord;
                            if (refBtr == null || !refBtr.IsLayout) continue;

                            Layout layout = tr.GetObject(refBtr.LayoutId, OpenMode.ForRead, false, true) as Layout;
                            string lName = layout.LayoutName;

                            if (lName == "Model" || !GetNumberFromName(lName, out int layNumber)) continue;

                            layNumber += difference;

                            checkedRef.UpgradeOpen();

                            checkedRef.BlockReferenceChangeAttribute(tr, new Dictionary<string, string> { { attName, layNumber.ToString() } });
                        }
                    }
                }

                tr.Commit();
            }

        }
        private bool GetNumberFromName(string name, out int result)
        {
            string num = "";
            result = 0;

            for (int i = name.Length - 1; i >= 0; i--)
            {
                char c = name[i];
                if (c == ')' && i == name.Length - 1) continue;
                else if (char.IsDigit(c)) num = c + num;
                else break;
            }

            return int.TryParse(num, out result);
        }
    }
}
