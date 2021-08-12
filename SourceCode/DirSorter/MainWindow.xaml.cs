using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DirSorter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        CustomPickerDialog dialog = new();
        static Data data = new();
        static List<ListView> ListViews = new();

        public MainWindow()
        {
            data.ReadData();
            InitializeComponent();
            ListViews.Add(ListViewSortBundles);
            ListViews.Add(ListViewSortableFolders);
            initViewLists();
        }//ok

        public void AddButtonHandler(object sender, RoutedEventArgs e)
        {
            Tab.SelectedItem = Tab.Items.GetItemAt(1);
        }

        private void initViewLists()
        {
            ListSortBundles.InitListSortBundles(ListViewSortBundles, data.SortBundles);
            SortableFoldersHandler.InitListSortableFolders(ListViewSortableFolders, data.SortableFolders);
        }

        public void SortableFolderButtonHandler(object sender, RoutedEventArgs e)
        {
            SortableFolderTextBox.Text = dialog.FolderDialog();
        }

        public void SortBundleStorageFolderButtonHandler(object sender, RoutedEventArgs e)
        {
            SortBundleStorageFolder.Text = dialog.FolderDialog();
        }

        public void SortBundleStoingFileTypeButtonHandler(object sender, RoutedEventArgs e)
        {
            SortBundleStoingFileType.Text = System.IO.Path.GetExtension(dialog.FileDialog());//ok
        }

        public void CreateSortingAssociacionHandler(object sender, RoutedEventArgs e)
        {
            if (SortBundleStorageFolder.Text == "" || SortBundleStoingFileType.Text == "")
            {
                MessageBox.Show("Заполните все поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else if (!Directory.Exists(SortBundleStorageFolder.Text))
            {
                MessageBox.Show("Указанная папка не существует", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Data.SortBundle template = new()
            {
                Folder = SortBundleStorageFolder.Text,
                FileType = SortBundleStoingFileType.Text,
            };
            if (ListSortBundles.ElementExists(data, template))
            {
                MessageBox.Show("Типу файла может принадлежать только одна папка", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            SortBundleStorageFolder.Text = null;
            SortBundleStoingFileType.Text = null;
            data.SortBundles.Add(template);
            ListSortBundles.AddElement(ListViewSortBundles, template);
            data.WriteData();
        }

        public void AddSortableFolderButtonHandler(object sender, RoutedEventArgs e)
        {
            if (SortableFolderTextBox.Text == "")
            {
                MessageBox.Show("Заполните все поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else if (!Directory.Exists(SortableFolderTextBox.Text))
            {
                MessageBox.Show("Указанная папка не существует", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else if (data.SortableFolders.Contains(SortableFolderTextBox.Text))
            {
                MessageBox.Show("Похожая папка уже имеется в списке", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            data.SortableFolders.Add(SortableFolderTextBox.Text);
            data.WriteData();
            SortableFoldersHandler.AddElement(ListViews[1], SortableFolderTextBox.Text);
            SortableFolderTextBox.Text = null;
        }

        public static void RemoveSortBundleButtonHandler(object sender, RoutedEventArgs e)
        {
            data = ListSortBundles.RemoveElement(ListViews[0], data);
        }

        public static void RemoveStorableFolderButtonHandler(object sender, RoutedEventArgs e)
        {
            data = SortableFoldersHandler.RemoveElement(ListViews[1], data);
        }

        public async void StartSoringButtonHandler(object sender, RoutedEventArgs e)
        {

            Tab.SelectedItem = Tab.Items.GetItemAt(2);
            Tab.IsEnabled = false;
            foreach (string storableFolder in data.SortableFolders)
            {
                int percent = 100 / ((Directory.GetFiles(storableFolder).Length != 0) ? Directory.GetFiles(storableFolder).Length : 50);
                foreach (string checkedFile in Directory.GetFiles(storableFolder))
                {
                    CurrentFile.Content = checkedFile;
                    if (!File.Exists(checkedFile))
                    {
                        continue;
                    }
                    foreach (Data.SortBundle SortBundle in data.SortBundles)
                    {
                        if (SortBundle.FileType == Path.GetExtension(checkedFile))
                        {
                            try
                            {
                                if (Move.IsChecked.Value)
                                {
                                    if (File.Exists(@$"{SortBundle.Folder}\{Path.GetFileName(checkedFile)}"))
                                    {
                                        File.Delete(@$"{SortBundle.Folder}\{Path.GetFileName(checkedFile)}");
                                    }
                                    await Task.Run(() => File.Move(checkedFile, @$"{SortBundle.Folder}\{Path.GetFileName(checkedFile)}"));
                                }
                                else
                                {
                                    File.Delete(checkedFile);
                                }
                            }
                            catch (Exception err)
                            {
                                MessageBox.Show("Ошибка при перемещинии файла: " + checkedFile + '\n' + err.Message);
                                break;
                            }
                            break;
                        }
                    }
                    SortableProgress.Value += percent;
                }

            }
            Tab.IsEnabled = true;
            Tab.SelectedItem = Tab.Items.GetItemAt(0);
            SortableProgress.Value = 0;
            MessageBox.Show("Сортировка выполнена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public partial class SortableFoldersHandler : MainWindow
    {
        public static void InitListSortableFolders(ListView listViewSortableFolders, List<string> SortableFolders)
        {
            foreach (string SortableFolder in SortableFolders)
            {
                AddElement(listViewSortableFolders, SortableFolder);
            }
        }

        public static void AddElement(ListView listView, string ListViewSortableFolderElement)
        {
            ListViewSortableFolderElement toAdd = new() { Folder = ListViewSortableFolderElement };
            MenuItem buttonDelete = new() { Header = "Удалить" };
            buttonDelete.Click += RemoveStorableFolderButtonHandler;
            ContextMenu contextMenu = new();
            contextMenu.Items.Add(buttonDelete);
            ListViewItem listItem = new()
            {
                Content = toAdd,
                ContextMenu = contextMenu,
            };
            listView.Items.Add(listItem);
        }

        public class ListViewSortableFolderElement
        {
            public string Folder { get; set; }
        }

        public static Data RemoveElement(ListView listView, Data data)
        {
            ListViewSortableFolderElement template = (ListViewSortableFolderElement)((ListViewItem)listView.SelectedItem).Content;
            data.SortableFolders.Remove(template.Folder);
            listView.Items.Remove(listView.SelectedItem);
            data.WriteData();
            return data;
        }
    }

    public partial class ListSortBundles : MainWindow
    {
        public static void InitListSortBundles(ListView listView, List<Data.SortBundle> elemetsList)
        {
            foreach (Data.SortBundle ListViewSortBundle in elemetsList)
            {
                AddElement(listView, ListViewSortBundle);
            }
        }

        public static void AddElement(ListView listView, Data.SortBundle SortBundle)
        {
            MenuItem buttonDelete = new() { Header = "Удалить" };
            buttonDelete.Click += RemoveSortBundleButtonHandler;
            ContextMenu contextMenu = new();
            contextMenu.Items.Add(buttonDelete);
            ListViewItem listItem = new()
            {
                Content = SortBundle,
                ContextMenu = contextMenu,
            };
            listView.Items.Add(listItem);
        }

        public static Data RemoveElement(ListView listView, Data data)
        {
            ListViewItem template = (ListViewItem)listView.SelectedItem;
            data.SortBundles.Remove((Data.SortBundle)template.Content);
            listView.Items.Remove(listView.SelectedItem);
            data.WriteData();
            return data;
        }

        public static bool ElementExists(Data data, Data.SortBundle association)
        {
            foreach (Data.SortBundle sort in data.SortBundles)
            {
                if (sort.FileType == association.FileType)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class CustomPickerDialog
    {
        CommonOpenFileDialog dialog = new()
        {
            Multiselect = false
        };

        public string FileDialog()
        {
            dialog.IsFolderPicker = false;
            dialog.Title = "Выберите файл";
            return dialogStatus(dialog);
        }

        public string FolderDialog()
        {
            dialog.IsFolderPicker = true;
            dialog.Title = "Выберите папку";
            return dialogStatus(dialog);
        }

        private string dialogStatus(CommonOpenFileDialog dialog)
        {
            return dialog.ShowDialog() switch
            {
                CommonFileDialogResult.Cancel => null,
                CommonFileDialogResult.Ok => dialog.FileName,
                _ => null,
            };
        }
    }

    public class Data
    {
        private static string settingsDirectory = @$"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\120Codes\DirSorter";
        private string settingsFileForSortBundels = settingsDirectory + @"\association.json";
        private string settingsFileForSortableFolders = settingsDirectory + @"\dirs.json";

        public List<SortBundle> SortBundles = new();

        public List<string> SortableFolders = new();
        public class SortBundle
        {
            public string Folder { get; set; }
            public string FileType { get; set; }
        }

        public void ReadData()
        {
            if (!Directory.Exists(settingsDirectory))
            {
                Directory.CreateDirectory(settingsDirectory);
                return;
            }
            else if (!File.Exists(settingsFileForSortableFolders) || !File.Exists(settingsFileForSortBundels)) return;

            SortableFolders = JsonSerializer.Deserialize<List<string>>(readFile(settingsFileForSortableFolders));
            SortBundles = JsonSerializer.Deserialize<List<SortBundle>>(readFile(settingsFileForSortBundels));

            byte[] readFile(string file)
            {
                using (FileStream fs = File.Open(file, FileMode.Open))
                {
                    byte[] template = new byte[fs.Length];
                    fs.Read(template, 0, (int)fs.Length);
                    fs.Close();
                    return template;
                }
            }
        }

        public void WriteData()
        {
            writeFile(SortBundles, settingsFileForSortBundels);
            writeFile(SortableFolders, settingsFileForSortableFolders);

            void writeFile(object data, string directory)
            {
                byte[] template = JsonSerializer.SerializeToUtf8Bytes(data);
                using (FileStream fs = File.Open(directory, FileMode.Create))
                {
                    fs.Write(template, 0, template.Length);
                    fs.Close();
                }
            }
        }


    }
}
