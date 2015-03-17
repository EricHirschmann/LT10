using System;
using System.Collections.Generic;
using Evel.share.plugins;
using Evel.engine;
using Evel.gui.interfaces;
using Evel.interfaces;
using System.Reflection;
using System.Windows.Forms;

namespace Evel.gui {

    public struct GUIDescription {
        public Type projectType;
        public AvailablePlugin plugin;
    }

    public class AvailableGUIAssemblies {

        private static AvailableGUIAssemblies _instance;

        private List<GUIDescription> _availableGUIs;

        private AvailableGUIAssemblies() {
            this._availableGUIs = new List<GUIDescription>();
            Type defaultGroupGUIType = typeof(DefaultGroupGUI);
            ConstructorInfo[] constructors = defaultGroupGUIType.GetConstructors();
            AvailablePlugin[] plugs = PluginServices.getPlugins(AvailableAssemblies.LibraryDir, "IGroupGUI", constructors);
            if (plugs != null) {
                foreach (AvailablePlugin plugin in plugs) {
                    IGroupGUI gui = (IGroupGUI)PluginServices.getPluginInstance(plugin);//) {
                    GUIDescription gd = new GUIDescription();
                    gd.plugin = plugin;
                    gd.projectType = gui.ProjectType;
                    this._availableGUIs.Add(gd);
                }
            }
        }

        public static AvailableGUIAssemblies GetAvailableGUIAssembliesInstance() {
            if (_instance == null)
                _instance = new AvailableGUIAssemblies();
            return _instance;
        }

        public static IGroupGUI GetGroupGUI(Type projectType, DataGridView grid, List<ISpectrum> spectra, GroupDefinition groupDefinition, GroupTabPage groupTabPage) {

            //find plugin with accurate project type
            AvailableGUIAssemblies assemblies = GetAvailableGUIAssembliesInstance();
            foreach (GUIDescription gd in assemblies._availableGUIs) {
                if (gd.projectType == projectType)
                    return (IGroupGUI)PluginServices.getPluginInstance(gd.plugin, new object[] { grid, spectra, groupDefinition, groupTabPage });
            }

            //if there is no gui for this projectType use default gui
            return new DefaultGroupGUI(grid, spectra, groupDefinition, groupTabPage);
        }

    }
}
