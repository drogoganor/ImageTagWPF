using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Image = ImageTagWPF.Data.Image;

namespace ImageTagWPF.Code
{
    public class ImageDuplicateFinder
    {
        public List<Image> AllImages = new List<Image>();

        public List<BatchImage> BatchImages = new List<BatchImage>();

        public List<List<Image>> ThreadImageList = new List<List<Image>>();


        public ProcessOutputReport FileProcessData;

        public ImageDuplicateFinder()
        {

        }


        protected void InitializeBatchImages()
        {
            AllImages = App.ImageTag.Entities.Images.ToList();

            foreach (var allImage in AllImages)
            {
                if (!File.Exists(allImage.Path)) continue;

                Size size = new Size(1, 1);
                bool gotSize = false;
                try
                {
                    size = Util.GetImageSizeQuick(allImage.Path);
                    gotSize = true;
                }
                catch (Exception ex)
                {
                    App.Log.Error("Error getting image dimensions quick: " + ex.Message);
                }

                if (!gotSize)
                {
                    try
                    {
                        var img = Bitmap.FromFile(allImage.Path);
                        if (img != null)
                        {
                            size = new Size(img.Width, img.Height);
                        }
                    }
                    catch (Exception ex)
                    {
                        App.Log.Error("Error getting image dimensions: " + ex.Message);
                        continue;
                    }
                }


                BatchImages.Add(new BatchImage()
                {
                    Image = allImage,
                    Width = size.Width,
                    Height = size.Height
                });
            }
        }


        public void FindDuplicateFilesByContent()
        {
            App.Log.Info(" ");
            App.Log.Info("================================================");
            App.Log.Info("Find duplicate images by content");
            App.Log.Info("================================================");

            var token = App.CancellationTokenSource.Token;

            bool checkOnlySameDir = true;

            FileProcessData = new ProcessOutputReport()
            {
                OperationTitle = "Finding duplicate images by content"
            };


            InitializeBatchImages();


            var matchingSizes = from i in BatchImages
                                group i by new { i.Width, i.Height }
                                into grp
                                where grp.Count() > 1
                                select grp.Key;

            var matchingSizesList = matchingSizes.ToList();

            App.Log.Info("Found " + matchingSizesList.Count + " sets of images with same dimensions");

            var checkedHash = new HashSet<string>();

            foreach (var matchingSize in matchingSizesList)
            {
                var matchingImages =
                    BatchImages.Where(x => x.Height == matchingSize.Height && x.Width == matchingSize.Width).ToList();

                foreach (var matchingImage in matchingImages)
                {
                    var fileSize = Util.GetImageSizeQuick(matchingImage.Image.Path);

                    foreach (var innerImage in matchingImages)
                    {
                        if (token.IsCancellationRequested)
                        {
                            App.Log.Error("Aborted checking files by content.");
                            return;
                        }

                        if (checkedHash.Contains(innerImage.Image.Path))
                            continue;

                        if (checkOnlySameDir && Path.GetDirectoryName(matchingImage.Image.Path) != Path.GetDirectoryName(innerImage.Image.Path))
                            continue;

                        if (innerImage.Image.Path == matchingImage.Image.Path)
                            continue;

                        if (CheckImageMatch(matchingImage.Image.Path, fileSize, innerImage.Image.Path))
                        {
                            App.Log.Info("Found dupe: " + matchingImage.Image.Path + "  for: " + innerImage.Image.Path);

                            FileProcessData.Operations.Add(new ProcessOperation()
                            {
                                Message = "Duplicate: " + matchingImage.Image.Path + ", " + innerImage.Image.Path,
                                Output = ProcessRecommendedOutput.CompareFiles,
                                Severity = FileProcessSeverity.Error,
                                SourceFilename = matchingImage.Image.Path,
                                DestinationFilename = innerImage.Image.Path
                            });
                        }
                    }

                    checkedHash.Add(matchingImage.Image.Path);
                }
            }

            App.Log.Info("Finished checking files by content.");



            //foreach (var file in AllImages)
            //{
            //    if (!File.Exists(file.Path))
            //    {
            //        App.Log.Error("Couldn't find file: " + file.Path);
            //        continue;
            //    }

            //    // Compare files

            //    var matchingImages = new List<string>();

            //    var now = DateTime.Now;

            //    FindDupliatesForImage(file.Path);

            //    var span = DateTime.Now - now;

            //    App.Log.Info("Inner loop took: " + span.ToString("g"));


            //    if (matchingImages.Count > 0)
            //    {
            //        App.Log.Info("Found duplicate images for: " + file);
            //        foreach (var matchingImage in matchingImages)
            //        {
            //            App.Log.Info("Dupe: " + matchingImage);
            //        }
            //    }
            //}
        }


        protected void PopulateThreadImageList()
        {
            int divisor = 1024;
            int imagesLength = AllImages.Count;

            if (AllImages.Count <= divisor)
            {
               ThreadImageList.Add(AllImages);
            }
            else
            {
                int pageSize = imagesLength/divisor;
                int startIndex = 0;

                bool done = false;
                while (!done)
                {
                    var newList = AllImages.Skip(startIndex).Take(pageSize).ToList();

                    startIndex += pageSize;

                    if (newList.Count == 0)
                    {
                        done = true;
                        break;
                    }

                    ThreadImageList.Add(newList);

                    if (newList.Count < pageSize)
                    {
                        done = true;
                        break;
                    }
                }
            }

        }



        protected void FindDupliatesForImage(string file)
        {

            var fileSize = Util.GetImageSizeQuick(file);
            var taskList = new List<Task>();

            foreach (var list in ThreadImageList)
            {
                var task = Task.Run(() => CheckImageMatchForList(file, fileSize, list));
                taskList.Add(task);
            }

            bool done = false;

            while (!done)
            {
                bool allDone = true;
                foreach (var task in taskList)
                {
                    if (!task.IsCompleted)
                    {
                        allDone = false;
                        break;
                    }
                }

                if (allDone)
                {
                    done = true;
                    break;
                }
            }

        }


        protected void CheckImageMatchForList(string file, Size fileSize, List<Image> images)
        {
            foreach (var image in images)
            {
                if (CheckImageMatch(file, fileSize, image.Path))
                {
                    //matchingImages.Add(matchFile.Path);

                    App.Log.Info("Found dupe image: " + image.Path);
                }
            }
        }


        protected bool CheckImageMatch(string file1, Size file1Size, string file2)
        {
            if (!File.Exists(file1))
            {
                App.Log.Error("Couldn't find file: " + file1);
                return false;
            }

            float diffThreshold = 0.01f;


            if (file1 != file2)
            {
                //var matchHash = Util.GetMD5Hash(matchFile);

                //if (matchHash == fileHash)
                //{
                //    matchingImages.Add(matchFile);
                //}

                var matchSize = Util.GetImageSizeQuick(file2);
                if (matchSize != file1Size)
                    return false;

                try
                {
                    var diffVal = XnaFan.ImageComparison.ImageTool.GetPercentageDifference(file1, file2);
                    if (diffVal < diffThreshold)
                    {
                        //matchingImages.Add(matchFile.Path);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    App.Log.Error("Error comparing files: " + ex.Message);
                }
            }
            return false;
        }
    }
}
