using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Evel.interfaces;

namespace EVEL.ConsoleTesting {

    public struct Flag {
        public string name;
        public object value;
        public Type type;
        public bool required;
        public string description;
        public Flag(string name, Type type, string description, bool required) {
            this.name = name;
            this.type = type;
            this.description = description;
            this.required = required;
            this.value = null;
        }
    }

    public class CommandException : Exception {
        public CommandException(string message) : base(message) { }
    }

    public class TestException : Exception {
        public TestException(string message) : base(message) { }
        public TestException(Exception innerException) : base(innerException.Message, innerException) { }
    }

    public abstract class Test {

        public string Name = String.Empty;
        protected Flag[] templateflags;
        protected string testDescription = String.Empty;

        protected abstract void Init();
        protected abstract void RunTest(Dictionary<string, Flag> flags, IProject project);
        public abstract string TestParametersInfo(Dictionary<string, Flag> flags, IProject project);

        public Test() {
            Init();
        }

        public static void Run<TestType>(string[] args, IProject project) where TestType : Test, new() {
            TestType test = new TestType();
            test.Run(args, project);
        }

        public void Run(string[] args, IProject project) {
            if (args.Length > 1 && args[1] == "/?")
                Console.WriteLine(Help());
            else {
                string command = String.Join(" ", args);
                if (!Regex.IsMatch(command, String.Format(@"^{0}(\s/\w+\s[\w\d]+)*$", Name)))
                    throw new CommandException("Invalid command arguments or flags");
                Dictionary<string, Flag> flags = new Dictionary<string, Flag>();
                Array.Sort(templateflags, FlagComparison);
                Match match;
                for (int i = 0; i < templateflags.Length; i++) {
                    match = Regex.Match(command, String.Format(@"/{0}\s(?<value>\{1}+)", templateflags[i].name, (templateflags[i].type == typeof(int)) ? "d" : "w"));
                    if (templateflags[i].required && !match.Success)
                        throw new CommandException(String.Format("{0} is missing"));
                    else if (match.Success) {
                        Flag f = templateflags[i];
                        if (templateflags[i].type == typeof(int)) {
                            f.value = int.Parse(match.Groups["value"].Value);
                        } else {
                            f.value = match.Groups["value"].Value;
                        }
                        flags.Add(f.name, f);
                    }
                }
                Console.WriteLine(TestParametersInfo(flags, project));
                Console.Write("Continue? [y/n] ");
                if (Console.ReadKey().Key == ConsoleKey.Y) {
                    Console.WriteLine();
                    try {
                        RunTest(flags, project);
                    } catch (Exception exception) {
                        throw new TestException(exception);
                    }
                    Console.WriteLine("Finished!\n");
                } else
                    Console.WriteLine();
            }
        }

        private int FlagComparison(Flag f1, Flag f2) {
            return -f1.required.CompareTo(f2.required);
        }

        protected string Help() {
            StringBuilder builder = new StringBuilder(testDescription);
            
            builder.Append("\n\n");
            builder.Append(Name);
            string f;
            foreach (Flag flag in templateflags) {
                builder.Append(" ");
                f = String.Format("/{0} {1}", flag.name, flag.type == typeof(int) ? "number" : "string");
                builder.Append(flag.required ? String.Format("{0}", f) : String.Format("[{0}]", f));
            }
            builder.Append("\n\n\t/?\tshows this help\n");
            foreach (Flag flag in templateflags) {
                builder.AppendFormat("\t/{0}\t{1}\n", flag.name, flag.description);
            }
            return builder.ToString();
        }
    }
}
