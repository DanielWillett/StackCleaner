using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StackCleaner;

public class StackTraceCleaner
{
    private const char ConsoleEscapeCharacter = '\u001b';
    private const char PointerSymbol = '*';
    private const char MemberSeparatorSymbol = '.';
    private const char NullableSymbol = '?';
    private const char SpaceSymbol = ' ';
    private const string SpaceSymbolStr = " ";
    private const char DoubleQuotationMarkSymbol = '\"';
    private const string RootDirectorySymbol = "~";
    private const char GenericOpenSymbol = '<';
    private const char GenericCloseSymbol = '>';
    private const char AngleBracketOpeningUnityEscape = '〈';
    private const char AngleBracketClosingUnityEscape = '〉';
    private const char ParametersOpenSymbol = '(';
    private const char ParametersCloseSymbol = ')';
    private const char MethodBodyOpenSymbol = '{';
    private const char MethodBodyCloseSymbol = '}';
    private const char ArrayOpenSymbol = '[';
    private const char ArrayCloseSymbol = ']';
    private const char TypeNameGenericSeparator = '`';
    private const string ArraySymbol = "[]";
    private const string ListSeparatorSymbol = ", ";
    private const string GlobalSeparatorSymbol = "::";
    private const string LambdaSymbol = "=>";
    private const string HiddenMethodContentSymbol = "...";
    private const string RefSymbol = "ref";
    private const string OutSymbol = "out";
    private const string ParamsSymbol = "params";
    private const string AnonymousSymbol = "anonymous";
    private const string StaticSymbol = "static";
    private const string SetterSymbol = "set";
    private const string GetterSymbol = "get";
    private const string AsyncSymbol = "async";
    private const string EnumeratorSymbol = "enumerator";
    private const string UnityEndColorSymbol = "</color>";
    private const string StartSpanTagStyleClassP1 = "<span class=\"";
    private const string StartSpanTagStyleClassP2 = "\">";
    private const string OuterStartHtmlTagStyleClass = "<div class=\"" + BackgroundClassName + "\">";
    private const string OuterStartHtmlTagStyleSymbolP1 = "<div style=\"background-color:#";
    private const string OuterStartHtmlTagStyleSymbolP2 = ";\">";
    private const string OuterEndHtmlTagSymbol = "</div>";
    private const string HtmlEndSpanSymbol = "</span>";
    private const string StartParaTagSymbol = "<p>";
    private const string EndParaTagSymbol = "</p>";
    private const string GlobalSymbol = "global";
    private const string AtPrefixSymbol = " at ";
    private const string LineNumberPrefixSymbol = "LN #";
    private const string ColumnNumberPrefixSymbol = "COL #";
    private const string ILOffsetPrefixSymbol = "IL";
    private const string FilePrefixSymbol = "FILE: ";
    private const string HiddenLineWarning = "Some lines hidden for readability.";
    public const string BackgroundClassName = "st_bkgr";
    public const string KeywordClassName = "st_keyword";
    public const string MethodClassName = "st_method";
    public const string PropertyClassName = "st_property";
    public const string ParameterClassName = "st_parameter";
    public const string ClassClassName = "st_class";
    public const string StructClassName = "st_struct";
    public const string FlowKeywordClassName = "st_flow_keyword";
    public const string InterfaceClassName = "st_interface";
    public const string GenericParameterClassName = "st_generic_parameter";
    public const string EnumClassName = "st_enum";
    public const string NamespaceClassName = "st_namespace";
    public const string PunctuationClassName = "st_punctuation";
    public const string ExtraDataClassName = "st_extra_data";
    public const string LinesHiddenWarningClassName = "st_lines_hidden_warning";
    private static readonly Type TypeBoolean = typeof(bool);
    private static readonly Type TypeUInt8 = typeof(byte);
    private static readonly Type TypeCharacter = typeof(char);
    private static readonly Type TypeDecimal = typeof(decimal);
    private static readonly Type TypeDouble = typeof(double);
    private static readonly Type TypeSingle = typeof(float);
    private static readonly Type TypeInt32 = typeof(int);
    private static readonly Type TypeInt64 = typeof(long);
    private static readonly Type TypeObject = typeof(object);
    private static readonly Type TypeInt8 = typeof(sbyte);
    private static readonly Type TypeInt16 = typeof(short);
    private static readonly Type TypeString = typeof(string);
    private static readonly Type TypeUInt32 = typeof(uint);
    private static readonly Type TypeUInt64 = typeof(ulong);
    private static readonly Type TypeUInt16 = typeof(ushort);
    private static readonly Type TypeVoid = typeof(void);
    private static readonly Type TypeNullableValueType = typeof(Nullable<>);
    private static readonly Type TypeCompilerGenerated = typeof(CompilerGeneratedAttribute);
    private static readonly Dictionary<Type, MethodInfo> _compGenMethodRepls = new Dictionary<Type, MethodInfo>(64);
    internal static readonly IReadOnlyCollection<Type> DefaultHiddenTypes = Array.AsReadOnly(new Type[]
    {
        typeof(ExecutionContext),
        typeof(TaskAwaiter),
        typeof(TaskAwaiter<>),
        typeof(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter),
        typeof(ConfiguredTaskAwaitable<>.ConfiguredTaskAwaiter),
        typeof(System.Runtime.ExceptionServices.ExceptionDispatchInfo),
    });

    private static StackTraceCleaner? _instance;
    private readonly StackCleanerConfiguration _config;
    private readonly bool _isRgbaColor;
    private readonly bool _appendColor;
    private readonly bool _reqEndColor;
    private readonly bool _writeNewline;
    private readonly bool _writeParaTags;
    /// <summary>
    /// Default implementation of <see cref="StackTraceCleaner"/>.
    /// </summary>
    public static StackTraceCleaner Default => _instance ??= new StackTraceCleaner();

    /// <summary>
    /// This value and all it's properties are <see langword="readonly"/>. Trying to modify them will throw a <see cref="NotSupportedException"/>.
    /// </summary>
    public StackCleanerConfiguration Configuration => _config;

    /// <summary>
    /// Use <see cref="Default"/> to get a default implementation.
    /// </summary>
    private StackTraceCleaner() : this(StackCleanerConfiguration.Default) { }
    public StackTraceCleaner(StackCleanerConfiguration config)
    {
        config.Frozen = true;
        config.Colors.Frozen = true;
        _config = config;
        _isRgbaColor = config.Colors is not Color4Config;
        _appendColor = config.ColorFormatting
            is StackColorFormatType.UnityRichText
            or StackColorFormatType.TextMeshProRichText
            or StackColorFormatType.ANSIColor
            or StackColorFormatType.ExtendedANSIColor
            or StackColorFormatType.Html;
        _reqEndColor = _appendColor &&
                       config.ColorFormatting is StackColorFormatType.UnityRichText or StackColorFormatType.Html;
        _writeNewline = config.ColorFormatting != StackColorFormatType.Html;
        _writeParaTags = config.ColorFormatting == StackColorFormatType.Html;
    }
    public static StackTrace? GetStackTrace(Exception ex, bool fetchSourceInfo = true) => ex.StackTrace != null ? new StackTrace(ex, fetchSourceInfo) : null;
    public string GetString(StackTrace stackTrace)
    {
        if (stackTrace == null)
            throw new ArgumentNullException(nameof(stackTrace));

        using MemoryStream stream = new MemoryStream(256);
        stream.Position = 0;
        WriteToStream(stream, stackTrace, Encoding.UTF8);
        byte[] bytes = stream.GetBuffer();
        return Encoding.UTF8.GetString(bytes, 0, (int)stream.Length);
    }
    public void WriteToStream(Stream stream, StackTrace stackTrace, Encoding? encoding = null)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        if (stackTrace == null)
            throw new ArgumentNullException(nameof(stackTrace));
        if (!stream.CanWrite)
            throw new ArgumentException("Stream must be able to write.", nameof(stream));

        encoding ??= Encoding.UTF8;
        using TextWriter writer = new StreamWriter(stream, encoding, 256, true);
        WriteToTextWriterIntl(stackTrace, writer);
    }
    public Task WriteToStreamAsync(Stream stream, StackTrace stackTrace, Encoding? encoding = null, CancellationToken token = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        if (stackTrace == null)
            throw new ArgumentNullException(nameof(stackTrace));
        if (!stream.CanWrite)
            throw new ArgumentException("Stream must be able to write.", nameof(stream));

        encoding ??= Encoding.UTF8;
        TextWriter writer = new StreamWriter(stream, encoding, _config.ColorFormatting is StackColorFormatType.None
            or StackColorFormatType.ConsoleColor ? 256 : (_config.ColorFormatting == StackColorFormatType.ANSIColor ? 384 : 512), true);
        return WriteToTextWriterIntlAsync(stackTrace, writer, token, true);
    }
    /// <summary>
    /// Output the stack trace to <see cref="Console"/> using the appropriate color format.
    /// </summary>
    /// <param name="stackTrace">Stack trace to write.</param>
    /// <param name="writeToConsoleBuffer">Only set this to <see langword="true"/> if memory is a huge concern, writing to the console per span takes significantly (~5x) longer than writing to a memory buffer then writing the entire buffer to the console at once.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void WriteToConsole(StackTrace stackTrace, bool writeToConsoleBuffer = false)
    {
        if (stackTrace == null)
            throw new ArgumentNullException(nameof(stackTrace));

        if (_config.ColorFormatting is StackColorFormatType.ConsoleColor)
        {
            ConsoleColor currentColor = (ConsoleColor)255;
            ConsoleColor old = Console.ForegroundColor;
            foreach (SpanData span in EnumerateSpans(stackTrace))
            {
                if (_config.ColorFormatting != StackColorFormatType.None && span.Color != TokenType.Space)
                {
                    ConsoleColor old2 = currentColor;
                    currentColor = _isRgbaColor ? Color4Config.ToConsoleColor(GetColor(span.Color)) : (ConsoleColor)(GetColor(span.Color) - 1);
                    if (old2 != currentColor)
                        Console.ForegroundColor = currentColor;
                }

                if (span.Text != null)
                    Console.Write(span.Text);
                else
                    Console.Write(span.Char);
            }
            if (_config.ColorFormatting != StackColorFormatType.None)
                Console.ForegroundColor = old;
            Console.WriteLine();
        }
        else if (!writeToConsoleBuffer) Console.WriteLine(GetString(stackTrace));
        else WriteToTextWriterIntl(stackTrace, Console.Out);
    }
    public void WriteToTextWriter(StackTrace stackTrace, TextWriter writer)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));
        if (stackTrace == null)
            throw new ArgumentNullException(nameof(stackTrace));

        WriteToTextWriterIntl(stackTrace, writer);
    }
    public Task WriteToTextWriterAsync(StackTrace stackTrace, TextWriter writer, CancellationToken token = default)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));
        if (stackTrace == null)
            throw new ArgumentNullException(nameof(stackTrace));

        return WriteToTextWriterIntlAsync(stackTrace, writer, token, false);
    }
    private void WriteToTextWriterIntl(StackTrace trace, TextWriter writer)
    {
        TokenType currentColor = (TokenType)255;
        bool div = false;
        foreach (SpanData span in EnumerateSpans(trace))
        {
            if (!div && _writeParaTags && _config.HtmlWriteOuterDiv)
            {
                writer.Write(GetDivTag());
                div = true;
            }
            if (currentColor != span.Color)
            {
                if (currentColor != span.Color && (int)currentColor != 255 && ShouldWriteEnd(span.Color))
                    writer.Write(GetEndTag());

                if (_appendColor && span.Color is not TokenType.Space or TokenType.EndTag && currentColor != span.Color)
                    writer.Write(GetColorString(span.Color));
                currentColor = span.Color;
            }

            if (span.Text != null)
                writer.Write(span.Text);
            else
            {
                char c = span.Char;
                if (c == '<')
                {
                    if (_config.ColorFormatting is StackColorFormatType.UnityRichText or StackColorFormatType.TextMeshProRichText)
                    {
                        c = AngleBracketOpeningUnityEscape;
                    }
                }
                else if (c == '>' && _config.ColorFormatting is StackColorFormatType.UnityRichText or StackColorFormatType.TextMeshProRichText)
                {
                    c = AngleBracketClosingUnityEscape;
                }
                writer.Write(c);
            }
        }
        if ((int)currentColor != 255 && ShouldWriteEnd(currentColor))
            writer.Write(_config.ColorFormatting == StackColorFormatType.UnityRichText ? UnityEndColorSymbol : HtmlEndSpanSymbol);
        if (div)
            writer.Write(OuterEndHtmlTagSymbol);
        if (_config.ColorFormatting is StackColorFormatType.ANSIColor or StackColorFormatType.ExtendedANSIColor)
            writer.Write(GetANSIResetString());
        if (_writeNewline)
            writer.Write(Environment.NewLine);
    }
    private async Task WriteToTextWriterIntlAsync(StackTrace trace, TextWriter writer, CancellationToken token = default, bool dispose = true)
    {
        token.ThrowIfCancellationRequested();
        TokenType currentColor = (TokenType)255;
        bool div = false;
        foreach (SpanData span in EnumerateSpans(trace))
        {
            if (!div && _writeParaTags && _config.HtmlWriteOuterDiv)
            {
                writer.Write(GetDivTag());
                div = true;
            }
            if (currentColor != span.Color)
            {
                if (currentColor != span.Color && (int)currentColor != 255 && ShouldWriteEnd(span.Color))
                    await writer.WriteAsync(GetEndTag()).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();

                if (_appendColor && span.Color is not TokenType.Space or TokenType.EndTag && currentColor != span.Color)
                    await writer.WriteAsync(GetColorString(span.Color)).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                currentColor = span.Color;
            }

            if (span.Text != null)
                await writer.WriteAsync(span.Text).ConfigureAwait(false);
            else
            {
                char c = span.Char;
                if (c == '<')
                {
                    if (_config.ColorFormatting is StackColorFormatType.UnityRichText or StackColorFormatType.TextMeshProRichText)
                    {
                        c = AngleBracketOpeningUnityEscape;
                    }
                }
                else if (c == '>' && _config.ColorFormatting is StackColorFormatType.UnityRichText or StackColorFormatType.TextMeshProRichText)
                {
                    c = AngleBracketClosingUnityEscape;
                }
                await writer.WriteAsync(c).ConfigureAwait(false);
            }
            token.ThrowIfCancellationRequested();
        }
        if ((int)currentColor != 255 && ShouldWriteEnd(currentColor))
            await writer.WriteAsync(_config.ColorFormatting == StackColorFormatType.UnityRichText ? UnityEndColorSymbol : HtmlEndSpanSymbol).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        if (div)
            writer.Write(OuterEndHtmlTagSymbol);
        if (_config.ColorFormatting is StackColorFormatType.ANSIColor or StackColorFormatType.ExtendedANSIColor)
            await writer.WriteAsync(GetANSIResetString()).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        if (_writeNewline)
            await writer.WriteAsync(Environment.NewLine).ConfigureAwait(false);
        if (dispose)
            writer.Dispose();
    }
    private static string? GetKeyword(Type type)
    {
        if (type is null)
            return "null";
        if (type.IsPrimitive)
        {
            if (type == TypeBoolean)
                return "bool";
            if (type == TypeUInt8)
                return "byte";
            if (type == TypeCharacter)
                return "char";
            if (type == TypeDouble)
                return "double";
            if (type == TypeSingle)
                return "float";
            if (type == TypeInt32)
                return "int";
            if (type == TypeInt64)
                return "long";
            if (type == TypeInt8)
                return "sbyte";
            if (type == TypeInt16)
                return "short";
            if (type == TypeUInt32)
                return "uint";
            if (type == TypeUInt64)
                return "ulong";
            if (type == TypeUInt16)
                return "ushort";
        }
        else
        {
            if (type == TypeDecimal)
                return "decimal";
            if (type == TypeObject)
                return "object";
            if (type == TypeString)
                return "string";
            if (type == TypeVoid)
                return "void";
        }
        return null;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TokenType GetTypeColor(Type type) => type.IsGenericParameter ? TokenType.GenericParameter : (type.IsInterface ? TokenType.Interface : (type.IsValueType ? (type.IsEnum ? TokenType.Enum : TokenType.Struct) : TokenType.Class));
    private static string GetANSIResetString() => GetANSIForegroundString((ConsoleColor)(-1));
    private static unsafe string GetANSIForegroundString(ConsoleColor color)
    {
        // https://learn.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences?redirectedfrom=MSDN#text-formatting
        int num = color switch
        {
            ConsoleColor.Black => 30,
            ConsoleColor.DarkRed => 31,
            ConsoleColor.DarkGreen => 32,
            ConsoleColor.DarkYellow => 33,
            ConsoleColor.DarkBlue => 34,
            ConsoleColor.DarkMagenta => 35,
            ConsoleColor.DarkCyan => 36,
            ConsoleColor.Gray => 37,
            ConsoleColor.DarkGray => 90,
            ConsoleColor.Red => 91,
            ConsoleColor.Green => 92,
            ConsoleColor.Yellow => 93,
            ConsoleColor.Blue => 94,
            ConsoleColor.Magenta => 95,
            ConsoleColor.Cyan => 96,
            ConsoleColor.White => 97,
            _ => 0
        };
        char* chrs = stackalloc char[num == 0 ? 4 : 5];
        chrs[0] = ConsoleEscapeCharacter;
        chrs[1] = '[';
        chrs[2] = (char)(num / 10 + 48);
        if (num == 0)
            chrs[3] = 'm';
        else
        {
            chrs[3] = (char)(num % 10 + 48);
            chrs[4] = 'm';
        }

        return new string(chrs, 0, num == 0 ? 4 : 5);
    }
    private static unsafe string GetExtANSIForegroundString(int argb)
    {
        // https://learn.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences?redirectedfrom=MSDN#text-formatting
        byte r = unchecked((byte)(argb >> 16));
        byte g = unchecked((byte)(argb >> 8));
        byte b = unchecked((byte)argb);
        int l = 10 + (r > 9 ? r > 99 ? 3 : 2 : 1) + (g > 9 ? g > 99 ? 3 : 2 : 1) + (b > 9 ? b > 99 ? 3 : 2 : 1); 
        char* chrs = stackalloc char[l];
        chrs[0] = ConsoleEscapeCharacter;
        chrs[1] = '[';
        chrs[2] = '3';
        chrs[3] = '8';
        chrs[4] = ';';
        chrs[5] = '2';
        chrs[6] = ';';
        int index = 6;
        if (r > 99)
            chrs[++index] = (char)(r / 100 + 48);
        if (r > 9)
            chrs[++index] = (char)((r % 100) / 10 + 48);
        chrs[++index] = (char)(r % 10 + 48);
        chrs[++index] = ';';
        if (g > 99)
            chrs[++index] = (char)(g / 100 + 48);
        if (g > 9)
            chrs[++index] = (char)((g % 100) / 10 + 48);
        chrs[++index] = (char)(g % 10 + 48);
        chrs[++index] = ';';
        if (b > 99)
            chrs[++index] = (char)(b / 100 + 48);
        if (b > 9)
            chrs[++index] = (char)((b % 100) / 10 + 48);
        chrs[++index] = (char)(b % 10 + 48);
        chrs[index + 1] = 'm';
        return new string(chrs, 0, l);
    }
    private static unsafe string GetUnityString(int argb)
    {
        char* chrs = stackalloc char[15];
        chrs[0] = '<'; chrs[1] = 'c'; chrs[2] = 'o'; chrs[3] = 'l'; chrs[4] = 'o'; chrs[5] = 'r'; chrs[6] = '='; chrs[7] = '#';
        GetHex(argb, chrs + 8);
        chrs[14] = '>';
        return new string(chrs, 0, 15);
    }
    private unsafe string GetHtmlStartTag(TokenType token)
    {
        if (_config.HtmlUseClassNames)
        {
            string classname = token switch
            {
                TokenType.Keyword => KeywordClassName,
                TokenType.Method => MethodClassName,
                TokenType.Property => PropertyClassName,
                TokenType.Parameter => ParameterClassName,
                TokenType.Class => ClassClassName,
                TokenType.Struct => StructClassName,
                TokenType.FlowKeyword => FlowKeywordClassName,
                TokenType.Interface => InterfaceClassName,
                TokenType.GenericParameter => GenericParameterClassName,
                TokenType.Enum => EnumClassName,
                TokenType.Namespace => NamespaceClassName,
                TokenType.Punctuation => PunctuationClassName,
                TokenType.ExtraData => ExtraDataClassName,
                TokenType.LinesHiddenWarning => LinesHiddenWarningClassName,
                _ => string.Empty
            };
            return StartSpanTagStyleClassP1 + classname + StartSpanTagStyleClassP2;
        }
        int argb = GetColor(token);
        if (!_isRgbaColor)
            argb = Color32Config.ToArgb((ConsoleColor)(argb - 1));
        // <span style="color:#ffffff;">
        char* chrs = stackalloc char[29];
        chrs[0] = '<'; chrs[1] = 's'; chrs[2] = 'p'; chrs[3] = 'a'; chrs[4] = 'n'; chrs[5] = ' ';
        chrs[6] = 's'; chrs[7] = 't'; chrs[8] = 'y'; chrs[9] = 'l'; chrs[10] = 'e'; chrs[11] = '=';
        chrs[12] = '"'; chrs[13] = 'c'; chrs[14] = 'o'; chrs[15] = 'l'; chrs[16] = 'o'; chrs[17] = 'r';
        chrs[18] = ':'; chrs[19] = '#';
        GetHex(argb, chrs + 20);
        chrs[26] = ';'; chrs[27] = '"'; chrs[28] = '>';
        return new string(chrs, 0, 29);
    }
    private unsafe string GetDivTag()
    {
        if (_config.HtmlUseClassNames)
            return OuterStartHtmlTagStyleClass;
        
        int argb = _config.Colors.HtmlBackgroundColor;
        if (!_isRgbaColor)
            argb = Color32Config.ToArgb((ConsoleColor)(argb - 1));
        // <span style="color:#ffffff;">
        char* chrs = stackalloc char[39];
        chrs[0] = '<'; chrs[1] = 'd'; chrs[2] = 'i'; chrs[3] = 'v'; chrs[4] = ' '; chrs[5] = 's';
        chrs[6] = 't'; chrs[7] = 'y'; chrs[8] = 'l'; chrs[9] = 'e'; chrs[10] = '='; chrs[11] = '"';
        chrs[12] = 'b'; chrs[13] = 'a'; chrs[14] = 'c'; chrs[15] = 'k'; chrs[16] = 'g'; chrs[17] = 'r'; chrs[18] = 'o';
        chrs[19] = 'u'; chrs[20] = 'n'; chrs[21] = 'd'; chrs[22] = '-'; chrs[23] = 'c'; chrs[24] = 'o';
        chrs[25] = 'l'; chrs[26] = 'o'; chrs[27] = 'r';
        chrs[28] = ':'; chrs[29] = '#';
        GetHex(argb, chrs + 30);
        chrs[36] = ';'; chrs[37] = '"'; chrs[38] = '>';
        return new string(chrs, 0, 39);
    }
    private static unsafe string GetTMProString(int argb)
    {
        char* chrs = stackalloc char[9];
        chrs[0] = '<'; chrs[1] = '#';
        GetHex(argb, chrs + 2);
        chrs[8] = '>';
        return new string(chrs, 0, 9);
    }
    private static unsafe void GetHex(int argb, char* chrs)
    {
        byte d = (byte)((argb >> 20) & 15);
        chrs[0] = (char)(d > 9 ? d + 87 : d + 48);
        d = (byte)((argb >> 16) & 15);
        chrs[1] = (char)(d > 9 ? d + 87 : d + 48);
        d = (byte)((argb >> 12) & 15);
        chrs[2] = (char)(d > 9 ? d + 87 : d + 48);
        d = (byte)((argb >> 8) & 15);
        chrs[3] = (char)(d > 9 ? d + 87 : d + 48);
        d = (byte)((argb >> 4) & 15);
        chrs[4] = (char)(d > 9 ? d + 87 : d + 48);
        d = (byte)(argb & 15);
        chrs[5] = (char)(d > 9 ? d + 87 : d + 48);
    }
    private int GetColor(TokenType type) => type switch
    {
        TokenType.Keyword => _config.Colors.KeywordColor,
        TokenType.Method => _config.Colors.MethodColor,
        TokenType.Property => _config.Colors.PropertyColor,
        TokenType.Parameter => _config.Colors.ParameterColor,
        TokenType.Class => _config.Colors.ClassColor,
        TokenType.Struct => _config.Colors.StructColor,
        TokenType.FlowKeyword => _config.Colors.FlowKeywordColor,
        TokenType.Interface => _config.Colors.InterfaceColor,
        TokenType.GenericParameter => _config.Colors.GenericParameterColor,
        TokenType.Enum => _config.Colors.EnumColor,
        TokenType.Namespace => _config.Colors.NamespaceColor,
        TokenType.Punctuation => _config.Colors.PunctuationColor,
        TokenType.ExtraData => _config.Colors.ExtraDataColor,
        TokenType.LinesHiddenWarning => _config.Colors.LinesHiddenWarningColor,
        _ => _isRgbaColor ? unchecked((int)uint.MaxValue) : (int)ConsoleColor.Gray + 1
    };
    private string GetColorString(TokenType token)
    {
#pragma warning disable CS8509
        if (_isRgbaColor)
        {
            return _config.ColorFormatting switch
            {
                StackColorFormatType.UnityRichText => GetUnityString(GetColor(token)),
                StackColorFormatType.TextMeshProRichText => GetTMProString(GetColor(token)),
                StackColorFormatType.ANSIColor => GetANSIForegroundString(Color4Config.ToConsoleColor(GetColor(token))),
                StackColorFormatType.ExtendedANSIColor => GetExtANSIForegroundString(GetColor(token)),
                StackColorFormatType.Html => GetHtmlStartTag(token)
            };
        }

        return _config.ColorFormatting switch
        {
            StackColorFormatType.UnityRichText => GetUnityString(Color32Config.ToArgb((ConsoleColor)(GetColor(token) - 1))),
            StackColorFormatType.TextMeshProRichText => GetTMProString(Color32Config.ToArgb((ConsoleColor)(GetColor(token) - 1))),
            StackColorFormatType.ANSIColor => GetANSIForegroundString((ConsoleColor)(GetColor(token) - 1)),
            StackColorFormatType.ExtendedANSIColor => GetExtANSIForegroundString(Color32Config.ToArgb((ConsoleColor)(GetColor(token) - 1))),
            StackColorFormatType.Html => GetHtmlStartTag(token)
        };
#pragma warning restore CS8509
    }
    private IEnumerable<SpanData> EnumerateTypeName(Type type, bool isOut = false)
    {
        if (_config.UseTypeAliases && GetKeyword(type) is { } rtnName)
        {
            yield return new SpanData(rtnName, TokenType.Keyword);
        }
        else
        {
            if (type.IsPointer)
            {
                foreach (SpanData d in EnumerateTypeName(type.GetElementType()!))
                    yield return d;
                yield return new SpanData(PointerSymbol, TokenType.Punctuation);
                yield break;
            }
            if (type.IsArray)
            {
                foreach (SpanData d in EnumerateTypeName(type.GetElementType()!))
                    yield return d;
                yield return new SpanData(ArraySymbol, TokenType.Punctuation);
                yield break;
            }
            if (type.IsByRef)
            {
                if (type.GetElementType() is { } elemType)
                {
                    yield return new SpanData((isOut ? OutSymbol : RefSymbol) + SpaceSymbolStr, TokenType.Keyword);
                    foreach (SpanData d in EnumerateTypeName(elemType))
                        yield return d;
                    yield break;
                }
            }
            if (!type.IsGenericParameter && (type.IsGenericType || type.IsGenericTypeDefinition))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == TypeNullableValueType)
                {
                    foreach (SpanData d in EnumerateTypeName(type.GenericTypeArguments[0]))
                        yield return d;
                    yield return new SpanData(NullableSymbol, TokenType.Punctuation);
                    yield break;
                }

                string name = type.Name;
                int index = name.LastIndexOf(TypeNameGenericSeparator);
                if (index != -1)
                    name = name.Substring(0, index);
                yield return new SpanData(name, GetTypeColor(type));

                Type[] gens = type.GetGenericArguments();
                if (gens.Length > 0)
                {
                    yield return new SpanData(GenericOpenSymbol, TokenType.Punctuation);
                    for (int i = 0; i < gens.Length; i++)
                    {
                        if (i != 0)
                        {
                            yield return new SpanData(ListSeparatorSymbol, TokenType.Punctuation);
                        }

                        foreach (SpanData d in EnumerateTypeName(gens[i]))
                            yield return d;
                    }
                    yield return new SpanData(GenericCloseSymbol, TokenType.Punctuation);
                }
            }
            else
                yield return new SpanData(type.Name, GetTypeColor(type));
        }
    }
    private string GetEndTag() => _config.ColorFormatting switch
    {
        StackColorFormatType.UnityRichText => UnityEndColorSymbol,
        StackColorFormatType.Html => HtmlEndSpanSymbol,
        _ => string.Empty
    };
    private static MethodInfo? TryGetMethod(Type compGenType)
    {
        Type[] types = compGenType.Assembly.GetTypes();
        MethodInfo? method = null;
        for (int i = 0; i < types.Length; ++i)
        {
            MethodInfo[] methods = types[i].GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            for (int j = 0; j < methods.Length; ++j)
            {
                Attribute[] attrs = Attribute.GetCustomAttributes(methods[j]);
                for (int d = 0; d < attrs.Length; ++d)
                {
                    if (attrs[d] is StateMachineAttribute attr)
                    {
                        if (!_compGenMethodRepls.ContainsKey(attr.StateMachineType))
                            _compGenMethodRepls.Add(attr.StateMachineType, methods[j]);
                        if (attr.StateMachineType == compGenType)
                            method = methods[j];
                        break;
                    }
                }
            }
        }

        return method;
    }
    private bool ShouldWriteEnd(TokenType token) => _appendColor && _reqEndColor &&
                                                    token != TokenType.Space;
    private IEnumerable<SpanData> EnumerateSpans(StackTrace trace)
    {
        if (trace.GetFrames() is not { Length: > 0 } frames)
            yield break;
        bool hasSentOne = false;
        bool hasHidden = false;
        for (int f = 0; f < frames.Length; ++f)
        {
            StackFrame frame = frames[f];
            MethodBase info = frame.GetMethod();
            Type? declType = info.DeclaringType;
            bool async = false;
            bool enumerator = false;
            bool anonFunc = false;
            if (info.IsPrivate && info.Name.Equals(nameof(IEnumerator.MoveNext), StringComparison.Ordinal))
            {
                if (declType is not null)
                {
                    foreach (Type type in _config.GetHiddenTypes())
                    {
                        if (type == declType || (declType.IsGenericType && type.IsGenericTypeDefinition && declType.GetGenericTypeDefinition() == type))
                        {
                            hasHidden = true;
                            goto skip;
                        }
                    }
                    if (Attribute.IsDefined(declType, TypeCompilerGenerated))
                    {
                        if (typeof(IAsyncStateMachine).IsAssignableFrom(declType))
                        {
                            async = true;
                        }
                        else if (typeof(IEnumerator).IsAssignableFrom(declType))
                        {
                            enumerator = true;
                        }
                        else if (declType.GetInterfaces().Any(x => x.IsGenericType && x.Name.StartsWith("IAsyncEnumerator", StringComparison.Ordinal)))
                        {
                            async = true;
                            enumerator = true;
                        }
                        else goto next;

                        if (!_compGenMethodRepls.TryGetValue(declType, out MethodInfo? originalMethod))
                            originalMethod = TryGetMethod(declType);
                        if (originalMethod != null)
                            info = originalMethod;
                    }
                }
            }
            else if (declType is not null)
            {
                if (declType.IsSealed && Attribute.IsDefined(declType, TypeCompilerGenerated))
                {
                    anonFunc = true;
                }
                else
                {
                    foreach (Type type in _config.GetHiddenTypes())
                    {
                        if (type == declType || (declType.IsGenericType && type.IsGenericTypeDefinition && declType.GetGenericTypeDefinition() == type))
                        {
                            hasHidden = true;
                            goto skip;
                        }
                    }
                }
            }
            next:
            if (_writeNewline)
            {
                if (hasSentOne)
                {
                    yield return new SpanData(Environment.NewLine + AtPrefixSymbol, TokenType.FlowKeyword);
                }
                else
                {
                    yield return new SpanData(AtPrefixSymbol, TokenType.FlowKeyword);
                    hasSentOne = true;
                }
            }
            else if (_writeParaTags)
            {
                yield return new SpanData(StartParaTagSymbol, TokenType.EndTag);
                yield return new SpanData(AtPrefixSymbol, TokenType.FlowKeyword);
                hasSentOne = true;
            }
            bool isPropAccessor = false;
            MethodInfo? method2 = info as MethodInfo;
            if (!anonFunc)
            {
                if (info.IsStatic)
                    yield return new SpanData(StaticSymbol + SpaceSymbolStr, TokenType.Keyword);

                if (async)
                    yield return new SpanData(AsyncSymbol + SpaceSymbolStr, TokenType.Keyword);

                if (enumerator)
                    yield return new SpanData(EnumeratorSymbol + SpaceSymbolStr, TokenType.Keyword);

                if (info.IsSpecialName)
                {
                    if (info.Name.StartsWith("get_", StringComparison.Ordinal))
                    {
                        isPropAccessor = true;
                        yield return new SpanData(GetterSymbol + SpaceSymbolStr, TokenType.Keyword);
                    }
                    else if (info.Name.StartsWith("set_", StringComparison.Ordinal))
                    {
                        isPropAccessor = true;
                        yield return new SpanData(SetterSymbol + SpaceSymbolStr, TokenType.Keyword);
                    }
                }

                if (method2 != null)
                {
                    foreach (SpanData d in EnumerateTypeName(method2.ReturnType))
                        yield return d;
                    yield return new SpanData(SpaceSymbol, TokenType.Space);
                }
                Type? parentType = info.DeclaringType;
                bool dot = false;
                if (_config.IncludeNamespaces)
                {
                    string? ns = parentType?.Namespace;
                    if (string.IsNullOrEmpty(ns))
                    {
                        yield return new SpanData(GlobalSymbol, TokenType.Keyword);
                        yield return new SpanData(GlobalSeparatorSymbol, TokenType.Punctuation);
                    }
                    else
                    {
                        dot = true;
                        if (_config.ColorFormatting != StackColorFormatType.None)
                        {
                            int index = -1;
                            int lastIndex = -1;
                            while (true)
                            {
                                index = ns!.IndexOf(MemberSeparatorSymbol, index + 1);
                                if (index == -1) break;
                                if (lastIndex > 0)
                                {
                                    yield return new SpanData(MemberSeparatorSymbol, TokenType.Punctuation);
                                }
                                if (index >= ns.Length - 1) break;
                                yield return new SpanData(ns.Substring(lastIndex + 1, index - lastIndex - 1), TokenType.Namespace);
                                lastIndex = index;
                            }

                            if (lastIndex < ns.Length - 1)
                            {
                                if (lastIndex > 0)
                                    yield return new SpanData(MemberSeparatorSymbol, TokenType.Punctuation);
                                yield return new SpanData(ns.Substring(lastIndex + 1, ns.Length - lastIndex - 1), TokenType.Namespace);
                            }
                        }
                        else yield return new SpanData(ns!, TokenType.Space);
                    }
                }
                if (parentType != null)
                {
                    if (parentType.IsNested)
                    {
                        int c = 0;
                        Type? t = parentType;
                        for (; t != null; t = t.DeclaringType)
                            ++c;
                        do
                        {
                            Type? type = parentType;
                            for (int i = c - 1; i > 0 && type != null; --i)
                            {
                                type = type.DeclaringType;
                            }

                            if (type != null)
                            {
                                if (dot)
                                {
                                    yield return new SpanData(MemberSeparatorSymbol, TokenType.Punctuation);
                                }
                                else dot = true;
                                foreach (SpanData d in EnumerateTypeName(type))
                                    yield return d;
                            }
                            else break;
                            --c;
                        } while (c > 0);
                    }
                    else
                    {
                        if (dot)
                        {
                            yield return new SpanData(MemberSeparatorSymbol, TokenType.Punctuation);
                        }
                        foreach (SpanData d in EnumerateTypeName(parentType))
                            yield return d;
                    }
                }
                if (info is not ConstructorInfo)
                {
                    yield return new SpanData(MemberSeparatorSymbol, TokenType.Punctuation);
                    yield return new SpanData(!isPropAccessor || info.Name.Length < 5 ? info.Name : info.Name.Substring(4),
                        isPropAccessor ? TokenType.Property : TokenType.Method);

                    if (method2 != null &&
                        (method2.IsGenericMethod || method2.IsGenericMethodDefinition) &&
                        method2.GetGenericArguments() is { Length: > 0 } gens)
                    {
                        yield return new SpanData(GenericOpenSymbol, TokenType.Punctuation);
                        for (int i = 0; i < gens.Length; i++)
                        {
                            if (i != 0)
                            {
                                yield return new SpanData(ListSeparatorSymbol, TokenType.Punctuation);
                            }

                            foreach (SpanData d in EnumerateTypeName(gens[i]))
                                yield return d;
                        }
                        yield return new SpanData(GenericCloseSymbol, TokenType.Punctuation);
                    }
                }
            }
            else
            {
                yield return new SpanData(AnonymousSymbol + SpaceSymbolStr, TokenType.Keyword);
                if (method2 != null)
                {
                    foreach (SpanData d in EnumerateTypeName(method2.ReturnType))
                        yield return d;
                    yield return new SpanData(SpaceSymbol, TokenType.Space);
                }
            }

            if (!isPropAccessor)
            {
                ParameterInfo[] parameters = info.GetParameters();
                if (parameters.Length > 0)
                {
                    yield return new SpanData(ParametersOpenSymbol, TokenType.Punctuation);
                    for (int i = 0; i < parameters.Length; ++i)
                    {
                        ParameterInfo p = parameters[i];
                        if (i != 0)
                        {
                            yield return new SpanData(ListSeparatorSymbol, TokenType.Punctuation);
                        }

                        if (i == parameters.Length - 1 && p.ParameterType.IsArray && Attribute.IsDefined(p, typeof(ParamArrayAttribute)))
                        {
                            yield return new SpanData(ParamsSymbol + SpaceSymbolStr, TokenType.Keyword);
                        }

                        foreach (SpanData d in EnumerateTypeName(p.ParameterType, isOut: p.IsOut))
                            yield return d;
                        if (p.Name != null)
                            yield return new SpanData(SpaceSymbolStr + p.Name, TokenType.Parameter);
                    }
                    yield return new SpanData(ParametersCloseSymbol, TokenType.Punctuation);
                }
                else
                    yield return new SpanData(new string(ParametersOpenSymbol, 1) + ParametersCloseSymbol, TokenType.Punctuation);
            }

            if (anonFunc)
            {
                yield return new SpanData(SpaceSymbolStr + LambdaSymbol + SpaceSymbolStr, TokenType.Method);
                yield return new SpanData(MethodBodyOpenSymbol, TokenType.Punctuation);
                yield return new SpanData(SpaceSymbolStr + HiddenMethodContentSymbol + SpaceSymbolStr, TokenType.ExtraData);
                yield return new SpanData(MethodBodyCloseSymbol, TokenType.Punctuation);
            }

            string ed = string.Empty;
            if (_config.IncludeSourceData)
            {
                int ln = frame.GetFileLineNumber();
                if (ln != 0)
                    ed = SpaceSymbolStr + LineNumberPrefixSymbol + ln.ToString(_config.Locale);

                ln = frame.GetFileColumnNumber();
                if (ln != 0)
                    ed += SpaceSymbolStr + ColumnNumberPrefixSymbol + ln.ToString(_config.Locale);

                int ilOff = frame.GetILOffset();
                if (ilOff != -1)
                    ed += SpaceSymbolStr + ILOffsetPrefixSymbol + SpaceSymbolStr + ArrayOpenSymbol
                          + "0x" + ilOff.ToString("X6", _config.Locale) + ArrayCloseSymbol;

                string? file = null;
                try
                {
                    file = frame.GetFileName();
                }
                catch (Exception)
                {
                    // ignored
                }
                if (file != null)
                {
                    ed += SpaceSymbolStr + FilePrefixSymbol + DoubleQuotationMarkSymbol
                          + Path.Combine(Path.GetFileName(Path.GetDirectoryName(file)) ?? RootDirectorySymbol, Path.GetFileName(file)) +
                          DoubleQuotationMarkSymbol;
                }
                if (ed.Length > 0)
                {
                    if (_writeParaTags && _config.PutSourceDataOnNewLine)
                    {
                        yield return new SpanData(EndParaTagSymbol, TokenType.EndTag);
                        yield return new SpanData(StartParaTagSymbol, TokenType.EndTag);
                    }
                    yield return new SpanData((_config.PutSourceDataOnNewLine ? (_writeParaTags || !_writeNewline ? string.Empty : Environment.NewLine) : SpaceSymbolStr) + ed, TokenType.ExtraData);
                }
            }
            if (_writeParaTags)
                yield return new SpanData(EndParaTagSymbol, TokenType.EndTag);
            skip: ;
        }
        if (hasHidden && _config.WarnForHiddenLines)
        {
            if (_writeParaTags)
                yield return new SpanData(StartParaTagSymbol, TokenType.EndTag);
            yield return new SpanData((_writeNewline ? Environment.NewLine : string.Empty) + HiddenLineWarning, TokenType.LinesHiddenWarning);
            if (_writeParaTags)
                yield return new SpanData(EndParaTagSymbol, TokenType.EndTag);
        }
    }
    private readonly struct SpanData
    {
        public readonly string? Text;
        public readonly char Char;
        public readonly TokenType Color;
        internal SpanData(string text, TokenType color)
        {
            Text = text;
            Color = color;
            Char = default;
        }
        internal SpanData(char text, TokenType color)
        {
            Text = null;
            Color = color;
            Char = text;
        }
    }

    private enum TokenType : byte
    {
        Space = 0,
        Keyword,
        Method,
        Property,
        Parameter,
        Class,
        Struct,
        FlowKeyword,
        Interface,
        GenericParameter,
        Enum,
        Namespace,
        Punctuation,
        ExtraData,
        LinesHiddenWarning,
        EndTag
    }
}