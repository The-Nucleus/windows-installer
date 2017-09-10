using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NucleiInstaller
{
    class Program
    {
        static int ReadArchiveLength(string exePath)
        {
            byte[] buffer = new byte[4];
            using (var stream = new FileStream(exePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                stream.Seek(stream.Length - 4, SeekOrigin.Current);
                stream.Read(buffer, 0, 4);
                stream.Close();
            }
            return BitConverter.ToInt32(buffer, 0);
        }

        static void CreateInstaller(string currentPath, string zipName)
        {
            string archivePath = Path.Combine(Directory.GetCurrentDirectory(), zipName);
            string newPath = Path.Combine(Directory.GetCurrentDirectory(), "TmpInstaller.exe");

            if (File.Exists(newPath))
            {
                File.Delete(newPath);
            }
            File.Copy(currentPath, newPath);
            byte[] archiveBytes = File.ReadAllBytes(archivePath);
            using (var stream = new FileStream(newPath, FileMode.Append))
            {
                stream.Write(archiveBytes, 0, archiveBytes.Length);
                byte[] archiveLengthDescriptor = BitConverter.GetBytes(archiveBytes.Length);
                stream.Write(archiveLengthDescriptor, 0, 4);
                stream.Close();
            }
        }

        static void RunInstaller(string currentPath, int archiveLength)
        {
            string tempExtract = Path.GetTempFileName();
            using (var stream = new FileStream(currentPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var writeStream = new FileStream(tempExtract, FileMode.OpenOrCreate, FileAccess.Write)) {
                    stream.Seek(stream.Length - archiveLength - 4, SeekOrigin.Current);
                    stream.CopyTo(writeStream);
                    stream.Close();
                }
            }

            string tempDir = Path.GetTempFileName() + "dir";
            Directory.CreateDirectory(tempDir);

            using (Stream stream = File.OpenRead(tempExtract))
            using (var reader = ReaderFactory.Open(stream))
            {
                reader.WriteAllToDirectory(tempDir);
            }

            File.Delete(tempExtract);
            string metaPath = Path.Combine(tempDir, ".nucleus");
            if (!File.Exists(metaPath))
            {
                // Something went terribly wrong
                // TODO: Handle this
                return;
            }
            string[] lines = File.ReadAllLines(metaPath);
            if (lines.Length != 3)
            {
                // Something has gone horriby wrong
                // TODO: Handle this
                return;
            }
            string appName = lines[0];
            string appVersion = lines[1];
            string exeName = lines[2];
            File.Delete(metaPath);

            string appFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appName);
            if (!File.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            string versionFolder = Path.Combine(appFolder, appVersion);
            if (Directory.Exists(versionFolder))
            {
                // This might fail if the app is already open
                // TODO: Handle this
                Directory.Delete(versionFolder, true);
            }
            Directory.Move(tempDir, versionFolder);

            string newExe = Path.Combine(versionFolder, exeName);
            if (File.Exists(newExe))
            {
                System.Diagnostics.Process.Start(newExe);
            }
        }

        static void Main(string[] args)
        {
            string currentPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            int archiveLength = ReadArchiveLength(currentPath);

            Console.WriteLine(archiveLength);

            if (archiveLength == 0)
            {
                if (args.Length != 1)
                {
                    Console.WriteLine("You must provide a path to the archive to make the installer for");
                    return;
                }
                CreateInstaller(currentPath, args[0]);
            } else
            {
                RunInstaller(currentPath, archiveLength);
            }
        }
    }
}
