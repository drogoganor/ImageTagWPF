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

    public enum OrganizeOperation
    {
        Move,
        Copy
    }

    public class OrganizeFile
    {
        public Image Image { get; set; }
        public string Destination { get; set; }
        public OrganizeOperation Operation { get; set; }

        public string UniqueFileOperationID { get { return ((int)Image.ID) + Operation.ToString(); } }
    }
}
