﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using Microsoft.Win32;
using System.Windows.Threading;

namespace pserv4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        protected class DataView
        {
            public readonly DataController Controller;
            public readonly Button Button;

            public DataView(DataController controller, Button button)
            {
                Controller = controller;
                Button = button;
            }
        }

        private readonly Dictionary<MainViewType, DataView> KnownViews = new Dictionary<MainViewType, DataView>();
        public static ObservableCollection<DataObject> Items = new ObservableCollection<DataObject>();
        public static DataController CurrentController;
        private readonly MainViewType InitialControl;
        private MainViewType CurrentViewType;

        private SolidColorBrush SelectedBackgroundColor;
        private SolidColorBrush SelectedForegroundColor;
        private SolidColorBrush UnselectedBackgroundColor;
        private SolidColorBrush UnselectedForegroundColor;

        public static BitmapImage[] BIStart = new BitmapImage[2];
        public static BitmapImage[] BIStop = new BitmapImage[2];
        public static BitmapImage[] BIRestart = new BitmapImage[2];
        public static BitmapImage[] BIPause = new BitmapImage[2];
        public static BitmapImage[] BIContinue = new BitmapImage[2];

        private void SetBitmapImage(BitmapImage[] array, string image)
        {
            Image i = new Image();
            string filename = string.Format(@"pack://application:,,,/images/control_{0}.png", image);
            array[0] = new BitmapImage(new Uri(filename));
            i = new Image();
            filename = string.Format(@"pack://application:,,,/images/control_{0}_blue.png", image);
            array[1] = new BitmapImage(new Uri(filename));
        }

        public MainWindow(MainViewType initialControl)
        {
            InitializeComponent();
            InitialControl = initialControl;
            CurrentViewType = MainViewType.Invalid;
            KnownViews[MainViewType.Services] = new DataView(new services.ServicesDataController(), ButtonServices);
            KnownViews[MainViewType.Devices] = new DataView(new devices.DevicesDataController(), ButtonDevices);
            KnownViews[MainViewType.Windows] = new DataView(new windows.WindowsDataController(), ButtonWindows);
            KnownViews[MainViewType.Uninstaller] = new DataView(new uninstaller.UninstallerDataController(), ButtonUninstaller);
            KnownViews[MainViewType.Processes] = new DataView(new processes.ProcessesDataController(), ButtonProcesses);
            KnownViews[MainViewType.Modules] = new DataView(new modules.ModulesDataController(), ButtonModules);

            SetBitmapImage(BIStart, "play");
            SetBitmapImage(BIStop, "stop");
            SetBitmapImage(BIRestart, "repeat");
            SetBitmapImage(BIPause, "pause");
            SetBitmapImage(BIContinue, "fastforward");

            UnselectedBackgroundColor = new SolidColorBrush(Color.FromArgb(255, 0xF5, 0xF5, 0xF5));
            UnselectedForegroundColor = new SolidColorBrush(Colors.Black);
            SelectedBackgroundColor = new SolidColorBrush(Colors.CornflowerBlue);
            SelectedForegroundColor = new SolidColorBrush(Colors.White);

        }

        private void timer_Tick(object sender, EventArgs e)
        {
            Trace.TraceInformation("*** BEGIN TIMER TICK: {0}", System.Threading.Thread.CurrentThread.ManagedThreadId);

            RefreshDisplay(sender, null);

            Trace.TraceInformation("*** END TIMER TICK: {0}", System.Threading.Thread.CurrentThread.ManagedThreadId);
        }

        private void SwitchController(MainViewType newViewType)
        {
            Trace.TraceInformation("*** SwitchController {0}", System.Threading.Thread.CurrentThread.ManagedThreadId);
            if (CurrentViewType == newViewType)
                return;

            using (new WaitCursor())
            {
                Items.Clear();
                MainGridView.Columns.Clear();
                ICollectionView dataView = CollectionViewSource.GetDefaultView(Items);
                dataView.SortDescriptions.Clear();

                if (CurrentViewType != MainViewType.Invalid)
                {
                    KnownViews[CurrentViewType].Button.Background = UnselectedBackgroundColor;
                    KnownViews[CurrentViewType].Button.Foreground = UnselectedForegroundColor;
                }

                // cleanup
                CurrentController = KnownViews[newViewType].Controller;

                // create columns
                CurrentController.Refresh(Items);
                FindThisText.Text = "";

                MainListView.ItemsSource = Items;   //your query result 

                foreach (DataObjectColumn oc in CurrentController.Columns)
                {
                    GridViewColumn column = new GridViewColumn();
                    column.Header = oc.DisplayName;
                    column.DisplayMemberBinding = new Binding(oc.BindingName);
                    column.Width = System.Double.NaN;
                    MainGridView.Columns.Add(column);
                }
                CurrentController.MainListView = MainListView;
                MainListView.ContextMenu = CurrentController.ContextMenu;
                MainListView.ContextMenuOpening += MainListView_ContextMenuOpening;

                UpdateDefaultStatusBar();

                TbControlStart.Text = CurrentController.ControlStartDescription;
                TbControlStop.Text = CurrentController.ControlStopDescription;
                TbControlRestart.Text = CurrentController.ControlRestartDescription;
                TbControlPause.Text = CurrentController.ControlPauseDescription;
                TbControlContinue.Text = CurrentController.ControlContinueDescription;

                CreateInitialSort();
                CurrentViewType = newViewType;
                KnownViews[CurrentViewType].Button.Background = SelectedBackgroundColor;
                KnownViews[CurrentViewType].Button.Foreground = SelectedForegroundColor;
            }
        }

        void MainListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            try
            {
                CurrentController.OnContextMenuOpening(MainListView.SelectedItems, MainListView.ContextMenu);
            }
            catch(Exception)
            {
            }
        }

        private void ShowProperties(object sender, RoutedEventArgs e)
        {
            CurrentController.ShowProperties(sender, e);
        }

        private void SwitchToServices(object sender, RoutedEventArgs e)
        {
            SwitchController(MainViewType.Services);
        }

        private void SwitchToDevices(object sender, RoutedEventArgs e)
        {
            SwitchController(MainViewType.Devices);
        }

        private void SwitchToWindows(object sender, RoutedEventArgs e)
        {
            SwitchController(MainViewType.Windows);
        }

        private void SwitchToUninstaller(object sender, RoutedEventArgs e)
        {
            SwitchController(MainViewType.Uninstaller);
        }

        private void SwitchToProcesses(object sender, RoutedEventArgs e)
        {
            SwitchController(MainViewType.Processes);
        }

        private void SwitchToModules(object sender, RoutedEventArgs e)
        {
            SwitchController(MainViewType.Modules);
        }

        private void OnControlStart(object sender, RoutedEventArgs e)
        {
            CurrentController.OnControlStart(sender, e);
        }

        private void OnControlStop(object sender, RoutedEventArgs e)
        {
            CurrentController.OnControlStop(sender, e);
        }

        private void OnControlRestart(object sender, RoutedEventArgs e)
        {
            CurrentController.OnControlRestart(sender, e);
        }

        private void OnControlPause(object sender, RoutedEventArgs e)
        {
            CurrentController.OnControlPause(sender, e);
        }

        private void OnControlContinue(object sender, RoutedEventArgs e)
        {
            CurrentController.OnControlContinue(sender, e);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SwitchController(InitialControl);
            DispatcherTimer dt = new DispatcherTimer();
            dt.Tick += new EventHandler(timer_Tick);
            dt.Interval = new TimeSpan(0, 0, 5); // execute every hour
            dt.Start();

            Trace.TraceInformation("*** START TICK: {0}", System.Threading.Thread.CurrentThread.ManagedThreadId);
        }

        private void Zoom_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            bool handle = (Keyboard.Modifiers & ModifierKeys.Control) > 0;
            if (handle)
            {
                if (e.Delta > 0)
                    MainListView.FontSize += 1.0;
                else
                    MainListView.FontSize -= 1.0;

                if (MainListView.FontSize < 4.0)
                    MainListView.FontSize = 4.0;

                if (MainListView.FontSize > 60)
                    MainListView.FontSize = 60;
            }
            AutoResizeAllColumns();
        }

        private void AutoResizeAllColumns()
        {
            GridView gridView = MainListView.View as GridView;

            foreach (GridViewColumn column in gridView.Columns)
            {
                column.Width = System.Double.NaN;
            }
        }

        private void MainListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataObject item = ((FrameworkElement)e.OriginalSource).DataContext as DataObject;
            if (item != null)
            {
                ShowProperties(sender, e);
            }
        }

        private void SetControlButton(Button b, BitmapImage[] images, bool enabled)
        {
            b.IsEnabled = enabled;
            Image i = b.Content as Image;
            if( i != null )
            {
                i.Source = enabled ? images[1] : images[0];
            }
            b.Background = UnselectedBackgroundColor;
            if (enabled)
            {
            }
            else
            {

            }
        }

        private void MainListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool enabled = MainListView.SelectedItems.Count > 0;

            CurrentController.OnSelectionChanged(MainListView.SelectedItems);

            SetControlButton(BtStart, BIStart, CurrentController.IsControlStartEnabled);
            SetControlButton(BtStop, BIStop, CurrentController.IsControlStopEnabled);
            SetControlButton(BtRestart, BIRestart, CurrentController.IsControlRestartEnabled);
            SetControlButton(BtPause, BIPause, CurrentController.IsControlPauseEnabled);
            SetControlButton(BtContinue, BIContinue, CurrentController.IsControlContinueEnabled);

            SbSelected.Text = string.Format("{0} selected", MainListView.SelectedItems.Count);
        }

        private void UpdateDefaultStatusBar()
        {
            int nVisible = 0;
            int nHighlighted = 0;
            int nDisabled = 0;
            int nHidden = 0;
            foreach (DataObject o in Items)
            {
                ++nVisible;
                if (o.IsDisabled)
                    ++nDisabled;

                else if (o.IsRunning)
                    ++nHighlighted;
            }
            SbVisible.Text = string.Format("{0} visible", nVisible);
            SbHighlighted.Text = string.Format("{0} highlighted", nHighlighted);
            SbDisabled.Text = string.Format("{0} disabled", nDisabled);
            SbHidden.Text = string.Format("{0} filtered", nHidden);
            SbTotal.Text = string.Format("{0} total", Items.Count);
            SbSelected.Text = string.Format("{0} selected", MainListView.SelectedItems.Count);
        }

        private void UpdateFilteredStatusBar()
        {
            int nVisible = 0;
            int nHighlighted = 0;
            int nDisabled = 0;
            int nHidden = 0;
            foreach (DataObject o in Items)
            {
                if (FilterDataObjectItem(o))
                {
                    ++nVisible;
                    if (o.IsDisabled)
                        ++nDisabled;

                    else if (o.IsRunning)
                        ++nHighlighted;

                }
                else
                {
                    ++nHidden;
                }
            }
            SbVisible.Text = string.Format("{0} visible", nVisible);
            SbHighlighted.Text = string.Format("{0} highlighted", nHighlighted);
            SbDisabled.Text = string.Format("{0} disabled", nDisabled);
            SbHidden.Text = string.Format("{0} filtered", nHidden);
        }

        private void FindThisText_TextChanged(object sender, TextChangedEventArgs e)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(Items);
            if (FindThisText.Text.Trim().Length == 0)
            {
                view.Filter = null;
                UpdateDefaultStatusBar();
            }
            else
            {
                view.Filter = new Predicate<object>(FilterDataObjectItem);
                UpdateFilteredStatusBar();
            }
        }

        private bool FilterDataObjectItem(object obj)
        {
            DataObject item = obj as DataObject;
            if (item == null) 
                return false;


            string findThisText = FindThisText.Text.Trim().ToLower();

            Type actualType = obj.GetType();

            foreach(DataObjectColumn oc in CurrentController.Columns)
            {
                object actualValue = actualType.GetProperty(oc.BindingName).GetValue(obj, null);
                if (actualValue != null)
                {
                    string sValue = actualValue as string;
                    if (sValue == null)
                        sValue = actualValue.ToString();

                    if( sValue.ToLower().Contains(findThisText) )
                        return true;
                }
            }
            return false;
        }

        private string LastHeaderClicked;
        private ListSortDirection LastSortDirection;

        private void MainListView_GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            string headerClicked = e.OriginalSource as string;
            if (headerClicked == null)
            {
                GridViewColumnHeader header = e.OriginalSource as GridViewColumnHeader;
                if (header != null)
                {
                    Binding binding = header.Column.DisplayMemberBinding as Binding;
                    headerClicked = binding.Path.Path;
                }
            }
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked != LastHeaderClicked)
                {
                    direction = ListSortDirection.Ascending;
                }
                else
                {
                    if (LastSortDirection == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                Sort(headerClicked, direction);
                LastHeaderClicked = headerClicked;
                LastSortDirection = direction;
            }
        }

        private void CreateInitialSort()
        {
            Binding b = MainGridView.Columns[0].DisplayMemberBinding as Binding;
            MainListView_GridViewColumnHeaderClickedHandler(this, new RoutedEventArgs(null, b.Path.Path));
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(Items);

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }

        private IList GetExportItems()
        {
            IList list = MainListView.SelectedItems;
            if (list.Count == 0)
                list = MainListView.Items;
            return list;
        }

        private void CopyToClipboard(object sender, RoutedEventArgs e)
        {
            CurrentController.SaveAsXml(null, GetExportItems());
        }

        private void SaveAsXML(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();

            // Set filter for file extension and default file extension 
            dialog.DefaultExt = ".xml";
            dialog.Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*";

            // Display OpenFileDialog by calling ShowDialog method 
            bool? result = dialog.ShowDialog();
            if( result.HasValue && result.Value)
            {
                CurrentController.SaveAsXml(dialog.FileName, GetExportItems());
            }
        }

        public static IEnumerable<ListViewItem> GetListViewItemsFromList(ListView lv)
        {
            return FindChildrenOfType<ListViewItem>(lv);
        }

        public static IEnumerable<T> FindChildrenOfType<T>(DependencyObject ob)
            where T : class
        {
            foreach (var child in GetChildren(ob))
            {
                T castedChild = child as T;
                if (castedChild != null)
                {
                    yield return castedChild;
                }
                else
                {
                    foreach (var internalChild in FindChildrenOfType<T>(child))
                    {
                        yield return internalChild;
                    }
                }
            }
        }

        public static IEnumerable<DependencyObject> GetChildren(DependencyObject ob)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(ob);

            for (int i = 0; i < childCount; i++)
            {
                yield return VisualTreeHelper.GetChild(ob, i);
            }
        }

        private void RefreshDisplay(object sender, RoutedEventArgs e)
        {
            Trace.TraceInformation("BEGIN RefreshDisplay()");
            Trace.Indent();
            CurrentController.Refresh(Items);

            Trace.Unindent();
            Trace.TraceInformation("END RefreshDisplay()");
        }

        private void MainListView_KeyDown(object sender, KeyEventArgs e)
        {
            if( e.Key == Key.F5 )
            {
                RefreshDisplay(null, null);
            }
        }
    }
}
