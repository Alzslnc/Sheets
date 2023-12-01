using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
