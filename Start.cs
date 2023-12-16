using Autodesk.AutoCAD.Runtime;

namespace Sheets
{
    public class Start
    {
        [CommandMethod("Sheets_Create")]
        public void Sheets_Create_Command()
        {
            SheetsCreateClass sheetsCreateClass = new SheetsCreateClass();
            sheetsCreateClass.Sheets_Create();
        }
        [CommandMethod("Sheets_Link")]
        public void Sheets_Link_Command()
        {
            SheetLinkAcadClass sheetLinks = new SheetLinkAcadClass();
            sheetLinks.SheetLink();
        }
        [CommandMethod("Layout_Create")]
        public void Layout_Create_Command()
        {
            LayoutCreateAcadClass layoutCreateAcadClass = new LayoutCreateAcadClass();
            layoutCreateAcadClass.Start();
        }
    }
}
