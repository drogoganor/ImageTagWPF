using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using ColorPickerWPF.Code;
using ImageTagWPF.Model;

namespace ImageTagWPF.Code
{
    [Serializable]
    public class ImageTagSettings : WriteableFileObject<ImageTagSettings>
    {
        public string DefaultDirectory { get; set; }
        public List<TagCategoryColor> TagCategoryColors { get; set; }
        public List<string> FileExtensions { get; set; }
        public string DefaultImageViewProgram { get; set; }

        public ImageTagSettings()
        {
            TagCategoryColors = new List<TagCategoryColor>();
            FileExtensions = new List<string>();


            string program = string.Empty;
            if (Util.TryGetRegisteredApplication(".jpg", out program))
            {
                DefaultImageViewProgram = program;
            }
        }

        public void InitializeDefaults()
        {
            DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures);

            TagCategoryColors.Clear();
            TagCategoryColors.Add(new TagCategoryColor()
            {
                TagType = TagType.Descriptor,
                FontColor = Colors.Black
            });
            TagCategoryColors.Add(new TagCategoryColor()
            {
                TagType = TagType.Character,
                FontColor = Colors.DarkOrange
            });
            TagCategoryColors.Add(new TagCategoryColor()
            {
                TagType = TagType.Series,
                FontColor = Colors.Crimson
            });
            TagCategoryColors.Add(new TagCategoryColor()
            {
                TagType = TagType.Artist,
                FontColor = Colors.MediumPurple
            });

            FileExtensions.AddRange(new []
            {
                "*.png",
                "*.jpg",
                "*.jpeg",
                "*.gif"
            });
        }

    }


    [Serializable]
    public class TagCategoryColor
    {
        public TagType TagType { get; set; }
        public Color FontColor { get; set; }
    }
}
