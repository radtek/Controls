﻿using System;
using System.IO;

namespace ApplicationLayer
{
    public class FileSystemWorker<T> : IDisposable where T : Item
    {
        private IBasicPanel<T> MainControl;
        private FileSystemQueue<T> Queue;
        private string CurrentProject;
        private string CurrentFile;

        internal string WorkDirectory
        {
            get { return MainControl.CurrentProjectsPath; }
        }

        public FileSystemWorker(IBasicPanel<T> control)
        {
            MainControl = control;
            AttachToControl();
            Queue = new FileSystemQueue<T>(control);
        }

        public void Dispose()
        {
            DettachFromControl();
            Queue.Dispose();
        }

        private void AttachToControl()
        {
            DettachFromControl();
            MainControl.ProjectChanged += OnProjectDeletedRenamedChanged;
            MainControl.ProjectCreated += OnProjectDeletedRenamedChanged;
            MainControl.ProjectRenamed += OnProjectDeletedRenamedChanged;
            MainControl.ProjectDeleted += OnProjectDeletedRenamedChanged;
            MainControl.FileChanged += OnRenamedChanged;
            MainControl.FileDeleted += OnRenamedChanged;
            MainControl.FileRenamed += OnRenamedChanged;
            MainControl.FileCreated += OnRenamedChanged;
        }

        private void DettachFromControl()
        {
            MainControl.ProjectChanged -= OnProjectDeletedRenamedChanged;
            MainControl.ProjectCreated -= OnProjectDeletedRenamedChanged;
            MainControl.ProjectRenamed -= OnProjectDeletedRenamedChanged;
            MainControl.ProjectDeleted -= OnProjectDeletedRenamedChanged;
            MainControl.FileChanged -= OnRenamedChanged;
            MainControl.FileDeleted -= OnRenamedChanged;
            MainControl.FileRenamed -= OnRenamedChanged;
            MainControl.FileCreated -= OnRenamedChanged;
        }

        private void OnRenamedChanged(object sender, FileArgs e)
        {
            if (e.RenamedArgs != null)
            {
                Queue.AddTask(Path.Combine(WorkDirectory, e.Project), e, e.Callback);
                return;
            }
            e.Files.ForEach(file => Queue.AddTask(Path.Combine(WorkDirectory, e.Project), e, e.Callback));
        }

        private void OnProjectDeletedRenamedChanged(object sender, ProjectArgs e)
        {
            Queue.AddTask(Path.Combine(WorkDirectory, e.Project), e, e.Callback);
        }
    }
}
