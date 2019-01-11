using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace Popcron.Builder
{
    public class Archiver
    {
        public static void Zip(string input, string output, string platform)
        {
            FileStream fsOut = File.Create(output);
            ZipOutputStream zipStream = new ZipOutputStream(fsOut);

            zipStream.SetLevel(6); //0-9, 9 being the highest level of compression

            // This setting will strip the leading part of the folder path in the entries, to
            // make the entries relative to the starting folder.
            // To include the full path for each entry up to the drive root, assign folderOffset = 0.
            int length = Path.GetFileNameWithoutExtension(input).Length + 1;
            int folderOffset = input.Length + (input.EndsWith("/") ? 0 : 1) - length;
            if (platform == "webgl")
            {
                folderOffset = input.Length;
            }

            CompressFolder(input, zipStream, folderOffset, platform);

            zipStream.IsStreamOwner = true; // Makes the Close also Close the underlying stream
            zipStream.Close();
        }
        
        private static void CompressFolder(string path, ZipOutputStream zipStream, int folderOffset, string platform)
        {
            string[] files = Directory.GetFiles(path);
            foreach (string filename in files)
            {
                if (Settings.File.BlacklistedFiles.Contains(Path.GetFileName(filename)))
                {
                    //dont process this file
                    continue;
                }

                FileInfo fi = new FileInfo(filename);

                string entryName = filename.Substring(folderOffset); // Makes the name in zip based on the folder
                entryName = ZipEntry.CleanName(entryName.Replace(platform, Settings.File.ExecutableName)); // Removes drive from name and fixes slash direction
                ZipEntry newEntry = new ZipEntry(entryName)
                {
                    DateTime = fi.LastWriteTime, // Note the zip format stores 2 second granularity
                    Size = fi.Length
                };

                zipStream.PutNextEntry(newEntry);

                // Zip the file in buffered chunks
                // the "using" will close the stream even if an exception occurs
                byte[] buffer = new byte[4096];
                using (FileStream streamReader = File.OpenRead(filename))
                {
                    StreamUtils.Copy(streamReader, zipStream, buffer);
                }
                zipStream.CloseEntry();
            }

            string[] directories = Directory.GetDirectories(path);
            foreach (var directory in directories)
            {
                if (Settings.File.BlacklistedDirectories.Contains(Path.GetFileNameWithoutExtension(directory)))
                {
                    //dont process this folder
                    continue;
                }

                CompressFolder(directory, zipStream, folderOffset, platform);
            }
        }
    }
}
