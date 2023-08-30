using System.Reflection;
using System.Text.RegularExpressions;

namespace sukalambda
{
    public abstract class Command : Attribute
    {
        public string name { get; init; }
        public Regex? regex { get; init; }
        public string help { get; init; }
        public Command(string name, string? regex, string help)
        {
            this.name=name;
            if (regex == null) this.regex = null;
            else this.regex=new(regex);
            this.help=help;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class InGameCommand : Command
    {
        public InGameCommand(string name, string? regex, string help) : base(name, regex, help) { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class OutGameCommand : Command
    {
        public OutGameCommand(string name, string? regex, string help) : base(name, regex, help) { }
    }
    public class CommandRouter
    {
        public readonly
            Dictionary<string,  // account
            Dictionary<Character, 
            Dictionary<string,  // command name
                Tuple<InGameCommand,
                    Func<string,  // command body
                        SukaLambdaEngine, bool>>>>> accountToInGameMethod = new();

        public readonly
            Dictionary<string,  // command name
                Tuple<OutGameCommand,
                    Func<string,  // account
                        string,   // command body
                        RootController, bool>>> outGameMethod = new();

        public CommandRouter()
        {
            RegisterOutGameCommand();
        }

        public void RegisterOutGameCommand()
        {
            var assembly = Assembly.GetExecutingAssembly();
            foreach (Type type in assembly.GetTypes())
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    OutGameCommand? attribute = method.GetCustomAttribute<OutGameCommand>();
                    if (attribute is null) continue;
                    outGameMethod[attribute.name] = new Tuple<OutGameCommand, Func<string, string, RootController, bool>>(
                        attribute, method.CreateDelegate<Func<string, string, RootController, bool>>());
                }
            }
        }

        public void ExecuteCommand(string account, string command, RootController controller)
        {
            string[] cmdSplitted = Regex.Split(command, @"\s+");
            string commandName = cmdSplitted[0];
            string commandBody = String.Join(" ", cmdSplitted[1..]);
            SukaLambdaEngine? vm = controller.vm;
            if (accountToInGameMethod.ContainsKey(account) && accountToInGameMethod[account].Count > 0 && vm != null)
            {
                foreach (var kvp in accountToInGameMethod[account])  // for all characters of this account
                {
                    if (kvp.Key.removedFromMap) continue;
                    if (kvp.Value.TryGetValue(commandName, out Tuple<InGameCommand, Func<string, SukaLambdaEngine, bool>>? t)
                        && (t.Item1.regex == null || t.Item1.regex.Match(commandBody).Success))
                        t.Item2(commandBody, vm);
                }
            }
            if (outGameMethod.ContainsKey(commandName))
                outGameMethod[commandName].Item2(account, commandBody, controller);
        }

        public void RegisterCommandsForCharacter(Character character)
        {
            accountToInGameMethod.TryAdd(character.accountId, new());
            accountToInGameMethod[character.accountId].TryAdd(character, new());
            MethodInfo[] methods = new MethodInfo[] { };
            foreach (Skill skill in character.skills)
            {
                methods = methods.Concat(skill.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)).ToArray();
                foreach (MethodInfo method in methods)
                {
                    InGameCommand? attribute = method.GetCustomAttribute<InGameCommand>();
                    if (attribute is null) continue;
                    accountToInGameMethod[character.accountId][character][attribute.name] =
                        new Tuple<InGameCommand, Func<string, SukaLambdaEngine, bool>>
                        (attribute, method.CreateDelegate<Func<string, SukaLambdaEngine, bool>>(skill));
                }
            }
        }
        public void UnregisterCommandsForCharacter(Character character)
        {
            accountToInGameMethod[character.accountId].Remove(character);
        }
    }
}
