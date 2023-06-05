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
            Dictionary<string,  // command
                Tuple<InGameCommand, Func<string, SukaLambdaEngine, bool>>>>> accountToInGameMethod = new();

        public static readonly Dictionary<string, Tuple<OutGameCommand, Func<string, bool>>> outGameMethod = new();

        public SukaLambdaEngine? vm = null;
        public static Regex leftSplitter = new(@"^(.*)\s+(.*)$");

        public static void RegisterOutGameCommand()
        {
            var assembly = Assembly.GetExecutingAssembly();
            foreach (Type type in assembly.GetTypes())
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Static))
                {
                    OutGameCommand? attribute = method.GetCustomAttribute<OutGameCommand>();
                    if (attribute is null) continue;
                    outGameMethod[attribute.name] = new Tuple<OutGameCommand, Func<string, bool>>(
                        attribute, method.CreateDelegate<Func<string, bool>>());
                }
        }

        public void ExecuteCommand(string account, string command, SukaLambdaEngine? vm = null)
        {
            Match match = leftSplitter.Match(command);
            if (!match.Success || match.Groups.Count < 2) return;
            string commandName = match.Groups[0].Value;
            string commandBody = match.Groups[1].Value;
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
                outGameMethod[commandName].Item2(commandBody);
        }

        public void RegisterCommandsForCharacter(Character character)
        {
            accountToInGameMethod.TryAdd(character.accountId, new());
            accountToInGameMethod[character.accountId].TryAdd(character, new());
            MethodInfo[] methods = character.GetType().GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (MethodInfo method in methods)
            {
                InGameCommand? attribute = method.GetCustomAttribute<InGameCommand>();
                if (attribute is null) continue;
                accountToInGameMethod[character.accountId][character][attribute.name] =
                    new Tuple<InGameCommand, Func<string, SukaLambdaEngine, bool>>
                    (attribute, method.CreateDelegate<Func<string, SukaLambdaEngine, bool>>(character));
            }
        }
        public void UnregisterCommandsForCharacter(Character character)
        {
            accountToInGameMethod[character.accountId].Remove(character);
        }
    }
}
