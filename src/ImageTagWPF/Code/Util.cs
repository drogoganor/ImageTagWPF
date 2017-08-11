using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using ImageTagWPF.Data;
using ImageTagWPF.Model;

using Microsoft.Win32;

using Image = ImageTagWPF.Data.Image;
using Color = System.Windows.Media.Color;
using PixelFormat = System.Windows.Media.PixelFormat;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace ImageTagWPF.Code
{
    public static class Util
    {
        private static Random Random = new Random();


        public static string GetFileHashSHA1(string filename)
        {
            string hash = null;
            using (var cryptoProvider = new SHA1CryptoServiceProvider())
            {
                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    hash = BitConverter.ToString(cryptoProvider.ComputeHash(fs)).Replace("-", "").ToLowerInvariant();
                }
            }
            return hash;
        }

        public static IEnumerable<FileInfo> GetFilesByExtensions(this DirectoryInfo dir, params string[] extensions)
        {
            if (extensions == null) throw new ArgumentNullException("extensions");
            IEnumerable<FileInfo> files = Enumerable.Empty<FileInfo>();
            foreach (string ext in extensions)
            {
                files = files.Concat(dir.GetFiles(ext));
            }
            return files;
        }

        // https://stackoverflow.com/questions/5936628/get-items-in-view-within-a-listbox
        public static bool BoundsContainsItem(this FrameworkElement container, FrameworkElement element)
        {
            if (!element.IsVisible) return false;

            Rect bounds =
                element.TransformToAncestor(container)
                    .TransformBounds(new Rect(0.0, 0.0, element.ActualWidth, element.ActualHeight));
            var rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);
            return rect.Contains(bounds.TopLeft) || rect.Contains(bounds.BottomRight);
        }

        /*
        public static Color ToColor(this TagType type)
        {
            switch (type)
            {
                case TagType.Descriptor:
                    return Colors.Black;
                case TagType.Subject:
                    return Colors.DarkOrange;
                case TagType.Artist:
                    return Colors.MediumPurple;
                case TagType.Series:
                    return Colors.Crimson;
                default:
                    return Colors.Gray;
            }
        }
        */


        public static BitmapImage GetThumbnailForImage(string fullPath, int thumbWidth = 200)
        {
            BitmapImage thumbImage = null;
            if (File.Exists(fullPath))
            {
                thumbImage = new BitmapImage();
                bool isWebM = false;
                try
                {
                    // BitmapImage.UriSource must be in a BeginInit/EndInit block
                    thumbImage.BeginInit();
                    thumbImage.CacheOption = BitmapCacheOption.OnLoad;
                    thumbImage.UriSource = new Uri(fullPath, UriKind.Absolute);

                    // To save significant application memory, set the DecodePixelWidth or  
                    // DecodePixelHeight of the BitmapImage value of the image source to the desired 
                    // height or width of the rendered image. If you don't do this, the application will 
                    // cache the image as though it were rendered as its normal size rather then just 
                    // the size that is displayed.
                    // Note: In order to preserve aspect ratio, set DecodePixelWidth
                    // or DecodePixelHeight but not both.
                    thumbImage.DecodePixelWidth = thumbWidth;
                    thumbImage.EndInit();

                    thumbImage.Freeze();
                }
                catch (Exception ex)
                {
                    if (fullPath.ToLowerInvariant().EndsWith(".webm"))
                    {
                        isWebM = true;
                    }
                    else
                    {
                        App.Log.Error("Couldn't create thumbnnail for: " + fullPath + " : " + ex.Message);
                        thumbImage = null;
                    }
                }

                if (isWebM)
                {
                    try
                    {
                        var tempImageFilename = Guid.NewGuid().ToString() + ".png";
                        var tempImagePath = Path.Combine(App.ImageTag.Settings.TempDirectory, tempImageFilename);
                        
                        var ffMpeg = new NReco.VideoConverter.FFMpegConverter();
                        ffMpeg.GetVideoThumbnail(fullPath, tempImagePath, 5);
                        
                        thumbImage = new BitmapImage();
                        thumbImage.BeginInit();
                        thumbImage.CacheOption = BitmapCacheOption.OnLoad;
                        thumbImage.UriSource = new Uri(tempImagePath, UriKind.Absolute);
                        thumbImage.DecodePixelWidth = thumbWidth;
                        thumbImage.EndInit();
                        thumbImage.Freeze();

                        File.Delete(tempImagePath);
                    }
                    catch (Exception ex)
                    {
                        App.Log.Error("Couldn't create thumbnnail for webm: " + fullPath + " : " + ex.Message);
                        thumbImage = null;
                    }

                }
            }
            return thumbImage;
        }


        public static BitmapImage GetThumbnailForImageSafe(string fullPath, int thumbWidth = 200)
        {
            BitmapImage thumbImage = null;
            if (File.Exists(fullPath))
            {
                thumbImage = new BitmapImage();
                try
                {
                    Stream stream = new System.IO.MemoryStream();  // Create new MemoryStream  
                    Bitmap bitmap = new Bitmap(fullPath);  // Create new Bitmap (System.Drawing.Bitmap) from the existing image file (albumArtSource set to its path name)  
                    bitmap.Save(stream, ImageFormat.Png);  // Save the loaded Bitmap into the MemoryStream - Png format was the only one I tried that didn't cause an error (tried Jpg, Bmp, MemoryBmp)  
                    bitmap.Dispose();  // Dispose bitmap so it releases the source image file  

                    // BitmapImage.UriSource must be in a BeginInit/EndInit block
                    thumbImage.BeginInit();
                    thumbImage.CacheOption = BitmapCacheOption.OnLoad;
                    //thumbImage.UriSource = new Uri(fullPath, UriKind.Absolute);
                    thumbImage.StreamSource = stream;

                    // To save significant application memory, set the DecodePixelWidth or  
                    // DecodePixelHeight of the BitmapImage value of the image source to the desired 
                    // height or width of the rendered image. If you don't do this, the application will 
                    // cache the image as though it were rendered as its normal size rather then just 
                    // the size that is displayed.
                    // Note: In order to preserve aspect ratio, set DecodePixelWidth
                    // or DecodePixelHeight but not both.
                    thumbImage.DecodePixelWidth = thumbWidth;
                    thumbImage.EndInit();

                    thumbImage.Freeze();
                }
                catch (Exception ex)
                {
                    App.Log.Error("Couldn't create thumbnnail for: " + fullPath + " : " + ex.Message);
                    thumbImage = null;
                }

            }
            return thumbImage;
        }


        public static void UpdateParentTagsRecursive(Image image, Tag tag)
        {
            foreach (var parentTag in tag.ParentTags)
            {
                if (!image.Tags.Contains(parentTag))
                    image.Tags.Add(parentTag);
                
                UpdateParentTagsRecursive(image, parentTag);
            }
        }

        public static void UpdateImageTags(List<ImageFileThumbData> images, List<TagModel> tags, CancellationToken token, bool addOnly = false)
        {
            int threadDivisor = 16;
            int threadListSize = images.Count/threadDivisor;
            bool useThreading = false;

            if (!useThreading || images.Count < threadDivisor)
            {
                // Do it the old way, just iterate
                // Go through selected files and add tags
                foreach (var imageItem in images)
                {
                    if (token.IsCancellationRequested)
                        break;

                    UpdateSingleImageTags(imageItem, tags, addOnly);
                }
            }
            else
            {
                // Thread it!
                var imagesListLists = new List<List<ImageFileThumbData>>();

                for (int i = 0; i < threadDivisor; i++)
                {
                    List<ImageFileThumbData> threadList = null;

                    if (i == threadDivisor - 1)
                    {
                        // Last; take all remainder
                        threadList = images.Skip(i*threadListSize).ToList();
                    }
                    else
                    {
                        threadList = images.Skip(i * threadListSize).Take(threadListSize).ToList();
                    }

                    imagesListLists.Add(threadList);
                }


                ThreadSafeImages = new BlockingCollection<Image>();

                var taskList = new List<Task>();
                foreach (var imageList in imagesListLists)
                {
                    var task = Task.Run(() =>
                    {
                        foreach (var imageItem in imageList)
                        {
                            UpdateSingleImageTags(imageItem, tags, addOnly, useSafeCollection:true);
                        }
                    });
                    taskList.Add(task);
                }

                // Check for dupes
                
                //var firstList = imagesListLists[0];
                //var fullList = new List<ImageFileThumbData>();
                //foreach (var imagesList in imagesListLists)
                //{
                //    fullList.AddRange(imagesList);
                //}


                //var dupeImages = from i in fullList
                //                    group i by new { i.FullPath }
                //                    into grp
                //                    where grp.Count() > 1
                //                    select grp.Key;

                //foreach (var dupe in dupeImages)
                //{
                //    App.Log.Error("Duplicate file: " + dupe.FullPath);
                //}


                bool allTasksDone = false;
                while (!allTasksDone)
                {
                    bool done = true;
                    foreach (var task in taskList)
                    {
                        if (!task.IsCompleted)
                        {
                            done = false;
                            break;
                        }
                    }

                    if (done)
                    {
                        foreach (var threadSafeImage in ThreadSafeImages)
                        {
                            App.ImageTag.Entities.Images.Add(threadSafeImage);
                        }
                        ThreadSafeImages.Dispose();

                        allTasksDone = true;
                        break;
                    }
                }
            }


            

        }


        private static BlockingCollection<Image> ThreadSafeImages;

        public static void UpdateSingleImageTags(ImageFileThumbData imageItem, List<TagModel> tags, bool addOnly = false, bool useSafeCollection = false)
        {
            // Get the image data
            if (imageItem != null)
            {
                // If it's still null, must be legit
                if (imageItem.Image == null)
                {
                    // Add a new image to the database, and tag it

                    var newImageRecord = new Image()
                    {
                        Path = imageItem.FullPath,
                        Checksum = Util.GetFileHashSHA1(imageItem.FullPath)
                    };
                    

                    // Add all selected tags
                    foreach (var tag in tags)
                    {
                        if (!newImageRecord.Tags.Contains(tag.Tag))
                            newImageRecord.Tags.Add(tag.Tag);

                        // Also add parent tags
                        UpdateParentTagsRecursive(newImageRecord, tag.Tag);

                        // Update recently used on tag
                        tag.Tag.LastUsed = DateTime.Now;
                    }

                    imageItem.Image = newImageRecord;


                    if (!useSafeCollection)
                        App.ImageTag.Entities.Images.Add(newImageRecord);
                    else
                        ThreadSafeImages.Add(newImageRecord);
                    

                }
                else
                {


                    // We have this image already, just add the tag to it
                    var existingImage = imageItem.Image;

                    // Remove tags that we don't have selected
                    if (!addOnly)
                    {
                        var selectedTagIDs = tags.Select(x => (int)x.Tag.ID);
                        var removedList = existingImage.Tags.Where(x => !selectedTagIDs.Contains((int)x.ID)).ToArray();
                        foreach (var removeTag in removedList)
                        {
                            existingImage.Tags.Remove(removeTag);
                        }
                    }

                    // Add all selected tags
                    foreach (var tag in tags)
                    {
                        if (existingImage.Tags.Count == 0 || !existingImage.Tags.Contains(tag.Tag))
                        {
                            existingImage.Tags.Add(tag.Tag);

                            // Also add parent tags
                            UpdateParentTagsRecursive(existingImage, tag.Tag);

                            // Update recently used on tag
                            tag.Tag.LastUsed = DateTime.Now;
                        }

                    }

                }

            }
        }

        public static void RemoveImageTags(List<ImageFileThumbData> images, List<TagModel> tags)
        {
            // Go through selected files and add tags
            foreach (var imageItem in images)
            {
                // Get the image data
                if (imageItem != null)
                {
                    if (imageItem.Image == null)
                    {
                        // Skip, it's not tagged
                    }
                    else
                    {
                        // We have this image already, just add the tag to it
                        var existingImage = imageItem.Image;

                        foreach (var removeTag in tags)
                        {
                            existingImage.Tags.Remove(removeTag.Tag);
                        }

                    }

                }
            }


        }

        public static void UpdateImageParentTags(Image image)
        {
            // Go through selected files and add tags
            var tags = image.Tags.ToList();

            foreach (var tag in tags)
            {
                // Get the image data
                if (tag != null)
                {
                    UpdateParentTagsRecursive(image, tag);
                    // Update recently used on tag
                    tag.LastUsed = DateTime.Now;
                }
            }
        }

        public static bool RetryMove(string filename, string dest, int interval = 1000, int retries = 10)
        {
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    File.Move(filename, dest);

                    return true;
                }
                catch (Exception ex)
                {
                    App.Log.Error("Couldn't rename file: " + filename + ": " + dest + ". Retry " + (i + 1));
                }

                Thread.Sleep(interval);
            }
            return false;
        }

        public static void UpdateImageRating(List<ImageFileThumbData> images, int rating)
        {

            // Go through selected files and add tags
            foreach (var imageItem in images)
            {
                if (App.CancellationTokenSource.Token.IsCancellationRequested)
                    break;

                // Get the image data
                if (imageItem != null)
                {
                    if (imageItem.Image == null)
                    {
                        // Add a new image to the database, and tag it

                        var newImageRecord = new Image()
                        {
                            Path = imageItem.FullPath,
                            Checksum = Util.GetFileHashSHA1(imageItem.FullPath)
                        };

                        newImageRecord.Rating = rating;

                        imageItem.Image = newImageRecord;

                        App.ImageTag.Entities.Images.Add(newImageRecord);
                    }
                    else
                    {
                        // We have this image already, just add the tag to it
                        var existingImage = imageItem.Image;

                        existingImage.Rating = rating;
                    }
                }
            }
        }


        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static T GetVisualChild<T>(this Visual referenceVisual) where T : Visual
        {
            Visual child = null;

            if (referenceVisual == null)
                return null;

            for (Int32 i = 0; i < VisualTreeHelper.GetChildrenCount(referenceVisual); i++)
            {
                child = VisualTreeHelper.GetChild(referenceVisual, i) as Visual;
                if (child != null && child is T)
                {
                    break;
                }
                else if (child != null)
                {
                    child = GetVisualChild<T>(child);
                    if (child != null && child is T)
                    {
                        break;
                    }
                }
            }
            return child as T;
        }
        
        
        /// <summary> 
         /// Searches a TreeView for the provided object and selects it if found 
         /// </summary> 
         /// <param name="treeView">The TreeView containing the item</param> 
         /// <param name="item">The item to search and select</param> 
        public static void SelectItem(this TreeView treeView, object item, bool select = true)
        {
            ExpandAndSelectItem(treeView, item, select);
        }

        public static void ExpandFirstItem(this TreeView treeView)
        {
            foreach (var item in treeView.Items)
            {
                TreeViewItem itm = (TreeViewItem)treeView.ItemContainerGenerator.ContainerFromItem(item);
                if (itm == null) continue;
                itm.IsExpanded = true;
            }
        }

        /// <summary> 
        /// Finds the provided object in an ItemsControl's children and selects it 
        /// </summary> 
        /// <param name="parentContainer">The parent container whose children will be searched for the selected item</param> 
        /// <param name="itemToSelect">The item to select</param> 
        /// <returns>True if the item is found and selected, false otherwise</returns> 
        private static bool ExpandAndSelectItem(ItemsControl container, object itemToSelect, bool select = true)
        {
            // Check all items at the current level 
            foreach (Object currentItem in container.Items)
            {
                // If the data item matches the item we want to select, set the corresponding IsSelected to true 
                if (currentItem == itemToSelect)
                {
                    TreeViewItem currentContainer = container.ItemContainerGenerator.ContainerFromItem(currentItem) as TreeViewItem;
                    if (currentContainer != null)
                    {
                        currentContainer.IsExpanded = true;

                        if (select)
                        {
                            currentContainer.IsSelected = true;
                            currentContainer.BringIntoView();
                            currentContainer.Focus();
                        }

                        // The itemToSelect was found and has been selected 
                        return true;
                    }
                }
            }

            // If we get to this point, the SelectedItem was not found at the 
            // current level, so we must check the children 
            foreach (Object item in container.Items)
            {
                TreeViewItem currentContainer = container.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;

                // If the currentContainer has children 
                if (currentContainer != null && currentContainer.Items.Count > 0)
                {
                    // Keep track of if the TreeViewItem was expanded or not 
                    bool wasExpanded = currentContainer.IsExpanded;

                    // Expand the current TreeViewItem so we can check its child TreeViewItems 
                    currentContainer.IsExpanded = true;

                    // If the TreeViewItem child containers have not been generated, we must listen to 
                    // the StatusChanged event until they are 
                    if (currentContainer.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
                    {
                        //store the event handler in a variable so we can remove it (in the handler itself) 
                        EventHandler eh = null;
                        eh = new EventHandler(delegate
                        {
                            if (currentContainer.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                            {
                                //SelectItem(currentContainer, itemToSelect);
                                if (ExpandAndSelectItem(currentContainer, itemToSelect, select) == false)
                                {
                                    //The assumption is that code executing in this EventHandler is the result of the parent not 
                                    //being expanded since the containers were not generated. 
                                    //since the itemToSelect was not found in the children, collapse the parent since it was previously collapsed 
                                    currentContainer.IsExpanded = false;
                                }

                                //remove the StatusChanged event handler since we just handled it (we only needed it once) 
                                currentContainer.ItemContainerGenerator.StatusChanged -= eh;
                            }
                        });
                        currentContainer.ItemContainerGenerator.StatusChanged += eh;
                    }
                    else //otherwise the containers have been generated, so look for item to select in the children 
                    {
                        if (ExpandAndSelectItem(currentContainer, itemToSelect, select) == false)
                        {
                            //restore the current TreeViewItem's expanded state 
                            currentContainer.IsExpanded = wasExpanded;
                        }
                        else //otherwise the node was found and selected, so return true 
                        {
                            return true;
                        }
                    }
                }
            }

            //no item was found 
            return false;
        }


        public static string GetFullDirectory(this OrganizeDirectory dir)
        {
            if (dir.ParentDirectories.Count > 0)
            {
                var parent = dir.ParentDirectories.ToList()[0];
                var parentDir = GetFullDirectory(parent);
                return Path.Combine(parentDir, dir.Name);
            }

            return dir.Name;
        }

        //https://stackoverflow.com/questions/5512921/wpf-richtextbox-appending-coloured-text
        public static void AppendText(this RichTextBox box, string text, Color color)
        {
            TextRange tr = new TextRange(box.Document.ContentEnd, box.Document.ContentEnd);
            tr.Text = text;
            try
            {
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
            }
            catch (FormatException) { }
        }

        public static bool TryGetRegisteredApplication(string extension, out string registeredApp)
        {
            string extensionId = GetClassesRootKeyDefaultValue(extension);
            if (extensionId == null)
            {
                registeredApp = null;
                return false;
            }

            string openCommand = GetClassesRootKeyDefaultValue(
                    Path.Combine(new[] { extensionId, "shell", "open", "command" }));

            if (openCommand == null)
            {
                registeredApp = null;
                return false;
            }

            if (openCommand.Contains("\""))
            {
                openCommand = openCommand.Split(new[] { "\"" }, StringSplitOptions.RemoveEmptyEntries)[0];
            }
            else
            {
                openCommand = openCommand.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)[0];
            }

            registeredApp = openCommand;

            return true;
        }

        private static string GetClassesRootKeyDefaultValue(string keyPath)
        {
            using (var key = Registry.ClassesRoot.OpenSubKey(keyPath))
            {
                if (key == null)
                {
                    return null;
                }

                var defaultValue = key.GetValue(null);
                if (defaultValue == null)
                {
                    return null;
                }

                return defaultValue.ToString();
            }
        }


        // https://stackoverflow.com/questions/15558107/quickest-way-to-compare-two-bitmapimages-to-check-if-they-are-different-in-wpf
        public static bool IsEqual(this BitmapImage image1, BitmapImage image2)
        {
            if (image1 == null || image2 == null)
            {
                return false;
            }
            return image1.ToBytes().SequenceEqual(image2.ToBytes());
        }

        public static byte[] ToBytes(this BitmapImage image)
        {
            byte[] data = new byte[] { };
            if (image != null)
            {
                try
                {
                    var encoder = new BmpBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    using (MemoryStream ms = new MemoryStream())
                    {
                        encoder.Save(ms);
                        data = ms.ToArray();
                    }
                    return data;
                }
                catch (Exception ex)
                {
                }
            }
            return data;
        }

        public static bool ImageCompareString(Bitmap firstImage, Bitmap secondImage)
        {
            MemoryStream ms = new MemoryStream();
            firstImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            String firstBitmap = Convert.ToBase64String(ms.ToArray());
            ms.Position = 0;

            secondImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            String secondBitmap = Convert.ToBase64String(ms.ToArray());

            if (firstBitmap.Equals(secondBitmap))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static List<bool> GetHash(Bitmap bmpSource)
        {
            List<bool> lResult = new List<bool>();
            //create new image with 16x16 pixel
            Bitmap bmpMin = new Bitmap(bmpSource, new Size(16, 16));
            for (int j = 0; j < bmpMin.Height; j++)
            {
                for (int i = 0; i < bmpMin.Width; i++)
                {
                    //reduce colors to true / false                
                    lResult.Add(bmpMin.GetPixel(i, j).GetBrightness() < 0.5f);
                }
            }
            return lResult;
        }

        public static string GetMD5Hash(string filename)
        {
            using (FileStream fStream = File.OpenRead(filename))
            {
                return GetHash<MD5>(fStream);
            }
        }

        public static String GetHash<T>(Stream stream) where T : HashAlgorithm
        {
            StringBuilder sb = new StringBuilder();

            MethodInfo create = typeof(T).GetMethod("Create", new Type[] { });
            using (T crypt = (T)create.Invoke(null, null))
            {
                byte[] hashBytes = crypt.ComputeHash(stream);
                foreach (byte bt in hashBytes)
                {
                    sb.Append(bt.ToString("x2"));
                }
            }
            return sb.ToString();
        }

        public static Size GetImageSizeQuick(string file)
        {
            int height = 0;
            int width = 0;
            using (var imageStream = File.OpenRead(file))
            {
                var decoder = BitmapDecoder.Create(imageStream, BitmapCreateOptions.IgnoreColorProfile,
                    BitmapCacheOption.Default);
                height = decoder.Frames[0].PixelHeight;
                width = decoder.Frames[0].PixelWidth;
            }

            return new Size(width, height);
        }
    }
}
