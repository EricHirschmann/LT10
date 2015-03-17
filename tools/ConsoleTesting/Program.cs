using System;
using Evel.interfaces;
using Evel.engine;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;

namespace EVEL.ConsoleTesting {
    class Program {

        static IProject project = null;
        static string[] separators = {" ", "\t"};
        static Regex argumentsRegex = new Regex(@"(?<quoted>"")?(?(quoted).+""|\S+)", RegexOptions.Compiled);
       
        static void Main(string[] args) {
            Console.WriteLine("Program for testing NEED capabilities. (31 May 2011)");
            CultureInfo culture = new CultureInfo(CultureInfo.CurrentCulture.LCID);
            culture.NumberFormat.NumberDecimalSeparator = ".";
            Thread.CurrentThread.CurrentCulture = culture;
            AvailableAssemblies.LibraryDir = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
            ReturnAttributeValue rav = null;
            try {
                if (args.Length == 1)
                    project = AvailableAssemblies.getProject(args[0], rav);
            } catch (Exception) {
                Console.WriteLine("Project not found");
            }
            string command = "";
            string[] commandArgs;
            int docId = 0;
            while ((command = GetCommand(out commandArgs)) != "exit") {
                try {
                    switch (command) {
                        case "commands": Console.WriteLine("load\nunload\ntest1\nIntsChange\ndocuments\nprompt\nfsearch\nssearch\nspecdb"); break;
                        case "load": project = AvailableAssemblies.getProject(commandArgs[1], rav); break;
                        case "unload": project = null; break;
                        case "test1": Test.Run<Test1>(commandArgs, project); break;
                        case "IntsChange": Test.Run<IntsChangeTest>(commandArgs, project); break;
                        case "prompt": Test.Run<PromptTest>(commandArgs, project); break;
                        case "fsearch": Test.Run<SearchTest>(commandArgs, project); break;
                        case "ssearch": Test.Run<SeriesSearchTest>(commandArgs, project); break;
                        case "specdb": Test.Run<ParametersDBTest>(commandArgs, project); break;
                        case "documents":
                            if (project != null)
                                foreach (ISpectraContainer container in project.Containers)
                                    Console.WriteLine("\t{0}: {1}\t{2}\n\t\t{3}\n\t\tSpectra: {4}", docId++, container.Name, (container.Enabled) ? "" : "Disabled", container.Model.Name, container.Spectra[0].Name + " (" + (container.Spectra.Count - 1).ToString() + " more)");
                            else
                                Console.WriteLine("No project is loaded!");
                            break;
                        default: Console.WriteLine("Unknown command"); break;
                    }
                } catch (IndexOutOfRangeException) {
                    Console.WriteLine("This command requires arguments!");
                    //System.Diagnostics.Debug.WriteLine(String.Format("{0}\n{1}", e.Message, e.StackTrace));
                } catch (TestException e) {
                    Console.WriteLine("Test has thrown exception:\n{0}\n{1}", e.InnerException.Message, e.InnerException.StackTrace);
                } catch (Exception e) {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private static string GetCommand(out string[] args) {
            Console.Write("\n{0}> ", (project == null) ? "" : project.Caption);

            MatchCollection mc = argumentsRegex.Matches(Console.ReadLine());
            args = new string[mc.Count];
            for (int i = 0; i < mc.Count; i++)
                if (mc[i].Value.Contains("\""))
                    args[i] = Regex.Match(mc[i].Value, @"""(.+)""").Groups[1].Value;
                else
                    args[i] = mc[i].Value;

            //args = Console.ReadLine().Split(separators, StringSplitOptions.RemoveEmptyEntries);
            if (args.Length > 0)
                return args[0];
            else
                return "";
        }

        internal static void PrintSpectrum(ISpectrum spectrum) {
            int g, c, p;
            Console.WriteLine("{0}, chisq = {1:G6}", spectrum.Name, spectrum.Fit);
            for (g = 0; g < spectrum.Parameters.GroupCount; g++) {
                if (spectrum.Parameters[g] != null)
                    if ((spectrum.Parameters[g].Definition.Type & GroupType.Hidden) == 0 && spectrum.Parameters[g].Components.Size > 0) {
                        Console.Write("{0} parameters", spectrum.Parameters[g].Definition.name);
                        if (spectrum.Parameters[g] is ContributedGroup)
                            Console.WriteLine(" contribution={0:G6}", ((ContributedGroup)spectrum.Parameters[g]).contribution.Value);
                        else
                            Console.WriteLine();
                        for (p = 0; p < spectrum.Parameters[g].Components[0].Size; p++)
                            Console.Write("{0,10}", spectrum.Parameters[g].Components[0][p].Definition.Name);
                        Console.WriteLine();
                        for (c = 0; c < spectrum.Parameters[g].Components.Size; c++) {
                            for (p = 0; p < spectrum.Parameters[g].Components[c].Size; p++)
                                Console.Write("{0,10:G6}", spectrum.Parameters[g].Components[c][p].Value);
                            Console.WriteLine();
                        }
                    }
                    Console.WriteLine();
            }
        }
    }
}
