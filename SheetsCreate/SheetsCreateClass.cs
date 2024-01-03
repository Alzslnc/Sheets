using System.Threading;

namespace Sheets
{
    public class SheetsCreateClass
    {     
        public void Start()
        {
            ViewportLayersClass vlc = new ViewportLayersClass();
            IniData = new IniData();
            Thread thread = new Thread(FormThread);
            thread.Start();
            while (IniData.Action == Action.none) Thread.Sleep(200);

            if (IniData.Action == Action.Ok)
            {
                SheetsCreateAcadClass sheetsCreateAcadClass = new SheetsCreateAcadClass();
                sheetsCreateAcadClass.CreateBlock(IniData);
            }
            else if (IniData.Action == Action.Delete)
            { 
                SheetsDeleteClass sheetsDeleteClass = new SheetsDeleteClass();
                sheetsDeleteClass.DeleteBlock(IniData);
            }
        }       
        private void FormThread()
        { 
            SheetsCreateForm form = new SheetsCreateForm(IniData);
            form.ShowDialog();
            if (form.DialogResult != System.Windows.Forms.DialogResult.OK) IniData.Action = Action.Cancel;            
        }
        IniData IniData { get; set; }
    }
}
