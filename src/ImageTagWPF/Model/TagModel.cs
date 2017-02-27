using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using ColorPickerWPF.Code;
using ImageTagWPF.Code;
using ImageTagWPF.Data;

namespace ImageTagWPF.Model
{
    public class TagModel
    {
        public Tag Tag { get; set; }
        public string HexColor { get; set; }

        public TagModel()
        {
            HexColor = Colors.Black.ToHexString();
        }
    }
}
