using System.CommandLine;

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
}