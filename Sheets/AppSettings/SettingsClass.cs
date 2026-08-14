using BaseFunction;

namespace Sheets.AppSettings
{
    public class SettingsClass : BaseClass
    {
        //области и листы
        public ExtentsCreateType ExtentsCreateType { get => _ExtentsCreateType; set { SetData(ref _ExtentsCreateType, value); } }
        private ExtentsCreateType _ExtentsCreateType = ExtentsCreateType.area;
        public bool ExtentsAlongCurve { get => _ExtentsAlongCurve; set { SetData(ref _ExtentsAlongCurve, value); } }
        private bool _ExtentsAlongCurve = false;
        public bool YOnNorth { get => _YOnNorth; set { SetData(ref _YOnNorth, value); } }
        private bool _YOnNorth = true;
        public int ExtentsOverlap { get => _ExtentsOverlap; set { if (value < 0 || value > 50) return; SetData(ref _ExtentsOverlap, value); } }
        private int _ExtentsOverlap = 10;

        //схема листов
        public NumType NumType { get => _NumType; set { SetData(ref _NumType, value); } }
        private NumType _NumType = NumType.none;
        public PositionType PositionType { get => _PositionTypee; set { SetData(ref _PositionTypee, value); } }
        private PositionType _PositionTypee = PositionType.point;
        public bool SelectPosition { get => _SelectPosition; set { SetData(ref _SelectPosition, value); } }
        private bool _SelectPosition = false;
        public double Scale { get => _Scale; set { if (value < 0.0001 || value > 500000) return; SetData(ref _Scale, value); } }
        private double _Scale = 500;
        public double HatchScale { get => _HatchScale; set { if (value < 0.0001 || value > 1000) return; SetData(ref _HatchScale, value); } }
        private double _HatchScale = 0.2;
        public string Prefix { get => _Prefix; set { SetData(ref _Prefix, value); } }
        private string _Prefix = "Лист";
        public string Header { get => _Header; set { SetData(ref _Header, value); } }
        private string _Header = "Схема листов";
        public string HatchPattern { get => _HatchPattern; set { SetData(ref _HatchPattern, value); } }
        private string _HatchPattern = "ANSI32";
        public double HatchAngle { get => _HatchAngle; set { if (value < 0.0000 || value > 360) return; SetData(ref _HatchAngle, value); } }
        private double _HatchAngle = 0;
        public double HeaderHeight { get => _HeaderHeight; set { if (value < 0.0001 || value > 10000) return; SetData(ref _HeaderHeight, value); } }
        private double _HeaderHeight = 3.5;
        public double FontHeight { get => _FontHeight; set { if (value < 0.0001 || value > 10000) return; SetData(ref _FontHeight, value); } }
        private double _FontHeight = 2.5;
        public int NumListShift { get => _NumListShift; set { if (value < 0 || value > 50) return; SetData(ref _NumListShift, value); } }
        private int _NumListShift = 0;
        public string BlockName { get => _BlockName; set { SetData(ref _BlockName, value); } }
        private string _BlockName = "";
        public string AttributeName { get => _AttributeName; set { SetData(ref _AttributeName, value); } }
        private string _AttributeName = "";
        public string ViewportLayerName { get => _ViewportLayerName; set { SetData(ref _ViewportLayerName, value); } }
        private string _ViewportLayerName = "";
    }
}