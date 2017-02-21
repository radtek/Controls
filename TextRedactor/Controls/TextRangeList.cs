﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;

namespace Controls
{
    public class TextRangeList<T> where T : TextRange
    {
        private readonly List<TextRange> MainList = new List<TextRange>();
        private FlowDocument Document;
        private int TextRangeLength;
        private static readonly object Lock = new object();
        private static readonly object Equals = new object();

        public TextRangeList(FlowDocument document, int textRangeLength = 3840)
        {
            Document = document;
            MainList.Add((T)new TextRange(document.ContentStart, document.ContentEnd));
            TextRangeLength = textRangeLength;
        }

        public void OnTextRangeChanged(int position)
        {
            TextPointer pointer = Document.ContentStart.GetPositionAtOffset(position);
            for (int i = 0; i < MainList.Count; i++)
            {
                if (!IsValid(MainList[i]) && !MainList[i].Contains(pointer)) continue;
                Repopulate(i);
                break;
            }
            OnCollectionChanged(1, Changed.Changed);
        }

        private TextPointer GetEnd(TextPointer pointer)
        {
            return pointer.GetPositionAtOffset(TextRangeLength) ?? Document.ContentEnd;
        }

        private bool CanCreateItem(TextPointer pointer)
        {
            return pointer.GetPositionAtOffset(1) != null;
        }

        public void Repopulate(int index)
        {
            MainList.RemoveRange(index, MainList.Count - index);
            TextPointer startPoistion = index != 0 ? MainList[index].End : Document.ContentStart;
            int length = new TextRange(startPoistion, Document.ContentEnd).Text.Length;
            int temp = !MainList.Any() ? 0 : MainList[index].Text.Length;
            while (temp < length)
            {
                if (!CanCreateItem(startPoistion)) { return; }
                TextPointer pointer = startPoistion;
                startPoistion = GetEnd(pointer);
                MainList.Add((T)new TextRange(pointer, startPoistion));
            }
            //double count = length / TextRangeLength + 1;
            //for (int i = index; i < count; i++)
            //{
            //    if (!CanCreateItem(startPoistion)) { return; }
            //    TextPointer pointer = startPoistion;//Document.ContentStart.GetOffsetToPosition(startPoistion) == 0 ? startPoistion : startPoistion.GetPositionAtOffset(1);
            //    startPoistion = GetEnd(pointer);
            //    MainList.Add((T)new TextRange(pointer, startPoistion));
            //}
            OnCollectionChanged(index, Changed.Populated);
        }

        private bool IsEquals(object obj, object obj1, Dispatcher disp)
        {
            lock (Equals)
            {
                var range = (TextRange)obj;
                var range1 = (TextRange)obj1;
                if (range == null || range1 == null)
                {
                    return false;
                }
                string a = "";
                disp.Invoke(() => a = range.Text);
                return a == range1.Text; //String.Compare(range.Text, range1.Text, StringComparison.CurrentCulture) == 0;
            }
        }

        public byte[] SereaLize(int index)
        {
            byte[] bytes = new byte[0];
            Application.Current.Dispatcher.Invoke(() =>
            {
                using (var stream = new MemoryStream())
                {
                    MainList[index].Save(stream, DataFormats.Rtf);
                    bytes = stream.GetBuffer();
                }
            });
            return bytes;
        }

        private TextRange LoadTextRange(TextRange range, byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                range.Load(stream, DataFormats.Rtf);
            }
            range.Text = range.Text.Remove(range.Text.Length - 2);
            return range;
        }

        public void SynchronizeTo(TextRangeList<T> list)
        {
            Monitor.Enter(Lock);
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                if (Count > i && IsEquals(list[i], this[i], list.Document.Dispatcher)) { continue; }
                if (Count <= i)
                {
                    MainList.Add((T)LoadTextRange(new TextRange(Document.ContentEnd, Document.ContentEnd), list.SereaLize(i)));
                    continue;
                }
                LoadTextRange(this[i], list.SereaLize(i));
            }
            Monitor.Exit(Lock);
        }

        private bool IsValid(TextRange item)
        {
            return item.Text.Length == 0 || item.Text.Length <= TextRangeLength;
        }

        public int Count => MainList.Count;

        private void OnCollectionChanged(int index, Changed state)
        {
            CollectionChanged?.Invoke(index, state);
        }

        public TextRange this[int index] => MainList[index];

        public delegate void CollectionChangedHandler(int index, Changed state);

        public event CollectionChangedHandler CollectionChanged;
    }
    public enum Changed
    {
        Changed = 1,
        Populated = 2,
    }
}