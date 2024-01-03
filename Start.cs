using Autodesk.AutoCAD.Runtime;

namespace Sheets
{
    public class Start
    {
        [CommandMethod("Sheets_Create")]
        public void Sheets_Create_Command()
        {
            SheetsCreateClass startClass = new SheetsCreateClass();
            startClass.Start();
        }
        [CommandMethod("Sheets_Delete")]
        public void Sheets_Delete_Command()
        {
            SheetsDeleteClass startClass = new SheetsDeleteClass();
            startClass.Start();
        }
        [CommandMethod("Sheets_Link")]
        public void Sheets_Link_Command()
        {
            SheetLinkAcadClass startClass = new SheetLinkAcadClass();
            startClass.Start();
        }
        [CommandMethod("Layout_Create")]
        public void Layout_Create_Command()
        {
            LayoutCreateAcadClass startClass = new LayoutCreateAcadClass();
            startClass.Start();
        }
    }
}
