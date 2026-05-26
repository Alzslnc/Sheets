using Autodesk.AutoCAD.Runtime;
using BaseFunction;
using System.Collections.Generic;

namespace DProgramV2024.Ribbon
{
    public class ExampleRibbon : IExtensionApplication
    {
        public void Initialize()
        {
            Buttons();
            CountMenus();
        }
        public void Terminate() { }

        private void Buttons()
        {
            StartEvents startEvents = new StartEvents();

            startEvents.Buttons.Add(new Button("nCommand", "Схема листов",
                new List<ButtonCommand> { new ButtonCommand("Sheets_Create", "Создать схемы", "Создает схему листов."), }));            
            startEvents.Buttons.Add(new Button("nCommand", "Схема листов",
                new List<ButtonCommand> { new ButtonCommand("Extents_Create", "Создать границы", "Создает границы для последующего создания видовых экранов."), }));
            startEvents.Buttons.Add(new Button("nCommand", "Схема листов",
               new List<ButtonCommand> { new ButtonCommand("Layout_Create", "Создать листы", "Создает листы на основе выбранных в модели границ."), }));


            startEvents.Buttons.Add(new Button("nCommand", "Схема листов",
               new List<ButtonCommand> { new ButtonCommand("Layout_Create", "Настройки", "Параметры команд."), }));

            startEvents.Initialize();
        }
        private void CountMenus()
        {
        }
    }
}
