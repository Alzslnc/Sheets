namespace Sheets
{
    public class IniData
    {
        private readonly object Lock = new object();
        public IniData() 
        {
            ViewportLayersClass = new ViewportLayersClass();
            if (!Settings.Default.NoBlock) BlockReferenceDataClass = new BlockReferenceDataClass();
        }
        public ViewportLayersClass ViewportLayersClass { get; set; }
        public BlockReferenceDataClass BlockReferenceDataClass { get; set; } = null;
        public string Layer { get; set; } = "";
        public string Block { get; set; } = "";
        public string AttributeTag { get; set; } = "";
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
        public PrefixType PrefixType { get; set; } = PrefixType.Manual;
    }


   
}
