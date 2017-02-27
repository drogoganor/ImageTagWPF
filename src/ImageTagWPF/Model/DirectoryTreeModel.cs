using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageTagWPF.Model
{
    public class DirectoryModel
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public bool IsExpanded { get; set; }
        public List<DirectoryModel> ChildDirectories { get; set; }

        public DirectoryModel()
        {
            ChildDirectories = new List<DirectoryModel>();
        }
    }
}
