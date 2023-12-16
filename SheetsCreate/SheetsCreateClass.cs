using System.Threading;

namespace Sheets
{
    public class SheetsCreateClass
    {     
        public void Sheets_Create()
        {
            ViewportLayersClass vlc = new ViewportLayersClass();
            IniData = new IniData();
            Thread thread = new Thread(FormThread);
            thread.Start();
            while (IniData.Action == Action.none) Thread.Sleep(200);
            if (IniData.Action == Action.Cancel) return;
            SheetsCreateAcadClass sheetsCreateAcadClass = new SheetsCreateAcadClass();
            sheetsCreateAcadClass.CreateBlock(IniData);
        }       
        private void FormThread()
        { 
            SheetsCreateForm form = new SheetsCreateForm(IniData);
            form.ShowDialog();
            if (form.DialogResult == System.Windows.Forms.DialogResult.OK) IniData.Action = Action.Ok;
            else IniData.Action = Action.Cancel;
        }
        IniData IniData { get; set; }
    }
}
