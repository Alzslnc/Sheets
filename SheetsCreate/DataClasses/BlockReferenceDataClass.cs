using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Sheets
{
    public class BlockReferenceDataClass
    {
        public BlockReferenceDataClass() 
        {
            GetData();
        }
        private void GetData()
        {
            using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
            {
                using (BlockTable bt = tr.GetObject(HostApplicationServices.WorkingDatabase.BlockTableId, OpenMode.ForRead) as BlockTable)
                {
                    foreach (ObjectId bid in bt)
                    {
                        using (BlockTableRecord btr = tr.GetObject(bid, OpenMode.ForRead) as BlockTableRecord)
                        {
                            if (!btr.HasAttributeDefinitions) continue;
                            BlockReferenceData data = new BlockReferenceData(btr.Name);                        
                            foreach (ObjectId id in btr)
                            {
                                if (!id.ObjectClass.Equals(RXClass.GetClass(typeof(AttributeDefinition)))) continue;
                               
                                using (AttributeDefinition att = tr.GetObject(id, OpenMode.ForRead) as AttributeDefinition)
                                { 
                                    if (att == null) continue;
                                    if (!data.Tags.Contains(att.Tag)) data.Tags.Add(att.Tag);
                                }
                            }
                            BlockReferenceDatas.Add(data);
                        }
                    }
                }
                tr.Commit();
            }
        }
        public BlockReferenceData GetBlockReferenceData(string name)
        {
            foreach (BlockReferenceData block in BlockReferenceDatas)
            {
                if (block.Name.Equals(name)) return block;
            }
            return null;
        }
        public List<BlockReferenceData> BlockReferenceDatas { get; private set; } = new List<BlockReferenceData>();
    }
    public class BlockReferenceData
    {        
        public BlockReferenceData(string name) 
        { 
            Name = name;
        }
        public string Name { get; private set; } = "";
        public List<string> Tags { get; private set; } = new List<string>();
    }

}
