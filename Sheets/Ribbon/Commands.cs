using Autodesk.AutoCAD.Runtime;
using BaseFunction;
using System.Collections.Generic;

namespace Sheets.Ribbon
{
    public class Commands
    {
        public static List<CommandGroup> CommandClasses = new List<CommandGroup>
        {
            new CommandGroup("nCommand", "Схема листов",
                   new CommandItem("Создать схемы","Sheets_Create","Создает схему листов.",                   
                        () => { Program.ExtentsCreateClass.Create(); })
            ),
            new CommandGroup("nCommand", "Схема листов",
                   new CommandItem("Создать границы","Extents_Create","Создает границы для последующего создания видовых экранов.",
                        () => { Program.SheetsCreateClass.Create(); })
            ),
            new CommandGroup("nCommand", "Схема листов",
                   new CommandItem("Создать листы","Layout_Create","Создает листы на основе выбранных в модели границ.",
                        () => { Program.LayoutCreateClass.Create(); })
            ),

            new CommandGroup("nCommand", "Схема листов",
                   new CommandItem("Настройки","Sheets_Settings","Настройки программы.",
                        () => { Program.SheetsSettingsClass.Start(); })
            ),

        };
    }
}
