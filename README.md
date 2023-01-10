# StackCleaner

#### Clears up stack traces to make them much more readable during debugging.
#### Supports highly customizable color formatting in the following formats:
* ConsoleColor,
* ANSI color codes (4-bit),
* Extended ANSI color codes (32-bit where supported),
* Unity Rich Text,
* Unity TextMeshPro Rich Text,
* Html (with <span> tags).

# Usage

```cs
using StackCleaner;
using Color = System.Drawing.Color;
```

### Get StackTrace from Exception
```cs
StackTrace stackTrace = StackTraceCleaner.GetStackTrace(exception);
```

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

  // Relative source file path will be included in the source data.
    HtmlUseClassNames = false,

  // Relative source file path will be included in the source data.
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
    }
});
```
