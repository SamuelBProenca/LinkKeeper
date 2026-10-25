using LinkKeeper.Core;
using System.Text.Json;

if (args.Length == 1 && args[0] == "--help")
{
    Console.WriteLine("LinkKeeper: valida comandos; ainda não armazena links.\nUso: LinkKeeper <URL> --jobs|--fun|--art [/7d|/7dias|/permanente]");
    return 0;
}
try
{
    var parsed = new CommandParser(TimeProvider.System).Parse(string.Join(" ", args));
    Console.WriteLine(JsonSerializer.Serialize(new { mode = "validation-only", link = parsed }));
    return 0;
}
catch (ArgumentException error)
{
    Console.Error.WriteLine(error.Message);
    return 2;
}
