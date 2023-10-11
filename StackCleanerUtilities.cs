using System;
using System.IO;

namespace StackCleaner;
internal class StackCleanerUtilities
{
    internal static bool IsChildOf(string? shorterPath, string longerPath, bool includeSubDirectories = true)
    {
        if (string.IsNullOrEmpty(shorterPath))
            return true;
        if (string.IsNullOrEmpty(longerPath))
            return false;
        DirectoryInfo parent = new DirectoryInfo(shorterPath);
        DirectoryInfo child = new DirectoryInfo(longerPath);
        return IsChildOf(parent, child, includeSubDirectories);
    }

    internal static bool IsChildOf(DirectoryInfo shorterPath, DirectoryInfo longerPath, bool includeSubDirectories = true)
    {
        string shortFullname = shorterPath.FullName;
        if (!includeSubDirectories)
            return longerPath.Parent != null && longerPath.Parent.FullName.Equals(shortFullname, StringComparison.Ordinal);
        while (longerPath.Parent != null)
        {
            if (longerPath.Parent.FullName.Equals(shortFullname, StringComparison.Ordinal))
                return true;
            longerPath = longerPath.Parent;
        }

        return false;
    }

    // https://stackoverflow.com/questions/51179331/is-it-possible-to-use-path-getrelativepath-net-core2-in-winforms-proj-targeti
    internal static string GetRelativePath(string relativeTo, string path)
    {
        if (!IsChildOf(relativeTo, path))
            return path;
        if (string.IsNullOrEmpty(relativeTo))
        {
            path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            if (path.IndexOf(Path.DirectorySeparatorChar) == -1)
                path = "." + Path.DirectorySeparatorChar + path;
            return path;
        }
        path = Path.GetFullPath(path);
        relativeTo = Path.GetFullPath(relativeTo);
        Uri uri = new Uri(relativeTo);
        string rel = Uri.UnescapeDataString(uri.MakeRelativeUri(new Uri(path)).ToString()).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        int index = rel.IndexOf(Path.DirectorySeparatorChar);
        if (index == -1)
            rel = "." + Path.DirectorySeparatorChar + rel;
        else
        {
            if (index != rel.Length - 1)
            {
                rel = rel.Substring(index + 1);
                if (rel.IndexOf(Path.DirectorySeparatorChar) == -1)
                    rel = "." + Path.DirectorySeparatorChar + rel;
            }
        }

        return rel;
    }
}
