using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Reflection;
using System.IO;

namespace Evel.share.plugins {

    public struct AvailablePlugin {
        public string assemblyPath;
        public string className;
    }

    public class PluginServices {

        public static AvailablePlugin[] getPlugins(string path, string interfaceName, ConstructorInfo[] constructors) {
            ArrayList plugins = new ArrayList();
            Assembly objDll;
            string[] dlls = Directory.GetFiles(path, "*.dll");
            try {
                foreach (string dllName in dlls) {
                    objDll = Assembly.LoadFrom(dllName);
                    ExamineAssembly(objDll, interfaceName, plugins, constructors);
                }
            } catch (Exception e) {
                Console.WriteLine(e.Message);
            }
            AvailablePlugin[] result = null;
            if (plugins.Count > 0) {
                result = new AvailablePlugin[plugins.Count];
                plugins.CopyTo(result);
            }
            return result;
        }

        private static Type[] getParameterTypes(ParameterInfo[] parameters) {
            Type[] result = new Type[parameters.Length];
            for (int p = 0; p < parameters.Length; p++) {
                result[p] = parameters[p].ParameterType;
            }
            return result;
        }

        private static void ExamineAssembly(Assembly objDll, string interfaceName, ArrayList plugins, ConstructorInfo[] constructors) {
            foreach (Type type in objDll.GetTypes()) {
                if (type.IsPublic) {
                    if ((type.Attributes & TypeAttributes.Abstract) != TypeAttributes.Abstract) {
                        Type objInterface = type.GetInterface(interfaceName, true);
                        bool constructorsMatch = true;
                        if (constructors != null) {
                            int i = 0;
                            while (constructorsMatch && (i < constructors.Length)) {
                                constructorsMatch = type.GetConstructor(getParameterTypes(constructors[i].GetParameters())) != null;
                                i++;
                            }
                        }
                        if (objInterface != null && constructorsMatch) {
                            AvailablePlugin plugin = new AvailablePlugin();
                            plugin.className = type.FullName;
                            plugin.assemblyPath = objDll.Location;
                            plugins.Add(plugin);
                        }
                    }
                }
            }
        }

        public static object getPluginInstance(AvailablePlugin plugin) {
            //try {
                Assembly dll = Assembly.LoadFrom(plugin.assemblyPath);
                object obj = dll.CreateInstance(plugin.className);
                return obj;
            //} catch (Exception e) {
                
                //throw new Exception(String.Format("Couldn't initialize {0} model. Possible reasons:"+
                //    "\n\t- Errors while parsing xml file containing model data," +
                //    "\n\t- Errors in model implementation," +
                //    "\n\t- Missing deltax definition file ({1})," +
                //    "\n\t- Deltax definition file is not a valid xml document.",
                //    Path.GetFileName(plugin.assemblyPath), Path.ChangeExtension(Path.GetFileName(plugin.assemblyPath), "xml")));
            //}
        }

        public static object getPluginInstance(AvailablePlugin plugin, object[] args) {
            //try {
                Assembly dll = Assembly.LoadFrom(plugin.assemblyPath);
                object obj = dll.CreateInstance(plugin.className, true, BindingFlags.CreateInstance, null, args, null, null);
                return obj;
            //} catch {
            //    throw new Exception(String.Format("Couldn't initialize {0} project. Possible reasons:" +
            //        "\n\t- Errors while parsing xml file containing project data," +
            //        "\n\t- Errors in project implementation," +
            //        "\n\t- Project doesn't implement Evel.engine.ProjectBase class constructors",
            //        Path.GetFileName(plugin.assemblyPath)));
            //}
        }

    }
}
