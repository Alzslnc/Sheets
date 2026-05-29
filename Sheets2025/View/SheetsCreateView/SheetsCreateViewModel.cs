using Autodesk.AutoCAD.Windows.Data;
using BaseFunction;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Sheets.View.SheetsCreateView
{
    internal class SheetsCreateViewModel : BaseClass
    {
        public SheetsCreateViewModel()
        {
            SettingsRef = Settings.Default;
            GetViewportLayers();
            GetBlockRefDatas();                   
            CloseCommand = new RelayCommand(_ => CloseCommandHandler((System.Windows.Window)_));
            CreateCommand = new RelayCommand(_ => CreateCommandHandler((System.Windows.Window)_));
            GetHatchTypeCommand = new RelayCommand(_ => GetHatchTypeCommandHandler());
            SettingsRef.PropertyChanged += SettingsPropertyChanged;

        }

        private void GetBlockRefDatas()
        {
            foreach (BlockRefData blockRefData in Support.GetBlockRefDatas())
            {
                BlockRefDatas.Add(blockRefData);
            }
            BlockRefData refData = BlockRefDatas.FirstOrDefault(x => x.Name == SettingsRef.BlockName);
            if (refData != null)
            {
                SelectedBlockRefData = refData;
            }
        }

        private void GetViewportLayers()
        {
            foreach (string s in Support.GetViewportLayers())
            {
                ViewportLayers.Add(s);
            }
        }
        public ICommand GetHatchTypeCommand { get; set; }
        private void GetHatchTypeCommandHandler()
        {
            if (Support.GetHatchPattern(out string result)) SettingsRef.HatchPattern = result;
        }
        public ICommand CreateCommand { get; set; }
        private void CreateCommandHandler(System.Windows.Window window)
        {
            SettingsRef.BlockName = SelectedBlockRefData != null ? SelectedBlockRefData.Name : "";

            if (SettingsRef.NumType == NumType.byReferenceAtteibute && (SelectedBlockRefData == null || string.IsNullOrEmpty(SettingsRef.AttributeName)))
            {
                System.Windows.MessageBox.Show("Не выбран атрибут для нумерации листов");
                return;            
            }

            window.DialogResult = true;
            window.Close();
        }
        public ICommand CloseCommand { get; set; }
        private static void CloseCommandHandler(System.Windows.Window window)
        {
            window.DialogResult = false;
            window.Close();
        }

        public SettingsClass SettingsRef { get; }      
        private void SettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SettingsRef.NumType):
                    {                       
                        BlockDataVisible = true;
                        NumListShiftVisible = true;
                        return;
                    }            
            }
        }
        public bool BlockDataVisible { get => (SettingsRef.NumType == NumType.byReferenceAtteibute); set { Call(); } }
        public bool NumListShiftVisible { get => (SettingsRef.NumType == NumType.byList); set { Call(); } }
        public ObservableCollection<string> ViewportLayers { get; } = new ObservableCollection<string>();
        public ObservableCollection<BlockRefData> BlockRefDatas { get; } = new ObservableCollection<BlockRefData>();
        public BlockRefData SelectedBlockRefData { get; set; } = null;
    }    
}
