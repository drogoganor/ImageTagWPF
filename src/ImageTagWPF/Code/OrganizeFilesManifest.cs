using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageTagWPF.Data;

namespace ImageTagWPF.Code
{
    public class OrganizeFilesManifest
    {
        public Dictionary<string, OrganizeFile> Files { get; set; }
        

        public OrganizeFilesManifest()
        {
            Files = new Dictionary<string, OrganizeFile>();
        }
    }

    public enum OrganizeOperationType
    {
        Move,
        Copy
    }

    public class OrganizeFile
    {
        public Image Image { get; set; }
        
        public List<OrganizeOperation> Operations = new List<OrganizeOperation>();

        public string ID { get { return Image.ID.ToString("D0"); } }

        public void AddOperation(OrganizeOperation operation)
        {
            if (operation.Operation == OrganizeOperationType.Move
                && Operations.Any(x => x.Operation == OrganizeOperationType.Move))
            {
                //App.Log.Error("Duplicate move operation for " + Image.Path);

                // Update instead
                var moveOp = this.Operations.FirstOrDefault(x => x.Operation == OrganizeOperationType.Move);
                moveOp.Destination = operation.Destination;

                return;
            }

            Operations.Add(operation);
        }
    }

    public class OrganizeOperation
    {
        public string Destination { get; set; }
        public OrganizeOperationType Operation { get; set; }

    }

}
