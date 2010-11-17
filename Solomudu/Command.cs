using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Solomudu
{
    delegate void CommandDelegate(Brain brain, string[] args);
    class Command
    {
        public String C { get; set; }
        public CommandDelegate D { get; set; }
        // todo: access restrictions?


        static Dictionary<String, Command> CommandTable = new Dictionary<string, Command>();
        public static void InitializeCommands()
        {
            foreach (var type in Assembly.GetCallingAssembly().GetTypes())
            {
                foreach (var mi in type.GetMethods())
                {
                    foreach (var att in mi.GetCustomAttributes(false).OfType<CommandAttribute>())
                    {
                        if (!mi.IsStatic) continue;
                        CommandTable.Add(
                            att.CommandName,
                            new Command
                            {
                                C = att.CommandName,
                                D = (CommandDelegate)(Delegate.CreateDelegate(typeof(CommandDelegate), mi))
                            }
                            );
                    }
                }
            }
        }

        public static CommandDelegate BestMatch(string cmd)
        {
            return (from p in CommandTable
                    where p.Key.StartsWith(cmd)
                    orderby p.Key ascending
                    select p.Value.D).FirstOrDefault();
        }
    }

    [AttributeUsage(AttributeTargets.Method,
        AllowMultiple = true,
        Inherited = false)]
    class CommandAttribute : Attribute
    {
        public string CommandName { get; set; }

        public CommandAttribute(string name)
        {
            CommandName = name;
        }

    }
}
