﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Image = System.Drawing.Image;

namespace Controls
{
    /// <summary>
    /// Логика взаимодействия для NotesBrowser.xaml
    /// </summary>
    public partial class NotesBrowser : BasicPanel<Note>
    {
        private BinaryFormatter serializer = new BinaryFormatter();
        public NotesBrowser()
        {
            InitializeComponent();
        }
        protected override void OnSave(string Name)
        {
            CloseNotes(Name);
        }
        internal void CloseNotes(string path)
        {
            using (var stream = File.OpenWrite(path))
            {
                serializer.Serialize(stream, Notes);
            }
        }

        internal override string GenerateName(string name)
        {
            string generattingName = name;
            int i = 0;
            while (true)
            {
                if (!ItemsCollection.ContainsKey(generattingName)) { return generattingName; }
                i++;
                generattingName = name + i;

            }
        }

        internal void LoadNotes(string path)
        {
            BinaryFormatter serializer = new BinaryFormatter();
            if (!File.Exists(path)) {
                Notes.Clear();
                return;
            }
            using (var stream = File.OpenRead(path))
            {
                RefreshNotes(serializer.Deserialize(stream) as Dictionary<string, Note>);
            }
            MainControl.Items.Refresh();
        }

        public void RefreshNotes(Dictionary<string, Note> collection)
        {
            Notes.Clear();
            //for (int i=0;i<Notes.Count;i++)
            //{
            //    RemoveItem(Notes.ElementAt(i).Value.Name);
            //    i--;
            //}
           
            foreach (var note in collection)
            {
                AddItem(note.Value);
            }
            MainControl.Items.Refresh();
        }
        public Image NotePicture { get; set; }
        private void DelNote_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var im = sender as System.Windows.Controls.Image;
            if (im != null)
            {
               // ParentControl.BrowseProject.DelFlag(Notes[im.Tag.ToString()]);
                Notes.Remove(im.Tag.ToString());
                ParentControl.BrowseProject.OpenFile(ParentControl.BrowseProject.CurentFile, Path.GetFileNameWithoutExtension(ParentControl.BrowseProject.CurentFile));
              //  MainControl.Items.Refresh();
            }
        }

        protected override void AddDynamicControls()
        {
            NotesContainer.Children.Add(CloneTextBox);
            NotesContainer.PreviewMouseDown -= CloneTextBox_LostFocus;
            NotesContainer.PreviewMouseDown += CloneTextBox_LostFocus;
            MainControl.IsEnabled = false;
        }

        protected override void RemoveDynamicControls()
        {
            NotesContainer.PreviewMouseDown -= CloneTextBox_LostFocus;
            NotesContainer.Children.Remove(CloneTextBox);
            MainControl.IsEnabled = true;
        }

        protected override void CloneTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            base.CloneTextBox_LostFocus(sender, e);
            if (!IsValid) { return; }
            var mouseEventArgs = e as MouseEventArgs;
            if (mouseEventArgs == null) { return; }
            var positionCLick = mouseEventArgs.GetPosition(Application.Current.MainWindow);
            if (!CloneTextBoxLocation.Contains((int)positionCLick.X, (int)positionCLick.Y))
            {
                if (CloneTextBox.Tag.ToString() != CloneTextBox.Text)
                {
                    string name = CloneTextBox.Tag.ToString();
                    //File.Move(file, Path.GetDirectoryName(file) + "\\" + CloneTextBox.Text + Path.GetExtension(file));
                    Notes.Add(CloneTextBox.Text, Notes[name]);
                    Notes[CloneTextBox.Text].Name = CloneTextBox.Text;
                    Notes.Remove(name);
                }
                MainControl.Items.Refresh();
                EndChangingDynamicItem();
            }
        }

        protected override void OnValidate()
        {
            if (!Notes.ContainsKey(CloneTextBox.Tag.ToString()) || Notes.ContainsKey(CloneTextBox.Text) || !File.Exists(ParentControl.BrowseProject.LoadedFile))
            {
                IsValid = false;
                if (Binding == null) { Binding = CloneTextBox.GetBindingExpression(TextBox.TextProperty); }
                Validation.MarkInvalid(Binding, new ValidationError(new ExceptionValidationRule(), Binding));

            }
            else
            {
                IsValid = true;
            }
        }

        private void TextName_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TextBox text = sender as TextBox;
            if (text != null)
            {
                BeginChangingDynamicItem(text);
            }
        }
    }
}