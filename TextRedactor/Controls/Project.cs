﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Controls
{
    [Serializable]
    public class Project : Item
    {
        public Project(string Name, DateTime CreateDate)
        {
            this.Name = Name;
            this.CreateDate = CreateDate;
        }

        public Project(string name)
        {
            Name = name;
            CreateDate = DateTime.Now;
        }

        private string name;
        public override string Name
        {
            get { return name; }
            set
            {
                name = value;
            }
        }
        public string Author
        {
            get { return defaultAuthor; }
            set { defaultAuthor = value; }
        }
        private string defaultAuthor = Environment.UserName;

        public DateTime CreateDate { get; private set; }

        public DateTime PublishingDate { get; internal set; }
        public List<LoadedFile> ListFiles {get { return Files; } }

        internal List<LoadedFile> Files = new List<LoadedFile>();
    }

    [Serializable]
    public class LoadedFile
    {
        public string Name { get; set; }
        public string Path { get; set; }

        public LoadedFile(string path, string projectPath)
        {
            if (!File.Exists(path)) { throw new Exception("Wrong path or bad file."); }
            Path = projectPath+"\\Files\\" + System.IO.Path.GetFileName(path);
            Name = System.IO.Path.GetFileNameWithoutExtension(path);
        }
    }
}