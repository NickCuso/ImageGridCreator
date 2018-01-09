using HD;
using ImageMagick;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ImageGridCreator
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    #region Data
    static readonly string fileFormatFilter = "Images|" + FileCsv(new string[] {
        "png",
        "jpg",
        "jpeg",
        "bmp",
        "gif",
      });
    #endregion

    #region Properties
    public MagickImage firstImage
    {
      get
      {
        return new MagickImage(((ImageFileInfo)FileList.Items[0]).fullPath);
      }
    }

    public int targetResolutionWidth
    {
      get
      {
        if (string.IsNullOrWhiteSpace(ResolutionWidthText.Text))
        {
          return 1024;
        }

        return int.Parse(ResolutionWidthText.Text);
      }
    }

    public int tileCountWidth
    {
      get
      {
        if (string.IsNullOrWhiteSpace(GridSizeWidthText.Text))
        {
          return 1;
        }

        return int.Parse(GridSizeWidthText.Text);
      }
    }

    public int targetResolutionHeight
    {
      get
      {
        if (string.IsNullOrWhiteSpace(GridSizeHeightText.Text))
        {
          return 800;
        }

        int tileCountHeight = int.Parse(GridSizeHeightText.Text);
        double aspectRatio = (double)tileCountWidth / tileCountHeight;
        return (int)(targetResolutionWidth * aspectRatio);
      }
    }
    #endregion

    #region Init
    public MainWindow()
    {
      InitializeComponent();
    }
    #endregion

    #region Events
    void SelectFilesButton_Click(
      object sender,
      RoutedEventArgs e)
    {
      OpenFileDialog dialog = new OpenFileDialog
      {
        Multiselect = true,
        Filter = fileFormatFilter
      };
      bool? result = dialog.ShowDialog(this);
      if (result.HasValue == false || result.Value == false)
      {
        return;
      }

      AddFiles(dialog.FileNames);
    }

    void UpdateDefaultSettings()
    {
      // Update Tile Count
      if (FileList.Items.Count > 0)
      {
        GridSizeHeightText.Text = (Math.Ceiling((double)FileList.Items.Count / tileCountWidth)).ToString("N0");
        ResolutionWidthText.Text = (firstImage.Width * tileCountWidth).ToString();
      }
      else
      {
        if (GridSizeHeightText != null)
        {
          GridSizeHeightText.Text = GridSizeWidthText.Text;
        }
        if (ResolutionWidthText != null)
        {
          ResolutionWidthText.Text = "1024";
        }
      }
    }

    void MoveUpButton_Click(
      object sender,
      RoutedEventArgs e)
    {
      Move(moveUpVsDown: true);
    }

    void MoveDownButton_Click(
      object sender,
      RoutedEventArgs e)
    {
      Move(moveUpVsDown: false);
    }

    void RemoveButton_Click(
      object sender,
      RoutedEventArgs e)
    {
      for (int i = FileList.SelectedItems.Count - 1; i >= 0; i--)
      {
        FileList.Items.Remove(FileList.SelectedItems[i]);
      }
      GridSizeWidthText.Text = ((int)Math.Round(Math.Sqrt(FileList.Items.Count))).ToString();
      UpdateDefaultSettings();
      Refresh();
    }

    void FileList_SelectionChanged(
      object sender,
      SelectionChangedEventArgs e)
    {
      if(FileList.SelectedItem == null)
      {
        return;
      }
      PreviewImage.Source = new BitmapImage(new Uri(((ImageFileInfo)FileList.SelectedItem).fullPath));
    }

    void SaveButton_Click(
      object sender,
      RoutedEventArgs e)
    {
      SaveFileDialog dialog = new SaveFileDialog
      {
        Filter = fileFormatFilter
      };
      bool? result = dialog.ShowDialog(this);
      if (result.HasValue && result.Value)
      {
        using (MagickImageCollection images = new MagickImageCollection())
        {
          int tileCountWidth = int.Parse(GridSizeWidthText.Text);
          int tileCountHeight = int.Parse(GridSizeHeightText.Text);
          int targetResolutionWidth = int.Parse(ResolutionWidthText.Text);
          int tileWidth = (int)Math.Round((double)targetResolutionWidth / tileCountWidth);
          int tileHeight = tileWidth;

          MagickColor color = null;
          for (int i = 0; i < FileList.Items.Count; i++)
          {
            ImageFileInfo imageFileInfo = (ImageFileInfo)FileList.Items[i];
            MagickImage magickImage = new MagickImage(imageFileInfo.fullPath);
            color = magickImage.BackgroundColor;
            images.Add(magickImage);
          }

          MontageSettings settings = new MontageSettings
          {
            // this is each image's size
            Geometry = new MagickGeometry(tileWidth, tileHeight),
            // Grid cell counts
            TileGeometry = new MagickGeometry(tileCountWidth, tileCountHeight)
          };

          using (IMagickImage saveResult = images.Montage(settings))
          {
            if (color.A > 0)
            {
              saveResult.Transparent(color);
            }

            if (saveResult.Width > targetResolutionWidth)
            {
              saveResult.Resize(targetResolutionWidth, targetResolutionHeight);
            }
            saveResult.Write(dialog.FileName);
            Process process = new Process();
            process.StartInfo.FileName = dialog.FileName;
            process.Start();
          }
        }
      }
    }

    void GridSizeWidthText_PreviewTextInput(
      object sender,
      TextCompositionEventArgs e)
    {
      if (ContainsOnlyNumbers(e.Text) == false)
      {
        e.Handled = true;
      }
    }

    void GridSizeHeightText_PreviewTextInput(
      object sender,
      TextCompositionEventArgs e)
    {
      if (ContainsOnlyNumbers(e.Text) == false)
      {
        e.Handled = true;
      }
    }

    void ResolutionWidthText_PreviewTextInput(
      object sender,
      TextCompositionEventArgs e)
    {
      if (ContainsOnlyNumbers(e.Text) == false)
      {
        e.Handled = true;
      }
    }

    void ResolutionWidthText_TextChanged(
      object sender,
      TextChangedEventArgs e)
    {
      RefreshCompression();
    }

    void GridSizeWidthText_TextChanged(
      object sender,
      TextChangedEventArgs e)
    {
      UpdateDefaultSettings();
      Refresh();
    }

    void GridSizeHeightText_TextChanged(
      object sender,
      TextChangedEventArgs e)
    {
      Refresh();
    }

    void FileList_Drop(
      object sender,
      DragEventArgs e)
    {
      AddFiles((string[])e.Data.GetData(DataFormats.FileDrop, false)); 
    }
    #endregion

    #region Private Write
    void AddFiles(
      string[] fileNames)
    {
      for (int fileNameIndex = 0; fileNameIndex < fileNames.Length; fileNameIndex++)
      {
        string fileName = fileNames[fileNameIndex];
        ImageFileInfo imageFileInfo = new ImageFileInfo(fileName);
        FileList.Items.Add(imageFileInfo);
      }
      GridSizeWidthText.Text = ((int)Math.Ceiling(Math.Sqrt(FileList.Items.Count))).ToString();

      UpdateDefaultSettings();

      Refresh();
    }

    void Move(
      bool moveUpVsDown)
    {
      int i, delta;
      if (moveUpVsDown)
      {
        i = 0;
        delta = 1;
      }
      else
      {
        i = FileList.Items.Count - 1;
        delta = -1;
      }

      List<object> tempSelectedItems = CloneTemp();

      for (; i >= 0 && i < FileList.Items.Count; i += delta)
      {
        if (i - delta < 0 || i - delta >= FileList.Items.Count)
        {
          continue;
        }

        object imageFileInfo = FileList.Items[i];
        if (tempSelectedItems.Contains(imageFileInfo))
        {
          object temp = FileList.Items[i - delta];
          if (tempSelectedItems.Contains(temp))
          { // Skip if the item to swap with is also selected
            continue;
          }
          FileList.Items[i - delta] = imageFileInfo;
          FileList.Items[i] = temp;
        }
      }

      for (int tempIndex = 0; tempIndex < tempSelectedItems.Count; tempIndex++)
      {
        FileList.SelectedItems.Add(tempSelectedItems[tempIndex]);
      }
    }

    List<object> CloneTemp()
    {
      List<object> tempSelectedItems = new List<object>();
      for (int tempIndex = 0; tempIndex < FileList.SelectedItems.Count; tempIndex++)
      {
        tempSelectedItems.Add(FileList.SelectedItems[tempIndex]);
      }

      return tempSelectedItems;
    }

    void Refresh()
    {
      RefreshCompression();
    }

    void RefreshCompression()
    {
      double compression = 0;

      if (FileList.Items.Count > 0)
      {
        if (firstImage.Width * tileCountWidth > targetResolutionWidth)
        {
          compression = ((double)targetResolutionWidth / tileCountWidth) / firstImage.Width;
        }
      }

      if (ResolutionHeightLabel != null)
      {
        ResolutionHeightLabel.Content = $"x {targetResolutionHeight} ({compression:N0}% compression)";
      }
    }
    #endregion

    #region Private Read
    static string FileCsv(
      string[] extensions)
    {
      StringBuilder stringBuilder = new StringBuilder();
      for (int i = 0; i < extensions.Length; i++)
      {
        if (i > 0)
        {
          stringBuilder.Append(";");
        }

        stringBuilder.Append("*.");
        stringBuilder.Append(extensions[i]);
      }

      return stringBuilder.ToString();
    }

    bool ContainsOnlyNumbers(
      string value)
    {
      int test;
      return int.TryParse(value, out test);
    }
    #endregion
  }
}
