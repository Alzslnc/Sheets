using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sheets
{
    public class IniData
    {
        private readonly object Lock = new object();
        public IniData(List<string> layers) 
        { 
            Layers = layers;
        }
        public List<string> Layers { get; set; } = new List<string>();
        public string Layer { get; set; } = "";
        public Action Action 
        {
            get
            {
                lock (Lock)
                {
                    return _Action; 
                }   
            }
            set 
            {
                lock (Lock)
                {
                    _Action = value;
                }
            } 
        }
        private Action _Action = Action.none;
    }


   
}
