# ImageTagWPF

A simple, portable image tagger written in WPF for .NET 4.6.1. Uses a local SQLite database to store all data.

It requires read/write permissions in the folder it is run from.

**Warning: This is an amateur effort - make a backup of your images if you use them with this program!**

Please note there are known performance problems with some of the bulk image processing operations.

[Download latest zip](../../raw/master/installer/ImageTagWPF.zip)

### Image Tagging

Select the "Image Tagging" tab to start tagging images.

![screenshot](https://raw.githubusercontent.com/drogoganor/ImageTagWPF/master/images/imagetag.jpg)

Select a folder to explore using the Pick button on the top left.

You can explore the folder tree by clicking a folder name on the left pane. Images for the selected directory will be loaded in the main explore pane.

Select an image (or multiple images with ctrl+click or shift+click), then select a tag from the list of tags (top). You can select more than one tag. Tags will be applied to all selected images.

**Please note that tagging a lot of images takes time.** You can see which tagging processes are running at the bottom of the window.

### Image Explore

Select the "Image Explore" tab to search for images by tag(s).

![screenshot](https://raw.githubusercontent.com/drogoganor/ImageTagWPF/master/images/exploreimages.jpg)

Select tags from the top right to add them to your search. You can optionally specify a minimum rating to search for.

Press search to return images matching your search terms.

If you want to change the tags or rating of a selected image, you can check the Edit Mode checkbox, then select tags and/or rating. Changes will be saved immediately. You must uncheck Edit Mode to search again. 

**Warning: There is a known bug using Edit Mode while a process operation is underway - the app will crash.**

You can drag and drop selected images from this window into other applications or windows. Alternatively, you can drag and drop selected images by dragging one of the grey "Drag Copy" panels at the top right.

### Edit Tags

Select the "Tags" tab to add, change, and delete tags.

![screenshot](https://raw.githubusercontent.com/drogoganor/ImageTagWPF/master/images/tagmanage.jpg)

You can alter a tag by selecting it from the list on the left, setting the name, type or description of the tag, then saving changes by clicking Save. You can create a new tag by clicking Create New instead. Finally, you can delete a tag by selecting it then clicking Delete. Be careful with this because it will remove this tag from all images linked to it.

Tags also have a heirarchy - you can specify child tags for each tag using the list on the right. When tagging an image, all parent tags will also be added to the image tags automatically.

### Output Folders

Select the "Output Folders" tab to specify output directories for tagged images.

![screenshot](https://raw.githubusercontent.com/drogoganor/ImageTagWPF/master/images/outputfolders.jpg)

On this tab you can construct an output directory tree for your tagged images. 

Create a root node by entering a directory in the Name field, then clicking Add New. Please note that it is a good idea to make a new directory that is only for your tagged images.

Then, add child nodes by selecting the root node, entering details, then Add New again. Or, alter a node then click Save Changes to update it. Finally you can delete a node from the tree by selecting it and clicking Delete. Please note that this does not delete any files or folders.

Each node can have a set of tags and/or a minimum rating that determines what files will be moved into it. There are a number of options:

* Ignore Parent Tags
..* Just find images with the current tags. Unchecking this will also filter by the cumulative parent tags.
* "Or" Search Terms
..* Match images with any of the tags specified. Unchecked requires images to have all of the tags.
* Copy Only
..* Make a copy of matching images and place them in this directory. These images will be untracked.
* These Tags Only
..* Match images that only have these tags, and none other.

**Warning: Setting a node to "Copy Only" will permit the application to delete the contents of this directory when doing an Auto Organize.**

### Process

Select the "Process" tab to organize your images according to your Output Folders, and other operations.

![screenshot](https://raw.githubusercontent.com/drogoganor/ImageTagWPF/master/images/process.jpg)

On this tab you can select from several operations:

* Auto Organize
..* Move images into your Output Folders as necessary.
* Consolidate Duplicates
..* Find identical files by checksum and consolidate into a single record. Uncheck Ignore Filename to require both the filename and checksum be identical.
* Find Orphaned Records
..* Find records where the image couldn't be found on disk. Should auto-resolve if an identical image is found in your tagged images directory.
* Find Orphaned Images
..* Find images in your tagged directory that aren't being tracked in the database.
* Replace Path
..* Replace image paths in the database. Good to use if you moved your images directory. Enter the old directory in Search and the new directory in Replace.
* Update Parent Tags
..* Go through tags of all tagged images and apply any parent tags that might be missing.
* Delist Entire Directory
..* Remove from the database all image records that have a path starting with the Dir value.
* Find Dupes By Content
..* Extremely slow and experimental process to identify duplicate images with the same resolution, but different filesize/checksums. You would be well advised to use a specialized image duplicate finder application instead of this.
* Clear Copy Directories
..* Deletes all files from folders in Output Folders that have "Copy Only" set to true.
* Clear Log
..* Clears the log window.

Some operations won't resolve conflicts automatically, so you might need to proceed to the "Resolve" tab. Here you can select a conflict item, then click the Resolve Issue button to try and solve the conflict. For a duplicate file, you will be prompted to select which file to keep.

# Thanks to

https://github.com/teichgraf/WriteableBitmapEx/

https://www.codeproject.com/Articles/374386/Simple-image-comparison-in-NET

https://stackoverflow.com/questions/5936628/get-items-in-view-within-a-listbox

