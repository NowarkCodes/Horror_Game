using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

namespace ParrelSync
{
    public class Project : System.ICloneable
    {
        public string name;
        public string projectPath;
        string rootPath;
        public string assetPath;
        public string projectSettingsPath;
        public string libraryPath;
        public string packagesPath;
        public string autoBuildPath;
        public string localPackages;

        char[] separator = new char[1] { '/' };


        /// <summary>
        /// Default constructor
        /// </summary>
        public Project()
        {

        }


        /// <summary>
        /// Initialize the project object by parsing its full path returned by Unity into a bunch of individual folder names and paths.
        /// </summary>
        /// <param name="path"></param>
        public Project(string path)
        {
            ParsePath(path);
        }


        /// <summary>
        /// Create a new object with the same settings
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            Project newProject = new Project();
            newProject.rootPath = rootPath;
            newProject.projectPath = projectPath;
            newProject.assetPath = assetPath;
            newProject.projectSettingsPath = projectSettingsPath;
            newProject.libraryPath = libraryPath;
            newProject.name = name;
            newProject.separator = separator;
            newProject.packagesPath = packagesPath;
            newProject.autoBuildPath = autoBuildPath;
            newProject.localPackages = localPackages;


            return newProject;
        }


        /// <summary>
        /// Update the project object by renaming and reparsing it. Pass in the new name of a project, and it'll update the other member variables to match.
        /// </summary>
        /// <param name="name"></param>
        public void updateNewName(string newName)
        {
            name = newName;
            ParsePath(rootPath + "/" + name + "/Assets");
        }


        /// <summary>
        /// Debug override so we can quickly print out the project info.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string printString = name + "\n" +
                                 rootPath + "\n" +
                                 projectPath + "\n" +
                                 assetPath + "\n" +
                                 projectSettingsPath + "\n" +
                                 packagesPath + "\n" +
                                 autoBuildPath + "\n" +
                                 localPackages + "\n" +
                                 libraryPath;
            return (printString);
        }

        private void ParsePath(string path) {
            if (string.IsNullOrEmpty(path)) {
                Debug.LogError("Project path is null or empty.");
                return;
            }

            projectPath = path;
            List<string> pathArray = projectPath.Split(separator).ToList();

            if (pathArray.Count == 0) {
                Debug.LogError("Failed to parse project path.");
                return;
            }

            name = pathArray.Last();
            pathArray.RemoveAt(pathArray.Count - 1);
            rootPath = string.Join(separator[0].ToString(), pathArray);

            assetPath = Path.Combine(projectPath, "Assets");
            projectSettingsPath = Path.Combine(projectPath, "ProjectSettings");
            libraryPath = Path.Combine(projectPath, "Library");
            packagesPath = Path.Combine(projectPath, "Packages");
            autoBuildPath = Path.Combine(projectPath, "AutoBuild");
            localPackages = Path.Combine(projectPath, "LocalPackages");
        }
    }
}