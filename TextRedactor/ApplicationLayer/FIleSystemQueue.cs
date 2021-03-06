﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace ApplicationLayer
{
    internal class FileSystemQueue<T> : IDisposable where T : Item
    {
        private List<FileSystemTask> CallStack;
        private IBasicPanel<Project> Control;
        private readonly string NoteExtension = ".not";
        private Thread QueueThread;
        private bool Work = true;

        public FileSystemQueue(IBasicPanel<T> control)
        {
            CallStack = new List<FileSystemTask>();
            Control = control as IBasicPanel<Project>;
            QueueThread = new Thread(StartWork);
            QueueThread.Name = "Thread of FileSystemQueue";
            QueueThread.Start();
        }

        internal void AddTask(string path, EventArgs args, Action<bool, string, EventArgs> callback, Priority priority = Priority.Normal)
        {
            CallStack.Add(new FileSystemTask(path,args, callback, Thread.CurrentThread, priority));
        }

        private void StartWork()
        {
            while (Work || CallStack.Any())
            {
                if (CallStack.Count == 0) { continue; }
                FileSystemTask task = GetTaskByPriority();
                if(task == null) { continue; }
                string message = "";
                task.DoFeedBack(DoParseAndDo(task, ref message), message, task.Args);
                CallStack.Remove(task);
            }
        }

        private FileSystemTask GetTaskByPriority()
        {
            FileSystemTask task;
            for (int i = 0; i < Enum.GetValues(typeof(Priority)).Length; i++)
            {
                task = CallStack.Find(item => item.Priority == (Priority)i);
                if (task != null) { return task; }
            }
            return null;
        }

        private bool Do(Happened happened, bool isFolder, EventArgs args, ref string message)
        {
            switch (happened)
            {
                case Happened.Created:
                {
                    return isFolder
                        ? BrowseSystem.CreateProject((ProjectArgs)args, Control, ref message)
                        : BrowseSystem.CreateFile((FileArgs)args, Control, ref message);
                }
                case Happened.Deleted:
                    {
                        return isFolder
                        ? TransactionDirectory.RemoveDirectory(Path.Combine(Control.FSWorker.WorkDirectory, ((ProjectArgs)args).Project))
                        : TransactionFile.DeleteFiles((FileArgs)args);
                    }
            }
            return false;
        }

        private bool DoParseAndDo(FileSystemTask task, ref string message)
        {

            if (task.IsFolder == true)
            {
                var args = task.Args as ProjectArgs;
                if (args == null)
                {
                    message = "Invalid cast";
                    return false;
                }
                RenamedArgs renamedArgs = args.RenamedArgs;
                if (renamedArgs != null)
                {
                    return BrowseSystem.RenameProject(args, Control, ref message);
                }
                return Do(args.Happened, true, args, ref message);
            }
            else if (task.IsFolder == false)
            {
                bool resposne = true;
                var args = task.Args as FileArgs;
                if (args == null)
                {
                    message = "Invalid cast";
                    return false;
                }
                RenamedArgs renamedArgs = args.RenamedArgs;
                if (renamedArgs != null)
                {
                    string path = Path.Combine(task.Path, "Files");
                    ComplexTransaction transaction = new ComplexTransaction();
                    transaction.AddOperation(() =>
                    RenameFile(Path.Combine(path, renamedArgs.From + BrowseSystem.Extension), Path.Combine(path, renamedArgs.To + BrowseSystem.Extension)),
                        () =>
                        RenameFile(Path.Combine(path, renamedArgs.To + BrowseSystem.Extension), Path.Combine(path, renamedArgs.From + BrowseSystem.Extension)));
                    if (File.Exists(Path.Combine(path, renamedArgs.From + NoteExtension)))
                    {
                        transaction.AddOperation(
                            () =>
                                RenameFile(Path.Combine(path, renamedArgs.From + NoteExtension),
                                   Path.Combine(path, renamedArgs.To + NoteExtension)),
                            () =>
                                RenameFile(Path.Combine(path, renamedArgs.To + NoteExtension),
                                    Path.Combine(path, renamedArgs.From + NoteExtension)));
                    }
                    transaction.DoOperation();
                    return File.Exists(Path.Combine(path, renamedArgs.To + BrowseSystem.Extension));
                    //bool res =  TransactionFile.MoveFile(
                    //        Path.Combine(path, renamedArgs.From + BrowseSystem.Extension), Path.Combine(path, renamedArgs.To + BrowseSystem.Extension),
                    //        ref message);
                    //if(!File.Exists(Path.Combine(path, renamedArgs.From + NoteExtension))) { return res; }
                    //     res &= TransactionFile.MoveFile(
                    //               Path.Combine(path, renamedArgs.From + NoteExtension),
                    //               Path.Combine(path, renamedArgs.To + NoteExtension),
                    //               ref message);
                    //    renamedArgs = new RenamedArgs(Path.Combine(path, renamedArgs.From + BrowseSystem.Extension), Path.Combine(path, renamedArgs.To + BrowseSystem.Extension), renamedArgs.Callback);
                    // return res;
                }
                    resposne = Do(args.Happened, false, args, ref message);
                return resposne;
            }
            message = "Invalid parameter";
            return false;
        }

        private bool RenameFile(string pathFrom, string pathTo)
        {
            string message = string.Empty;
            return TransactionFile.MoveFile(
                            pathFrom, pathTo,
                            ref message);
        }

        public void Dispose()
        {
            Work = false;
            QueueThread.Join();
            QueueThread.Abort();
        }
    }
}
