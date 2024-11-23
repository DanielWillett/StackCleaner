namespace StackCleaner.Tests;

internal class Program
{
    static async Task Main(string[] args)
    {
        StackTraceCleaner cleaner = new StackTraceCleaner(new StackCleanerConfiguration
        {
            ColorFormatting = StackColorFormatType.ExtendedANSIColor,
            Colors = Color32Config.Default,
            IncludeNamespaces = false,
            IncludeFileData = true,
            IncludeLineData = true,
            IncludeILOffset = true,
            IncludeSourceData = true,
            IncludeAssemblyData = true
        });

        try
        {
            await Task.Delay(3);
            await Task.Delay(3);
            await new Program().Task1();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Console.WriteLine(cleaner.GetString(ex));
        }
    }

    public async Task Task1()
    {
        await Task.Delay(3);
        await Task.Delay(3);
        await Task.Delay(3);
        await Task2();
    }

    public async Task Task2()
    {
        await Task.Delay(3);
        await Task.Delay(3);
        await Task.Delay(3);
        await Task3();
    }

    public async Task Task3()
    {
        await Task.Delay(3);
        throw new TestException();
    }
}
