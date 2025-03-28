using System; // Add this line
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using Debug = UnityEngine.Debug;

namespace ParrelSync
{
    public class ClonesManager
    {
        public const string CloneFileName = ".clone";
        public const string CloneNameSuffix = "_clone";
        public const string ProjectName = "ParrelSync";
        public const int MaxCloneProjectCount = 10;
        public const string ArgumentFileName = ".parrelsyncarg";
        public const string DefaultArgument = "client";

        #region Managing clones

        public static Project CreateCloneFromCurrent()
        {
            if (IsClone())
            {
                Debug.LogError("This project is already a clone. Cannot clone it.");
                return null;
            }

            string currentProjectPath = ClonesManager.GetCurrentProjectPath();
            return ClonesManager.CreateCloneFromPath(currentProjectPath);
        }

        public static Project CreateCloneFromPath(string sourceProjectPath)
        {
            if (string.IsNullOrEmpty(sourceProjectPath))
            {
                Debug.LogError("Source project path is null or empty.");
                return null;
            }

            Project sourceProject = new Project(sourceProjectPath);
            string cloneProjectPath = null;

            try
            {
                for (int i = 0; i < MaxCloneProjectCount; i++)
                {
                    string originalProjectPath = ClonesManager.GetCurrentProject().projectPath;
                    string possibleCloneProjectPath = originalProjectPath + ClonesManager.CloneNameSuffix + "_" + i;

                    if (!Directory.Exists(possibleCloneProjectPath))
                    {
                        cloneProjectPath = possibleCloneProjectPath;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(cloneProjectPath))
                {
                    Debug.LogError("The number of cloned projects has reach its limit. Limit: " + MaxCloneProjectCount);
                    return null;
                }

                Project cloneProject = new Project(cloneProjectPath);

                Debug.Log("Start cloning project, original project: " + sourceProject + ", clone project: " + cloneProject);

                ClonesManager.CreateProjectFolder(cloneProject);

                Debug.Log("Library copy: " + cloneProject.libraryPath);
                ClonesManager.CopyDirectoryWithProgressBar(sourceProject.libraryPath, cloneProject.libraryPath,
                    "Cloning Project Library '" + sourceProject.name + "'. ");
                Debug.Log("Packages copy: " + cloneProject.libraryPath);
                ClonesManager.CopyDirectoryWithProgressBar(sourceProject.packagesPath, cloneProject.packagesPath,
                  "Cloning Project Packages '" + sourceProject.name + "'. ");


                ClonesManager.LinkFolders(sourceProject.assetPath, cloneProject.assetPath);
                ClonesManager.LinkFolders(sourceProject.projectSettingsPath, cloneProject.projectSettingsPath);
                ClonesManager.LinkFolders(sourceProject.autoBuildPath, cloneProject.autoBuildPath);
                ClonesManager.LinkFolders(sourceProject.localPackages, cloneProject.localPackages);

                ClonesManager.RegisterClone(cloneProject);

                return cloneProject;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create clone from path: {ex.Message}");
            }

            return null;
        }

        private static void RegisterClone(Project cloneProject)
        {
            string identifierFile = Path.Combine(cloneProject.projectPath, ClonesManager.CloneFileName);
            File.Create(identifierFile).Dispose();

            string argumentFilePath = Path.Combine(cloneProject.projectPath, ClonesManager.ArgumentFileName);
            File.WriteAllText(argumentFilePath, DefaultArgument, System.Text.Encoding.UTF8);

            string collabignoreFile = Path.Combine(cloneProject.projectPath, "collabignore.txt");
            File.WriteAllText(collabignoreFile, "*");
        }

        public static void OpenProject(string projectPath)
        {
            if (!Directory.Exists(projectPath))
            {
                Debug.LogError("Cannot open the project - provided folder (" + projectPath + ") does not exist.");
                return;
            }

            if (projectPath == ClonesManager.GetCurrentProjectPath())
            {
                Debug.LogError("Cannot open the project - it is already open.");
                return;
            }

            ValidateCopiedFoldersIntegrity.ValidateFolder(projectPath, GetOriginalProjectPath(), "Packages");

            string fileName = GetApplicationPath();
            string args = "-projectPath \"" + projectPath + "\"";
            Debug.Log("Opening project \"" + fileName + " " + args + "\"");
            ClonesManager.StartHiddenConsoleProcess(fileName, args);
        }

        private static string GetApplicationPath()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    return EditorApplication.applicationPath;
                case RuntimePlatform.OSXEditor:
                    return EditorApplication.applicationPath + "/Contents/MacOS/Unity";
                case RuntimePlatform.LinuxEditor:
                    return EditorApplication.applicationPath;
                default:
                    throw new System.NotImplementedException("Platform has not supported yet ;(");
            }
        }

        public static bool IsCloneProjectRunning(string projectPath)
        {
            string UnityLockFilePath = new string[] { projectPath, "Temp", "UnityLockfile" }
                .Aggregate(Path.Combine);

            switch (Application.platform)
            {
                case (RuntimePlatform.WindowsEditor):
                    if (Preferences.AlsoCheckUnityLockFileStaPref.Value)
                        return File.Exists(UnityLockFilePath) && FileUtilities.IsFileLocked(UnityLockFilePath);
                    else
                        return File.Exists(UnityLockFilePath);
                case (RuntimePlatform.OSXEditor):
                    return File.Exists(UnityLockFilePath);
                case (RuntimePlatform.LinuxEditor):
                    return File.Exists(UnityLockFilePath);
                default:
                    throw new System.NotImplementedException("IsCloneProjectRunning: Unsupport Platfrom: " + Application.platform);
            }
        }

        public static void DeleteClone(string cloneProjectPath)
        {
            if (ClonesManager.IsClone()) return;

            if (cloneProjectPath == string.Empty) return;
            if (cloneProjectPath == ClonesManager.GetOriginalProjectPath()) return;

            string identifierFile;
            string args;
            switch (Application.platform)
            {
                case (RuntimePlatform.WindowsEditor):
                    Debug.Log("Attempting to delete folder \"" + cloneProjectPath + "\"");

                    identifierFile = Path.Combine(cloneProjectPath, ClonesManager.ArgumentFileName);
                    File.Delete(identifierFile);

                    args = "/c " + @"rmdir /s/q " + string.Format("\"{0}\"", cloneProjectPath);
                    StartHiddenConsoleProcess("cmd.exe", args);

                    break;
                case (RuntimePlatform.OSXEditor):
                    Debug.Log("Attempting to delete folder \"" + cloneProjectPath + "\"");

                    identifierFile = Path.Combine(cloneProjectPath, ClonesManager.ArgumentFileName);
                    File.Delete(identifierFile);

                    FileUtil.DeleteFileOrDirectory(cloneProjectPath);

                    break;
                case (RuntimePlatform.LinuxEditor):
                    Debug.Log("Attempting to delete folder \"" + cloneProjectPath + "\"");
                    identifierFile = Path.Combine(cloneProjectPath, ClonesManager.ArgumentFileName);
                    File.Delete(identifierFile);

                    FileUtil.DeleteFileOrDirectory(cloneProjectPath);

                    break;
                default:
                    Debug.LogWarning("Not in a known editor. Where are you!?");
                    break;
            }
        }

        #endregion

        #region Creating project folders

        public static void CreateProjectFolder(Project project)
        {
            string path = project.projectPath;
            Debug.Log("Creating new empty folder at: " + path);
            Directory.CreateDirectory(path);
        }

        [System.Obsolete]
        public static void CopyLibraryFolder(Project sourceProject, Project destinationProject)
        {
            if (Directory.Exists(destinationProject.libraryPath))
            {
                Debug.LogWarning("Library copy: destination path already exists! ");
                return;
            }

            Debug.Log("Library copy: " + destinationProject.libraryPath);
            ClonesManager.CopyDirectoryWithProgressBar(sourceProject.libraryPath, destinationProject.libraryPath,
                "Cloning project '" + sourceProject.name + "'. ");
        }

        #endregion

        #region Creating symlinks

        private static void CreateLinkMac(string sourcePath, string destinationPath)
        {
            sourcePath = sourcePath.Replace(" ", "\\ ");
            destinationPath = destinationPath.Replace(" ", "\\ ");
            var command = string.Format("ln -s {0} {1}", sourcePath, destinationPath);

            Debug.Log("Mac hard link " + command);

            ClonesManager.ExecuteBashCommand(command);
        }

        private static void CreateLinkLinux(string sourcePath, string destinationPath)
        {
            sourcePath = sourcePath.Replace(" ", "\\ ");
            destinationPath = destinationPath.Replace(" ", "\\ ");
            var command = string.Format("ln -s {0} {1}", sourcePath, destinationPath);           

            Debug.Log("Linux Symlink " + command);

            ClonesManager.ExecuteBashCommand(command);
        }

        private static void CreateLinkWin(string sourcePath, string destinationPath)
        {
            string cmd = "/C mklink /J " + string.Format("\"{0}\" \"{1}\"", destinationPath, sourcePath);
            Debug.Log("Windows junction: " + cmd);
            ClonesManager.StartHiddenConsoleProcess("cmd.exe", cmd);
        }

        public static void LinkFolders(string sourcePath, string destinationPath)
        {
            if ((Directory.Exists(destinationPath) == false) && (Directory.Exists(sourcePath) == true))
            {
                switch (Application.platform)
                {
                    case (RuntimePlatform.WindowsEditor):
                        CreateLinkWin(sourcePath, destinationPath);
                        break;
                    case (RuntimePlatform.OSXEditor):
                        CreateLinkMac(sourcePath, destinationPath);
                        break;
                    case (RuntimePlatform.LinuxEditor):
                        CreateLinkLinux(sourcePath, destinationPath);
                        break;
                    default:
                        Debug.LogWarning("Not in a known editor. Application.platform: " + Application.platform);
                        break;
                }
            }
            else
            {
                Debug.LogWarning("Skipping Asset link, it already exists: " + destinationPath);
            }
        }

        #endregion

        #region Utility methods

        private static bool? isCloneFileExistCache = null;

        public static bool IsClone()
        {
            if (isCloneFileExistCache == null)
            {
                string cloneFilePath = Path.Combine(ClonesManager.GetCurrentProjectPath(), ClonesManager.CloneFileName);
                isCloneFileExistCache = File.Exists(cloneFilePath);
            }

            return (bool)isCloneFileExistCache;
        }

        public static string GetCurrentProjectPath()
        {
            return Application.dataPath.Replace("/Assets", "");
        }

        public static Project GetCurrentProject()
        {
            string pathString = ClonesManager.GetCurrentProjectPath();
            return new Project(pathString);
        }

        public static string GetArgument()
        {
            string argument = "";
            if (IsClone())
            {
                string argumentFilePath = Path.Combine(GetCurrentProjectPath(), ClonesManager.ArgumentFileName);
                if (File.Exists(argumentFilePath))
                {
                    argument = File.ReadAllText(argumentFilePath, System.Text.Encoding.UTF8);
                }
            }

            return argument;
        }

        public static string GetOriginalProjectPath()
        {
            if (IsClone())
            {
                string cloneProjectPath = ClonesManager.GetCurrentProject().projectPath;

                int index = cloneProjectPath.LastIndexOf(ClonesManager.CloneNameSuffix);
                if (index > 0)
                {
                    string originalProjectPath = cloneProjectPath.Substring(0, index);
                    if (Directory.Exists(originalProjectPath)) return originalProjectPath;
                }

                return string.Empty;
            }
            else
            {
                return ClonesManager.GetCurrentProjectPath();
            }
        }

        public static List<string> GetCloneProjectsPath()
        {
            List<string> projectsPath = new List<string>();
            for (int i = 0; i < MaxCloneProjectCount; i++)
            {
                string originalProjectPath = ClonesManager.GetCurrentProject().projectPath;
                string cloneProjectPath = originalProjectPath + ClonesManager.CloneNameSuffix + "_" + i;

                if (Directory.Exists(cloneProjectPath))
                    projectsPath.Add(cloneProjectPath);
            }

            return projectsPath;
        }

        public static void CopyDirectoryWithProgressBar(string sourcePath, string destinationPath,
            string progressBarPrefix = "")
        {
            var source = new DirectoryInfo(sourcePath);
            var destination = new DirectoryInfo(destinationPath);

            long totalBytes = 0;
            long copiedBytes = 0;

            ClonesManager.CopyDirectoryWithProgressBarRecursive(source, destination, ref totalBytes, ref copiedBytes,
                progressBarPrefix);
            EditorUtility.ClearProgressBar();
        }

        private static void CopyDirectoryWithProgressBarRecursive(DirectoryInfo source, DirectoryInfo destination,
            ref long totalBytes, ref long copiedBytes, string progressBarPrefix = "")
        {
            if (source.FullName.ToLower() == destination.FullName.ToLower())
            {
                Debug.LogError("Cannot copy directory into itself.");
                return;
            }

            if (totalBytes == 0)
            {
                totalBytes = ClonesManager.GetDirectorySize(source, true, progressBarPrefix);
            }

            if (!Directory.Exists(destination.FullName))
            {
                Directory.CreateDirectory(destination.FullName);
            }

            foreach (FileInfo file in source.GetFiles())
            {
                try
                {
                    file.CopyTo(Path.Combine(destination.ToString(), file.Name), true);
                }
                catch (IOException)
                {
                }

                copiedBytes += file.Length;

                float progress = (float)copiedBytes / (float)totalBytes;
                bool cancelCopy = EditorUtility.DisplayCancelableProgressBar(
                    progressBarPrefix + "Copying '" + source.FullName + "' to '" + destination.FullName + "'...",
                    "(" + (progress * 100f).ToString("F2") + "%) Copying file '" + file.Name + "'...",
                    progress);
                if (cancelCopy) return;
            }

            foreach (DirectoryInfo sourceNestedDir in source.GetDirectories())
            {
                DirectoryInfo nextDestingationNestedDir = destination.CreateSubdirectory(sourceNestedDir.Name);
                ClonesManager.CopyDirectoryWithProgressBarRecursive(sourceNestedDir, nextDestingationNestedDir,
                    ref totalBytes, ref copiedBytes, progressBarPrefix);
            }
        }

        private static long GetDirectorySize(DirectoryInfo directory, bool includeNested = false,
            string progressBarPrefix = "")
        {
            EditorUtility.DisplayProgressBar(progressBarPrefix + "Calculating size of directories...",
                "Scanning '" + directory.FullName + "'...", 0f);

            long filesSize = directory.GetFiles().Sum((FileInfo file) => file.Length);

            long directoriesSize = 0;
            if (includeNested)
            {
                IEnumerable<DirectoryInfo> nestedDirectories = directory.GetDirectories();
                foreach (DirectoryInfo nestedDir in nestedDirectories)
                {
                    directoriesSize += ClonesManager.GetDirectorySize(nestedDir, true, progressBarPrefix);
                }
            }

            return filesSize + directoriesSize;
        }

        private static void StartHiddenConsoleProcess(string fileName, string args)
        {
            System.Diagnostics.Process.Start(fileName, args);
        }

        private static void ExecuteBashCommand(string command)
        {
            command = command.Replace("\"", "\"\"");

            var proc = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "-c \"" + command + "\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            using (proc)
            {
                proc.Start();
                proc.WaitForExit();

                if (!proc.StandardError.EndOfStream)
                {
                    UnityEngine.Debug.LogError(proc.StandardError.ReadToEnd());
                }
            }
        }

        public static void OpenProjectInFileExplorer(string path)
        {
            System.Diagnostics.Process.Start(@path);
        }
        #endregion
    }
}
