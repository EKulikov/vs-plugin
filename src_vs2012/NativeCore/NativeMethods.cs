﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace BlackBerry.NativeCore
{
    /// <summary>
    /// Class collecting P/Invoke definitions.
    /// </summary>
    static class NativeMethods
    {
        /// <summary>
        /// Indicates type of the Windows Exe binary (x86 or x64) or not executable at all.
        /// </summary>
        /// <param name="applicationName">Full path to the file to check</param>
        /// <param name="binaryType">If file is binary the bitness of the app is indicated by lpBinaryType value.</param>
        /// <returns>True if the file is binary false otherwise</returns>
        [DllImport("kernel32.dll", CharSet=CharSet.Auto)]
        public static extern bool GetBinaryType([MarshalAs(UnmanagedType.LPTStr)] string applicationName, out uint binaryType);

        /// <summary>
        /// Interface to unmanaged code for getting the ShortPathName for a given directory.
        /// </summary>
        /// <param name="path">Path to be converted</param>
        /// <param name="shortPath">Returned ShortPathName</param>
        /// <param name="shortPathLength">Length of the ShortPathName</param>
        /// <returns></returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern int GetShortPathName(
                 [MarshalAs(UnmanagedType.LPTStr)]
                   string path,
                 [MarshalAs(UnmanagedType.LPTStr)]
                   StringBuilder shortPath,
                 int shortPathLength);

        /// <summary> GDB works with short path names only, which requires converting the path names to/from long ones. This function 
        /// returns the long path name for a given short one. </summary>
        /// <param name="path">Short path name. </param>
        /// <param name="longPath">Returns this long path name. </param>
        /// <param name="longPathLength"> Length of this long path name. </param>
        /// <returns></returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern int GetLongPathName(
            [MarshalAs(UnmanagedType.LPTStr)]
            string path,
            [MarshalAs(UnmanagedType.LPTStr)]
            StringBuilder longPath,
            int longPathLength);

        /// <summary>
        /// Gets the short version of specified path.
        /// </summary>
        public static string GetShortPathName(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            const int size = 1024;
            var buffer = new StringBuilder(size);

            do
            {
                int result = GetShortPathName(path, buffer, buffer.Capacity);

                // did the call failed to convert the path?
                if (result == 0)
                    return null;
                // was the buffer too small:
                if (result == buffer.Capacity)
                {
                    buffer.Capacity += size;
                    continue;
                }

                return buffer.ToString(0, result);
            } while (false);

            return null;
        }

        /// <summary>
        /// Gets the long version of specified path.
        /// </summary>
        public static string GetLongPathName(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            // is it really a short name?
            if (path.IndexOf('~') < 0)
                return path;

            // expand to full-path:
            const int size = 1024;
            var buffer = new StringBuilder(size);

            do
            {
                int result = GetLongPathName(path, buffer, buffer.Capacity);

                // did the call failed to convert the path?
                if (result == 0)
                    return null;
                // was the buffer too small:
                if (result == buffer.Capacity)
                {
                    buffer.Capacity += size;
                    continue;
                }

                if (Path.DirectorySeparatorChar != '/')
                    buffer.Replace('/', Path.DirectorySeparatorChar);
                return buffer.ToString(0, result);
            } while (false);

            return null;
        }

        /// <summary>
        /// Converts the existing path from any case formatting, to the one on the disk.
        /// </summary>
        public static string RecapitalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            if (File.Exists(path))
                return RecapitalizePath(new FileInfo(path));
            if (Directory.Exists(path))
                return RecapitalizePath(new DirectoryInfo(path));

            // unable to do anything good:
            return path;
        }

        private static string RecapitalizePath(DirectoryInfo info)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            var parentInfo = info.Parent;
            if (parentInfo == null)
                return info.Name;

            return Path.Combine(RecapitalizePath(parentInfo), parentInfo.GetDirectories(info.Name)[0].Name);
        }

        static string RecapitalizePath(FileInfo info)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            DirectoryInfo dirInfo = info.Directory;
            if (dirInfo == null)
                return info.FullName;

            return Path.Combine(RecapitalizePath(dirInfo), dirInfo.GetFiles(info.Name)[0].Name);
        }
    }
}
