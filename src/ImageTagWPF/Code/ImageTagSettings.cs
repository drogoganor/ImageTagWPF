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
    using System.IO;
    using System.Xml.Serialization;

    [Serializable]
    public class ImageExtensionViewerProgram
    {
        public string Extension { get; set; }

        public string ViewerProgram { get; set; }
    }

    [Serializable]
    public class ImageTagSettings : WriteableFileObject<ImageTagSettings>
    {
        public bool PortableMode = false;

        public string DefaultDirectory { get; set; }

        public List<TagCategoryColor> TagCategoryColors { get; set; }

        public List<ImageExtensionViewerProgram> Extensions { get; set; }

        [XmlIgnore]
        public string TempDirectory { get; set; }

        public string TempDirectoryName { get; set; } = "temp";
        public string ImageTagCacheFolderName { get; set; } = "ImageTagWPF";

        public ImageTagSettings()
        {
            TagCategoryColors = new List<TagCategoryColor>();
            Extensions = new List<ImageExtensionViewerProgram>();
        }

        public void InitializeDirectories()
        {
            if (!this.PortableMode)
            {
                this.TempDirectory = Path.Combine(Path.GetTempPath(), this.ImageTagCacheFolderName);
            }
            else
            {
                this.TempDirectory = Path.Combine(Environment.CurrentDirectory, this.TempDirectoryName);
            }

            if (!Directory.Exists(this.TempDirectory))
            {
                try
                {
                    Directory.CreateDirectory(this.TempDirectory);
                }
                catch (Exception ex)
                {
                    App.Log.Error("Couldn't create temporary directory: " + this.TempDirectory + " : " + ex.Message);
                    this.TempDirectory = Path.GetTempPath();
                }
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
                TagType = TagType.Subject,
                FontColor = Colors.DarkOrange
            });
            TagCategoryColors.Add(new TagCategoryColor()
            {
                TagType = TagType.Event,
                FontColor = Colors.Blue,
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

            var defaultExts = new[] { "*.png", "*.jpg", "*.jpeg", "*.gif", "*.webm", };
            
            foreach (var fileExtension in defaultExts)
            {
                var program = string.Empty;
                Util.TryGetRegisteredApplication(fileExtension.Substring(1), out program);

                Extensions.Add(new ImageExtensionViewerProgram()
                {
                    Extension = fileExtension,
                    ViewerProgram = program
                });
            }
        }

    }


    [Serializable]
    public class TagCategoryColor
    {
        public TagType TagType { get; set; }
        public Color FontColor { get; set; }
    }
}
