
BEGIN TRANSACTION;

CREATE TABLE "Image" (
	"ID"	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
	"Path"	TEXT NOT NULL UNIQUE,
	"Checksum"	TEXT NOT NULL,
	"Rating"	INTEGER,
	"Explicit" INTEGER
);

CREATE TABLE "Tag" (
	"ID"	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
	"Name"	TEXT NOT NULL UNIQUE,
	"Description"	TEXT,
	"TagType"	INTEGER NOT NULL DEFAULT 0,
	"LastUsed" DATETIME
);

CREATE TABLE "ImageTag" (
	"ImageID"	INTEGER NOT NULL,
	"TagID"	INTEGER NOT NULL,
	PRIMARY KEY(ImageID,TagID),
  FOREIGN KEY(ImageID) REFERENCES Image(ID),
  FOREIGN KEY(TagID) REFERENCES Tag(ID)
);

CREATE TABLE "TagParent" (
	"ParentID"	INTEGER NOT NULL,
	"ChildID"	INTEGER NOT NULL,
	PRIMARY KEY(ParentID,ChildID),
  FOREIGN KEY(ParentID) REFERENCES Tag(ID),
  FOREIGN KEY(ChildID) REFERENCES Tag(ID)
);


CREATE UNIQUE INDEX "TagID_IDX" ON "Tag" ("ID" );
CREATE UNIQUE INDEX "ImageID_IDX" ON "Image" ("ID" );

CREATE UNIQUE INDEX "TagName_IDX" ON "Tag" ("Name" );
CREATE INDEX "ImageChecksum_IDX" ON "Image" ("Checksum" );
CREATE UNIQUE INDEX "ImagePath_IDX" ON "Image" ("Path" );

CREATE INDEX "TagLastUsed_IDX" ON "Tag" ("LastUsed" );


CREATE UNIQUE INDEX "ImageTag_IDX" ON "ImageTag" ("ImageID", "TagID" );
CREATE UNIQUE INDEX "TagParent_IDX" ON "TagParent" ("ParentID", "ChildID" );



CREATE TABLE "OrganizeDirectory" (
	"ID"	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
	"Name"	TEXT NOT NULL,
	"Rating" INTEGER,
	"Description" TEXT,
	"ForeColor" TEXT,
	"BackColor" TEXT,
 	"IgnoreParent" INTEGER NOT NULL DEFAULT 0,
	"OrTags" INTEGER NOT NULL DEFAULT 0,
	"CopyOnly" INTEGER NOT NULL DEFAULT 0,
	"TheseTagsOnly" INTEGER NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX "OrganizeDirectory_IDX" ON "OrganizeDirectory" ("ID" );


CREATE TABLE "OrganizeDirectoryDirectory" (
	"ParentDirectoryID"	INTEGER NOT NULL,
	"ChildDirectoryID"	INTEGER NOT NULL,
	PRIMARY KEY(ParentDirectoryID, ChildDirectoryID),
	FOREIGN KEY(ParentDirectoryID) REFERENCES OrganizeDirectory(ID),
	FOREIGN KEY(ChildDirectoryID) REFERENCES OrganizeDirectory(ID)
);

CREATE UNIQUE INDEX "OrganizeDirectoryDirectory_IDX" ON "OrganizeDirectoryDirectory" ("ParentDirectoryID", "ChildDirectoryID" );
CREATE INDEX "OrgDirDir_Parent_IDX" ON "OrganizeDirectoryDirectory" ("ParentDirectoryID" );
CREATE INDEX "OrgDirDir_Child_IDX" ON "OrganizeDirectoryDirectory" ("ChildDirectoryID" );


CREATE TABLE "OrganizeDirectoryTags" (
	"DirectoryID"	INTEGER NOT NULL,
	"TagID"	INTEGER NOT NULL,
	PRIMARY KEY(DirectoryID, TagID),
	FOREIGN KEY(DirectoryID) REFERENCES OrganizeDirectory(ID),
	FOREIGN KEY(TagID) REFERENCES Tag(ID)
);

CREATE UNIQUE INDEX "OrganizeDirectoryTags_IDX" ON "OrganizeDirectoryTags" ("DirectoryID", "TagID" );
CREATE INDEX "OrgDirectoryTags_Tag_IDX" ON "OrganizeDirectoryTags" ("TagID" );
CREATE INDEX "OrgDirectoryTags_Dir_IDX" ON "OrganizeDirectoryTags" ("DirectoryID" );


COMMIT;
