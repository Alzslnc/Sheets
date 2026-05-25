using BaseFunction;
using System;
using System.IO;

namespace Sheets
{
    public static class Settings
    {
        static Settings()
        {
            Load();
        }

        public static void Load()
        {
            if (BaseXMLClass.GetSerialisationResult(Name, typeof(SettingsClass), false) is SettingsClass model) Default = model;
            else
            {
                if (BaseXMLClass.GetSerialisationResult("Settings", typeof(SettingsClass), true) is SettingsClass model2)
                {
                    Default = model2;
                    Save();
                }
                else Default = new SettingsClass();
            }
        }
        public static void Save()
        {
            BaseXMLClass.SetSerialisationResult(Name, Default, false);
        }

        static readonly string Name = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AlzAcadProgramSettings", "SheetsSettings.xml");

        public static SettingsClass Default { get; private set; } = new SettingsClass();
    }

    public class SettingsClass : BaseClass
    {
        public ExtentsCreateType ExtentsCreateType { get => _ExtentsCreateType; set { SetData(ref _ExtentsCreateType, value); } }
        private ExtentsCreateType _ExtentsCreateType = ExtentsCreateType.area;
        public bool ExtentsAlongCurve { get => _ExtentsAlongCurve; set { SetData(ref _ExtentsAlongCurve, value); } }
        private bool _ExtentsAlongCurve = false;
        public bool YOnNorth { get => _YOnNorth; set { SetData(ref _YOnNorth, value); } }
        private bool _YOnNorth = true;
        public int ExtentsOverlap { get => _ExtentsOverlap; set { if (value < 0 || value > 50) return; SetData(ref _ExtentsOverlap, value); } }
        private int _ExtentsOverlap = 10;
       
    }

}



