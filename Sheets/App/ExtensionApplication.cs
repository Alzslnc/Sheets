using Autodesk.AutoCAD.Runtime;
using BaseFunction;
using Sheets.Ribbon;

namespace Sheets.App
{
    public class ExtensionApplication : IExtensionApplication
    {
        public void Initialize()
        {
            new StartEvents().Initialize(Commands.CommandClasses);
            CountMenus.CreateCountMenus();
        }
        public void Terminate()
        {
        }
    }
}
