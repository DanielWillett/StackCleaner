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
    private const string HtmlEndSpanSymbol = "</span>";
    private const string GlobalSymbol = "global";
    private const string AtPrefixSymbol = " at ";
    private const string LineNumberPrefixSymbol = "LN #";
    private const string ColumnNumberPrefixSymbol = "COL #";
    private const string ILOffsetPrefixSymbol = "IL";
    private const string FilePrefixSymbol = "FILE: ";
    private const string HiddenLineWarning = "Some lines hidden for readability.";
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
    /// <summary>
    /// Default implementation of <see cref="StackTraceCleaner"/>.
    /// </summary>
    public static StackTraceCleaner Default => _instance ??= new StackTraceCleaner();

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
            or StackColorFormatType.ExtendedANSIColor;
        _reqEndColor = _appendColor &&
                       config.ColorFormatting is StackColorFormatType.UnityRichText
                           or StackColorFormatType.TextMeshProRichText;
    }
    public static StackTrace? GetStackTrace(Exception ex) => ex.StackTrace != null ? new StackTrace(ex, true) : null;
    public string GetString(StackTrace stackTrace)
    {
        if (stackTrace == null)
            throw new ArgumentNullException(nameof(stackTrace));

        using MemoryStream stream = new MemoryStream(256);
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
    public Task WriteToStreamAsync(Stream stream, StackTrace stackTrace, Encoding? encoding = null)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        if (stackTrace == null)
            throw new ArgumentNullException(nameof(stackTrace));
        if (!stream.CanWrite)
            throw new ArgumentException("Stream must be able to write.", nameof(stream));

        encoding ??= Encoding.UTF8;
        TextWriter writer = new StreamWriter(stream, encoding, 256, true);
        return WriteToTextWriterIntlAsync(stackTrace, writer);
    }
    public void WriteToConsole(StackTrace stackTrace)
    {
        if (stackTrace == null)
            throw new ArgumentNullException(nameof(stackTrace));

        if (_config.ColorFormatting is StackColorFormatType.None or StackColorFormatType.ConsoleColor)
        {
            TokenType currentColor = (TokenType)255;
            ConsoleColor old = Console.ForegroundColor;
            foreach (SpanData span in EnumerateSpans(stackTrace))
            {
                if (_config.ColorFormatting != StackColorFormatType.None && span.Color != TokenType.Space && currentColor != span.Color)
                {
                    Console.ForegroundColor = (ConsoleColor)(GetColor(span.Color) - 1);
                    currentColor = span.Color;
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
        else WriteToTextWriterIntl(stackTrace, Console.Out);
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
        byte r = (byte)(argb << 16);
        byte g = (byte)(argb << 8);
        byte b = (byte)argb;
        int l = 9 + (r > 9 ? r > 99 ? 3 : 2 : 1) + (g > 9 ? g > 99 ? 3 : 2 : 1) + (b > 9 ? b > 99 ? 3 : 2 : 1); 
        char* chrs = stackalloc char[l];
        chrs[0] = ConsoleEscapeCharacter;
        chrs[1] = '[';
        chrs[2] = '3';
        chrs[3] = '8';
        chrs[4] = ';';
        int index = 3;
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
        return new string(chrs, 0, 15);
    }
    private static unsafe string GetUnityString(int argb)
    {
        char* chrs = stackalloc char[15];
        chrs[0] = '<'; chrs[1] = 'c'; chrs[2] = 'o'; chrs[3] = 'l'; chrs[4] = 'o'; chrs[5] = 'r'; chrs[6] = '='; chrs[7] = '#';
        byte d = (byte)((argb << 16) & 15);
        chrs[8] = (char)(d > 9 ? d + 87 : d + 48);
        d = (byte)((argb << 20) & 15);
        chrs[9] = (char)(d > 9 ? d + 87 : d + 48);
        d = (byte)((argb << 8) & 15);
        chrs[10] = (char)(d > 9 ? d + 87 : d + 48);
        d = (byte)((argb << 12) & 15);
        chrs[11] = (char)(d > 9 ? d + 87 : d + 48);
        d = (byte)(argb & 15);
        chrs[12] = (char)(d > 9 ? d + 87 : d + 48);
        d = (byte)((argb << 4) & 15);
        chrs[13] = (char)(d > 9 ? d + 87 : d + 48);
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
            return "<span class=\"" + classname + "\">";
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
        byte d = (byte)((argb << 16) & 15);
        chrs[20] = (char)(d > 9 ? d + 87 : d + 48);
        d = (byte)((argb << 20) & 15);
        chrs[21] = (char)(d > 9 ? d + 87 : d + 48);
        d = (byte)((argb << 8) & 15);
        chrs[22] = (char)(d > 9 ? d + 87 : d + 48);
        d = (byte)((argb << 12) & 15);
        chrs[23] = (char)(d > 9 ? d + 87 : d + 48);
        d = (byte)(argb & 15);
        chrs[24] = (char)(d > 9 ? d + 87 : d + 48);
        d = (byte)((argb << 4) & 15);
        chrs[25] = (char)(d > 9 ? d + 87 : d + 48);
        chrs[26] = ';'; chrs[27] = '"'; chrs[28] = '>';
        return new string(chrs, 0, 15);
    }
    private static unsafe string GetTMProString(int argb)
    {
        char* chrs = stackalloc char[9];
        chrs[0] = '<'; chrs[1] = '#';
        byte d = (byte)((argb << 16) & 15);
        chrs[2] = (char)(d > 9 ? d + 87 : d + 48);
        d = (byte)((argb << 20) & 15);
        chrs[3] = (char)(d > 9 ? d + 87 : d + 48);
        d = (byte)((argb << 8) & 15);
        chrs[4] = (char)(d > 9 ? d + 87 : d + 48);
        d = (byte)((argb << 12) & 15);
        chrs[5] = (char)(d > 9 ? d + 87 : d + 48);
        d = (byte)(argb & 15);
        chrs[6] = (char)(d > 9 ? d + 87 : d + 48);
        d = (byte)((argb << 4) & 15);
        chrs[7] = (char)(d > 9 ? d + 87 : d + 48);
        chrs[8] = '>';
        return new string(chrs, 0, 9);
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
                    yield return new SpanData((isOut ? OutSymbol : RefSymbol) + SpaceSymbolStr, TokenType.Punctuation);
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
    private void WriteToTextWriterIntl(StackTrace trace, TextWriter writer)
    {
        TokenType currentColor = (TokenType)255;
        foreach (SpanData span in EnumerateSpans(trace))
        {
            if (_appendColor && span.Color != TokenType.Space && currentColor != span.Color)
                writer.Write(GetColorString(span.Color));

            if (span.Text != null)
                writer.Write(span.Text);
            else
                writer.Write(span.Char);

            if (_appendColor && _reqEndColor &&
                _config.ColorFormatting is StackColorFormatType.UnityRichText or StackColorFormatType.Html &&
                span.Color != TokenType.Space && currentColor != span.Color)
                writer.Write(_config.ColorFormatting == StackColorFormatType.UnityRichText ? UnityEndColorSymbol : HtmlEndSpanSymbol);
            currentColor = span.Color;
        }

        if (_config.ColorFormatting is StackColorFormatType.ANSIColor or StackColorFormatType.ExtendedANSIColor)
            writer.Write(GetANSIResetString());
        writer.WriteLine();
    }
    private async Task WriteToTextWriterIntlAsync(StackTrace trace, TextWriter writer, CancellationToken token = default, bool dispose = true)
    {
        token.ThrowIfCancellationRequested();
        TokenType currentColor = (TokenType)255;
        foreach (SpanData span in EnumerateSpans(trace))
        {
            if (_appendColor && span.Color != TokenType.Space && currentColor != span.Color)
                await writer.WriteAsync(GetColorString(span.Color)).ConfigureAwait(false);

            if (span.Text != null)
                await writer.WriteAsync(span.Text).ConfigureAwait(false);
            else
                await writer.WriteAsync(span.Char).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();

            if (_appendColor && _reqEndColor &&
                _config.ColorFormatting is StackColorFormatType.UnityRichText or StackColorFormatType.Html &&
                span.Color != TokenType.Space && currentColor != span.Color)
                await writer.WriteAsync(_config.ColorFormatting == StackColorFormatType.UnityRichText ? UnityEndColorSymbol : HtmlEndSpanSymbol).ConfigureAwait(false);
            currentColor = span.Color;
        }
        if (_config.ColorFormatting is StackColorFormatType.ANSIColor or StackColorFormatType.ExtendedANSIColor)
            await writer.WriteAsync(GetANSIResetString()).ConfigureAwait(false);
        await writer.WriteLineAsync().ConfigureAwait(false);
        if (dispose)
            writer.Dispose();
    }
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
            if (hasSentOne)
                yield return new SpanData(Environment.NewLine + AtPrefixSymbol, TokenType.FlowKeyword);
            else
            {
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
                            int lastIndex = 0;
                            while (true)
                            {
                                index = ns!.IndexOf(MemberSeparatorSymbol, index + 1);
                                if (index == -1) break;
                                if (lastIndex > 0)
                                {
                                    yield return new SpanData(MemberSeparatorSymbol, TokenType.Punctuation);
                                }
                                if (index >= ns.Length - 1) break;
                                yield return new SpanData(ns.Substring(lastIndex, index - lastIndex - 1), TokenType.Namespace);
                                lastIndex = index;
                            }

                            if (lastIndex < ns.Length - 1)
                            {
                                if (lastIndex > 0)
                                    yield return new SpanData(MemberSeparatorSymbol, TokenType.Punctuation);
                                yield return new SpanData(ns.Substring(lastIndex, ns.Length - lastIndex - 1), TokenType.Namespace);
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
            if (_config.IncludeSourceData)
            {
                bool anyExtra = false;
                int ln = frame.GetFileLineNumber();
                if (ln != 0)
                {
                    if (!anyExtra)
                    {
                        if (_config.PutSourceDataOnNewLine)
                            yield return new SpanData(Environment.NewLine, TokenType.Space);
                        else
                            yield return new SpanData(SpaceSymbol, TokenType.Space);
                        anyExtra = true;
                    }

                    yield return new SpanData(SpaceSymbolStr + LineNumberPrefixSymbol + ln.ToString(_config.Locale), TokenType.ExtraData);
                }
                ln = frame.GetFileColumnNumber();
                if (ln != 0)
                {
                    if (!anyExtra)
                    {
                        if (_config.PutSourceDataOnNewLine)
                            yield return new SpanData(Environment.NewLine, TokenType.Space);
                        else
                            yield return new SpanData(SpaceSymbol, TokenType.Space);
                        anyExtra = true;
                    }
                    yield return new SpanData(SpaceSymbolStr + ColumnNumberPrefixSymbol + ln.ToString(_config.Locale), TokenType.ExtraData);
                }

                int ilOff = frame.GetILOffset();
                if (ilOff != -1)
                {
                    if (!anyExtra)
                    {
                        if (_config.PutSourceDataOnNewLine)
                            yield return new SpanData(Environment.NewLine, TokenType.Space);
                        else
                            yield return new SpanData(SpaceSymbol, TokenType.Space);
                        anyExtra = true;
                    }
                    yield return new SpanData(SpaceSymbolStr + ILOffsetPrefixSymbol + SpaceSymbolStr + ArrayOpenSymbol
                     + "0x" + ilOff.ToString("X6", _config.Locale) + ArrayCloseSymbol, TokenType.ExtraData);
                }

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
                    if (!anyExtra)
                    {
                        if (_config.PutSourceDataOnNewLine)
                            yield return new SpanData(Environment.NewLine, 0);
                        else
                            yield return new SpanData(SpaceSymbol, 0);
                    }
                    yield return new SpanData(SpaceSymbolStr + FilePrefixSymbol + DoubleQuotationMarkSymbol
                                              + Path.Combine(Path.GetFileName(Path.GetDirectoryName(file)) ?? RootDirectorySymbol, Path.GetFileName(file)) +
                                              DoubleQuotationMarkSymbol, TokenType.ExtraData);
                }
            }
            skip: ;
        }
        if (hasHidden && _config.WarnForHiddenLines)
            yield return new SpanData(Environment.NewLine + HiddenLineWarning, TokenType.LinesHiddenWarning);
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
    }
}