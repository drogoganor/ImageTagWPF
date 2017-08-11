using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageTagWPF.Controls;
using ImageTagWPF.Data;
using ImageTagWPF.Model;
using log4net;
using log4net.Config;
using TinyIoC;
using Color = System.Windows.Media.Color;
using Image = ImageTagWPF.Data.Image;

namespace ImageTagWPF.Code
{
    public class ImageTag
    {
        public delegate void ImageTagItemDispatchHandler(ImageTagDispatchItem item);

        public event ImageTagItemDispatchHandler OnStartItem;
        public event ImageTagItemDispatchHandler OnFinishItem;


        protected Task UpdateTask;
        protected Action UpdateAction;
        protected bool Running = false;
        public List<ImageTagDispatchItem> DispatchQueue = new List<ImageTagDispatchItem>();
        //protected BlockingCollection<ImageTagDispatchItem> DispatchQueue;

        public const string ImageTagDbName = "imagetag.db";
        public const string SettingsName = "Settings.xml";

        public ImageTagEntities Entities;

        public ImageTagSettings Settings;

        public ProcessOutputReport FileProcessData;

        protected OrganizeFilesManifest OrganizeFilesManifest;

        public ImageTag()
        {
            // Load settings
            var settingsPath = Path.Combine(Environment.CurrentDirectory, SettingsName);
            if (File.Exists(settingsPath))
            {
                Settings = ImageTagSettings.LoadFromXml(settingsPath);
            }
            else
            {
                Settings = new ImageTagSettings();
                Settings.InitializeDefaults();
                Settings.SaveToXml(SettingsName);
            }

            Settings.InitializeDirectories();
        }

        public void Initialize()
        {
            var dbPath = Path.Combine(Environment.CurrentDirectory, ImageTagDbName);
            if (!File.Exists(dbPath))
            {

                // Create it!
                var bytes = ImageTagWPF.Properties.Resources.imagetag;

                try
                {
                    File.WriteAllBytes(dbPath, bytes);
                }
                catch (Exception ex)
                {
                    App.Log.Error("Error writing new DB: " + ex.Message);
                    return;
                }
            }

            try
            {
                Entities = new ImageTagEntities();
            }
            catch (Exception ex)
            {
                App.Log.Error("Error creating DbContext: " + ex.Message);
                return;
            }


        }



        public void Enqueue(ImageTagDispatchItem item)
        {
            DispatchQueue.Add(item);
            OnStartItem?.Invoke(item);
        }

        public void Dequeue(ImageTagDispatchItem item)
        {
            DispatchQueue.Remove(item);
            OnFinishItem?.Invoke(item);
        }

        public void Start()
        {
            if (!Running)
            {
                Running = true;
                UpdateAction = ThreadUpdate;
                UpdateTask = new Task(UpdateAction);
                UpdateTask.Start();
            }
        }

        public void Stop()
        {
            int MillisecondsTimeout = 2000;
            if (Running && UpdateTask != null)
            {
                Running = false;
                var result = UpdateTask.Wait(MillisecondsTimeout);
                //var result = Thread.Join(MillisecondsTimeout);
                if (!result)
                {
                    App.Log.Error("Couldn't stop task after " + MillisecondsTimeout/1000 +
                                  " seconds. Waiting indefinitely...");

                    //Thread.Join();
                    UpdateTask.Wait();
                }
            }
        }


        protected void ThreadUpdate()
        {
            while (Running)
            {
                ImageTagDispatchItem item = null;
                if (DispatchQueue.Count > 0)
                {
                    item = DispatchQueue[0];
                    if (item != null)
                    {
                        item.IsRunning = true;


                        item.Action();

                        DispatchQueue.RemoveAt(0);

                        //var token = CancellationTokenSource.Token;
                        //item.Token = token;

                        //ThreadPool.QueueUserWorkItem(new WaitCallback(state => { item.Action(); }), token);

                        //App.Log.Info("Executed task " + item.Description + " OK.");

                        item.IsRunning = false;
                        OnFinishItem?.Invoke(item);
                        
                        item.Finish();

                        App.CancellationTokenSource.Dispose();
                        App.CancellationTokenSource = new CancellationTokenSource();
                    }
                }
            }
        }

        public Color GetColorForTagType(TagType type)
        {
            Color color = Colors.Gray;

            var result = Settings.TagCategoryColors.FirstOrDefault(x => x.TagType == type);
            if (result != null)
            {
                color = result.FontColor;
            }
            return color;
        }


        public void SaveSettings()
        {
            if (Settings != null)
            {
                Settings.SaveToXml(SettingsName);
            }
        }


        public void PersistData()
        {
            try
            {
                App.ImageTag.Entities.SaveChanges();
                App.Log.Info("Saved to database OK.");
            }
            catch (Exception ex)
            {
                var msg = "Error saving to database: " + ex.Message;
                App.Log.Error(msg);

                MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




        public void OrganizeImages(bool suppressSuccessMessages = false)
        {
            var rootDir = App.ImageTag.Entities.OrganizeDirectories.FirstOrDefault(x => x.ParentDirectories.Count == 0);
            if (rootDir != null)
            {
                if (!Directory.Exists(rootDir.Name))
                {
                    try
                    {
                        Directory.CreateDirectory(rootDir.Name);
                    }
                    catch (Exception ex)
                    {
                        App.Log.Error("Couldn't create root directory: " + rootDir.Name + " : " + ex.Message);
                        return;
                    }
                }

                FileProcessData = new ProcessOutputReport()
                {
                    OperationTitle = "Organizing images for directory: " + rootDir.Name
                };

                OrganizeFilesManifest = new OrganizeFilesManifest();


                App.Log.Info(" ");
                App.Log.Info("================================================");
                App.Log.Info("Organizing images for directory: " + rootDir.Name);
                App.Log.Info("================================================");





                App.Log.Info("Organizing images...");

                ClearCopyDirectories();


                //OrganizeImagesForDir(rootDir, string.Empty, App.ImageTag.Entities.Images, suppressSuccessMessages);

                GetOrganizeManifestForDir(rootDir, string.Empty, App.ImageTag.Entities.Images);
                
                OrganizeImagesForManifest(OrganizeFilesManifest, suppressSuccessMessages);

                PersistData();

                App.Log.Info("Finished organizing files.");

            }
        }

        protected void AddOrganizeManifestFile(OrganizeFile organizeFile)
        {
            string id = organizeFile.ID;
            if (!OrganizeFilesManifest.Files.ContainsKey(id))
            {
                OrganizeFilesManifest.Files.Add(id, organizeFile);
            }
        }


        protected OrganizeFile GetOrganizeFileByID(string id)
        {
            if (OrganizeFilesManifest.Files.ContainsKey(id))
            {
                return OrganizeFilesManifest.Files[id];
            }
            return null;
        }




        protected void GetOrganizeManifestForDir(OrganizeDirectory dir, string cumulativePath,
            IQueryable<Image> cumulativeQuery)
        {
            var fullPath = dir.Name;
            if (!String.IsNullOrEmpty(cumulativePath))
            {
                fullPath = Path.Combine(cumulativePath, dir.Name);
            }
            string msg;


            // Find all items that have our tags
            var tags = dir.Tags.Select(x => (int) x.ID).ToList();
            if (tags.Count > 0)
            {
                var imageResults = new List<Image>();

                if (dir.IgnoreParent != 0)
                {
                    // Get from all images
                    cumulativeQuery = App.ImageTag.Entities.Images;
                }

                // Filter by rating
                bool useRating = dir.Rating.HasValue;
                if (useRating)
                {
                    int rating = (int) dir.Rating;
                    cumulativeQuery = cumulativeQuery.Where(x => x.Rating.HasValue && (int) x.Rating.Value >= rating);
                }

                // Filter by tags
                if (dir.TheseTagsOnly != 0)
                {
                    imageResults =
                        cumulativeQuery.Where(y => tags.All(t => y.Tags.All(mt => (int) mt.ID == t))).ToList();
                }
                else
                {
                    if (dir.OrTags != 0) // Get OR
                    {
                        imageResults =
                            cumulativeQuery.Where(y => tags.Any(t => y.Tags.Any(mt => (int) mt.ID == t))).ToList();
                    }
                    else
                    {
                        imageResults = // Get AND
                            cumulativeQuery.Where(y => tags.All(t => y.Tags.Any(mt => (int) mt.ID == t))).ToList();
                    }

                }


                foreach (var imageResult in imageResults)
                {
                    if (File.Exists(imageResult.Path))
                    {
                        var targetPath = Path.Combine(fullPath, Path.GetFileName(imageResult.Path));

                        string id = imageResult.ID.ToString("D0");
                        var organizeFile = GetOrganizeFileByID(id);
                        if (organizeFile == null)
                        {
                            organizeFile = new OrganizeFile() { Image = imageResult };

                            AddOrganizeManifestFile(organizeFile);
                        }

                        
                        if (dir.CopyOnly != 0)
                        {


                            organizeFile.AddOperation(new OrganizeOperation()
                            {
                                Destination = targetPath,
                                Operation = OrganizeOperationType.Copy
                            });
                        }
                        else
                        {
                            organizeFile.AddOperation(new OrganizeOperation()
                            {
                                Destination = targetPath,
                                Operation = OrganizeOperationType.Move
                            });
                        }
                        
                    }
                    else
                    {
                        // Couldn't find image in db                    
                        msg = "Couldn't find image in DB: " + imageResult.Path;
                        App.Log.Error(msg);

                        FileProcessData.Operations.Add(new ProcessOperation()
                        {
                            Message = msg,
                            Output = ProcessRecommendedOutput.FindOrphaned,
                            Severity = FileProcessSeverity.Error,
                            SourceFilename = imageResult.Path
                        });
                    }

                }
            }


            foreach (var organizeDirectory in dir.ChildDirectories)
            {
                GetOrganizeManifestForDir(organizeDirectory, fullPath, cumulativeQuery);
            }
        }


        protected void OrganizeImagesForDir(OrganizeDirectory dir, string cumulativePath,
            IQueryable<Image> cumulativeQuery, bool suppressSuccessMessages = false)
        {
            var fullPath = dir.Name;
            if (!String.IsNullOrEmpty(cumulativePath))
            {
                fullPath = Path.Combine(cumulativePath, dir.Name);
            }
            string msg;


            // Find all items that have our tags
            var tags = dir.Tags.Select(x => (int) x.ID).ToList();
            if (tags.Count > 0)
            {
                var imageResults = new List<Image>();

                if (dir.IgnoreParent != 0)
                {
                    // Get from all images
                    cumulativeQuery = App.ImageTag.Entities.Images;
                }

                bool useRating = dir.Rating.HasValue;
                if (useRating)
                {
                    int rating = (int) dir.Rating;
                    cumulativeQuery = cumulativeQuery.Where(x => x.Rating.HasValue && (int) x.Rating.Value >= rating);
                }


                if (dir.TheseTagsOnly != 0)
                {
                    imageResults =
                        cumulativeQuery.Where(y => tags.All(t => y.Tags.All(mt => (int) mt.ID == t))).ToList();
                }
                else
                {
                    if (dir.OrTags != 0) // Get or
                    {
                        imageResults =
                            cumulativeQuery.Where(y => tags.Any(t => y.Tags.Any(mt => (int) mt.ID == t))).ToList();
                    }
                    else
                    {
                        imageResults = // Get and
                            cumulativeQuery.Where(y => tags.All(t => y.Tags.Any(mt => (int) mt.ID == t))).ToList();
                    }

                }


                foreach (var imageResult in imageResults)
                {
                    if (File.Exists(imageResult.Path))
                    {
                        var targetPath = Path.Combine(fullPath, Path.GetFileName(imageResult.Path));

                        if (targetPath == imageResult.Path)
                            continue;

                        var targetDir = Path.GetDirectoryName(targetPath);

                        if (!Directory.Exists(targetDir))
                        {
                            try
                            {
                                Directory.CreateDirectory(targetDir);

                                if (!suppressSuccessMessages)
                                    App.Log.Info("Created directory OK: " + targetDir);
                            }
                            catch (Exception ex)
                            {
                                msg = "Couldn't create directory: " + targetDir + " : " + ex.Message;
                                App.Log.Error(msg);

                                FileProcessData.Operations.Add(new ProcessOperation()
                                {
                                    Message = msg,
                                    Output = ProcessRecommendedOutput.CheckPermissions,
                                    Severity = FileProcessSeverity.Error,
                                    SourceFilename = targetDir,
                                });
                                continue;
                            }
                        }


                        if (!File.Exists(targetPath))
                        {
                            if (dir.CopyOnly != 0)
                            {
                                // Copy only
                                try
                                {
                                    File.Copy(imageResult.Path, targetPath);
                                }
                                catch (Exception ex)
                                {
                                    msg = "Couldn't copy file: " + imageResult.Path + " : " + ex.Message;
                                    App.Log.Error(msg);

                                    FileProcessData.Operations.Add(new ProcessOperation()
                                    {
                                        Message = msg,
                                        Output = ProcessRecommendedOutput.CheckPermissions,
                                        Severity = FileProcessSeverity.Error,
                                        SourceFilename = imageResult.Path,
                                        DestinationFilename = targetPath
                                    });

                                    continue;
                                }


                                msg = "Copied file OK from " + imageResult.Path + "  to  " + targetPath + " . Tags: " +
                                      String.Join(", ", dir.Tags.Select(x => x.Name).ToArray());

                                if (!suppressSuccessMessages)
                                    App.Log.Info(msg);

                                FileProcessData.Operations.Add(new ProcessOperation()
                                {
                                    Message = msg,
                                    Output = ProcessRecommendedOutput.None,
                                    Severity = FileProcessSeverity.Info,
                                    SourceFilename = imageResult.Path,
                                    DestinationFilename = targetPath
                                });
                            }
                            else
                            {
                                // Move the file and update database
                                try
                                {
                                    File.Move(imageResult.Path, targetPath);
                                }
                                catch (Exception ex)
                                {
                                    msg = "Couldn't move file: " + imageResult.Path + " : " + ex.Message;
                                    App.Log.Error(msg);

                                    FileProcessData.Operations.Add(new ProcessOperation()
                                    {
                                        Message = msg,
                                        Output = ProcessRecommendedOutput.CheckPermissions,
                                        Severity = FileProcessSeverity.Error,
                                        SourceFilename = imageResult.Path,
                                        DestinationFilename = targetPath
                                    });

                                    continue;
                                }

                                // Update directory for object
                                imageResult.Path = targetPath;


                                msg = "Moved file OK from " + imageResult.Path + "  to  " + targetPath + " . Tags: " +
                                      String.Join(", ", dir.Tags.Select(x => x.Name).ToArray());

                                if (!suppressSuccessMessages)
                                    App.Log.Info(msg);

                                FileProcessData.Operations.Add(new ProcessOperation()
                                {
                                    Message = msg,
                                    Output = ProcessRecommendedOutput.None,
                                    Severity = FileProcessSeverity.Info,
                                    SourceFilename = imageResult.Path,
                                    DestinationFilename = targetPath
                                });

                            }

                        }
                        else
                        {
                            msg = "File already existed: " + targetPath + "\t  from " + imageResult.Path + " .\t Tags: " +
                                  String.Join(", ", dir.Tags.Select(x => x.Name).ToArray());
                            App.Log.Error(msg);

                            FileProcessData.Operations.Add(new ProcessOperation()
                            {
                                Message = "File already existed: " + targetPath,
                                Output = ProcessRecommendedOutput.CompareFiles,
                                Severity = FileProcessSeverity.Error,
                                SourceFilename = imageResult.Path,
                                DestinationFilename = targetPath
                            });
                        }

                    }
                    else
                    {
                        msg = "Couldn't find image in DB: " + imageResult.Path;
                        App.Log.Error(msg);

                        FileProcessData.Operations.Add(new ProcessOperation()
                        {
                            Message = msg,
                            Output = ProcessRecommendedOutput.FindOrphaned,
                            Severity = FileProcessSeverity.Error,
                            SourceFilename = imageResult.Path
                        });
                    }

                }
            }


            foreach (var organizeDirectory in dir.ChildDirectories)
            {
                OrganizeImagesForDir(organizeDirectory, fullPath, cumulativeQuery, suppressSuccessMessages);
            }
        }



        protected void OrganizeImagesForManifest(OrganizeFilesManifest manifest, bool suppressSuccessMessages = false)
        {
            var items = manifest.Files.Values.ToList();

            string msg = null;
            foreach (var manifestItem in items)
            {
                if (manifestItem.Image != null && File.Exists(manifestItem.Image.Path))
                {
                    var orderedOperations = manifestItem.Operations.OrderByDescending(x => x.Operation);

                    foreach (var operation in orderedOperations)
                    {
                        // Don't move or copy if same
                        if (manifestItem.Image.Path.ToLowerInvariant().Trim() == operation.Destination.ToLowerInvariant().Trim())
                            continue;


                        var targetDir = Path.GetDirectoryName(operation.Destination);

                        if (!Directory.Exists(targetDir))
                        {
                            try
                            {
                                Directory.CreateDirectory(targetDir);

                                if (!suppressSuccessMessages)
                                    App.Log.Debug("Created directory OK: " + targetDir);
                            }
                            catch (Exception ex)
                            {
                                msg = "Couldn't create directory: " + targetDir + " : " + ex.Message;
                                App.Log.Error(msg);

                                FileProcessData.Operations.Add(new ProcessOperation()
                                {
                                    Message = msg,
                                    Output = ProcessRecommendedOutput.CheckPermissions,
                                    Severity = FileProcessSeverity.Error,
                                    SourceFilename = targetDir,
                                });
                                continue;
                            }
                        }


                        if (!File.Exists(operation.Destination))
                        {
                            if (operation.Operation == OrganizeOperationType.Copy)
                            {
                                // Copy only
                                try
                                {
                                    File.Copy(manifestItem.Image.Path, operation.Destination);
                                }
                                catch (Exception ex)
                                {
                                    msg = "Couldn't copy file: " + manifestItem.Image.Path + " to " +
                                          operation.Destination + " : " + ex.Message;
                                    App.Log.Error(msg);

                                    FileProcessData.Operations.Add(new ProcessOperation()
                                    {
                                        Message = msg,
                                        Output = ProcessRecommendedOutput.CheckPermissions,
                                        Severity = FileProcessSeverity.Error,
                                        SourceFilename = manifestItem.Image.Path,
                                        DestinationFilename = operation.Destination
                                    });

                                    continue;
                                }


                                msg = "Copied file OK from " + manifestItem.Image.Path + "  to  " +
                                      operation.Destination;
                                //+ " . Tags: " + String.Join(", ", dir.Tags.Select(x => x.Name).ToArray());

                                if (!suppressSuccessMessages)
                                    App.Log.Debug(msg);

                                FileProcessData.Operations.Add(new ProcessOperation()
                                {
                                    Message = msg,
                                    Output = ProcessRecommendedOutput.None,
                                    Severity = FileProcessSeverity.Info,
                                    SourceFilename = manifestItem.Image.Path,
                                    DestinationFilename = operation.Destination
                                });
                            }
                            else
                            {
                                // Move the file and update database
                                try
                                {
                                    File.Move(manifestItem.Image.Path, operation.Destination);
                                }
                                catch (Exception ex)
                                {
                                    msg = "Couldn't move file: " + manifestItem.Image.Path + " to " +
                                          operation.Destination + " : " + ex.Message;
                                    App.Log.Error(msg);

                                    FileProcessData.Operations.Add(new ProcessOperation()
                                    {
                                        Message = msg,
                                        Output = ProcessRecommendedOutput.CheckPermissions,
                                        Severity = FileProcessSeverity.Error,
                                        SourceFilename = manifestItem.Image.Path,
                                        DestinationFilename = operation.Destination
                                    });

                                    continue;
                                }

                                // Update directory for object
                                manifestItem.Image.Path = operation.Destination;

                                msg = "Moved file OK from " + manifestItem.Image.Path + "  to  " + operation.Destination;
                                //+ " . Tags: " + String.Join(", ", dir.Tags.Select(x => x.Name).ToArray());

                                if (!suppressSuccessMessages)
                                    App.Log.Debug(msg);

                                FileProcessData.Operations.Add(new ProcessOperation()
                                {
                                    Message = msg,
                                    Output = ProcessRecommendedOutput.None,
                                    Severity = FileProcessSeverity.Info,
                                    SourceFilename = manifestItem.Image.Path,
                                    DestinationFilename = operation.Destination
                                });
                            }
                        }
                        else
                        {
                            // File existed for this filename - prompt to replace?
                            if (operation.Operation == OrganizeOperationType.Move)
                            {
                                // Update directory for object
                                msg = "File existed on move from " + manifestItem.Image.Path + "  to  " + operation.Destination;
                                App.Log.Warn(msg);

                                FileProcessData.Operations.Add(new ProcessOperation()
                                {
                                    Message = msg,
                                    Output = ProcessRecommendedOutput.CompareFiles,
                                    Severity = FileProcessSeverity.Warn,
                                    SourceFilename = manifestItem.Image.Path,
                                    DestinationFilename = operation.Destination
                                });
                            }
                        }
                    }
                }
                else
                {
                }
            }
        }


        public void ClearCopyDirectories()
        {
            var dirs = App.ImageTag.Entities.OrganizeDirectories.Where(x => x.CopyOnly != 0);
            foreach (var organizeDirectory in dirs)
            {
                var fullDir = organizeDirectory.GetFullDirectory();
                if (Directory.Exists(fullDir))
                {
                    string[] filePaths = Directory.GetFiles(fullDir);
                    if (filePaths.Length > 0)
                        App.Log.Info("Clearing copy directory: " + fullDir);

                    foreach (string filePath in filePaths)
                    {
                        try
                        {
                            File.Delete(filePath);
                        }
                        catch (Exception ex)
                        {
                            App.Log.Error("Couldn't delete file in copy directory: " + fullDir + ": " + ex.Message);
                            throw ex;
                        }
                    }
                }
            }
        }

        public void FindOrphanedRecords()
        {
            var rootDir = App.ImageTag.Entities.OrganizeDirectories.FirstOrDefault(x => x.ParentDirectories.Count == 0);
            if (rootDir != null)
            {
                if (!Directory.Exists(rootDir.Name))
                {
                    try
                    {
                        Directory.CreateDirectory(rootDir.Name);
                    }
                    catch (Exception ex)
                    {
                        App.Log.Error("Couldn't create root directory: " + rootDir.Name + " : " + ex.Message);
                        return;
                    }
                }
            }


            FileProcessData = new ProcessOutputReport()
            {
                OperationTitle = "Finding orphaned image records"
            };

            App.Log.Info(" ");
            App.Log.Info("================================================");
            App.Log.Info("Finding orphaned image records...");
            App.Log.Info("================================================");

            foreach (var entitiesImage in App.ImageTag.Entities.Images)
            {
                if (!File.Exists(entitiesImage.Path))
                {
                    var msg = "Found orphaned record, looking for matching file: " + entitiesImage.Path;
                    App.Log.Info(msg);

                    var filename = Path.GetFileName(entitiesImage.Path);
                    var files = Directory.EnumerateFiles(rootDir.Name, filename, SearchOption.AllDirectories);

                    bool found = false;
                    foreach (var file in files)
                    {
                        var checksum = Util.GetFileHashSHA1(file);

                        if (checksum == entitiesImage.Checksum)
                        {
                            msg = "Found matching file, updating: " + file;
                            App.Log.Info(msg);

                            entitiesImage.Path = file;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        App.Log.Info("Couldn't find matching image for: " + entitiesImage.Path);

                        FileProcessData.Operations.Add(new ProcessOperation()
                        {
                            Message = msg,
                            Output = ProcessRecommendedOutput.FindOrphaned,
                            Severity = FileProcessSeverity.Error,
                            SourceFilename = entitiesImage.Path
                        });
                    }
                }
            }

            App.Log.Info("Searched for orphaned records completed.");

            PersistData();


        }

        public void UpdateParentTags()
        {
            App.Log.Info(" ");
            App.Log.Info("================================================");
            App.Log.Info("Updating parent tags for ALL images...");
            App.Log.Info("================================================");

            foreach (var entitiesImage in App.ImageTag.Entities.Images)
            {
                Util.UpdateImageParentTags(entitiesImage);
            }

            PersistData();

            App.Log.Info("Parent tags for all images updated OK.");
        }

        public void ConsolidateDuplicateFiles(bool ignoreFilename = false, bool deleteFileToo = false)
        {

            App.Log.Info(" ");
            App.Log.Info("================================================");
            App.Log.Info("Consolidating records with duplicate checksum and filename");
            App.Log.Info("================================================");

            var dupeChecksums = from i in App.ImageTag.Entities.Images
                group i by new {i.Checksum}
                into grp
                where grp.Count() > 1
                select grp.Key;

            foreach (var checksum in dupeChecksums)
            {
                var images = App.ImageTag.Entities.Images.Where(x => x.Checksum == checksum.Checksum).ToList();

                var imageName = string.Empty;
                var fullImagePath = string.Empty;
                bool allSameFilename = true;

                if (!ignoreFilename)
                {
                    foreach (var image in images)
                    {
                        var thisFilename = Path.GetFileName(image.Path).ToLowerInvariant();
                        if (String.IsNullOrEmpty(imageName))
                        {
                            imageName = thisFilename;
                            fullImagePath = image.Path;
                        }
                        else if (thisFilename != imageName.ToLowerInvariant())
                        {
                            App.Log.Error("Found dupe file with different filenames, continuing: " + image.Path + ", " +
                                          fullImagePath);
                            allSameFilename = false;
                            break;
                        }
                    }
                }


                if (ignoreFilename || allSameFilename)
                {
                    // consolidate tags
                    var firstImage = images[0];

                    App.Log.Info("Removing duplicate records for: " + firstImage.Path);

                    var removeList = new List<Image>();
                    foreach (var image in images)
                    {
                        if (image == firstImage)
                            continue;

                        foreach (var imageTag in image.Tags)
                        {
                            firstImage.Tags.Add(imageTag);
                            // ratings too
                            if (!firstImage.Rating.HasValue && image.Rating.HasValue)
                            {
                                firstImage.Rating = image.Rating;
                            }

                            removeList.Add(image);
                        }
                    }

                    foreach (var image in removeList)
                    {
                        App.ImageTag.Entities.Images.Remove(image);
                        App.Log.Info("Removed duplicate record for: " + image.Path);


                        if (deleteFileToo)
                        {
                            try
                            {
                                File.Delete(image.Path);
                                App.Log.Info("Deleted duplicate file: " + image.Path);
                            }
                            catch (Exception ex)
                            {
                                App.Log.Error("Couldn't delete duplicate file: " + image.Path + ": " + ex.Message);
                            }
                        }

                    }
                }
            }



            PersistData();

            App.Log.InfoFormat("Finished consolidating duplicate records.");
        }




        public void ReplaceDir(string search, string replace)
        {
            App.Log.Info(" ");
            App.Log.Info("================================================");
            App.Log.Info("Replace directory: " + search + "  with  " + replace);
            App.Log.Info("================================================");


            var foundItems = App.ImageTag.Entities.Images.Where(x => x.Path.ToLower().StartsWith(search.ToLower()));
            foreach (var foundItem in foundItems)
            {
                var newPath = foundItem.Path.Replace(search, replace);

                // Try find a duplicate

                // Check new file exists
                if (!File.Exists(newPath))
                {
                    App.Log.Error("File did not exist: " + newPath);
                }
                else
                {
                    var checksum = Util.GetFileHashSHA1(newPath);
                    if (checksum == foundItem.Checksum)
                    {
                        foundItem.Path = newPath;

                        App.Log.Info("Changed directory to: " + newPath);
                    }
                    else
                    {
                        App.Log.Error("Checksums didn't match for: " + newPath + ", " + foundItem.Path);
                    }
                }
            }


            PersistData();
            App.Log.Info("Finished replacing directory.");
        }

        public void FindOrphanedFiles()
        {
            string rootDir = App.ImageTag.Settings.DefaultDirectory;

            var dir = App.ImageTag.Entities.OrganizeDirectories.FirstOrDefault(x => x.ParentDirectories.Count == 0);
            if (dir != null)
            {
                rootDir = dir.Name;
            }

            var token = App.CancellationTokenSource.Token;

            if (!Directory.Exists(rootDir))
                return;

            App.Log.Info(" ");
            App.Log.Info("================================================");
            App.Log.Info("Find orphaned files for: " + rootDir);
            App.Log.Info("================================================");

            var fileExts = App.ImageTag.Settings.Extensions.Select(x => x.Extension).Select(x => x.Substring(1, x.Length - 1)).ToArray();

            int index = 0;
            int pageSize = 1000;

            bool done = false;
            while (!done)
            {
                bool filesFound = false;
                var files = Directory.EnumerateFiles(rootDir, "*.*", SearchOption.AllDirectories)
                    .Where(x => fileExts.Any(x.EndsWith)).Skip(index).Take(pageSize).Select(x => x.ToLower()).ToList();

                var existingImages = App.ImageTag.Entities.Images.Where(x => files.Contains(x.Path.ToLower())).ToList();
                if (existingImages.Count != files.Count)
                {
                    // Must be a missing file in this batch; find it!
                    var missingFiles =
                        files.Where(file => !existingImages.Any(ex => file.ToLower() == ex.Path.ToLower()));
                    foreach (var missingFile in missingFiles)
                    {
                        // Orphaned
                        App.Log.Info("Orphaned file: " + missingFile);
                    }
                }


                if (files.Count == 0 || files.Count < pageSize)
                {
                    done = true;
                    break;
                }


                if (token.IsCancellationRequested)
                {
                    App.Log.Error("Aborted finding orphaned files.");
                    return;
                }

                /*
                foreach (var fileInfo in files)
                {
                    filesFound = true;
                    if (!App.ImageTag.Entities.Images.Any(x => x.Path.ToLower() == fileInfo.ToLower()))
                    {
                        // Orphaned
                        App.Log.Info("Orphaned file: " + fileInfo);
                    }
                }*/


                index += pageSize;
            }

            App.Log.Info("Finished finding orphaned files.");
        }


        public void CheckFileDupesTest()
        {
            string file1 = @"T:\tagged\vg girls\ruto\db949157b51ff370afb7af71f0fb9709.jpg";
            string file2 = @"T:\tagged\vg girls\ruto\1467537285648.jpg";

            var img1 = new Bitmap(file1);
            var img2 = new Bitmap(file2);

            //var diffVal = XnaFan.ImageComparison.ImageTool.GetPercentageDifference(file1, file2);
            var hash1 = Util.GetMD5Hash(file1);
            var hash2 = Util.GetMD5Hash(file2);

            if (hash1 != hash2)
            {
                App.Log.Info("Files different: " + file1 + ", " + file2);
            }
            else
            {
                App.Log.Info("Files same: " + file1 + ", " + file2);
            }

            file2 = @"T:\tagged\vg girls\ruto\587046bf956493c50594eb588a0c6411.jpeg";

            hash2 = Util.GetMD5Hash(file2);


            if (hash1 != hash2)
            {
                App.Log.Info("Files different: " + file1 + ", " + file2);
            }
            else
            {
                App.Log.Info("Files same: " + file1 + ", " + file2);
            }

        }


        public void FindDuplicateFilesByContent()
        {
            var dupeFinder = new ImageDuplicateFinder();
            dupeFinder.FindDuplicateFilesByContent();

            FileProcessData = dupeFinder.FileProcessData;
            
        }

        public void DelistDirectory(string directory)
        {
            directory = directory.Trim().ToLower();
            if (!directory.EndsWith("\\"))
                directory += "\\";

            var dirName = Path.GetDirectoryName(directory);

            if (!String.IsNullOrEmpty(dirName))
            {

                var existingImages = App.ImageTag.Entities.Images.Where(x => x.Path.ToLower().StartsWith(dirName)).ToList();
                long fileCount = 0;
                foreach (var existingImage in existingImages)
                {
                    var imageDir = Path.GetDirectoryName(existingImage.Path).ToLower();
                    if (dirName == imageDir)
                    {
                        App.ImageTag.Entities.Images.Remove(existingImage);
                        fileCount++;
                    }
                }

                PersistData();

                App.Log.Info("Untagged " + fileCount + " images.");
                App.Log.Info("Untagged directory OK: " + dirName);
            }
        }
    }
}
