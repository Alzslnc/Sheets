using Autodesk.AutoCAD.Runtime;

namespace Sheets
{
    public class Start
    {       
       
        [CommandMethod("Extents_Create")]
        public static void Extents_Create() => Program.ExtentsCreateClass.Create();
        [CommandMethod("Sheets_Create")]
        public static void Sheets_Create() => Program.SheetsCreateClass.Create();
        [CommandMethod("Layout_Create")]
        public static void Layout_Create() => Program.LayoutCreateClass.Create();

        [CommandMethod("Sheets_Settings")]
        public static void Sheets_Settings() => Program.SheetsSettingsClass.Start();
    }
}
