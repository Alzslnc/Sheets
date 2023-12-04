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
    }
}
