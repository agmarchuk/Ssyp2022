using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using factograph;
using System.Xml.Linq;

namespace CManager
{//
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class CM_Window : Window
    {

        public CM_Window()
        {
            this.exeBase = new DirectoryInfo(".").FullName;
            CassetteExtension.App_Bin_Path = this.exeBase + "/../App_Bin/";
            InitializeComponent();
            LoadIcons();

            rotateCW.Content = new Image() { Source = new BitmapImage(new Uri(exeBase + "/../icons/RotCW.bmp")) };
            rotateCW.Click += new RoutedEventHandler(rotateCW_Click);
            rotateCCW.Content = new Image() { Source = new BitmapImage(new Uri(exeBase + "/../icons/RotCCW.bmp")) };
            rotateCCW.Click += new RoutedEventHandler(rotateCCW_Click);
            changeOriginal.Content = new Image() { Source = new BitmapImage(new Uri(exeBase + "/../icons/Change.bmp")) };
            changeOriginal.Click += new RoutedEventHandler(changeOriginal_Click);
            makeDeepZoom.Content = new Image() { Source = new BitmapImage(new Uri(exeBase + "/../icons/scanner_m.jpg")) };
            makeDeepZoom.Click += new RoutedEventHandler(makeDeepZoom_Click);
            runOriginal.Click += new RoutedEventHandler(runOriginal_Click);

            wrapPanel1.PreviewKeyUp += new KeyEventHandler(wrapPanel1_PreviewKeyUp);
            wrapPanel1.PreviewMouseDoubleClick += new MouseButtonEventHandler(wrapPanel1_PreviewMouseDoubleClick);
            wrapPanel1.PreviewMouseDown += new MouseButtonEventHandler(wrapPanel1_PreviewMouseDown);

            wp_contextMenu1 = new ContextMenu();
            MenuItem mi1 = new MenuItem { Header = "Нов.коллекция" };
            mi1.Click += new RoutedEventHandler((sender, e) =>
            {
                TreeViewItem selected = (TreeViewItem)treeView1.SelectedItem;
                XElement xselected = selected.Tag as XElement;
                if(xselected == null) return;
                string collectionId = xselected.Attribute(ONames.rdfabout).Value;
                string subCollectionId = cass.GenerateNewId();
                cass.AddCollection(cass.Name + "_" + (subCollectionId.Substring(subCollectionId.Length-4, 4)), collectionId, out subCollectionId);
                cass.Save();
                RebuildTreeView(selected);
                ViewItem(xselected);
            });
            wp_contextMenu1.Items.Add(mi1);
            wrapPanel1.ContextMenu = wp_contextMenu1;
            item_contextMenu1 = new ContextMenu();
            MenuItem mi2 = new MenuItem { Header = "Открепить" };
            mi2.Click += new RoutedEventHandler((sender, e) =>
            {
                TreeViewItem selected = (TreeViewItem)treeView1.SelectedItem;
                XElement xselected = selected.Tag as XElement;
                if(xselected == null) return;
                ListBoxItem item_selected = wrapPanel1.SelectedItem as ListBoxItem;
                if(item_selected == null) return;
                XElement xitem = item_selected.Tag as XElement;
                if(xitem == null) return;
                cass.RemoveItemToWastebasket(xitem.Attribute(ONames.rdfabout).Value);
                RebuildTreeView(selected);
                ViewItem(xselected);
            });
            item_contextMenu1.Items.Add(mi2);
            MenuItem mi3 = new MenuItem { Header = "Вызов DeepZoom" };
            mi3.Click += new RoutedEventHandler((sender, e) =>
            {
                ListBoxItem item_selected = wrapPanel1.SelectedItem as ListBoxItem;
                if(item_selected == null) return;
                XElement xitem = item_selected.Tag as XElement;
                if(xitem == null) return;
                if(xitem.Name != ONames.TagDocument || xitem.Element(ONames.TagIisstore) == null || xitem.Element(ONames.TagIisstore).Attribute(ONames.AttDocumenttype).Value != "scanned/dz")
                    return;
                string uri = xitem.Element(ONames.TagIisstore).Attribute(ONames.AttUri).Value;
                var dogIndex = uri.IndexOf("@");
                var casseteeName = uri.Substring(7, dogIndex - 7);
                var fileName = uri.Substring(uri.Length - 9).Replace("/", "");
                //var url = casseteeName + "/documents/deepzoom/" + fileName + ".xml";
                Clipboard.SetText(@"<div id='silverlightControlHost' style='width:150px; height:150px;'>"
                                  + "<object "
                                  + "DeepZoomSource='cassettes/"+casseteeName+"/documents/deepzoom/" + fileName + ".xml'"
                                  + "id='silverlightControl' data='data:application/x-silverlight-2,' type='application/x-silverlight-2' width='100%' height='100%'>"
                                  + "<param name='source' value='DeepZoomLight.xap'/>"
                                  + "<param name='onError' value='onSilverlightError' />"
                                  + "<param name='background' value='white' />"
                                  + "<param name='minRuntimeVersion' value='3.0.40624.0' />"
                                  + "<param name='enablehtmlaccess' value='True'/>"
                                  + "<param name='autoUpgrade' value='true' />"
                                  + "<a href='http://go.microsoft.com/fwlink/?LinkID=149156&v=3.0.40624.0' style='text-decoration:none'>"
                                  + "  <img src='http://go.microsoft.com/fwlink/?LinkId=108181' alt='Get Microsoft Silverlight' style='border-style:none'/>"
                                  + "</a>"
                                  + "</object>"
                                  + "</div> ");
            });
            item_contextMenu1.Items.Add(mi3);

            //wp_contextMenu1.Opened += new RoutedEventHandler(contextMenu1_Opened);

            panelForList.SizeChanged += new SizeChangedEventHandler(panelForList_SizeChanged);
            startVideo.Click += new RoutedEventHandler(startVideo_Click);
            stopVideo.Click += new RoutedEventHandler(stopVideo_Click);
            pauseVideo.Click += new RoutedEventHandler(pauseVideo_Click);
            mediaElement1.IsVisibleChanged += new DependencyPropertyChangedEventHandler(mediaElement1_IsVisibleChanged);

            InitDND();
        }

        private ContextMenu wp_contextMenu1;
        private ContextMenu item_contextMenu1;
        //private void contextMenu1_Opened(object sender, RoutedEventArgs e)
        //{
        //    wp_contextMenu1.Items.Add("item3");
        //    debugField.Text = "sender: " + e.Source.GetType().ToString();
        //}

        private void LoadIcons()
        {
            try
            {
                iconDocumentLarge = MakeImageSourceFromFile(exeBase + "/../icons/document.jpg");
                iconPhotoLarge = MakeImageSourceFromFile(exeBase + "/../icons/photo.jpg");
                iconVideoLarge = MakeImageSourceFromFile(exeBase + "/../icons/video.jpg");
                iconAudioLarge = MakeImageSourceFromFile(exeBase + "/../icons/audio.jpg");
                iconClosedFolderLarge = MakeImageSourceFromFile(exeBase + "/../icons/ClosedFolder100.bmp");
                iconScannerLarge = MakeImageSourceFromFile(exeBase + "/../icons/scanner.bmp");

                iconClosedFolderSmall = MakeImageSourceFromFile(exeBase + "/../icons/TreeClosed.bmp");
                iconOpenFolderSmall = MakeImageSourceFromFile(exeBase + "/../icons/TreeOpen.bmp");
                iconDocumentSmall = MakeImageSourceFromFile(exeBase + "/../icons/Document_small.bmp");
                iconPhotoSmall = MakeImageSourceFromFile(exeBase + "/../icons/Photo_small.bmp");
                iconVideoSmall = MakeImageSourceFromFile(exeBase + "/../icons/Video_small.bmp");
                iconAudioSmall = MakeImageSourceFromFile(exeBase + "/../icons/Audio_small.bmp");
                iconUnknownSmall = MakeImageSourceFromFile(exeBase + "/../icons/Doc_unknown_small.bmp");
            }
            catch(Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке иконок: " + ex.Message);
            }
        }

        void wrapPanel1_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //TODO: не очень надежный критерий того, что мышка не попала в айтем
            if(e.OriginalSource.GetType() == typeof(ScrollViewer))
            {
                wrapPanel1.UnselectAll();
            }
        }

        void wrapPanel1_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ExtendItem();
        }
        void wrapPanel1_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            ExtendItem();
        }

        private void ExtendItem()
        {
            var wselected = (ListBoxItem)wrapPanel1.SelectedItem;
            if(wselected == null) return;
            XElement xtag = (XElement)wselected.Tag;
            var tselected = (TreeViewItem)treeView1.SelectedItem;
            TreeViewItem newselected = tselected.Items.Cast<TreeViewItem>().FirstOrDefault(tvi => ((XElement)tvi.Tag).Equals(xtag));
            if(newselected == null) return;
            tselected.IsSelected = false;
            tselected.IsExpanded = true;
            newselected.IsSelected = true;
        }


        // Иконки
        private ImageSource iconDocumentLarge;
        private ImageSource iconPhotoLarge;
        private ImageSource iconVideoLarge;
        private ImageSource iconAudioLarge;
        private ImageSource iconClosedFolderLarge;
        private ImageSource iconScannerLarge;
        private ImageSource iconClosedFolderSmall;
        private ImageSource iconOpenFolderSmall;
        private ImageSource iconDocumentSmall;
        private ImageSource iconPhotoSmall;
        private ImageSource iconVideoSmall;
        private ImageSource iconAudioSmall;
        private ImageSource iconUnknownSmall;
        //private ImageSource iconChange;
        private string exeBase = "."; // База запускаемого кода
        //private SGraph.SOntologyModel ontologyModel; // онтологическая модель
        private Dictionary<string, ImageInfo> UriInd = null; // таблица соответствия между Uri фотодокумента и его загруженным значением
        // Обработчики команд
        void commandLoad_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog fdialog = new Microsoft.Win32.OpenFileDialog() { Filter = "Cassette info files (*.finfo)|*.finfo" };
            if(fdialog.ShowDialog().Value)
            {
                var finfo = new FileInfo(fdialog.FileName);
                var cassObj = new Cassette(finfo.Directory.FullName);
                this.SetCassette(cassObj);
                //debugField.Text = "HasValue: " + fdialog.FileName;
            }
        }
        void commandCreate_Click(object sender, RoutedEventArgs e)
        {

            var fdialog = new Microsoft.Win32.SaveFileDialog() { };
            if(fdialog.ShowDialog().Value)
            {
                var cassObj = Cassette.Create(fdialog.FileName); // new Cassette(fdialog.FileName);
                //var cassObj = new Cassette(finfo.Directory.FullName);
                this.SetCassette(cassObj);
                //debugField.Text = "HasValue: " + fdialog.FileName;
            }
        }
        void command_Exit(object sender, RoutedEventArgs e) { this.Close(); }
        void command_MakeVideo(object sender, RoutedEventArgs e)
        {
            if(cass == null) return;
            string batname = cass.Dir.FullName + "/convertVideo.bat";
            if(!File.Exists(batname)) return;
            string batnamenew = cass.Dir.FullName + "/" + DateTime.Now.Ticks.ToString() + ".bat";
            File.Move(batname, batnamenew);
            this.commandMakeVideo.Background = null;
            var mv_process = new System.Diagnostics.Process();
            mv_process.StartInfo = new System.Diagnostics.ProcessStartInfo(batnamenew);
            mv_process.EnableRaisingEvents = true;
            mv_process.Exited += new EventHandler((_, ea) =>
            {
                //this.commandMakeVideo.Background = null;
                File.Delete(batnamenew);
            });
            mv_process.Start();
        }

        // Загрузка значений из кассеты
        private Cassette cass;
        private bool cassIsReady = false;
        private List<RelationSpec> user_menu_list;
        private void SetCassette(Cassette cas)
        {
            this.Title = "В работе: " + cas.Name;
            this.cass = cas;
            this.BuildTreeView();
            //// Построение маленьких имиджей и таблицы соответствия
            UriInd = new Dictionary<string, ImageInfo>();
            //Далее может быть какое-то предвычисление
            // .......
            // Построение списка меню пользователя
            user_menu_list = new List<RelationSpec>();

            // Почистим имеющиеся чекбоксы
            while(toolBar1.Items[toolBar1.Items.Count - 1].GetType() != typeof(Separator))
                toolBar1.Items.RemoveAt(toolBar1.Items.Count - 1);
            // Теперь сформируем чекбоксы и список меню
            foreach(XElement member in cass.GetInverseXItems("menurootcollection"))
            {
                // Нужно выявить коллекцию, в которую надо "помещать" документы
                var menu_position = member.Element(factograph.ONames.TagCollectionitem);
                if(menu_position == null) continue;
                XAttribute mp_att = menu_position.Attribute(ONames.rdfresource);
                if(mp_att == null) continue;
                string mp_id = mp_att.Value;
                CheckBox chb = new CheckBox() { Content=cass.GetXItemById(mp_id).Element(factograph.ONames.TagName).Value };
                chb.Click += new RoutedEventHandler(ChangeState);
                RelationSpec rs = new RelationSpec()
                {
                    relationName = factograph.ONames.TagCollectionmember,
                    linkSelf = factograph.ONames.TagCollectionitem,
                    linkOther = factograph.ONames.TagIncollection,
                    idOther = mp_id,
                    usedCheckbox = chb
                };
                chb.Tag = rs;
                user_menu_list.Add(rs);
                toolBar1.Items.Add(chb);
            }
            // перепостроение правой панели
            ((TreeViewItem)treeView1.Items[0]).IsSelected = true;
            this.cassIsReady = true;
            // Привязка события появления необходимости в перевычислении превьюшек по видео
            //this.cass.NeedToCalculateVideoPreview += new EventHandler((sender, e) => { this.commandMakeVideo.Background = Brushes.AliceBlue; });
            this.RefreshViewPort();
        }

        //void chb_Click(object sender, RoutedEventArgs e)
        //{
        //    throw new NotImplementedException();
        //}

        // Обновляет точки и коллекции взаимодействия с экраном
        private void RefreshViewPort()
        {
            RDFDocs dlist = this.FindResource("RDFDocList") as RDFDocs;
            dlist.Clear();
            var query = cass.DataDocuments().Select(rdoc =>
            {
                //XElement rdoc = cass.GetXItemByUri(ur);
                XElement iisstore = rdoc.Element(ONames.TagIisstore);
                return new RDFDocFields()
                {
                    Id = rdoc.Attribute(ONames.rdfabout).Value,
                    Uri = iisstore.Attribute(ONames.AttUri).Value,
                    Owner = iisstore.Attribute(ONames.AttOwner)==null? null : iisstore.Attribute(ONames.AttOwner).Value
                };
            });
            foreach(RDFDocFields fields in query)
            {
                dlist.Add(fields);
            }
        }
        private void MarkNeedToCalculateVideo()
        {
            if(this.cass.NeedToCalculate)
            {
                this.commandMakeVideo.Background = Brushes.Aqua;
                this.cass.NeedToCalculate = false;
            }
        }

        private void BuildTreeView()
        {
            treeView1.BeginInit();
            treeView1.Items.Clear();
            // Дерево кассеты
            TVItem tv1 = new TVItem {label = {Text = this.cass.Name}, icon = {Source = iconClosedFolderSmall}};
            TreeViewItem cassItem = new TreeViewItem {Header = tv1, IsExpanded = true, Tag = this.cass.GetXItemById(this.cass.CollectionId)};
            foreach(var sc in Subitems(this.cass.CollectionId))
                cassItem.Items.Add(sc);
            // Дерево мусора
            TVItem tv2 = new TVItem {label = {Text = "Мусорная корзина"}, icon = {Source = iconClosedFolderSmall}};
            TreeViewItem waste = new TreeViewItem() { Header = tv2, Tag = this.cass.GetXItemById(this.cass.Wastebasket) };
            foreach(var sc in Subitems(this.cass.Wastebasket))
                waste.Items.Add(sc);

            //// Дерево меню
            //TVItem tv3 = new TVItem(); tv3.label.Text = "Меню";
            //TreeViewItem menuItem = new TreeViewItem() { Header = tv3, IsExpanded = false };
            //foreach (var sc in Subitems("menurootcollection"))
            //    menuItem.Items.Add(sc);

            treeView1.Items.Add(cassItem);
            treeView1.Items.Add(waste);
            //treeView1.Items.Add(menuItem);
            treeView1.EndInit();
        }
        private void RebuildTreeView(TreeViewItem item)
        {
            string collectionId = ((XElement)item.Tag).Attribute(ONames.rdfabout).Value;
            item.Items.Clear();
            foreach(var sc in Subitems(collectionId))
                item.Items.Add(sc);
        }

        private IEnumerable<TreeViewItem> Subitems(string collectionId)
        {
            var v = cass.GetSubItems(collectionId)
                .Where(xe => xe != null) // такой вариант случался из-за ручного убирания записи о фотодокументе
                .OrderBy(xe => xe.Name == ONames.TagCollection ? 0 : 1)
                .Select(xe =>
                {
                    string id = xe.Attribute(ONames.rdfabout).Value;
                    TVItem tvHeader = new TVItem();
                    var xname = xe.Element("name");
                    tvHeader.label.Text = xname==null?"noname" : xname.Value;
                    XName type = xe.Name;
                    tvHeader.icon.Source =
                        type == ONames.TagCollection ? iconClosedFolderSmall :
                        (type == ONames.TagDocument ? iconDocumentSmall :
                        (type == ONames.TagPhotodoc ? iconPhotoSmall :
                        (type == ONames.TagVideo ? iconVideoSmall :
                        (type == ONames.TagAudio ? iconAudioSmall : iconUnknownSmall))));
                    TreeViewItem tvi = new TreeViewItem() { Header = tvHeader, Tag = xe };
                    foreach(var sc in Subitems(id))
                        tvi.Items.Add(sc);
                    return tvi;
                });
            return v;
        }
        // Обработчик события изменения выделенного в TreeView айтема
        void treeView1_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView tv = e.Source as TreeView;
            TreeViewItem tvi = (TreeViewItem)tv.SelectedItem;
            if(tvi == null) return;
            var xtag = (XElement)tvi.Tag;
            ViewItem(xtag);
        }
        /// <summary>
        /// Построение разных видов айтема в (правой) панели визуализации 
        /// </summary>
        /// <param name="xtag"></param>
        private void ViewItem(XElement xitem)
        {
            string itemId = xitem == null ? cass.Wastebasket : xitem.Attribute(ONames.rdfabout).Value;
            wrapPanel1.Visibility = Visibility.Collapsed;
            itemViewPanel.Visibility = Visibility.Collapsed;
            if(xitem==null || xitem.Name == ONames.TagCollection || xitem.Name == ONames.TagCassette)
            {
                wrapPanel1.Visibility = Visibility.Visible;
                var subitems = this.cass.GetSubItems(itemId);
                wrapPanel1.Items.Clear();
                // почистим чекбоксы юзеровского меню
                //digest_checkBox1.IsChecked = false;
                foreach(CheckBox chb in user_menu_list.Select(r => r.usedCheckbox)) chb.IsChecked = false;
                foreach(var xe in subitems.OrderBy(xe => xe.Name == ONames.TagCollection ? 0 : 1))
                {
                    var xtype = xe.Name;
                    var iisstore = xe.Element(ONames.TagIisstore);
                    string uri = iisstore == null ? null : iisstore.Attribute(ONames.AttUri).Value;
                    ImageSource iSource =
                        xtype == ONames.TagDocument ? (iisstore != null && iisstore.Attribute(ONames.AttDocumenttype).Value == "scanned/dz" ? iconScannerLarge : iconDocumentLarge) :
                        //(xtype == ONames.TagPhotodoc ? MakeImageSourceFromFile(cass.Dir + "/documents/small/" + uri.Substring(uri.Length-9,9) + ".jpg")   : //(UriInd.ContainsKey(uri) ? UriInd[uri] : zz) 
                        (xtype == ONames.TagVideo ? iconVideoLarge :
                        (xtype == ONames.TagAudio ? iconAudioLarge :
                        (xtype == ONames.TagCollection ? iconClosedFolderLarge : iconDocumentLarge)));
                    // Отдельно обработаем вариант фотодокумента, чтобы правильно сформировать источник для иконки
                    if(xtype == ONames.TagPhotodoc)
                    {
                        iSource = GetCachedImageSource(uri);
                    }
                    var ib = new ItBox();
                    ib.ContextMenu = this.item_contextMenu1;
                    var xname = xe.Element(ONames.TagName);
                    ib.label.Text = xname==null?"noname": xname.Value;
                    if(iSource!=null && iSource.Width > iSource.Height) ib.image1.Width = 100;
                    else ib.image1.Height = 100;
                    ib.image1.Source = iSource;
                    ListBoxItem lbi = new ListBoxItem();
                    lbi.Tag = xe;
                    lbi.Content = ib;
                    wrapPanel1.Items.Add(lbi);
                }
            }
            else if(xitem.Name == ONames.TagPhotodoc)
            {
                itemViewPanel.Visibility = Visibility.Visible;
                imageView1.Visibility = Visibility.Visible;
                mediaElement1.Visibility = Visibility.Collapsed;
                // Это нужно, чтобы фотки не "застревали"
                wrapPanel1.Items.Clear();
                var iisstore = xitem.Element(ONames.TagIisstore);
                string uri = iisstore.Attribute(ONames.AttUri).Value;
                ImageSource imageSource = MakeImageSourceFromFile(cass.Dir.FullName + "/" + cass.GetPreviewPhotodocumentPath(uri, "normal"));
                //ImageSource imageSource = GetCachedImageSource(uri);
                //Image imageView = new Image();
                if(imageSource != null) CalculateImageViewSize(imageSource);
                imageView1.Source = imageSource;
                //itemViewPanel.Children.Clear();
                //itemViewPanel.Children.Add(imageView);
                SetState(); // Установка checkBox
            }
            else if(xitem.Name == ONames.TagVideo || xitem.Name == ONames.TagAudio)
            {
                var iisstore = xitem.Element(ONames.TagIisstore);
                string uri = iisstore.Attribute(ONames.AttUri).Value;
                itemViewPanel.Visibility = Visibility.Visible;
                imageView1.Visibility = Visibility.Collapsed;
                mediaElement1.Visibility = Visibility.Visible;
                if(xitem.Name == ONames.TagVideo)
                {
                    mediaElement1.Source = new Uri(cass.Dir.FullName + "/" + "documents/medium/" + uri.Substring(uri.Length - 9, 9) + ".flv");
                }
                else
                {
                    mediaElement1.Source = new Uri(cass.Dir.FullName + "/originals/" + uri.Substring(uri.Length - 9, 9) + ".mp3");
                }
                mediaElement1.Height = 288;
                mediaElement1.LoadedBehavior = MediaState.Manual;
                mediaElement1.Play();
                //// Это нужно, чтобы фотки не "застревали"
                //wrapPanel1.Items.Clear();
                //var iisstore = xitem.Element(ONames.TagIisstore);
                //string uri = iisstore.Attribute(ONames.AttUri).Value;
                //ImageSource imageSource = MakeImageSourceFromFile(cass.Dir.FullName + "/" + cass.GetPreviewPhotodocumentPath(uri, "normal"));
                ////ImageSource imageSource = GetCachedImageSource(uri);
                ////Image imageView = new Image();
                //CalculateImageViewSize(imageSource);
                //imageView1.Source = imageSource;
                SetState(); // Установка checkBox
            }
        }
        void stopVideo_Click(object sender, RoutedEventArgs e) { mediaElement1.Stop(); }
        void startVideo_Click(object sender, RoutedEventArgs e) { mediaElement1.Play(); }
        void pauseVideo_Click(object sender, RoutedEventArgs e) { mediaElement1.Pause(); }
        void mediaElement1_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(!mediaElement1.IsVisible) mediaElement1.Source = null;
        }


        private ImageSource GetCachedImageSource(string uri)
        {
            ImageSource iSource;
            if(UriInd.ContainsKey(uri)) iSource = UriInd[uri].Source;
            else
            {
                iSource = MakeImageSourceFromFile(cass.Dir.FullName + "/" + cass.GetPreviewPhotodocumentPath(uri, "small"));
                UriInd.Add(uri, new ImageInfo() { Source = iSource });
            }
            return iSource;
        }
        private void CalculateImageViewSize(ImageSource imageSource)
        {
            double factor = imageSource.Width / imageSource.Height;
            if(panelForList.ActualWidth / panelForList.ActualHeight > factor)
            {
                imageView1.Height = panelForList.ActualHeight;
                imageView1.Width = imageView1.Height * factor;
            }
            else
            {
                imageView1.Width = panelForList.ActualWidth;
                imageView1.Height = imageView1.Width / factor;
            }
        }
        void runOriginal_Click(object sender, RoutedEventArgs e)
        {
            // Найдем выделенный элемент, к которому относится команда 
            XElement xitem = GetSelectedXItem();
            if(xitem == null) return;
            // Если выделенный элемент является документом, то попробуем "запустить" его оригинал
            XName tag = xitem.Name;
            if(tag == ONames.TagPhotodoc || tag == ONames.TagAudio || tag == ONames.TagDocument || tag == ONames.TagVideo)
            {
                XElement iisstore = xitem.Element(ONames.TagIisstore); if(iisstore == null) return;
                XAttribute uri = iisstore.Attribute(ONames.AttUri); if(uri == null) return;
                string originalLocation = cass.Dir.FullName.Replace('\\', '/') + "/" + cass.GetOriginalDocumentPath(uri.Value);
                try { System.Diagnostics.Process.Start(originalLocation); }
                catch(Exception) { }
            }
        }
        void rotateCW_Click(object sender, RoutedEventArgs e) { rotate(true); }
        void rotateCCW_Click(object sender, RoutedEventArgs e) { rotate(false); }
        void rotate(bool cw)
        {
            XElement xitem = GetSelectedXItem();

            // Внесение поворота в трансформацию iisstore документа
            if(xitem.Name != ONames.TagPhotodoc) return;
            XElement iisstore = xitem.Element(ONames.TagIisstore);
            var transformAtt = iisstore.Attribute("transform");
            if(transformAtt == null) { transformAtt = new XAttribute("transform", ""); iisstore.Add(transformAtt); }
            int nrotations = transformAtt == null ? 0 : transformAtt.Value.Length;
            nrotations += cw ? 1 : 3;
            string transformString = "";
            for(int i = 0; i < nrotations % 4; i++) transformString += "r";
            transformAtt.Value = transformString;

            RemakePhotoPreviewsAndRefresh(xitem);

            cass.Save();
        }

        private void RemakePhotoPreviewsAndRefresh(XElement xitem)
        {
            // Очистка ненужных данных, чтобы файлы не "застряли" в имиджах
            itemViewPanel.Visibility = Visibility.Collapsed;
            imageView1.Source = null;
            wrapPanel1.Visibility = Visibility.Collapsed;
            wrapPanel1.Items.Clear();
            XElement iisstore = xitem.Element(ONames.TagIisstore);
            var uri = iisstore.Attribute(ONames.AttUri).Value;
            if(this.UriInd.ContainsKey(uri)) this.UriInd.Remove(uri);
            System.GC.Collect();
            cass.MakePhotoPreviews(xitem.Element(ONames.TagIisstore), "smn");
            // Перевычисление правой панели
            XElement yitem = (XElement)((TreeViewItem)treeView1.SelectedItem).Tag;
            this.ViewItem(yitem);
        }
        void changeOriginal_Click(object sender, RoutedEventArgs e)
        {
            XElement xitem = GetSelectedXItem();
            if(xitem.Name != "document" && xitem.Name != "photo-doc" && xitem.Name != "video-doc" && xitem.Name != "audio-doc") return;
            XElement iisstore = xitem.Element("iisstore");
            string documenttype = iisstore.Attribute("documenttype").Value;
            string uri = iisstore.Attribute("uri").Value;

            Microsoft.Win32.OpenFileDialog fdialog = new Microsoft.Win32.OpenFileDialog() { Filter = "Document files (*.*)|*.*" };
            if(fdialog.ShowDialog().Value)
            {
                var finfo = new FileInfo(fdialog.FileName);
                // проверка на тип
                var point = finfo.FullName.LastIndexOf('.'); if(point == -1) return;
                string ext = finfo.FullName.Substring(point).ToLower();
                DocType triple = Cassette.docTypes.Where(doct => doct.ext == ext || doct.ext == "unknown").First();
                if(triple.ext != ext) return;
                // копируем оригинал под имеющимся расширением 
                try
                {
                    File.Delete(cass.Dir + "/" + cass.GetOriginalDocumentPath(uri));
                    File.Copy(finfo.FullName, cass.Dir.FullName + "/originals/" + uri.Substring(uri.Length - 9, 9) + ext);
                    iisstore.Attribute(ONames.AttOriginalname).Value = finfo.FullName;
                    iisstore.Attribute(ONames.AttSize).Value = "" + finfo.Length;
                    iisstore.Attribute(ONames.AttDocumenttype).Value = triple.content_type;
                    iisstore.Attribute(ONames.AttDocumentmodification).Value = System.DateTime.Now.ToString("s");
                    if(xitem.Name == "photo-doc")
                    {
                        RemakePhotoPreviewsAndRefresh(xitem);
                    }
                    cass.Save();
                }
                catch(Exception) { return; } // Надо бы эту нештатную ситуацию зафиксировать в каком-нибудь логе

                debugField.Text =  cass.Dir.FullName + "/originals/" + uri.Substring(uri.Length - 9, 9) + ext;

            }
        }
        void makeDeepZoom_Click(object sender, RoutedEventArgs e)
        {
            XElement xitem = GetSelectedXItem();

            ToDeepZoom(xitem);
        }

        private void ToDeepZoom(XElement xitem)
        {
            if(xitem==null) { MessageBox.Show("Выделите папку с фотодокументами или фотодокумент."); return; }
            if(xitem.Name == ONames.TagPhotodoc)
            {
                XElement iisstore = xitem.Element(ONames.TagIisstore);
                cass.MakePhotoPreviews(iisstore, "z");
                cass.Save();
            }
            else if(xitem.Name == ONames.TagCollection)
            {
                string uri = "iiss://" + cass.Name + "@iis.nsk.su/"
                    + cass._folderNumber + "/" + cass._documentNumber;
                string sdocId = cass.GenerateNewId();
                XElement sdoc = new XElement(ONames.TagDocument,
                    new XAttribute(ONames.rdfabout, sdocId),
                    xitem.Element(ONames.TagName),
                    new XElement(ONames.TagIisstore,
                        new XAttribute(ONames.AttDocumenttype, "scanned/dz"),
                        new XAttribute(ONames.AttUri, uri)));
                cass.db.Add(sdoc);
                string xitemId = xitem.Attribute(ONames.rdfabout).Value;
                foreach(XAttribute att in
                    cass.GetInverseXItems(xitemId)
                    .Select(cm => cm.Elements()
                                    .Select(sel => sel.Attribute(ONames.rdfresource))
                                    .Where(att => att != null && att.Value == xitemId))
                     .SelectMany(resourceattributes => resourceattributes))
                {
                    att.Value = sdocId;
                }
                // 
                List<XElement> colmembers = cass.GetInverseXItems(sdocId, ONames.TagIncollection)
                    .OrderBy(x =>
                    {
                        var photoDocId = x.Element(ONames.TagCollectionitem).Attribute(ONames.rdfresource).Value;
                        var path = cass.GetXItemById(photoDocId)
                            .Element("iisstore")
                            .Attribute(ONames.AttOriginalname).Value;
                        var last = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
                        return path.Substring(last + 1).ToLower();
                    })
                    .ToList();

                int ii = 0;
                List<XElement> photoDocs = new List<XElement>();
                foreach(XElement collmem in colmembers)
                {
                    string scanDocId = cass.GenerateNewId();
                    var photoDocLink = collmem.Element(ONames.TagCollectionitem);
                    var photoDocId = photoDocLink.Attribute(ONames.rdfresource).Value;
                    var photoDoc = cass.GetXItemById(photoDocId);
                    // учитывать только фотки
                    if (photoDoc.Name != ONames.TagPhotodoc) continue;
                    photoDocs.Add(photoDoc);
                    XElement scanDoc = new XElement("scanned-page",
                     new XAttribute(ONames.rdfabout, scanDocId),
                     new XElement("page-number", ++ii),
                     new XElement("scanned-document",
                         new XAttribute(ONames.rdfresource, sdocId)),
                     new XElement("photo-page",
                         new XAttribute(ONames.rdfresource, photoDocId)));
                    cass.db.Add(scanDoc);
                    collmem.Remove();
                    cass.MakePhotoPreviews(photoDoc.Element(ONames.TagIisstore), "z");
                }

                XNamespace dp = "http://schemas.microsoft.com/deepzoom/2009";
                var rootDir = cass.Dir + "/documents/deepzoom";
                if(!Directory.Exists(rootDir))
                    Directory.CreateDirectory(rootDir);
                rootDir += "/";
               // var xmlsToArch = new Dictionary<string, string>();
                var document =   new XElement(dp + "Collection",
                                    new XAttribute(XNamespace.Xmlns + "dp", dp),
                                    new XAttribute("xmlns", dp.NamespaceName),
                                    new XAttribute(dp + "MaxLevel", "7"),
                                    new XAttribute(dp + "TileSize", "256"),
                                    new XAttribute(dp + "Format", "jpg"),
                                    new XAttribute(dp + "NextItemId", photoDocs.Count()),
                                    new XAttribute(dp + "ServerFormat", "Default"),
                                    photoDocs.Select((photoDoc, i) =>
                                              {
                                                  var uriPhoto =  photoDoc.Element("iisstore").Attribute("uri").Value;
                                                  var url = uriPhoto.Substring(uriPhoto.Length - 9);
                                                 // xmlsToArch.Add(rootDir + url+"_files", url+"_files");
                                                  url += ".xml";
                                                 // xmlsToArch.Add(rootDir + url, url.Substring(0, 4));
                                                  var Width = photoDoc.Element("iisstore").Attribute("width").Value;
                                                  var Height = photoDoc.Element("iisstore").Attribute("height").Value;
                                                  return new XElement(dp + "I",
                                                      new XAttribute(dp + "Id", i),
                                                      new XAttribute(dp + "N", i),
                                                      new XAttribute(dp + "Source", url),
                                                      new XElement(dp + "Size",
                                                          new XAttribute(dp + "Width", Width)
                                                          , new XAttribute(dp + "Height", Height)));
                                              }));

                var imageXml = rootDir + cass._folderNumber +  cass._documentNumber;
                document.Save(imageXml + ".xml");

             //   xmlsToArch.Add(imageXml + ".xml", "");
             //  Archive.ToZip.ZipFiles(imageXml+".zip", xmlsToArch);
                 //Archive.ToArchive.Create(imageXml, xmlsToArch);             
                cass.IncrementDocumentNumber();
                cass.Save();
                                   
                // Перевычисление правой панели
                XElement yitem = (XElement)((TreeViewItem)treeView1.SelectedItem).Tag;
                this.ViewItem(yitem);
            }
        }
        /// <summary>
        /// Выдает выделенный элемент или null. Выделенным считается или выделенный в
        /// левой панели, или, если wrapPanel видна и в правой панели есть выделение, то - оно
        /// </summary>
        /// <returns></returns>
        private XElement GetSelectedXItem()
        {
            // Выяснение к какому фотодокументу относится команда поворота
            var selected = (TreeViewItem)treeView1.SelectedItem;
            if(selected == null) return null;
            var right_selected = (ListBoxItem)wrapPanel1.SelectedItem;
            return wrapPanel1.Visibility == Visibility.Visible && right_selected != null 
                ? (XElement)right_selected.Tag 
                : (XElement)selected.Tag;
        }
        void panelForList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(itemViewPanel.Visibility == Visibility.Visible && imageView1.Source != null)
            {
                CalculateImageViewSize(imageView1.Source);
            }
        }

        private static ImageSource MakeImageSourceFromFile(string fname)
        {
            BitmapImage bi = new BitmapImage();
            Uri _source = new Uri(fname);
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.UriSource = _source;
            bi.EndInit();

            //bi.Freeze();
            int width = bi.PixelWidth;
            int height = bi.PixelHeight;
            //int stride = width / 8;
            PixelFormat pf = bi.Format;
            int stride = (width * pf.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[height * stride];
            bi.CopyPixels(pixels, stride, 0);
            BitmapSource bms = BitmapSource.Create(width, height, bi.DpiX, bi.DpiY,
                pf, bi.Palette, pixels, stride);
            //return bi;
            return bms;
        }

        private void newRDFdoc_Click(object sender, RoutedEventArgs e)
        {
            string ow = this.owner.Text;
            if(string.IsNullOrEmpty(ow)) return;
            cass.AddNewRdf(ow);
            cass.Save();
            RefreshViewPort();
        }

        private void MenuBuildPhotoPreviews_Click(object sender, RoutedEventArgs e)
        {
            debugField.Text = "Rebuilding...";
            //cass.RebuildPhotoPreviews();
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;
            bw.DoWork += new DoWorkEventHandler((sndr, ex) =>
            {
                ex.Result = cass.RebuildPhotoPreviewsAsync(bw);
            });
            bw.ProgressChanged += new ProgressChangedEventHandler((sndr, ex) =>
            {
                debugField.Text = "" + ex.ProgressPercentage + "%";
                progressBar.Value = ex.ProgressPercentage;
            });
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler((sndr, ex) =>
            {
                // Почему-то так не работает
                //if (ex.Cancelled) debugField.Text = "Rebuild cancelled.";
                //else debugField.Text = "Rebuild done!";
                debugField.Text = "Rebuild " + ex.Result;
            });
            cancelReceive.Tag = bw;
            bw.RunWorkerAsync();
        }
        private void MenuBuildVideoPreviews_Click(object sender, RoutedEventArgs e)
        {
            cass.RebuildVideoPreviews();
            if(this.cass.NeedToCalculate)
            {
                this.commandMakeVideo.Background = Brushes.Aqua;
                this.cass.NeedToCalculate = false;
            }
        }
        /// <summary>
        /// Перевычисление типов документов, это возможно нужно при расширении системы обратываемых расширений
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuRecalcDocTypes_Click(object sender, RoutedEventArgs e)
        {
            this.cass.RecalculateDocsType();
        }
        /// <summary>
        /// Специальное преобразование данных. Программируется, исполняется и тело преобразователя стирается 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuSpecial_Click(object sender, RoutedEventArgs e)
        {
            
            // From Zip Deep Zoom To One file with .tabe.txt
            
                       
            
            
            
            
            //Deep Zoom From JPGs in folders to newspaper

            //var rootMainDir = @"G:\Work\work\DZ_scans";
          
            //var yearDirs = Directory.GetDirectories(rootMainDir);
            //   // .SkipWhile(yearDir=>Path.GetFileName(yearDir)!="1997");
            //foreach(var yearDir in yearDirs)
            //{
            //    var yearId = "";
            //    var yearDirectoryName = Path.GetFileName(yearDir);
            //    cass.AddCollection(yearDirectoryName, cass.CollectionId, out yearId);
                
            //    foreach(var editionDir in Directory.GetDirectories(yearDir))
            //    {
            //        var editionFolderNumber = cass._folderNumber;
            //        var editionDocumentNumber = cass._documentNumber;
            //        cass.IncrementDocumentNumber();
            //        var editionDirectoryName = Path.GetFileName(editionDir);
            //        var editionId = cass.GenerateNewId() + "_" + yearDirectoryName + "_" + editionDirectoryName;
            //         cass.AddToDb(new XElement("document",
            //                                    new XAttribute(ONames.rdfabout, editionId),
            //                                    new XElement(ONames.TagName, editionDirectoryName),
            //                                    new  XElement(ONames.TagIisstore,
            //                                        new XAttribute(ONames.AttUri,
            //                                            "iiss://"+cass.Name+"@iis.nsk.su/"
            //                                            +editionFolderNumber+"/"
            //                                            +editionDocumentNumber),
            //                                        new XAttribute(ONames.AttDocumenttype, "scanned/dz"))));
            //        cass.AddToDb(new XElement(ONames.TagCollectionmember,
            //            new XAttribute(ONames.rdfabout, "cm_"+editionId+"_"+yearId),
            //            new XElement(ONames.TagCollectionitem,
            //                new XAttribute(ONames.rdfresource, editionId)),
            //            new XElement(ONames.TagIncollection,
            //                new XAttribute(ONames.rdfresource, yearId))));
            //        var filesToZip = new Dictionary<string, string>();
                    
            //        foreach (var pageXml in Directory.GetFiles(editionDir))
            //        {
            //            var pagenumber = Path.GetFileNameWithoutExtension(pageXml);
            //            var scanDocId = cass.GenerateNewId()+ "_" + yearDirectoryName + "_" + editionDirectoryName +"_"+ pagenumber;
            //            cass.AddToDb(new XElement("scanned-page",
            //                                      new XAttribute(ONames.rdfabout, scanDocId),
            //                                      new XElement("page-number", pagenumber),
            //                                      new XElement("scanned-document",
            //                                                   new XAttribute(ONames.rdfresource, editionId))
            //                             ));
            //            filesToZip.Add(pageXml, editionDirectoryName);
            //            var pageFolder = pagenumber + "_files";
            //            filesToZip.Add(editionDir+@"\" + pageFolder, editionDirectoryName + @"\" + pageFolder);
            //        }
            //        var tempXEditionName = editionFolderNumber + editionDocumentNumber;
            //        if(!Directory.Exists(cass.Dir.FullName+@"\documents\deepzoom"))
            //            Directory.CreateDirectory(cass.Dir.FullName+@"\documents\deepzoom");
            //        var zipName = cass.Dir.FullName+@"\documents\deepzoom\" + tempXEditionName+ ".zip";
            //        if(!File.Exists(zipName))
            //        {

            //            var tempXEditionPath = Path.Combine(yearDir, tempXEditionName + ".xml");
            //            if (File.Exists(tempXEditionPath))
            //                File.Delete(tempXEditionPath);
            //            File.Copy(editionDir + ".xml", tempXEditionPath);
            //            filesToZip.Add(tempXEditionPath, "");
            //            Archive.ToZip.ZipFiles(zipName, filesToZip);
            //            File.Delete(tempXEditionPath);
            //        }
            //        cass.Save();
            //    }  
            //}
            
            //Older

            //string menuId = "menurootcollection";
            //List<XElement> menuCollectionMembers = this.cass.GetInverseXItems(menuId, factograph.ONames.TagIncollection).ToList();
            //int cnt = 0;
            //foreach (XElement mCollectionMember in menuCollectionMembers)
            //{
            //    string cElementId = mCollectionMember.Element(factograph.ONames.TagCollectionitem).Attribute(ONames.rdfresource).Value;
            //    XElement cElement = this.cass.GetXItemById(cElementId);
            //    XElement xname = cElement.Element(factograph.ONames.TagName);
            //    //debugField.Text += xname.Value + "|";
            //    List<XElement> dMembers = this.cass.GetInverseXItems(cElementId, factograph.ONames.TagIncollection).ToList();
            //    foreach (XElement dMem in dMembers)
            //    {
            //        string docId = dMem.Element(factograph.ONames.TagCollectionitem).Attribute(ONames.rdfresource).Value;
            //        string dMemId = dMem.Attribute(ONames.rdfabout).Value;
            //        XElement dRefl = new XElement(factograph.ONames.TagReflection, new XAttribute(ONames.rdfabout, dMemId),
            //            new XElement(factograph.ONames.TagReflected, new XAttribute(ONames.rdfresource, cElementId)),
            //            new XElement(factograph.ONames.TagIndoc, new XAttribute(ONames.rdfresource, docId)));
            //        this.cass.AddToDb(dRefl);
            //        dMem.Remove();
            //        cnt++;
            //    }
            //    XElement orgsys = new XElement(factograph.ONames.TagOrgsys, new XAttribute(ONames.rdfabout, cElementId), xname);
            //    this.cass.AddToDb(orgsys);
            //    cElement.Remove();
            //    XElement orgrelatives = new XElement(factograph.ONames.TagOrgrelatives, new XAttribute(ONames.rdfabout, mCollectionMember.Attribute(ONames.rdfabout).Value),
            //        new XElement(factograph.ONames.TagOrgparent, new XAttribute(ONames.rdfresource, "nepal2010NY_org")),
            //        new XElement(factograph.ONames.TagOrgchild, new XAttribute(ONames.rdfresource, cElementId)));
            //    this.cass.AddToDb(orgrelatives);
            //    mCollectionMember.Remove();
            //}
            //XElement orgmain = new XElement(factograph.ONames.TagOrgsys, new XAttribute(ONames.rdfabout, "nepal2010NY_org"),
            //    new XElement(factograph.ONames.TagName, "Новый год в Гималаях"));
            //this.cass.AddToDb(orgmain);
            //debugField.Text = "" + cnt;
            //this.cass.Save();

            // Это вывод фотографий коллекциями для формирования наборов для фильма
            //OutputCollectionsForFilm ocff = new OutputCollectionsForFilm(cass, @"D:\video\nepal");
            ////ocff.ScanCollection("Новый год");
            //ocff.ScanCollection("Торунг");
            //ocff.ScanCollection("ABC");
            //ocff.ScanCollection("Пролог");
            //ocff.ScanCollection("Разное");
            //ocff.ScanCollection("Домой!");
            //ocff.ScanOrgsys("Новый год в Гималаях");
        }
        private void MenuLinkToArchive_Click(object sender, RoutedEventArgs e)
        {
            var docs = cass.db.Elements()
                .Where(el => el.Name == ONames.TagDocument
                    || el.Name == ONames.TagPhotodoc
                    || el.Name == ONames.TagVideo
                    || el.Name == ONames.TagAudio)
                .Where(el => !cass.GetInverseXItems(el.Attribute(ONames.rdfabout).Value).Any(xel => xel.Name.ToString() == "archive-member"))
                .ToArray();
            bool changed = false;
            foreach(var doc in docs)
            {
                var am = cass.CreateArchiveMember(doc.Attribute(ONames.rdfabout).Value);
                cass.db.Add(am);
                changed = true;
            }
            if(changed) cass.Save();

        }
        private void ChangeState(object sender, RoutedEventArgs e)
        {
            bool? maybeChecked = ((CheckBox)sender).IsChecked;
            if(!maybeChecked.HasValue) return;
            bool pressed = maybeChecked.Value;
            XElement xselected = GetSelectedXItem();
            if(xselected == null) return;
            string itemId = xselected.Attribute(ONames.rdfabout).Value;
            RelationSpec tag = (RelationSpec)((CheckBox)sender).Tag;
            //string relationName = tag.Attribute("relationName").Value;
            //string linkSelf = tag.Attribute("linkSelf").Value;
            //string linkOther = tag.Attribute("linkOther").Value;
            //string idOther = tag.Attribute("idOther").Value;
            string idOther = tag.idOther;
            XElement relation = cass.GetRelation(itemId,
                factograph.ONames.TagCollectionitem,
                factograph.ONames.TagCollectionmember,
                factograph.ONames.TagIncollection, idOther);
            if(pressed && relation == null)
            {
                relation = new XElement(factograph.ONames.TagCollectionmember,
                    new XAttribute(ONames.rdfabout, cass.GenerateNewId()),
                    new XElement(factograph.ONames.TagCollectionitem, new XAttribute(ONames.rdfresource, itemId)),
                    new XElement(factograph.ONames.TagIncollection, new XAttribute(ONames.rdfresource, idOther)));
                cass.AddToDb(relation);
                cass.Save();
            }
            else if(!pressed && relation != null)
            {
                relation.Remove();
                cass.Save();
            }

        }
        private void SetState()
        {
            XElement xselected = GetSelectedXItem();
            if(xselected == null) return;
            string itemId = xselected.Attribute(ONames.rdfabout).Value;
            foreach(RelationSpec rs in user_menu_list)
            {
                CheckBox checkBox1 = rs.usedCheckbox;
                //RelationSpec tag = (RelationSpec)checkBox1.Tag;
                //string relationName = tag.Attribute("relationName").Value;
                //string linkSelf = tag.Attribute("linkSelf").Value;
                //string linkOther = tag.Attribute("linkOther").Value;
                string idOther = rs.idOther; //tag.Attribute("idOther").Value;
                XElement relation = cass.GetRelation(itemId,
                    factograph.ONames.TagCollectionitem,
                    factograph.ONames.TagCollectionmember,
                    factograph.ONames.TagIncollection, idOther);
                if(relation != null)
                {
                    checkBox1.IsChecked = true;
                }
                else
                {
                    checkBox1.IsChecked = false;
                }
            }
        }

    }
    /// <summary>
    /// Класс оперативного хранения загруженных имиджей
    /// </summary>
    internal class ImageInfo
    {
        internal ImageSource Source { get; set; }
        // TODO: в него можно добавить какие-то дополнительные атрибуты типа времени последнего использования или частоты использования
    }

    //XElement relation =
    //    new XElement("relation",
    //        new XAttribute("relationName", "collection-member"),
    //        new XAttribute("linkSelf", "collection-item"),
    //        new XAttribute("linkOther", "in-collection"),
    //        new XAttribute("idOther", mp_id));
    /// <summary>
    /// Служебный класс для хранения информации о проверяемом и устанавливаемом через checkbox отношении
    /// </summary>
    internal class RelationSpec
    {
        internal XName relationName { get; set; }
        internal XName linkSelf { get; set; }
        internal XName linkOther { get; set; }
        internal string idOther { get; set; }
        internal CheckBox usedCheckbox { get; set; }
    }

}
