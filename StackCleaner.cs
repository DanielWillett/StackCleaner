using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StackCleaner;
/// <summary>
/// Tool that clears up stack traces to make them much more readable during debugging.<br/>
/// Supports highly customizable color formatting in the following formats:<br/>
/// <br/>
/// • <see cref="ConsoleColor"/><br/>
/// • ANSI color codes (4-bit)<br/>
/// • Extended ANSI color codes (32-bit where supported)<br/>
/// • Unity Rich Text<br/>
/// • Unity TextMeshPro Rich Text<br/>
/// • Html (with tags)<br/>
/// </summary>
public class StackTraceCleaner
{
    private static bool IsMono { get; } = Type.GetType("Mono.Runtime") != null;

    // mono uses a different method to store remote stack traces than dotnet,
    // which don't get displayed on the stack trace.
    // this means any asnyc stack traces would not show any before the last context change
    // (this fixes that)
    private static Func<Exception, StackTrace[]>? MonoCapturedTracesAccessor
    {
        get
        {
            if (!hasCheckedRemoteAccessor)
            {
                hasCheckedRemoteAccessor = true;
                try
                {
                    // generate a getter function for the mono field Exception.captured_traces.
                    FieldInfo? field = typeof(Exception).GetField("captured_traces", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null)
                    {
                        DynamicMethod method = new DynamicMethod("get_captured_traces",
                            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                            CallingConventions.HasThis,
                            typeof(StackTrace[]), new Type[] { typeof(Exception) },
                            typeof(StackTraceCleaner), true);
                        ILGenerator il = method.GetILGenerator();
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldfld, field);
                        il.Emit(OpCodes.Ret);
                        _getMonoRemoteAccessor = (Func<Exception, StackTrace[]>)method.CreateDelegate(typeof(Func<Exception, StackTrace[]>));
                    }
                }
                catch
                {
                    // ignored
                }
            }
            return _getMonoRemoteAccessor;
        }
    }
    private static bool hasCheckedRemoteAccessor;
    private static Func<Exception, StackTrace[]>? _getMonoRemoteAccessor;
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
    private const string StartSpanTagStyleClassP1 = "<span class=\"";
    private const string StartSpanTagStyleClassP2 = "\">";
    private const string OuterStartHtmlTagStyleClass = "<div class=\"" + BackgroundClassName + "\">";
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
    /// <summary>Html class for the background div.</summary>
    public const string BackgroundClassName = "st_bkgr";
    /// <summary>Html class for keywords.</summary>
    public const string KeywordClassName = "st_keyword";
    /// <summary>Html class for methods.</summary>
    public const string MethodClassName = "st_method";
    /// <summary>Html class for properties.</summary>
    public const string PropertyClassName = "st_property";
    /// <summary>Html class for parameters.</summary>
    public const string ParameterClassName = "st_parameter";
    /// <summary>Html class for classes.</summary>
    public const string ClassClassName = "st_class";
    /// <summary>Html class for structs.</summary>
    public const string StructClassName = "st_struct";
    /// <summary>Html class for flow keywords.</summary>
    public const string FlowKeywordClassName = "st_flow_keyword";
    /// <summary>Html class for interface.</summary>
    public const string InterfaceClassName = "st_interface";
    /// <summary>Html class for generic parameters.</summary>
    public const string GenericParameterClassName = "st_generic_parameter";
    /// <summary>Html class for enums.</summary>
    public const string EnumClassName = "st_enum";
    /// <summary>Html class for namespaces.</summary>
    public const string NamespaceClassName = "st_namespace";
    /// <summary>Html class for punctuation.</summary>
    public const string PunctuationClassName = "st_punctuation";
    /// <summary>Html class for extra data (source data).</summary>
    public const string ExtraDataClassName = "st_extra_data";
    /// <summary>Html class for the lines hidden warning.</summary>
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
    private static readonly Type TypeStateMachineBase = typeof(StateMachineAttribute);
    private static readonly Dictionary<Type, MethodInfo?> CompilerGeneratedStateMachineSourceCache = new Dictionary<Type, MethodInfo?>(64);

    // types that are hidden by default. These are all the types used by the Task internal.
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
    private readonly bool _isArgbColor;
    private readonly bool _appendColor;
    private readonly bool _reqEndColor;
    private readonly bool _writeNewline;
    private readonly bool _writeParaTags;
    private readonly int _defBufferSizeMult;

    /// <summary>
    /// Default implementation of <see cref="StackTraceCleaner"/>.
    /// </summary>
    public static StackTraceCleaner Default => _instance ??= new StackTraceCleaner();

    /// <summary>Active configuration instance being used by this <see cref="StackTraceCleaner"/>.</summary>
    /// <remarks>
    /// This value and all it's properties are <see langword="readonly"/>. Trying to modify them will throw a <see cref="NotSupportedException"/>.
    /// </remarks>>
    public StackCleanerConfiguration Configuration => _config;

    /// <summary>
    /// Use <see cref="Default"/> to get a default implementation.
    /// </summary>
    private StackTraceCleaner() : this(StackCleanerConfiguration.Default) { }
    /// <summary>
    /// Load a new <see cref="StackTraceCleaner"/> with the specified <paramref name="config"/>.
    /// </summary>
    public StackTraceCleaner(StackCleanerConfiguration config)
    {
        // freeze config
        if (config.ColorFormatting == StackColorFormatType.ExtendedANSIColor && config.Colors is Color4Config)
            config.ColorFormatting = StackColorFormatType.ANSIColor;
        config.Frozen = true;
        config.Colors!.Frozen = true;
        _config = config;

        // are colors encoded as argb
        _isArgbColor = config.Colors is not Color4Config;

        // are colors added as text
        _appendColor = config.ColorFormatting
            is StackColorFormatType.UnityRichText
            or StackColorFormatType.TextMeshProRichText
            or StackColorFormatType.ANSIColor
            or StackColorFormatType.ExtendedANSIColor
            or StackColorFormatType.Html;

        // are end tags added to color spans
        _reqEndColor = _appendColor &&
                       config.ColorFormatting is StackColorFormatType.UnityRichText or StackColorFormatType.Html;

        // are newlines added
        _writeNewline = config.ColorFormatting != StackColorFormatType.Html;

        // are paragraph tags added
        _writeParaTags = config.ColorFormatting == StackColorFormatType.Html;
        
        // estimated buffer size per frame
        _defBufferSizeMult = _config.ColorFormatting is StackColorFormatType.None
            or StackColorFormatType.ConsoleColor
            ? 192
            : _config.ColorFormatting == StackColorFormatType.ANSIColor ? 384 : 768;
    }

    /// <summary>
    /// Conerts an <see cref="Exception"/> to a stack trace, just calls <see cref="StackTrace(Exception, bool)"/> if a stack trace has been added.
    /// </summary>
    /// <remarks><see cref="Exception.StackTrace"/> only gets set after calling <see langword="throw"/> on the <see cref="Exception"/>.</remarks>
    /// <param name="fetchSourceInfo">Whether or not to capture the file name, line number, and column number of the exception. 
    /// If this isn't needed it's best to set it to false to save computing time.</param>
    /// <param name="ex">Exception to fetch <see cref="StackTrace"/> from. This will only work after calling <see langword="throw"/> on it.</param>
    /// <returns>A <see cref="StackTrace"/> representing the source of the exception if present, otherwise <see langword="null"/>.</returns>
    public static StackTrace? GetStackTrace(Exception ex, bool fetchSourceInfo = true) => ex.StackTrace != null ? new StackTrace(ex, fetchSourceInfo) : null;

    /// <summary>
    /// Formats the <paramref name="exception"/>'s stack (and it's remote stacks when applicable) and returns it as a <see cref="string"/> using the runtime's default encoding.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    public string GetString(Exception exception)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));
        StackTrace stackTrace = new StackTrace(exception, _config.IncludeSourceData);
        Encoding encoding = Encoding.Default;
        using MemoryStream stream = new MemoryStream(encoding.GetMaxByteCount(_defBufferSizeMult * stackTrace.FrameCount));
        //stream.Position = 0;
        using TextWriter writer = new StreamWriter(stream, encoding, _defBufferSizeMult * stackTrace.FrameCount, true);
        StackTrace[]? remoteTraces = IsMono ? MonoCapturedTracesAccessor?.Invoke(exception) : null;
        if (remoteTraces is { Length: > 0 })
        {
            for (int i = 0; i < remoteTraces.Length; ++i)
            {
                WriteToTextWriterIntl(remoteTraces[i], writer, false);
            }
        }
        WriteToTextWriterIntl(stackTrace, writer, true);
        writer.Flush();
        byte[] bytes = stream.GetBuffer();
        return encoding.GetString(bytes, 0, (int)stream.Length);
    }
    /// <summary>
    /// Formats the <paramref name="stackTrace"/> and returns it as a <see cref="string"/> using the runtime's default encoding.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    public string GetString(StackTrace stackTrace)
    {
        if (stackTrace == null)
            throw new ArgumentNullException(nameof(stackTrace));

        Encoding encoding = Encoding.Default;
        using MemoryStream stream = new MemoryStream(encoding.GetMaxByteCount(_defBufferSizeMult * stackTrace.FrameCount));
        stream.Position = 0;
        using TextWriter writer = new StreamWriter(stream, encoding, _defBufferSizeMult * stackTrace.FrameCount, true);
        WriteToTextWriterIntl(stackTrace, writer);
        writer.Flush();
        byte[] bytes = stream.GetBuffer();
        return encoding.GetString(bytes, 0, (int)stream.Length);
    }

    /// <summary>
    /// Formats the <paramref name="stackTrace"/> and writes it to <paramref name="stream"/> using <paramref name="encoding"/> to encode it.
    /// </summary>
    /// <remarks>If <paramref name="encoding"/> is <see langword="null"/>, it's set to <see cref="Encoding.Default"/> instead.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"><paramref name="stream"/> is unable to be written to.</exception>
    public void WriteToStream(Stream stream, StackTrace stackTrace, Encoding? encoding = null)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        if (stackTrace == null)
            throw new ArgumentNullException(nameof(stackTrace));
        if (!stream.CanWrite)
            throw new ArgumentException("Stream must be able to write.", nameof(stream));

        encoding ??= Encoding.Default;
        using TextWriter writer = new StreamWriter(stream, encoding, _defBufferSizeMult * stackTrace.FrameCount, true);
        WriteToTextWriterIntl(stackTrace, writer);
    }
    /// <summary>
    /// Formats the <paramref name="exception"/>'s stack (and it's remote stacks when applicable) and writes it to <paramref name="stream"/> using <paramref name="encoding"/> to encode it.
    /// </summary>
    /// <remarks>If <paramref name="encoding"/> is <see langword="null"/>, it's set to <see cref="Encoding.Default"/> instead.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"><paramref name="stream"/> is unable to be written to.</exception>
    public void WriteToStream(Stream stream, Exception exception, Encoding? encoding = null)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));
        if (!stream.CanWrite)
            throw new ArgumentException("Stream must be able to write.", nameof(stream));

        encoding ??= Encoding.Default;
        StackTrace stackTrace = new StackTrace(exception, _config.IncludeSourceData);
        using TextWriter writer = new StreamWriter(stream, encoding, _defBufferSizeMult * stackTrace.FrameCount, true);
        StackTrace[]? remoteTraces = IsMono ? MonoCapturedTracesAccessor?.Invoke(exception) : null;
        if (remoteTraces is { Length: > 0 })
        {
            for (int i = 0; i < remoteTraces.Length; ++i)
            {
                WriteToTextWriterIntl(remoteTraces[i], writer, false);
            }
        }
        WriteToTextWriterIntl(stackTrace, writer, true);
        writer.Flush();
    }

    /// <summary>
    /// Formats the <paramref name="stackTrace"/> and writes it to a file at <paramref name="path"/> using <paramref name="encoding"/> to encode it.<br/>
    /// If the file exists, it'll be overwritten, otherwise it'll be created.
    /// </summary>
    /// <remarks>If <paramref name="encoding"/> is <see langword="null"/>, it's set to <see cref="Encoding.Default"/> instead.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="NotSupportedException"><paramref name="path"/> is not a valid writable file.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is not a valid writable file.</exception>
    /// <exception cref="System.Security.SecurityException">Missing file access.</exception>
    /// <exception cref="UnauthorizedAccessException">Missing file write access.</exception>
    public void WriteToFile(string path, StackTrace stackTrace, Encoding? encoding = null)
    {
        if (path == null)
            throw new ArgumentNullException(nameof(path));
        if (stackTrace == null)
            throw new ArgumentNullException(nameof(stackTrace));


        encoding ??= Encoding.Default;
        using FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, encoding.GetMaxByteCount(_defBufferSizeMult * stackTrace.FrameCount));
        WriteToStream(stream, stackTrace, encoding);
    }

    /// <summary>
    /// Formats the <paramref name="exception"/>'s stack (and it's remote stacks when applicable) and writes it to a file at <paramref name="path"/> using <paramref name="encoding"/> to encode it.<br/>
    /// If the file exists, it'll be overwritten, otherwise it'll be created.
    /// </summary>
    /// <remarks>If <paramref name="encoding"/> is <see langword="null"/>, it's set to <see cref="Encoding.Default"/> instead.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="NotSupportedException"><paramref name="path"/> is not a valid writable file.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is not a valid writable file.</exception>
    /// <exception cref="System.Security.SecurityException">Missing file access.</exception>
    /// <exception cref="UnauthorizedAccessException">Missing file write access.</exception>
    public void WriteToFile(string path, Exception exception, Encoding? encoding = null)
    {
        if (path == null)
            throw new ArgumentNullException(nameof(path));
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));


        encoding ??= Encoding.Default;
        using FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, encoding.GetMaxByteCount(_defBufferSizeMult * 8));
        WriteToStream(stream, exception, encoding);
    }

    /// <summary>
    /// Formats the <paramref name="stackTrace"/> and writes it to <paramref name="stream"/> asynchronously using <paramref name="encoding"/> to encode it.
    /// </summary>
    /// <remarks>If <paramref name="encoding"/> is <see langword="null"/>, it's set to <see cref="Encoding.Default"/> instead.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"><paramref name="stream"/> is unable to be written to.</exception>
    public async Task WriteToStreamAsync(Stream stream, StackTrace stackTrace, Encoding? encoding = null, CancellationToken token = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        if (stackTrace == null)
            throw new ArgumentNullException(nameof(stackTrace));
        if (!stream.CanWrite)
            throw new ArgumentException("Stream must be able to write.", nameof(stream));

        encoding ??= Encoding.UTF8;
        using TextWriter writer = new StreamWriter(stream, encoding, _defBufferSizeMult * stackTrace.FrameCount, true);
        await WriteToTextWriterIntlAsync(stackTrace, writer, true, token).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }
    /// <summary>
    /// Formats the <paramref name="exception"/>'s stack (and it's remote stacks when applicable) and writes it to <paramref name="stream"/> asynchronously using <paramref name="encoding"/> to encode it.
    /// </summary>
    /// <remarks>If <paramref name="encoding"/> is <see langword="null"/>, it's set to <see cref="Encoding.Default"/> instead.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"><paramref name="stream"/> is unable to be written to.</exception>
    public async Task WriteToStreamAsync(Stream stream, Exception exception, Encoding? encoding = null, CancellationToken token = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));
        if (!stream.CanWrite)
            throw new ArgumentException("Stream must be able to write.", nameof(stream));

        encoding ??= Encoding.UTF8;
        StackTrace stackTrace = new StackTrace(exception, _config.IncludeSourceData);
        using TextWriter writer = new StreamWriter(stream, encoding, _defBufferSizeMult * stackTrace.FrameCount, true);
        StackTrace[]? remoteTraces = IsMono ? MonoCapturedTracesAccessor?.Invoke(exception) : null;
        if (remoteTraces is { Length: > 0 })
        {
            for (int i = 0; i < remoteTraces.Length; ++i)
            {
                await WriteToTextWriterIntlAsync(remoteTraces[i], writer, false, token).ConfigureAwait(false);
            }
        }
        await WriteToTextWriterIntlAsync(stackTrace, writer, true, token).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
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
            foreach (SpanData span in EnumerateSpans(stackTrace, true))
            {
                if (_config.ColorFormatting != StackColorFormatType.None && span.Color != TokenType.Space)
                {
                    ConsoleColor old2 = currentColor;
                    currentColor = _isArgbColor ? Color4Config.ToConsoleColor(GetColor(span.Color)) : (ConsoleColor)(GetColor(span.Color) - 1);
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
        else
        {
            TextWriter cout = Console.Out;
            WriteToTextWriterIntl(stackTrace, cout);
            cout.Flush();
        }
    }

    /// <summary>
    /// Output the <paramref name="exception"/>'s stack (and it's remote stacks when applicable) to <see cref="Console"/> using the appropriate color format.
    /// </summary>
    /// <param name="exception">Exception to write.</param>
    /// <param name="writeToConsoleBuffer">Only set this to <see langword="true"/> if memory is a huge concern, writing to the console per span takes significantly (~5x) longer than writing to a memory buffer then writing the entire buffer to the console at once.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void WriteToConsole(Exception exception, bool writeToConsoleBuffer = false)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        if (_config.ColorFormatting is StackColorFormatType.ConsoleColor)
        {
            StackTrace[]? remoteTraces = IsMono ? MonoCapturedTracesAccessor?.Invoke(exception) : null;
            int c = remoteTraces == null ? 0 : remoteTraces.Length;
            for (int i = 0; i <= c; ++i)
            {
                StackTrace trace = i == c ? new StackTrace(exception, _config.IncludeSourceData) : remoteTraces![i];
                ConsoleColor currentColor = (ConsoleColor)255;
                ConsoleColor old = Console.ForegroundColor;
                foreach (SpanData span in EnumerateSpans(trace, true))
                {
                    if (_config.ColorFormatting != StackColorFormatType.None && span.Color != TokenType.Space)
                    {
                        ConsoleColor old2 = currentColor;
                        currentColor = _isArgbColor ? Color4Config.ToConsoleColor(GetColor(span.Color)) : (ConsoleColor)(GetColor(span.Color) - 1);
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
        }
        else if (!writeToConsoleBuffer) Console.WriteLine(GetString(exception));
        else
        {
            TextWriter cout = Console.Out;
            WriteToTextWriter(exception, cout);
            cout.Flush();
        }
    }

    /// <summary>
    /// Formats the <paramref name="stackTrace"/> and writes it to <paramref name="writer"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    public void WriteToTextWriter(StackTrace stackTrace, TextWriter writer)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));
        if (stackTrace == null)
            throw new ArgumentNullException(nameof(stackTrace));

        WriteToTextWriterIntl(stackTrace, writer);
        writer.Flush();
    }
    /// <summary>
    /// Formats the <paramref name="exception"/>'s stack (and it's remote stacks when applicable) and writes it to <paramref name="writer"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    public void WriteToTextWriter(Exception exception, TextWriter writer)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));
        StackTrace stackTrace = new StackTrace(exception, _config.IncludeSourceData);
        StackTrace[]? remoteTraces = IsMono ? MonoCapturedTracesAccessor?.Invoke(exception) : null;
        if (remoteTraces is { Length: > 0 })
        {
            for (int i = 0; i < remoteTraces.Length; ++i)
            {
                WriteToTextWriterIntl(remoteTraces[i], writer, false);
            }
        }
        WriteToTextWriterIntl(stackTrace, writer, true);
        writer.Flush();
    }

    /// <summary>
    /// Formats the <paramref name="stackTrace"/> and writes it to <paramref name="writer"/> asynchronously.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    public async Task WriteToTextWriterAsync(StackTrace stackTrace, TextWriter writer, CancellationToken token = default)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));
        if (stackTrace == null)
            throw new ArgumentNullException(nameof(stackTrace));

        await WriteToTextWriterIntlAsync(stackTrace, writer, true, token).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Formats the <paramref name="exception"/>'s stack (and it's remote stacks when applicable) and writes it to <paramref name="writer"/> asynchronously.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    public async Task WriteToTextWriterAsync(Exception exception, TextWriter writer, CancellationToken token = default)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        StackTrace stackTrace = new StackTrace(exception, _config.IncludeSourceData);
        StackTrace[]? remoteTraces = IsMono ? MonoCapturedTracesAccessor?.Invoke(exception) : null;
        if (remoteTraces is { Length: > 0 })
        {
            for (int i = 0; i < remoteTraces.Length; ++i)
            {
                await WriteToTextWriterIntlAsync(remoteTraces[i], writer, false, token).ConfigureAwait(false);
            }
        }
        await WriteToTextWriterIntlAsync(stackTrace, writer, true, token).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Formats the <paramref name="trace"/> and writes it to <paramref name="writer"/>.
    /// </summary>
    private void WriteToTextWriterIntl(StackTrace trace, TextWriter writer, bool warnIfApplicable = true)
    {
        TokenType currentColor = (TokenType)255;
        bool div = false;
        foreach (SpanData span in EnumerateSpans(trace, warnIfApplicable))
        {
            if (!div && _writeParaTags && _config.HtmlWriteOuterDiv)
            {
                // write the outer html tag if needed
                writer.Write(GetDivTag());
                div = true;
            }
            if (currentColor != span.Color)
            {
                // end last color if needed
                if (currentColor != span.Color && (int)currentColor != 255 && ShouldWriteEnd(span.Color))
                    writer.Write(GetEndTag());

                // start current color                space is ignored   end tag is ignored but still ends as to not overlap html tags
                if (_appendColor && span.Color is not TokenType.Space or TokenType.EndTag && currentColor != span.Color)
                {
                    writer.Write(GetColorString(span.Color));
                    currentColor = span.Color;
                }
            }

            // write string or char for span
            if (span.Text != null)
                writer.Write(span.Text);
            else
                writer.Write(span.Char);
        }

        // end last color
        if ((int)currentColor != 255 && ShouldWriteEnd(currentColor))
            writer.Write(_config.ColorFormatting == StackColorFormatType.UnityRichText ? UnityEndColorSymbol : HtmlEndSpanSymbol);
        
        // end outer html div
        if (div)
            writer.Write(OuterEndHtmlTagSymbol);

        // reset console color
        if (_config.ColorFormatting is StackColorFormatType.ANSIColor or StackColorFormatType.ExtendedANSIColor)
            writer.Write(GetANSIResetString());

        // end line
        if (_writeNewline)
            writer.Write(Environment.NewLine);
    }

    /// <summary>
    /// Formats the <paramref name="trace"/> and writes it to <paramref name="writer"/> asynchronously.
    /// </summary>
    private async Task WriteToTextWriterIntlAsync(StackTrace trace, TextWriter writer, bool warnIfApplicable = true, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        TokenType currentColor = (TokenType)255;
        bool div = false;
        foreach (SpanData span in EnumerateSpans(trace, warnIfApplicable))
        {
            if (!div && _writeParaTags && _config.HtmlWriteOuterDiv)
            {
                // write the outer html tag if needed
                await writer.WriteAsync(GetDivTag()).ConfigureAwait(false);
                div = true;
                token.ThrowIfCancellationRequested();
            }
            if (currentColor != span.Color)
            {
                // end last color if needed
                if (currentColor != span.Color && (int)currentColor != 255 && ShouldWriteEnd(span.Color))
                {
                    await writer.WriteAsync(GetEndTag()).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                }

                // start current color                space is ignored   end tag is ignored but still ends as to not overlap html tags
                if (_appendColor && span.Color is not TokenType.Space or TokenType.EndTag && currentColor != span.Color)
                {
                    await writer.WriteAsync(GetColorString(span.Color)).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    currentColor = span.Color;
                }
            }

            // write string or char for span
            if (span.Text != null)
                await writer.WriteAsync(span.Text).ConfigureAwait(false);
            else
                await writer.WriteAsync(span.Char).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
        }

        // end last color
        if ((int)currentColor != 255 && ShouldWriteEnd(currentColor))
            await writer.WriteAsync(_config.ColorFormatting == StackColorFormatType.UnityRichText ? UnityEndColorSymbol : HtmlEndSpanSymbol).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();

        // end outer html div
        if (div)
            writer.Write(OuterEndHtmlTagSymbol);

        // reset console color
        if (_config.ColorFormatting is StackColorFormatType.ANSIColor or StackColorFormatType.ExtendedANSIColor)
            await writer.WriteAsync(GetANSIResetString()).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();

        // end line
        if (_writeNewline)
            await writer.WriteAsync(Environment.NewLine).ConfigureAwait(false);
    }

    /// <summary>
    /// Converts a 'primitive' type to it's corresponding keyword (void, int, string, etc.)
    /// </summary>
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
            if (type == TypeDecimal) // decimal is not a primitive type
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

    /// <summary>
    /// Get what token color a type name's span should be.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TokenType GetTypeColor(Type type) => type.IsGenericParameter ? TokenType.GenericParameter : (type.IsInterface ? TokenType.Interface : (type.IsValueType ? (type.IsEnum ? TokenType.Enum : TokenType.Struct) : TokenType.Class));
    
    /// <summary>
    /// Get the ANSI reset string for a terminal to reset to its default color and highlight.
    /// </summary>
    private static string GetANSIResetString() => GetANSIForegroundString((ConsoleColor)(-1));

    /// <summary>
    /// Returns ANSI text format codes for each <see cref="ConsoleColor"/> formatted as <code>
    /// ESC[*code*m
    /// </code> where 'ESC' is '\u001b'.
    /// </summary>
    private static unsafe string GetANSIForegroundString(ConsoleColor color)
    {
        // https://learn.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences?redirectedfrom=MSDN#text-formatting
        int num = color switch
        {
            // ANSI terminal color codes
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

            // Foreground Default
            _ => 39 
        };
        char* chrs = stackalloc char[5];
        chrs[0] = ConsoleEscapeCharacter;
        chrs[1] = '[';
        chrs[2] = (char)(num / 10 + 48);
        chrs[3] = (char)(num % 10 + 48);
        chrs[4] = 'm';

        return new string(chrs, 0, 5);
    }

    /// <summary>
    /// Returns extended ANSI text format codes for 32 bit ARGB data formatted as <code>
    /// ESC[38;2;*r*;*g*;*b*m
    /// </code> where 'ESC' is '\u001b'.
    /// </summary>
    /// <param name="argb">32 bit ARGB data, convert using <see cref="System.Drawing.Color.ToArgb"/> and <see cref="System.Drawing.Color.FromArgb(int)"/>.</param>
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

    /// <summary>
    /// Returns an opening span tag for 32 bit ARGB data formatted as: <code>
    /// &lt;span style="color:#xxxxxx;"&gt;
    /// </code>
    /// or <code>
    /// &lt;span class="class"&gt;
    /// </code>
    /// (depending on the value of <see cref="StackCleanerConfiguration.HtmlUseClassNames"/>)
    /// </summary>
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
        if (!_isArgbColor)
            argb = ColorConfig.ToArgb((ConsoleColor)(argb - 1));
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

    /// <summary>
    /// Returns the opening div tag for <see cref="StackColorFormatType.Html"/> formatting in the format: <code>
    /// &lt;div style="background-color:#xxxxxx;"&gt;
    /// </code>
    /// or <code>
    /// &lt;div class="st_bkgr"&gt;
    /// </code>
    /// (depending on the value of <see cref="StackCleanerConfiguration.HtmlUseClassNames"/>)
    /// </summary>
    private unsafe string GetDivTag()
    {
        if (_config.HtmlUseClassNames)
            return OuterStartHtmlTagStyleClass;
        
        int argb = _config.Colors!.HtmlBackgroundColor;
        if (!_isArgbColor)
            argb = ColorConfig.ToArgb((ConsoleColor)(argb - 1));
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

    /// <summary>
    /// Returns a unity opening color tag for 32 bit ARGB data formatted as: <code>
    /// &lt;color=#xxxxxx&gt;
    /// </code>
    /// </summary>
    /// <param name="argb">32 bit ARGB data, convert using <see cref="System.Drawing.Color.ToArgb"/> and <see cref="System.Drawing.Color.FromArgb(int)"/>.</param>
    private static unsafe string GetUnityString(int argb)
    {
        char* chrs = stackalloc char[15];
        chrs[0] = '<'; chrs[1] = 'c'; chrs[2] = 'o'; chrs[3] = 'l'; chrs[4] = 'o'; chrs[5] = 'r'; chrs[6] = '='; chrs[7] = '#';
        GetHex(argb, chrs + 8);
        chrs[14] = '>';
        return new string(chrs, 0, 15);
    }

    /// <summary>
    /// Returns a TextMeshPro-compatible unity opening color tag for 32 bit ARGB data formatted as: <code>
    /// &lt;#xxxxxx&gt;
    /// </code>
    /// </summary>
    /// <param name="argb">32 bit ARGB data, convert using <see cref="System.Drawing.Color.ToArgb"/> and <see cref="System.Drawing.Color.FromArgb(int)"/>.</param>
    private static unsafe string GetTMProString(int argb)
    {
        char* chrs = stackalloc char[9];
        chrs[0] = '<'; chrs[1] = '#';
        GetHex(argb, chrs + 2);
        chrs[8] = '>';
        return new string(chrs, 0, 9);
    }
    
    /// <summary>
    /// Helper function to append 6 characters of hex (first 24 bits of <paramref name="argb"/>) to <paramref name="chrs"/>.
    /// </summary>
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

    /// <summary>
    /// Helper function to return the corresponding color of the token from config.
    /// </summary>
    private int GetColor(TokenType type) => type switch
    {
        TokenType.Keyword => _config.Colors!.KeywordColor,
        TokenType.Method => _config.Colors!.MethodColor,
        TokenType.Property => _config.Colors!.PropertyColor,
        TokenType.Parameter => _config.Colors!.ParameterColor,
        TokenType.Class => _config.Colors!.ClassColor,
        TokenType.Struct => _config.Colors!.StructColor,
        TokenType.FlowKeyword => _config.Colors!.FlowKeywordColor,
        TokenType.Interface => _config.Colors!.InterfaceColor,
        TokenType.GenericParameter => _config.Colors!.GenericParameterColor,
        TokenType.Enum => _config.Colors!.EnumColor,
        TokenType.Namespace => _config.Colors!.NamespaceColor,
        TokenType.Punctuation => _config.Colors!.PunctuationColor,
        TokenType.ExtraData => _config.Colors!.ExtraDataColor,
        TokenType.LinesHiddenWarning => _config.Colors!.LinesHiddenWarningColor,
        _ => _isArgbColor ? unchecked((int)uint.MaxValue) : (int)ConsoleColor.Gray + 1
    };

    /// <summary>
    /// Returns the correct start tag based on <paramref name="token"/> and the current configuration.
    /// </summary>
    private string GetColorString(TokenType token)
    {
#pragma warning disable CS8509
        if (_isArgbColor)
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
            StackColorFormatType.UnityRichText => GetUnityString(ColorConfig.ToArgb((ConsoleColor)(GetColor(token) - 1))),
            StackColorFormatType.TextMeshProRichText => GetTMProString(ColorConfig.ToArgb((ConsoleColor)(GetColor(token) - 1))),
            StackColorFormatType.ANSIColor => GetANSIForegroundString((ConsoleColor)(GetColor(token) - 1)),
            StackColorFormatType.ExtendedANSIColor => GetExtANSIForegroundString(ColorConfig.ToArgb((ConsoleColor)(GetColor(token) - 1))),
            StackColorFormatType.Html => GetHtmlStartTag(token)
        };
#pragma warning restore CS8509
    }

    /// <summary>
    /// Returns the correct end tag based on the current configuration
    /// </summary>
    private string GetEndTag() => _config.ColorFormatting switch
    {
        StackColorFormatType.UnityRichText => UnityEndColorSymbol,
        StackColorFormatType.Html => HtmlEndSpanSymbol,
        _ => string.Empty
    };

    /// <summary>
    /// Caches the associations between compiler-generated classes and their source methods of the
    /// entire assembly that <paramref name="compGenType"/> is from, then returns the associated method for <paramref name="compGenType"/>.
    /// </summary>
    /// <param name="compGenType">A compiler-generated state machine type.</param>
    /// <returns>The method that contains the code in the state machine,
    /// whether it's an <see langword="async"/> method, <see cref="IEnumerator"/>, or an async enumerator.</returns>
    private static MethodInfo? TryGetMethod(Type compGenType)
    {
        // cache all registered state machines and their source methods in the type's assembly.
        Type[] types = compGenType.Assembly.GetTypes();
        MethodInfo? method = null;
        lock (CompilerGeneratedStateMachineSourceCache)
        {
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
                            if (!CompilerGeneratedStateMachineSourceCache.ContainsKey(attr.StateMachineType))
                                CompilerGeneratedStateMachineSourceCache.Add(attr.StateMachineType, methods[j]);
                            if (method is null && attr.StateMachineType == compGenType)
                                method = methods[j];
                            break;
                        }
                    }
                }
            }
        }

        return method;
    }

    /// <summary>
    /// Helper method to determine whether writing an end color tag is needed.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    private bool ShouldWriteEnd(TokenType token) => _appendColor && _reqEndColor && token != TokenType.Space;

    /// <summary>
    /// Enumerates through the spans in a <see cref="StackTrace"/>.
    /// </summary>
    private IEnumerable<SpanData> EnumerateSpans(StackTrace trace, bool warnIfApplicable)
    {
        if (trace.GetFrames() is not { Length: > 0 } frames)
            yield break;
        bool hasSentOne = false;
        bool hasHidden = false;
        for (int f = 0; f < frames.Length; ++f)
        {
            StackFrame? frame = frames[f];
            MethodBase? insertAfter = null;
            MethodBase? info = frame.GetMethod();
        redo:
            if (info == null)
                continue;
            Type? declType = info.DeclaringType;
            bool async = false;
            bool enumerator = false;
            bool anonFunc = false;
            if (declType is not null)
            {
                // if the type is hidden, continue.
                foreach (Type type in _config.GetHiddenTypes())
                {
                    if (type == declType || (declType.IsGenericType && type.IsGenericTypeDefinition && declType.GetGenericTypeDefinition() == type))
                    {
                        hasHidden = true;
                        goto skip;
                    }
                }
                // determine whether or not the method is part of a compiler-generated state machine (will always be private and named 'MoveNext').
                if (info.IsPrivate && info.Name.Equals(nameof(IEnumerator.MoveNext), StringComparison.Ordinal))
                {
                    if (Attribute.IsDefined(declType, TypeCompilerGenerated))
                    {
                        // test for various compiler-generated state machines
                        if (typeof(IAsyncStateMachine).IsAssignableFrom(declType))
                        {
                            async = true;
                        }
                        else if (typeof(IEnumerator).IsAssignableFrom(declType))
                        {
                            enumerator = true;
                        }
                        // external reference required for a typeof operation, string comparison works okay
                        else if (declType.GetInterfaces().Any(x => x.IsGenericType && x.Name.StartsWith("IAsyncEnumerator", StringComparison.Ordinal)))
                        {
                            async = true;
                            enumerator = true;
                        }
                        else goto next2;

                        MethodInfo? originalMethod;
                        // get method from cache
                        lock (CompilerGeneratedStateMachineSourceCache)
                        {
                            if (!CompilerGeneratedStateMachineSourceCache.TryGetValue(declType, out originalMethod) &&
                                Attribute.IsDefined(declType, TypeStateMachineBase))
                            {
                                originalMethod = TryGetMethod(declType);
                                if (originalMethod == null)
                                    // add null value to cache so we don't keep trying to fetch it.
                                    CompilerGeneratedStateMachineSourceCache.Add(declType, null);
                            }
                        }
                        if (originalMethod != null)
                        {
                            info = originalMethod;
                            declType = info.DeclaringType;
                        }
                    }
                    next2:
                    // try to get the method name from the comp-gen method name, last resort
                    if (Attribute.IsDefined(info, TypeCompilerGenerated))
                    {
                        anonFunc = true;
                        string name = info.Name;
                        int st = name.IndexOf('<') + 1;
                        int end = name.IndexOf('>');
                        if (st > 0 && end > 2)
                        {
                            string methodname = name.Substring(st, end - st);

                            // avoids an ambiguous match error
                            insertAfter = declType?.GetMethods(BindingFlags.Instance | BindingFlags.Static
                                | BindingFlags.NonPublic | BindingFlags.Public)
                                .FirstOrDefault(x => x.Name.Equals(methodname, StringComparison.Ordinal));
                        }
                    }
                }
                // determine whether the method is a compiler-generated anonymous method (lambda, in-line delegate, etc) replacement.
                else if (declType.IsSealed && Attribute.IsDefined(declType, TypeCompilerGenerated))
                {
                    anonFunc = true;
                }
            }

            if (_writeNewline)
            {
                // go to next line if needed and write the 'at' symbol
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
                // write <p> if Html and 'at'
                yield return new SpanData(StartParaTagSymbol, TokenType.EndTag);
                yield return new SpanData(AtPrefixSymbol, TokenType.FlowKeyword);
                hasSentOne = true;
            }
            bool isPropAccessor = false;

            // constructors will be null here
            MethodInfo? method2 = info as MethodInfo;
            if (!anonFunc)
            {
                if (info.IsStatic) // static keyword
                    yield return new SpanData(StaticSymbol + SpaceSymbolStr, TokenType.Keyword);

                if (async) // async keyword
                    yield return new SpanData(AsyncSymbol + SpaceSymbolStr, TokenType.Keyword);

                if (enumerator) // enumerator 'keyword'
                    yield return new SpanData(EnumeratorSymbol + SpaceSymbolStr, TokenType.Keyword);

                if (info.IsSpecialName)
                {
                    // Check if the method is a property accessor.
                    // This is the fastest way (compared to looping through all properties in the current class and comparing their accessor methods).
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

                // return type
                if (method2 != null)
                {
                    foreach (SpanData d in EnumerateTypeName(method2.ReturnType))
                        yield return d;
                    yield return new SpanData(SpaceSymbol, TokenType.Space);
                }
                bool dot = false;
                if (_config.IncludeNamespaces)
                {
                    string? ns = declType?.Namespace;
                    if (string.IsNullOrEmpty(ns))
                    {
                        // global namespace
                        yield return new SpanData(GlobalSymbol, TokenType.Keyword);
                        yield return new SpanData(GlobalSeparatorSymbol, TokenType.Punctuation);
                    }
                    else
                    {
                        dot = true;
                        if (_config.ColorFormatting != StackColorFormatType.None)
                        {
                            // split namespace and add in color formatting where needed
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
                        // send whole namespace at once since splitting isn't needed
                        else yield return new SpanData(ns!, TokenType.Space);
                    }
                }
                if (declType != null)
                {
                    if (dot) // will be false for a global namespace
                        yield return new SpanData(MemberSeparatorSymbol, TokenType.Punctuation);
                    foreach (SpanData d in EnumerateTypeName(declType))
                        yield return d;
                }
                if (info is not ConstructorInfo) // constructors can not be generic and will not have a (useful) method name.
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
                // display anonymous signature in lambda format.
                yield return new SpanData(AnonymousSymbol + SpaceSymbolStr, TokenType.Keyword);
                if (method2 != null)
                {
                    foreach (SpanData d in EnumerateTypeName(method2.ReturnType))
                        yield return d;
                    yield return new SpanData(SpaceSymbol, TokenType.Space);
                }
                if (async)
                {
                    yield return new SpanData(AsyncSymbol, TokenType.Keyword);
                    yield return new SpanData(SpaceSymbol, TokenType.Space);
                }
            }

            // property accessors will always have the same arguemnts, no need to display them.
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

                        // only check for params if the last parameter is an array
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
                    // a char has to be converted to a string so it doesn't perform integer addition
                    yield return new SpanData(new string(ParametersOpenSymbol, 1) + ParametersCloseSymbol, TokenType.Punctuation);
            }

            if (anonFunc)
            {
                // "=> { ... }"
                yield return new SpanData(SpaceSymbolStr + LambdaSymbol + SpaceSymbolStr, TokenType.Method);
                yield return new SpanData(MethodBodyOpenSymbol, TokenType.Punctuation);
                yield return new SpanData(SpaceSymbolStr + HiddenMethodContentSymbol + SpaceSymbolStr, TokenType.ExtraData);
                yield return new SpanData(MethodBodyCloseSymbol, TokenType.Punctuation);
            }

            string ed = string.Empty;
            // source data (line, column number, IL offset, file)
            if (_config.IncludeSourceData && frame != null)
            {
                // line and column data
                if (_config.IncludeLineData)
                {
                    int ln = frame.GetFileLineNumber();
                    if (ln != 0)
                        ed = SpaceSymbolStr + LineNumberPrefixSymbol + ln.ToString(_config.Locale);

                    ln = frame.GetFileColumnNumber();
                    if (ln != 0)
                        ed += SpaceSymbolStr + ColumnNumberPrefixSymbol + ln.ToString(_config.Locale);
                }
                // IL offset data
                if (_config.IncludeILOffset)
                {
                    int ilOff = frame.GetILOffset();
                    if (ilOff != -1)
                        ed += SpaceSymbolStr + ILOffsetPrefixSymbol + SpaceSymbolStr + ArrayOpenSymbol
                              + "0x" + ilOff.ToString("X6", _config.Locale) + ArrayCloseSymbol;
                }
                // source file data
                if (_config.IncludeFileData)
                {
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
                }
                if (ed.Length > 0)
                {
                    if (_writeParaTags && _config.PutSourceDataOnNewLine)
                    {
                        yield return new SpanData(EndParaTagSymbol, TokenType.EndTag);
                        yield return new SpanData(StartParaTagSymbol, TokenType.EndTag);
                    }
                    yield return new SpanData((!_config.PutSourceDataOnNewLine || _writeParaTags || !_writeNewline ? string.Empty : Environment.NewLine) + ed, TokenType.Space);
                }
            }
            if (_writeParaTags)
                yield return new SpanData(EndParaTagSymbol, TokenType.EndTag);
            skip:
            if (frame != null && insertAfter != null)
            {
                info = insertAfter;
                insertAfter = null;
                frame = null;
                goto redo;
            }
        }

        // lines hidden warning
        if (hasHidden && _config.WarnForHiddenLines && warnIfApplicable)
        {
            if (_writeParaTags)
                yield return new SpanData(StartParaTagSymbol, TokenType.EndTag);
            yield return new SpanData((_writeNewline ? Environment.NewLine : string.Empty) + HiddenLineWarning, TokenType.LinesHiddenWarning);
            if (_writeParaTags)
                yield return new SpanData(EndParaTagSymbol, TokenType.EndTag);
        }
    }

    /// <summary>
    /// Enumerates through the spans in a type name declaration.
    /// </summary>
    /// <remarks>Most of the time this only returns one span
    /// but can be more if the type is generic or has an element type (pointers, arrays, nullable value types).</remarks>
    /// <param name="type">Type to format.</param>
    /// <param name="isOut">For parameter declarations, replaces <see langword="ref"/> with <see langword="out"/> in byref types.</param>
    private IEnumerable<SpanData> EnumerateTypeName(Type type, bool isOut = false)
    {
        if (_config.UseTypeAliases && GetKeyword(type) is { } rtnName)
        {
            // get type alias (void, int, string, etc)
            yield return new SpanData(rtnName, TokenType.Keyword);
        }
        else
        {
            // pointers (int*)
            if (type.IsPointer)
            {
                foreach (SpanData d in EnumerateTypeName(type.GetElementType()!))
                    yield return d;
                yield return new SpanData(PointerSymbol, TokenType.Punctuation);
                yield break;
            }

            // arrays (int[])
            if (type.IsArray)
            {
                foreach (SpanData d in EnumerateTypeName(type.GetElementType()!))
                    yield return d;
                yield return new SpanData(ArraySymbol, TokenType.Punctuation);
                yield break;
            }

            // by ref type (ref int or out int)
            if (type.IsByRef && type.GetElementType() is { } elemType)
            {
                yield return new SpanData((isOut ? OutSymbol : RefSymbol) + SpaceSymbolStr, TokenType.Keyword);
                foreach (SpanData d in EnumerateTypeName(elemType))
                    yield return d;
                yield break;
            }

            bool generic = !type.IsGenericParameter && (type.IsGenericType || type.IsGenericTypeDefinition);

            // check for Nullable<T> (nullable value type) and display it with a question mark (int?).
            if (generic && type.IsGenericType && type.GetGenericTypeDefinition() == TypeNullableValueType)
            {
                foreach (SpanData d in EnumerateTypeName(type.GenericTypeArguments[0]))
                    yield return d;
                yield return new SpanData(NullableSymbol, TokenType.Punctuation);
                yield break;
            }

            // enumerate nested type (will also recursively work on multi-nested types)
            if (!type.IsGenericParameter && type.IsNested && type.DeclaringType is { } decl)
            {
                foreach (SpanData d in EnumerateTypeName(decl))
                    yield return d;
                yield return new SpanData(MemberSeparatorSymbol, TokenType.Punctuation);
            }

            string name = type.Name;
            if (generic)
            {
                // remove the grave character from generic type names (Task`1 = Task<T>)
                int index = name.LastIndexOf(TypeNameGenericSeparator);
                if (index != -1)
                    name = name.Substring(0, index);
                yield return new SpanData(name, GetTypeColor(type));

                // generic arguments
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