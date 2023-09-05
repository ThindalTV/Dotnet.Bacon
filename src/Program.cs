using Bacon;
using Spectre.Console;
using System.CommandLine;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var rootCommand = new RootCommand("Everything is better with Bacon");

{
  var baconCommand = new Command("bacon", "The original Bacon experience");
  baconCommand.SetHandler(VBacon.VBacon.GetBaconStrips);
  rootCommand.AddCommand(baconCommand);
}

{
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
    , getDefaultValue: () => AnsiConsole.Profile.Width/2
    );

  var heightOption = new Option<int>(
      name: "--height"
    , description: "Height of the canvas"
    , getDefaultValue: () => AnsiConsole.Profile.Height
    );

  var lug00berOption = new Option<bool?>(
      name: "--lug00ber"
    , description: "lug00ber likes cubes just like me"
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

  var cubeCommand = new Command("cube", "Computers were made for rotating cubes")
  {
    parallelOption
  , durationOption
  , widthOption
  , heightOption
  , lug00berOption
  };

  cubeCommand.SetHandler((parallel, duration, width, height, lug00ber) => 
    { 
        new Cube().CubeMe(parallel, duration, width, height, lug00ber??false);
    }
    , parallelOption
    , durationOption
    , widthOption
    , heightOption
  , lug00berOption
    );
  rootCommand.AddCommand(cubeCommand);

  rootCommand.SetHandler(VBacon.VBacon.GetBaconStrips);
}

await rootCommand.InvokeAsync(args);
