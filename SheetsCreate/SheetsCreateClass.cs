using System.Threading;
using static BaseFunction.BaseLayerClass;

namespace Sheets
{
    public class SheetsCreateClass
    {
        public void Sheets_Create()
        {
            ViewportLayersClass vlc = new ViewportLayersClass();
            IniData = new IniData();
            Thread thread = new Thread(FormThread);
            thread.IsBackground = true;
            thread.Start();
            while (IniData.Action == Action.none) Thread.Sleep(200);
            if (IniData.Action == Action.Cancel) return;
            SheetsCreateAcadClass sheetsCreateAcadClass = new SheetsCreateAcadClass();
            sheetsCreateAcadClass.CreateBlock(IniData);
        }
       
        private void FormThread()
        { 
            Form1 form = new Form1(IniData);
            form.ShowDialog();
            if (form.DialogResult == System.Windows.Forms.DialogResult.OK) IniData.Action = Action.Ok;
            else IniData.Action = Action.Cancel;
        }

        IniData IniData { get; set; }
    }
}
