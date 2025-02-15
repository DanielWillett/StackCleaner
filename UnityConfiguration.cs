using System;
using UnityEngine;
namespace StackCleaner;

/// <remarks>
/// This is the only class that will try to load UnityEngine.CoreModule.dll.
/// </remarks>
public sealed class UnityColor32Config : ColorConfig
{
    private static UnityColor32Config? _default;

    /// <summary>
    /// Default values of <see cref="UnityColor32Config"/>. Frozen.
    /// </summary>
    public static UnityColor32Config Default => _default ??= new UnityColor32Config { Frozen = true };
    /// <summary>
    /// Color of keywords including types: <br/><see langword="null"/>, <see langword="bool"/>,
    /// <see langword="byte"/>, <see langword="char"/>, <see langword="double"/>,
    /// <see langword="decimal"/>, <see langword="float"/>, <see langword="int"/>, <see langword="long"/>,
    /// <see langword="sbyte"/>, <see langword="short"/>, <see langword="object"/>, <see langword="string"/>,
    /// <see langword="uint"/>, <see langword="bool"/>, <see langword="ulong"/>, <see langword="ushort"/>,
    /// <see langword="void"/><br/>
    /// method keywords: <br/><see langword="static"/>, <see langword="async"/>, <see langword="enumerator"/>,
    /// <see langword="get"/>, <see langword="set"/>, <see langword="anonymous"/><br/>
    /// and the <see langword="global"/> and <see langword="params"/> keywords.
    /// </summary>
    /// <exception cref="NotSupportedException">Object is frozen (has been given to a <see cref="StackTraceCleaner"/>).</exception>
    public new Color KeywordColor
    {
        get => FromArgb(base.KeywordColor);
        set
        {
            if (Frozen)
                throw new NotSupportedException(FrozenErrorText);
            base.KeywordColor = ToArgb(value);
        }
    }

    /// <summary>
    /// Color of method names.
    /// </summary>
    /// <exception cref="NotSupportedException">Object is frozen (has been given to a <see cref="StackTraceCleaner"/>).</exception>
    public new Color MethodColor
    {
        get => FromArgb(base.MethodColor);
        set
        {
            if (Frozen)
                throw new NotSupportedException(FrozenErrorText);
            base.MethodColor = ToArgb(value);
        }
    }

    /// <summary>
    /// Color of property names.
    /// </summary>
    /// <exception cref="NotSupportedException">Object is frozen (has been given to a <see cref="StackTraceCleaner"/>).</exception>
    public new Color PropertyColor
    {
        get => FromArgb(base.PropertyColor);
        set
        {
            if (Frozen)
                throw new NotSupportedException(FrozenErrorText);
            base.PropertyColor = ToArgb(value);
        }
    }

    /// <summary>
    /// Color of event names.
    /// </summary>
    /// <exception cref="NotSupportedException">Object is frozen (has been given to a <see cref="StackTraceCleaner"/>).</exception>
    public new Color EventColor
    {
        get => FromArgb(base.EventColor);
        set
        {
            if (Frozen)
                throw new NotSupportedException(FrozenErrorText);
            base.EventColor = ToArgb(value);
        }
    }

    /// <summary>
    /// Color of parameter names.
    /// </summary>
    /// <exception cref="NotSupportedException">Object is frozen (has been given to a <see cref="StackTraceCleaner"/>).</exception>
    public new Color ParameterColor
    {
        get => FromArgb(base.ParameterColor);
        set
        {
            if (Frozen)
                throw new NotSupportedException(FrozenErrorText);
            base.ParameterColor = ToArgb(value);
        }
    }

    /// <summary>
    /// Color of class type names.
    /// </summary>
    /// <exception cref="NotSupportedException">Object is frozen (has been given to a <see cref="StackTraceCleaner"/>).</exception>
    public new Color ClassColor
    {
        get => FromArgb(base.ClassColor);
        set
        {
            if (Frozen)
                throw new NotSupportedException(FrozenErrorText);
            base.ClassColor = ToArgb(value);
        }
    }

    /// <summary>
    /// Color of value type names (not including enums).
    /// </summary>
    /// <exception cref="NotSupportedException">Object is frozen (has been given to a <see cref="StackTraceCleaner"/>).</exception>
    public new Color StructColor
    {
        get => FromArgb(base.StructColor);
        set
        {
            if (Frozen)
                throw new NotSupportedException(FrozenErrorText);
            base.StructColor = ToArgb(value);
        }
    }

    /// <summary>
    /// Color of flow keywords, currently only used for the 'at' at the beginning of each declaration.
    /// </summary>
    /// <exception cref="NotSupportedException">Object is frozen (has been given to a <see cref="StackTraceCleaner"/>).</exception>
    public new Color FlowKeywordColor
    {
        get => FromArgb(base.FlowKeywordColor);
        set
        {
            if (Frozen)
                throw new NotSupportedException(FrozenErrorText);
            base.FlowKeywordColor = ToArgb(value);
        }
    }

    /// <summary>
    /// Color of interface type names.
    /// </summary>
    /// <exception cref="NotSupportedException">Object is frozen (has been given to a <see cref="StackTraceCleaner"/>).</exception>
    public new Color InterfaceColor
    {
        get => FromArgb(base.InterfaceColor);
        set
        {
            if (Frozen)
                throw new NotSupportedException(FrozenErrorText);
            base.InterfaceColor = ToArgb(value);
        }
    }

    /// <summary>
    /// Color of generic parameter names.
    /// </summary>
    /// <exception cref="NotSupportedException">Object is frozen (has been given to a <see cref="StackTraceCleaner"/>).</exception>
    public new Color GenericParameterColor
    {
        get => FromArgb(base.GenericParameterColor);
        set
        {
            if (Frozen)
                throw new NotSupportedException(FrozenErrorText);
            base.GenericParameterColor = ToArgb(value);
        }
    }

    /// <summary>
    /// Color of enum type names.
    /// </summary>
    /// <exception cref="NotSupportedException">Object is frozen (has been given to a <see cref="StackTraceCleaner"/>).</exception>
    public new Color EnumColor
    {
        get => FromArgb(base.EnumColor);
        set
        {
            if (Frozen)
                throw new NotSupportedException(FrozenErrorText);
            base.EnumColor = ToArgb(value);
    }
    }

    /// <summary>
    /// Color of namespaces.
    /// </summary>
    /// <exception cref="NotSupportedException">Object is frozen (has been given to a <see cref="StackTraceCleaner"/>).</exception>
    public new Color NamespaceColor
    {
        get => FromArgb(base.NamespaceColor);
        set
        {
            if (Frozen)
                throw new NotSupportedException(FrozenErrorText);
            base.NamespaceColor = ToArgb(value);
    }
    }

    /// <summary>
    /// Color of any punctuation: periods, commas, parenthesis, etc.
    /// </summary>
    /// <exception cref="NotSupportedException">Object is frozen (has been given to a <see cref="StackTraceCleaner"/>).</exception>
    public new Color PunctuationColor
    {
        get => FromArgb(base.PunctuationColor);
        set
        {
            if (Frozen)
                throw new NotSupportedException(FrozenErrorText);
            base.PunctuationColor = ToArgb(value);
        }
    }

    /// <summary>
    /// Color of the source data (line number, column number, IL offset, and file name).
    /// </summary>
    /// <exception cref="NotSupportedException">Object is frozen (has been given to a <see cref="StackTraceCleaner"/>).</exception>
    public new Color ExtraDataColor
    {
        get => FromArgb(base.ExtraDataColor);
        set
        {
            if (Frozen)
                throw new NotSupportedException(FrozenErrorText);
            base.ExtraDataColor = ToArgb(value);
    }
    }

    /// <summary>
    /// Color of the warning optionally shown when unnecessary lines are removed.
    /// </summary>
    /// <exception cref="NotSupportedException">Object is frozen (has been given to a <see cref="StackTraceCleaner"/>).</exception>
    public new Color LinesHiddenWarningColor
    {
        get => FromArgb(base.LinesHiddenWarningColor);
        set
        {
            if (Frozen)
                throw new NotSupportedException(FrozenErrorText);
            base.LinesHiddenWarningColor = ToArgb(value);
    }
    }

    /// <summary>
    /// Color of the optionally added background when writing as HTML.
    /// </summary>
    /// <exception cref="NotSupportedException">Object is frozen (has been given to a <see cref="StackTraceCleaner"/>).</exception>
    public new Color HtmlBackgroundColor
    {
        get => FromArgb(base.HtmlBackgroundColor);
        set
        {
            if (Frozen)
                throw new NotSupportedException(FrozenErrorText);
            base.HtmlBackgroundColor = ToArgb(value);
        }
    }

    /// <summary>
    /// Sets the default values for <see cref="UnityColor32Config"/>.
    /// </summary>
    public UnityColor32Config()
    {
        KeywordColor = new Color32(86, 156, 214, 255);
        MethodColor = new Color32(220, 220, 170, 255);
        PropertyColor = new Color32(220, 220, 220, 255);
        EventColor = new Color32(220, 220, 220, 255);
        ParameterColor = new Color32(156, 220, 254, 255);
        ClassColor = new Color32(78, 201, 176, 255);
        StructColor = new Color32(134, 198, 145, 255);
        FlowKeywordColor = new Color32(216, 160, 223, 255);
        InterfaceColor = new Color32(184, 215, 163, 255);
        GenericParameterColor = new Color32(184, 215, 163, 255);
        EnumColor = new Color32(184, 215, 163, 255);
        NamespaceColor = new Color32(220, 220, 220, 255);
        PunctuationColor = new Color32(180, 180, 180, 255);
        ExtraDataColor = new Color32(98, 98, 98, 255);
        LinesHiddenWarningColor = new Color32(220, 220, 0, 255);
        HtmlBackgroundColor = new Color32(30, 30, 30, 255);
    }

    /// <summary>
    /// Convert <see cref="ConsoleColor"/> to <see cref="Color"/>.
    /// </summary>
    public static Color ToColor(ConsoleColor color) => FromArgb(ToArgb(color));

    /// <summary>
    /// Convert ARGB data to <see cref="Color"/>.
    /// </summary>
    public static Color FromArgb(int value)
    {
        return new Color(
            unchecked((byte)(value >> 16)) / 255f,
            unchecked((byte)(value >> 8)) / 255f,
            unchecked((byte)value) / 255f,
            1f);
    }

    /// <summary>
    /// Convert to <see cref="Color"/> to ARGB data.
    /// </summary>
    public static int ToArgb(Color color)
    {
        return 0xFF << 24 |
               (byte)Math.Min(255, Mathf.RoundToInt(color.r * 255)) << 16 |
               (byte)Math.Min(255, Mathf.RoundToInt(color.g * 255)) << 8 |
               (byte)Math.Min(255, Mathf.RoundToInt(color.b * 255));
    }
}