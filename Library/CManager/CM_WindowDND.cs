using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using factograph;
using System.IO;

namespace CManager
{
    partial class CM_Window : Window
    {
        private BackgroundWorker backgroundWorker1;
        private void InitDND()
        {
            backgroundWorker1 = new BackgroundWorker();
            backgroundWorker1.DoWork += new DoWorkEventHandler(backgoundWorker1_DoWork);
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgoundWorker1_RunWorkerCompleted);
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler(backgoundWorker1_ProgressChanged);
            backgroundWorker1.WorkerSupportsCancellation = true;
            cancelReceive.Click += new RoutedEventHandler(cancelReceive_Click);
            cancelReceive.Tag = backgroundWorker1;
        }



        private void ehDragOver(object sender, DragEventArgs args)
        {
            args.Effects = DragDropEffects.None;
            if (!this.cassIsReady) { args.Handled = true; return; }
            bool areFiles = false;
            if (args.Data.GetDataPresent("FileDrop")) areFiles = true;
            if (areFiles)
            {
                args.Effects = DragDropEffects.Copy;
                //debugField.Text = args.Source.GetType().ToString();
                Control uc = (Control)args.Source;
                XElement componentTag = ExtractTagFromControl(uc);
                if (componentTag != null)
                {
                    if (componentTag.Name == ONames.TagCollection || componentTag.Name == ONames.TagCassette)
                        args.Effects = DragDropEffects.Copy;
                    else
                        args.Effects = DragDropEffects.None;
                }
                else
                    args.Effects = DragDropEffects.None;
                //debugField.Text = "Tag: " + componentTag;
            }
            // Mark the event as handled, so TextBox's native DragOver handler is not called.
            args.Handled = true;
        }
        private XElement ExtractTagFromControl(Control uc)
        {
            XElement componentTag = null;
            if (uc.GetType() == typeof(CManager.ItBox))
            {
                ItBox ibox = (ItBox)uc;
                var lbi = (ListBoxItem)ibox.Parent;
                lbi.IsSelected = true;
                componentTag = (XElement)lbi.Tag;
            }
            else if (uc.GetType() == typeof(ListBox))
            {
                ListBox lbox = (ListBox)uc;
                lbox.UnselectAll();
                var selected = (TreeViewItem)this.treeView1.SelectedItem;
                var v = selected.Tag;
                componentTag = (XElement)v;

            }
            else if (uc.GetType() == typeof(TreeViewItem))
            {
                componentTag = (XElement)uc.Tag;
            }
            else if (uc.GetType() == typeof(TVItem))
            {
                componentTag = (XElement)(uc.Parent as Control).Tag;
            }
            else
            {
            }
            return componentTag;
        }

        private bool async = true;
        private void ehDrop(object sender, DragEventArgs args)
        {
            // Mark the event as handled, so TextBox's native Drop handler is not called.
            args.Handled = true;
            bool areFiles = false;
            if (args.Data.GetDataPresent("FileDrop")) areFiles = true;
            if (areFiles)
            {
                XElement componentTag = ExtractTagFromControl((Control)args.Source);
                if (componentTag != null)
                {
                    if (componentTag.Name == ONames.TagCollection || componentTag.Name == ONames.TagCassette)
                    {
                        string[] ss = (string[])args.Data.GetData("FileDrop");
                        this.debugField.Text = "Получаю...";
                        string collectionId = componentTag.Attribute(ONames.rdfabout).Value;
                        if (async)
                        {
                            string[] filesAndCollectionId = new string[ss.Length + 1];
                            ss.CopyTo(filesAndCollectionId, 0);
                            filesAndCollectionId[ss.Length] = collectionId;
                            //this.treeView.Enabled = false;
                            //this.listView.Enabled = false;
                            //this.toolStripProgressBar1.Value = 0;
                            //this.toolStripProgressBar1.Visible = true;
                            treeView1.IsEnabled = false;
                            wrapPanel1.IsEnabled = false;
                            backgroundWorker1.RunWorkerAsync(filesAndCollectionId);
                        }
                    }
                }
            }

        }
        void backgoundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            e.Result = this.AddFilesAndDirectoriesAsync((string[])e.Argument, backgroundWorker1, e).ToList(); //ComputeFibonacci((int)e.Argument, worker, e);
        }
        void backgoundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            List<XElement> addedElements = (List<XElement>)e.Result;
            cass.Save();
            var selected = (TreeViewItem)treeView1.SelectedItem;
            RebuildTreeView(selected);
            selected.IsSelected = false;
            selected.IsSelected = true;
            //progressBar.Value = 90.0;
            if(e.Cancelled) 
                debugField.Text = "canceled";
            else
                debugField.Text = "ok.";
            //cancelReceive.Visibility = Visibility.Hidden;
            treeView1.IsEnabled = true;
            wrapPanel1.IsEnabled = true;
            this.MarkNeedToCalculateVideo();
        }
        void backgoundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage; //e.ProgressPercentage;
            debugField.Text = "" + e.ProgressPercentage + "%";
        }
        void cancelReceive_Click(object sender, RoutedEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker)cancelReceive.Tag;
            worker.CancelAsync();
        }
        //bool toCancelReceive = false;
        /// <summary>
        /// Асинхронно и рекурсивно добавляет набор файлов и директорий в кассету в указанную коллекцию
        /// и возвращает набор добавленных в базу данных XElement-записей - это для синхронизации
        /// </summary>
        /// <param name="filenamesAndCollectionId">К массиву имен файлов и директорий, последним элементом прикреплен (добавлен) идентификатор коллекции, в которую записываются внешние файлы</param>
        /// <param name="worker"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private IEnumerable<XElement> AddFilesAndDirectoriesAsync(string[] filenamesAndCollectionId,
            BackgroundWorker worker, DoWorkEventArgs e)
        {
            List<XElement> addedElements = new List<XElement>();
            string[] filenames = filenamesAndCollectionId.Take(filenamesAndCollectionId.Length - 1).ToArray();
            string collectionId = filenamesAndCollectionId[filenamesAndCollectionId.Length - 1];
            // правильно посчитаю число вводимых файлов
            int fnumber = 0;
            foreach (string fn in filenames)
            {
                if (File.Exists(fn)) { if (fn != "Thumbs.db") fnumber++; }
                else fnumber += 1 + CountTotalFiles(new DirectoryInfo(fn));
            }
            // а теперь добавлю файлы и директории с 
            int count = 0;
            foreach (string fname in filenames)
            {
                if (worker.CancellationPending) break;
                if (File.Exists(fname))
                {
                    if (fname != "Thumbs.db")
                        addedElements.AddRange(this.cass.AddFile(new FileInfo(fname), collectionId));
                    count++;
                    worker.ReportProgress(100 * count / fnumber);
                }
                else if (Directory.Exists(fname))
                {
                    //smallImageFullNames.AddRange(this.cass.AddDirectory(new DirectoryInfo(fname), collectionId));
                    addedElements.AddRange(AddDirectoryAsync(new DirectoryInfo(fname), collectionId, ref count, fnumber, worker));
                }
            }
            return addedElements;
        }
        private IEnumerable<XElement> AddDirectoryAsync(DirectoryInfo dir, string collectionId, ref int count, int fnumber,
            BackgroundWorker worker)
        {
            List<XElement> addedElements = new List<XElement>();
            // добавление коллекции
            string subCollectionId;
            List<XElement> ae = this.cass.AddCollection(dir.Name, collectionId, out subCollectionId).ToList();
            if (ae.Count > 0) addedElements.AddRange(ae);
            
            count++;
            foreach (FileInfo f in dir.GetFiles())
            {
                if (worker.CancellationPending) break;
                if (f.Name != "Thumbs.db")
                    addedElements.AddRange(this.cass.AddFile(f, subCollectionId));
                count++;
                worker.ReportProgress(100 * count / fnumber);
            }
            foreach (DirectoryInfo d in dir.GetDirectories())
            {
                if (worker.CancellationPending) break;
                addedElements.AddRange(AddDirectoryAsync(d, subCollectionId, ref count, fnumber, worker));
            }
            return addedElements;
        }
        private int CountTotalFiles(DirectoryInfo dir)
        {
            int cnt = dir.GetFiles().Length;
            foreach (DirectoryInfo subdir in dir.GetDirectories()) cnt += 1 + CountTotalFiles(subdir);
            return cnt;
        }

    }
}
