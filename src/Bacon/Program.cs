using System.CommandLine;
using System.Reflection;
using System.Text;

namespace Bacon;

[BaconLua]
public partial class Program
{
    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var rootCommand = new RootCommand("Everything is better with Bacon");
        var baconCommand = new Command("bacon", "The original Bacon experience");
        baconCommand.SetHandler(() => VBacon.VBacon.GetBaconStrips(BaconScript));
        rootCommand.AddCommand(baconCommand);

        var superBaconCommand = new Command("super-bacon", "The Super Bacon experience");
        superBaconCommand.SetHandler(SuperBacon);
        rootCommand.AddCommand(superBaconCommand);

        var parallelOption = new Option<bool>(
            name: "--parallel"
          , description: "Use more CPUs to fry more Eggs"
          , getDefaultValue: () => false
          );

        var durationOption = new Option<int>(
            name: "--duration"
          , description: "For how many seconds should we fry the Egg"
          , getDefaultValue: () => 5
          );

        var widthOption = new Option<int>(
            name: "--width"
          , description: "Width of the canvas"
          , getDefaultValue: () => 70
          );

        var heightOption = new Option<int>(
            name: "--height"
          , description: "Height of the canvas"
          , getDefaultValue: () => 40
          );

        var eggCommand = new Command("egg", "If you need some Egg with the Bacon")
                {
                  parallelOption
                , durationOption
                , widthOption
                , heightOption
                };

        eggCommand.SetHandler((parallel, duration, width, height) =>
          {
              new Egg().EggMe(parallel, duration, width, height);
          }
          , parallelOption
          , durationOption
          , widthOption
          , heightOption
          );
        rootCommand.AddCommand(eggCommand);

        rootCommand.SetHandler(() => VBacon.VBacon.GetBaconStrips(BaconScript));

        await rootCommand.InvokeAsync(args);
    }

    static void SuperBacon()
    {
        while(Console.KeyAvailable)
        {
            _ = Console.ReadKey();
        }

        var sb = new StringBuilder();
        Console.Write("\x1B[c");

        Thread.Sleep(100);

        while(Console.KeyAvailable)
        {
            var key = Console.ReadKey();
            sb.Append(key.KeyChar);
        }

        var response = sb.ToString();
        var split = response.Split(';');

        if (!split.Contains("4")) 
        {
            Console.WriteLine("\x1b[2J\x1b[HYour terminal is meh. Get one that supports sixel, for example Windows Terminal v1.22+");
            return;
        }

        using var stream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("Bacon.super-bacon.txt")
            ;
        using var reader = new StreamReader(stream!);
        Console.Write(reader.ReadToEnd());
    }
}