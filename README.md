# StackCleaner

Download the latest version on NuGet (Releases will no longer be kept up to date) 
https://www.nuget.org/packages/DanielWillett.StackCleaner

![stack cleaner comparison](https://github.com/DanielWillett/StackCleaner/assets/12886600/3f8738f6-5673-4cad-b67d-4c7e0169e97a)

#### Clears up stack traces to make them much more readable during debugging.
#### Supports highly customizable color formatting in the following formats:
* ConsoleColor,
* ANSI color codes (4-bit),
* ANSI color codes (3-bit),
* Extended ANSI color codes (32-bit where supported),
* Unity Rich Text,
* Unity TextMeshPro Rich Text,
* Html (with <span> tags).

# Usage

```cs
using StackCleaner;
using Color = System.Drawing.Color; // only needed when dealing with Color32Config.
```

### Get StackTrace from Exception
```cs
StackTrace stackTrace = StackTraceCleaner.GetStackTrace(exception);
```
All the following methods also have an overload to print an exception's stack trace and any existing remote stack traces on Mono.
Remote stack traces are mainly caused by a context switch in async functions.

### Write To Console
```cs
StackTraceCleaner cleaner = new StackTraceCleaner(new StackCleanerConfiguration
{
  ColorFormatting = StackColorFormatType.ExtendedANSIColor,
  IncludeFileData = true,
  IncludeILOffset = true,
  IncludeNamespaces = false,
  Colors = Color32Config.Default
});

// assume Exception ex was caught

Console.ForegroundColor = ConsoleColor.Red;
Console.WriteLine(ex.GetType().Name + " - " + ex.Message);

if (StackTraceCleaner.GetStackTrace(ex) is { } stackTrace)
  cleaner.WriteToConsole(stackTrace);
```

### Write To String
```cs
StackTraceCleaner cleaner = new StackTraceCleaner(new StackCleanerConfiguration
{
  ColorFormatting = StackColorFormatType.None,
  IncludeNamespaces = false
});

StackTrace stackTrace = /* etc */;
UnityEngine.UI.TextBox textBox = /* etc */;
textBox.text = cleaner.GetString(stackTrace);
```
### Write To File
```cs
StackTraceCleaner cleaner = new StackTraceCleaner(new StackCleanerConfiguration
{
  ColorFormatting = StackColorFormatType.Html,
  IncludeNamespaces = false,
  IncludeSourceData = false,
  HtmlUseClassNames = true
});

StackTrace stackTrace = /* etc */;
string str = cleaner.WriteToFile(@"C:\error.html", stackTrace, System.Text.Encoding.UTF8);
```
### Write To Stream
```cs
StackTraceCleaner cleaner = new StackTraceCleaner(new StackCleanerConfiguration
{
  ColorFormatting = StackColorFormatType.Html,
  IncludeNamespaces = false,
  IncludeSourceData = false,
  HtmlUseClassNames = true
});

StackTrace stackTrace = /* etc */;
NetworkStream stream = /* etc */;

// async variant also available
cleaner.WriteToStream(stream, stackTrace, System.Text.Encoding.UTF8);
```

### Write To TextWriter
```cs
StackTraceCleaner cleaner = new StackTraceCleaner(new StackCleanerConfiguration
{
  ColorFormatting = StackColorFormatType.UnityRichText,
  IncludeNamespaces = true,
  IncludeSourceData = false,
  HiddenTypes = new Type[]
  {
      typeof(ExecutionContext),
      typeof(TaskAwaiter),
      typeof(TaskAwaiter<>),
      typeof(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter),
      typeof(ConfiguredTaskAwaitable<>.ConfiguredTaskAwaiter),
      typeof(System.Runtime.ExceptionServices.ExceptionDispatchInfo),
      typeof(UnityEngine.Debug),
      typeof(UnityEngine.Assert),
      typeof(UnityEngine.PlayerLoop)
  }
});

StackTrace stackTrace = /* etc */;
Stream stream = /* etc */;

// async variant also available
using TextWriter writer = new TextWriter(stream, System.Text.Encoding.UTF8);
cleaner.WriteToTextWriter(stackTrace, writer);
```


### Configuration

#### Default
```cs
// Default values (see default configuration values below)
StackTraceCleaner.Default.WriteToConsole(stackTrace);
```

#### Custom
```cs
public static readonly StackTraceCleaner StackTraceCleaner = new StackTraceCleaner(new StackCleanerConfiguration
{

/* Format Configuration = default */

  // Determines how colors are added to formatting (if at all), see StackColorFormatType below.
    ColorFormatting = StackColorFormatType.None,

  // Only used for Source Data, determines what format provider is used to translate line numbers and IL offsets to strings.
    Locale = CultureInfo.InvariantCulture,

  // Source data includes line number, column number, IL offset, and file name when available.
    IncludeSourceData = true,

  // Appends a warning to the end of the stack trace that lines were removed for visibility.
    WarnForHiddenLines = false,

  // Source data is put on the line after the stack frame declaration.
    PutSourceDataOnNewLine = true,

  // Namespaces are included in the method declaration.
    IncludeNamespaces = true,

  // IL offsets are included in the source data.
    IncludeILOffset = false,

  // Line and column numbers are included in the source data.
    IncludeLineData = true,

  // Relative source file path will be included in the source data.
    IncludeFileData = false,

  // 'Primitive' types will use their aliases. For example, 'int' instead of 'Int32'.
    UseTypeAliases = false,
    
  // If ColorFormatting is set to 'Html', use css class names (defined as public constants in the StackTraceCleaner class) instead of style tags.
    HtmlUseClassNames = false,

  // If ColorFormatting is set to 'Html', write an outer <div> with a background color around the output HTML.
    HtmlWriteOuterDiv = false,

  // Types who's methods will be skipped in stack traces.
    HiddenTypes = /* See Below */,

/* Color Configuration */

  // As close as possible to the default Visual Studio C# formatting rules in 4 bit color (System.ConsoleColor)
  // Default value of Colors
    Colors = Color4Config.Default. 
    
  // The default Visual Studio C# formatting rules
    Colors = Color32Config.Default,

  // Color settings with 4 bit colors, best for Color formatting of ANSIColor or ConsoleColor (or None).
    Colors = new Color4Config
    {
      KeywordColor = ConsoleColor.Green
    },

  // Color settings with 32 bit colors, best for Color formatting of everything else (or None).
    Colors = new Color32Config
    {                            // alpha channel is ignored
      KeywordColor = Color.FromArgb(255, 230, 255, 153)
    },

  // Color settings with 32 bit colors when using UnityEngine (because System.Drawing is not available).
  // This is only available in the .NET Framework 4.6.1 or 4.8.1 and .NET Standard 2.1 builds.
    Colors = new UnityColor32Config
    {                                            // alpha channel is ignored
      KeywordColor = new Color(0.34f, 0.61f, 0.84f, 1f)
    }
});
```

### Colors
`enum StackColorFormatType`
* `None` => No color formatting, just raw text.
* `ConsoleColor` => Sets the `Console.ForegroundColor` for each section. Only applicable when printed to console with `WriteToConsole`
* `UnityRichText` => UnityEngine rich text tags. See more: [Unity Rich Text](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/StyledText.html)
* `TextMeshProRichText` => TextMeshPro rich text tags. See more: [Unity Rich Text](http://digitalnativestudios.com/textmeshpro/docs/rich-text/)
* `ANSIColor` => ANSI Terminal text formatting codes. See more: [Microsft: Virtual Terminal Text Formatting](https://learn.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences?redirectedfrom=MSDN#text-formatting).
* `ExtendedANSIColor` => ANSI Terminal text formatting codes. See more: [Microsft: Virtual Terminal Extended Color Formatting](https://learn.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences?redirectedfrom=MSDN#extended-colors).
* `Html` => Text is colored with &lt;span&gt; tags. Use classes instead of constant CSS styles by setting HtmlUseClassNames to true. The classes are public constants in StackTraceCleaner.
* `ANSIColorNoBright` => ANSI Terminal text formatting codes without the bright bit. Discord can display this in an `ansi` code block.
  
### Custom Color Provider

A `UnityColor32Config` is already included, but this shows one way you could create a custom color provider. The base properties are stored in Int32 values formatted in ARGB32 converted like shown below:
```cs
unchecked
{
    int argb = color.a << 24 | color.r << 16 | color.g << 8 | color.b;

    byte a = (byte)(argb >> 24), r = (byte)(argb >> 16), g = (byte)(argb >> 8), b = (byte)argb;
}
```
Please note that you can use StackCleaner without referencing `UnityEngine.dll` as long as you don't access the `UnityColor32Config` class.

UnityEngine.Color Custom Converter:
```cs
using StackCleaner;
using UnityEngine;
using Color = UnityEngine.Color;

internal sealed class UnityColorConfig : ColorConfig
{
    // intead of new you can also override the base properties to provide the colors as ARGB int32s directly.
    public new Color KeywordColor            { get => FromArgb(base.KeywordColor);            set => base.KeywordColor            = ToArgb(value); }
    public new Color MethodColor             { get => FromArgb(base.MethodColor);             set => base.MethodColor             = ToArgb(value); }
    public new Color PropertyColor           { get => FromArgb(base.PropertyColor);           set => base.PropertyColor           = ToArgb(value); }
    public new Color ParameterColor          { get => FromArgb(base.ParameterColor);          set => base.ParameterColor          = ToArgb(value); }
    public new Color ClassColor              { get => FromArgb(base.ClassColor);              set => base.ClassColor              = ToArgb(value); }
    public new Color StructColor             { get => FromArgb(base.StructColor);             set => base.StructColor             = ToArgb(value); }
    public new Color FlowKeywordColor        { get => FromArgb(base.FlowKeywordColor);        set => base.FlowKeywordColor        = ToArgb(value); }
    public new Color InterfaceColor          { get => FromArgb(base.InterfaceColor);          set => base.InterfaceColor          = ToArgb(value); }
    public new Color GenericParameterColor   { get => FromArgb(base.GenericParameterColor);   set => base.GenericParameterColor   = ToArgb(value); }
    public new Color EnumColor               { get => FromArgb(base.EnumColor);               set => base.EnumColor               = ToArgb(value); }
    public new Color NamespaceColor          { get => FromArgb(base.NamespaceColor);          set => base.NamespaceColor          = ToArgb(value); }
    public new Color PunctuationColor        { get => FromArgb(base.PunctuationColor);        set => base.PunctuationColor        = ToArgb(value); }
    public new Color ExtraDataColor          { get => FromArgb(base.ExtraDataColor);          set => base.ExtraDataColor          = ToArgb(value); }
    public new Color LinesHiddenWarningColor { get => FromArgb(base.LinesHiddenWarningColor); set => base.LinesHiddenWarningColor = ToArgb(value); }
    public new Color HtmlBackgroundColor     { get => FromArgb(base.HtmlBackgroundColor);     set => base.HtmlBackgroundColor     = ToArgb(value); }
    public static Color FromArgb(int value)
    {
        return new Color(
            unchecked((byte)(value >> 16)) / 255f,
            unchecked((byte)(value >> 8))  / 255f,
            unchecked((byte)value)         / 255f,
            1f);
    }
    public static int ToArgb(Color color)
    {
        return 0xFF << 24 |
               (byte)Math.Min(255, Mathf.RoundToInt(color.r * 255)) << 16 |
               (byte)Math.Min(255, Mathf.RoundToInt(color.g * 255)) << 8  |
               (byte)Math.Min(255, Mathf.RoundToInt(color.b * 255));
    }
}
```
