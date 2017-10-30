using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HD
{
  public struct ImageFileInfo
  {
    public string directory;
    public string fileName;

    public string fullPath
    {
      get
      {
        return Path.Combine(directory, fileName);
      }
    }

    public ImageFileInfo(
      string fullPathToFile)
    {
      int index = fullPathToFile.LastIndexOf(Path.DirectorySeparatorChar);
      if (index >= 0)
      {
        directory = fullPathToFile.Substring(0, index);
        fileName = fullPathToFile.Substring(index + 1);
      }
      else
      {
        directory = Environment.CurrentDirectory;
        fileName = fullPathToFile;
      }
    }

    public override string ToString()
    {
      return fileName;
    }
  }
}
