using System.Collections.Generic;
using System.IO;

public static class DirectoryExt
{
	public static List<string> GetAllFilesRecursively(string path) {

		string[] files = DirectoryExt.GetFiles(path);
		List<string> filePaths = new List<string>(files);

		foreach (string dirPath in DirectoryExt.GetDirectories(path))
		{
			filePaths.AddRange(GetAllFilesRecursively(dirPath));
		}

		return filePaths;
	}

    public static string[] GetFiles(string path)
    {
        string[] files = Directory.GetFiles(path);

        for (int i = 0; i < files.Length; i += 1)
        {
            files[i] = files[i].Replace("\\", "/");
        }
        return files;
    }

    public static string[] GetDirectories(string path)
    {
        string[] paths = Directory.GetDirectories(path);

        for (int i = 0; i < paths.Length; i += 1)
        {
            paths[i] = paths[i].Replace("\\", "/");
        }
        return paths;
    }

    public static string[] GetDirectories(string path, string searchPattern, SearchOption searchOption)
    {
        string[] paths = Directory.GetDirectories(path, searchPattern, searchOption);
        
        for (int i = 0; i < paths.Length; i+=1)
        {
            paths[i] = paths[i].Replace("\\", "/");
        }
        return paths;
    }
}

public static class PathExt
{
    public static readonly char DirectorySeparatorChar = '/';

    public static string GetDirectoryName(string path)
    {
        string result = Path.GetDirectoryName(path);
        return result.Replace("\\", "/");
    }
}