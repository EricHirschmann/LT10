using System;
using System.Collections.Generic;
using Evel.interfaces;
using Evel.share.plugins;
using System.IO;
using System.Reflection;
using System.Xml;
using Microsoft.Win32;
using System.IO.Compression;

namespace Evel.engine {

    public delegate void ReturnAttributeValue(string name, string value);

    public struct ModelDescription {
        public string name;
        public string description;
        public GroupDefinition[] groupDefinitions;
        public AvailablePlugin plugin;
        public Type projectType;
        //public ProjectDescription projectDescription;
    }

    public struct ProjectDescription {
        public string name;
        public string description;
        public string experimentalMethodName;
        public AvailablePlugin plugin;
        public Type projectType;
    }

    /// <summary>
    /// Singleton class containing available models. 
    /// To get a model, use indexer with name of dll file passed
    /// ex: IModel model = AvailableModels.getAvailableModels()["mexm"];
    /// </summary>
    public class AvailableAssemblies {
        private List<ModelDescription> _availableModels;
        private List<ProjectDescription> _availableProjects;
        private static char[] classSeparators = new char[] { '.' };

        private static AvailableAssemblies _instance = null;

        public static string LibraryDir; // = "D:\\Devel\\.NET\\cs\\EVEL\\Release\\bin\\lib\\";

        private AvailableAssemblies() { }

        //public static string LibraryDir = "D:\\shared_devel\\EVEL\\Release\\bin\\lib";
        //public static string LibraryDir = "D:\\Devel\\.NET\\cs\\EVEL\\Release\\bin\\lib\\";

        protected AvailableAssemblies(string path) {
            //LibraryDir = Path.Combine(Environment.CurrentDirectory, "lib");
        //const string userRoot = "HKEY_CURRENT_USER";
        //const string subkey = "Evel";
        //const string keyName = userRoot + "\\" + subkey;

        //Microsoft.Win32.Registry.SetValue(keyName, "libpath", "E:\\dotnet\\projects\\EVEL\\Release\\bin\\lib");
            //models
            this._availableModels = new List<ModelDescription>();
            AvailablePlugin[] plugs = PluginServices.getPlugins(path, "IModel", null);
            foreach (AvailablePlugin plugin in plugs) {
                //using (
                    IModel model = (IModel)PluginServices.getPluginInstance(plugin);//) {
                    ModelDescription md = new ModelDescription();
                    md.name = model.Name;
                    md.description = model.Description;
                    md.groupDefinitions = model.GroupsDefinition;
                    md.projectType = model.ProjectType;
                    md.plugin = plugin;
                    this._availableModels.Add(md);
                //}
            }
            //projects
            

            this._availableProjects = new List<ProjectDescription>();
            Type projectBaseType = typeof(ProjectBase);
            ConstructorInfo[] constructors = projectBaseType.GetConstructors();
            plugs = PluginServices.getPlugins(path, "IProject", constructors);
            foreach (AvailablePlugin plugin in plugs) {
                IProject project = (IProject)PluginServices.getPluginInstance(plugin,new object[] { });
                ProjectDescription pd = new ProjectDescription();
                pd.name = project.Name;
                pd.description = project.Description;
                pd.experimentalMethodName = project.ExperimentalMethodName;
                pd.plugin = plugin;
                pd.projectType = project.GetType();
                this._availableProjects.Add(pd);
            }
        }

        /// <summary>
        /// Returns instance of the model.
        /// </summary>
        /// <param name="modelName">Name of dll file containing model class</param>
        /// <returns>Model instance</returns>
        public static IModel getModel(string modelClassName) {
            if (_instance == null)
                _instance = new AvailableAssemblies(LibraryDir);
            for (int i = 0; i < _instance._availableModels.Count; i++) {
                //if (String.Equals(modelName, Path.GetFileNameWithoutExtension(_instance._availableModels[i].plugin.assemblyPath),
                string[] pluginClassNameSubs = _instance._availableModels[i].plugin.className.Split(classSeparators);
                string[] modelClassNameSubs = modelClassName.Split(classSeparators);
                string pluginClassName = pluginClassNameSubs[pluginClassNameSubs.Length - 1];
                modelClassName = modelClassNameSubs[modelClassNameSubs.Length - 1];
                if (String.Equals(modelClassName, pluginClassName,
                    StringComparison.CurrentCultureIgnoreCase)) {
                    return (IModel)PluginServices.getPluginInstance(_instance._availableModels[i].plugin);
                }
            }
            throw new SpectrumLoadException(String.Format("Couldn't find model \"{0}\"!", modelClassName));
        }

        public static IProject getProject(string projectClassName, object[] args) {
            if (_instance == null)
                _instance = new AvailableAssemblies(LibraryDir);
            for (int i = 0; i < _instance._availableProjects.Count; i++) {
                //if (String.Equals(modelName, Path.GetFileNameWithoutExtension(_instance._availableModels[i].plugin.assemblyPath),
                string[] pluginClassNameSubs = _instance._availableProjects[i].plugin.className.Split(classSeparators);
                string[] projectClassNameSubs = projectClassName.Split(classSeparators);
                string pluginClassName = pluginClassNameSubs[pluginClassNameSubs.Length - 1];
                projectClassName = projectClassNameSubs[projectClassNameSubs.Length - 1];
                if (String.Equals(projectClassName, pluginClassName,
                    StringComparison.CurrentCultureIgnoreCase)) {
                    return (IProject)PluginServices.getPluginInstance(_instance._availableProjects[i].plugin, args);
                }
            }
            throw new SpectrumLoadException(String.Format("Couldn't find project \"{0}\"!", projectClassName));
        }

        public static IProject getProject(string fileName, ReturnAttributeValue gav) {
            if (_instance == null)
                _instance = new AvailableAssemblies(LibraryDir);
            //XmlReader reader;
            string projectClassName = String.Empty;
            Stream stream = null;
            try {
                XmlReader reader = ProjectBase.getXmlReader(fileName, out stream);
                reader.ReadToFollowing("project");
                if (reader.MoveToFirstAttribute()) {
                    do {
                        switch (reader.Name) {
                            case "class":
                                projectClassName = reader.Value;
                                break;
                            //case "spectraCount": break;
                        }
                        if (gav != null)
                            gav(reader.Name, reader.Value);
                    } while (reader.MoveToNextAttribute());
                }
                
                if (projectClassName == String.Empty)
                    throw new SpectrumLoadException("This is not valid LT10 project");
            } catch (Exception) {
                throw new SpectrumLoadException("Project file is damaged, it is not a valid LT10 project file or file is in use by another process.");
            } finally {
                if (stream != null)
                    stream.Close();
            }
            object[] args = new object[] { fileName };
            for (int i = 0; i < _instance._availableProjects.Count; i++) {
                //if (String.Equals(modelName, Path.GetFileNameWithoutExtension(_instance._availableModels[i].plugin.assemblyPath),
                string[] pluginClassNameSubs = _instance._availableProjects[i].plugin.className.Split(classSeparators);
                string[] projectClassNameSubs = projectClassName.Split(classSeparators);
                string pluginClassName = pluginClassNameSubs[pluginClassNameSubs.Length - 1];
                projectClassName = projectClassNameSubs[projectClassNameSubs.Length - 1];
                if (String.Equals(projectClassName, pluginClassName,
                    StringComparison.CurrentCultureIgnoreCase)) {
                    return (IProject)PluginServices.getPluginInstance(_instance._availableProjects[i].plugin, args);
                }
            }
            throw new SpectrumLoadException(String.Format("Couldn't find project \"{0}\"!", projectClassName));
        }

        public static List<ModelDescription> AvailableModels {
            get {
                if (_instance == null)
                    _instance = new AvailableAssemblies(LibraryDir);
                return _instance._availableModels; 
            }
        }

        public static ProjectDescription GetProjectDesription(Type projectType) {
            if (_instance == null)
                _instance = new AvailableAssemblies(LibraryDir);
            foreach (ProjectDescription pd in _instance._availableProjects) {
                if (pd.projectType == projectType)
                    return pd;
            }
            throw new SpectrumLoadException("Project not found");
        }
    }
}
