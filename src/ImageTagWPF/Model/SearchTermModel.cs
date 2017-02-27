using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageTagWPF.Model
{
    public class SearchTermModel
    {
        public TagModel Tag { get; set; }
        public bool Include { get; set; }

        public SearchTermModel()
        {
            Include = true;
        }
    }
}
