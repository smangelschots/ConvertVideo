using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Text;
using ConvertMovies.Properties;
using ConvertMovies;

namespace convertMovies
{
    class MainClass
    {
        public static void Main(string[] args)
        {

            string path = Settings.Default.SourcePath;
            string destinationPath = Settings.Default.DestinationPath;

            if (string.IsNullOrEmpty(path))
            {
                Console.Write("Give the source path: ");
                path = Console.ReadLine();
            }

            if (string.IsNullOrEmpty(destinationPath))
            {
                Console.Write("Give the destination path: ");
                Console.Write("Give the destination path:");
                destinationPath = Console.ReadLine();
            }

            string tempPath = Settings.Default.TempPath;

            if (Directory.Exists(tempPath) == false)
                Directory.CreateDirectory(tempPath);

            DirectoryInfo dir = new DirectoryInfo(path);
            var destinationDir = new DirectoryInfo(destinationPath);
            var dirs = dir.GetDirectories();
            foreach (var item in dirs)
            {
                try
                {
                    if (ContainsVideoTs(item))
                    {
                        if (FileExists(destinationDir, item.Name) == false)
                        {

                            bool convert = true; 
                            for (int i = 1; i < 10; i++)
                            {

                                if (convert == false) break;

                                Console.WriteLine(string.Format("--- Start converting {0} -------", item.Name));

                                var result = ConvertMovie(path, tempPath, item.Name,i);
                                if (result == null)
                                    break;

                                string saveFile = string.Format(@"{0}.mp4", Path.Combine(destinationPath, item.Name));

                                if (result.HasErrors == false)
                                {
                                    if (File.Exists(result.File))
                                        File.Move(result.File, saveFile);
                                    if (Settings.Default.LogToFile)
                                    {
                                        File.AppendAllText(string.Format("{0}_Result.txt", result.File), result.Message);
                                    }
                                }
                                else
                                {
                                    if (File.Exists(result.File))
                                    {
                                        FileInfo resultFile = new FileInfo(result.File);
                                        if (resultFile.Length > 314572800)
                                        {
                                            if (File.Exists(result.File))
                                            {
                                                File.Move(result.File, saveFile);
                                                if(Settings.Default.DeleteSource)
                                                {
                                                    item.Delete(true);
                                                }
                                                convert = false;
                                            }
                                               
                                        }
                                        else
                                        {
                                            File.AppendAllText(string.Format("{0}_error.txt", result.File), result.ErrorMessage);
                                        }
                                    }
                                    // File.AppendAllText(string.Format("{0}_Result.txt", result.File), result.Message);
                                }
                            }
                        }
                        else
                        {
                            if (Settings.Default.DeleteSource)
                            {
                                item.Delete(true);
                            }
                            Console.WriteLine(string.Format("**** Skip converting {0} ****", item.Name));
                        }
                    }
                }
                catch (Exception ex)
                {
                    File.AppendAllText(string.Format("{0}_System_error.txt", item.Name), ex.Message + Environment.NewLine + ex.StackTrace);
                }

            }
            Console.ReadLine();
        }

        private static bool ContainsVideoTs(DirectoryInfo item)
        {
            var dir = item.GetDirectories("VIDEO_TS", SearchOption.TopDirectoryOnly);

            if (dir.Length > 0)
                return true;

            return false;
        }

        public static bool FileExists(DirectoryInfo dir, string filename)
        {
            var items = dir.GetFiles();
            foreach (var item in items)
            {
                if (item.Name.ToLower().Contains(filename.ToLower()))
                    return true;

            }

            return false;
        }

        public static Result ConvertMovie(string source, string destination, string folder, int settingIndex)
        {

            string settings = Settings.Default[string.Format("HandbrakeSettings{0}", settingIndex)].ToString();

            if (string.IsNullOrEmpty(settings)) return null;

            string sourcePath = Path.Combine(source, folder);
            string destinationPath = string.Format(@"{0}.mp4", Path.Combine(destination, folder));
            
            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();
            var result = new Result();
            result.File = destinationPath;
            using (Process process = new Process())
            {
                var info = new ProcessStartInfo();
                info.FileName = Settings.Default.HandBrakeLocation;
                info.Arguments = string.Format(settings, sourcePath, destinationPath,Settings.Default.TempPath );

                process.StartInfo = info;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                {
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            outputWaitHandle.Set();

                        }
                        else
                        {
                            output.AppendLine(e.Data);
                            Console.Write("\r{0}", e.Data);
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            errorWaitHandle.Set();
                        }
                        else
                        {
                            error.AppendLine(e.Data);
                            // Console.Write("\r{0}", e.Data);
                            result.HasErrors = true;
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                    result.ErrorMessage = error.ToString();
                    result.Message = output.ToString();
                }

            }
            return result;
        }
    }
}
