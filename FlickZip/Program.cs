using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Archives.SevenZip;
using ICSharpCode.SharpZipLib.BZip2;
using Ionic.Zip;
using LHADecompressor;
using DiscUtils;
using DiscUtils.Iso9660;
using SevenZipExtractor;
using SevenZip;
using LZ4;
using Microsoft.Deployment.Compression.Cab;

namespace FlickZip
{
    class Program
    {
        static void Main(string[] args)
        {
            string zipPath = @"file.lha";
            string extractPath = @"PATH\";
            string format = ".lha";
            if (args.Length > 0)
            {
                zipPath = args[0];
                format = Path.GetExtension(args[0]);
            }

            if (format == ".zip")
            {
                unzip(zipPath, extractPath);
                Console.Out.Write("Success.");
            } else if (format == ".rar")
            {
                unrar(zipPath, extractPath);
                Console.Out.Write("Success.");
            } else if (format == ".7z")
            {
                unsevenzip(zipPath, extractPath);
                Console.Out.Write("Success.");
            } else if (format == ".tar")
            {
                untar(zipPath, extractPath);
                Console.Out.Write("Success.");
            } else if (format == ".gz")
            {
                ungzip(zipPath, extractPath);
                Console.Out.Write("Success.");
            } else if (format == ".bz2")
            {
                unbzip2(zipPath, extractPath);
                Console.Out.Write("Success.");
            } else if (format == ".iso")
            {
                uniso(zipPath, extractPath);
                Console.Out.Write("Success.");
            }
            else if (format == ".cab")
            {
                uncab(zipPath, extractPath);
                Console.Out.Write("Success.");
            }
            else if (format == ".jar")
            {
                unjar(zipPath, extractPath);
                Console.Out.Write("Success.");
            }
            else if (format == ".arj")
            {
                unarj(zipPath, extractPath);
                Console.Out.Write("Success.");
            }
            else
            {
                Console.Out.Write("Error Invalid Format.");
            }
        }

        static void unarj(string filePath, string destinationPath)
        {
            using (ArchiveFile archiveFile = new ArchiveFile(filePath))
            {
                foreach (SevenZipExtractor.Entry entry in archiveFile.Entries)
                {
                    // construct the full path to the extracted file
                    string fullPath = Path.Combine(destinationPath, entry.FileName);

                    // create the directory if it doesn't exist
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                    // extract the file
                    entry.Extract(fullPath);
                }
            }
        }

        static void unjar(string jarPath, string extractPath)
        {
            using (ZipFile zip = ZipFile.Read(jarPath))
            {
                foreach (ZipEntry entry in zip)
                {
                    entry.Extract(extractPath, ExtractExistingFileAction.OverwriteSilently);
                }
            }
        }

        static void uncab(string cabPath, string extractPath)
        {
            CabInfo cab = new CabInfo(cabPath);
            cab.Unpack(extractPath);
        }

        static void uniso(string isoPath, string extractPath)
        {
            void ExtractDirectory(DiscDirectoryInfo Dinfo, string RootPath, string PathinISO)
            {
                if (!string.IsNullOrWhiteSpace(PathinISO))
                {
                    PathinISO += "\\" + Dinfo.Name;
                }
                RootPath += "\\" + Dinfo.Name;
                AppendDirectory(RootPath);
                foreach (DiscDirectoryInfo dinfo in Dinfo.GetDirectories())
                {
                    ExtractDirectory(dinfo, RootPath, PathinISO);
                }
                foreach (DiscFileInfo finfo in Dinfo.GetFiles())
                {
                    using (Stream FileStr = finfo.OpenRead())
                    {
                        using (FileStream Fs = File.Create(RootPath + "\\" + finfo.Name))
                        {
                            FileStr.CopyTo(Fs, 4 * 1024); 
                        }
                    }
                }
            }
            void AppendDirectory(string path)
            {
                try
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                }
                catch (DirectoryNotFoundException Ex)
                {
                    AppendDirectory(Path.GetDirectoryName(path));
                }
                catch (PathTooLongException Exx)
                {
                    AppendDirectory(Path.GetDirectoryName(path));
                }
            }
            using (FileStream ISOStream = File.Open(isoPath, FileMode.Open))
            {
                CDReader Reader = new CDReader(ISOStream, true, true);
                ExtractDirectory(Reader.Root, extractPath, "");
                Reader.Dispose();
            }
        }

        static void unbzip2(string bz2Path, string extractPath)
        {
            using (var fileStream = new FileStream(bz2Path, FileMode.Open))
            {
                using (var bzipStream = new BZip2InputStream(fileStream))
                {
                    using (var reader = new BinaryReader(bzipStream))
                    {
                        var fileBytes = reader.ReadBytes((int)bzipStream.Length);
                        var outputPath = Path.Combine(extractPath, Path.GetFileNameWithoutExtension(bz2Path));
                        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                        File.WriteAllBytes(outputPath, fileBytes);
                    }
                }
            }
        }

        static void ungzip(string gzPath, string extractPath)
        {
            using (var fileStream = new FileStream(gzPath, FileMode.Open))
            {
                using (var archive = ArchiveFactory.Open(fileStream))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.IsDirectory)
                        {
                            continue;
                        }
                        
                        var outputPath = Path.Combine(extractPath, entry.Key);
                        
                        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                        
                        entry.WriteToDirectory(extractPath, new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }
            }
        }

        static void untar(string tarPath, string extractPath)
        {
            using (var archive = ArchiveFactory.Open(tarPath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        entry.WriteToDirectory(extractPath, new ExtractionOptions
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }
            }
        }

        static void unsevenzip(string sevenZipPath, string extractPath)
        {
            using (var archive = SevenZipArchive.Open(sevenZipPath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        entry.WriteToDirectory(extractPath, new ExtractionOptions
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }
            }
        }

        static void unrar(string rarPath, string extractPath)
        {
            using (var archive = ArchiveFactory.Open(rarPath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        entry.WriteToDirectory(extractPath, new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }
            }
        }
        
        static void unzip(string zipPath, string extractPath)
        {
            using (ICSharpCode.SharpZipLib.Zip.ZipInputStream zipStream = new ICSharpCode.SharpZipLib.Zip.ZipInputStream(File.OpenRead(zipPath)))
            {
                ICSharpCode.SharpZipLib.Zip.ZipEntry entry;

                while ((entry = zipStream.GetNextEntry()) != null)
                {
                    string entryPath = Path.Combine(extractPath, entry.Name);

                    if (entry.IsDirectory)
                    {
                        Directory.CreateDirectory(entryPath);
                    }
                    else
                    {
                        using (FileStream fileStream = File.Create(entryPath))
                        {
                            byte[] buffer = new byte[4096];
                            int bytesRead;

                            while ((bytesRead = zipStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                fileStream.Write(buffer, 0, bytesRead);
                            }
                        }
                    }
                }
            }
        }
    }
}
