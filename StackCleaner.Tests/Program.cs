namespace StackCleaner.Tests;

internal class Program : ITest
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
            //((ITest)new Program()).TestEvent -= () => { };
            //((ITest)new Program()).Execute();
            //_ = ((ITest)new Program()).Count;
            //((ITest)new Program())[1] = "";
            //_ = ((ITest)new Program())[1];

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

    string ITest.this[int index]
    {
        get
        {
            throw new TestException();
        }
        set
        {
            throw new TestException();
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

    /// <inheritdoc />
    void ITest.Execute()
    {
        throw new Exception("Test explicit implementation");
    }

    /// <inheritdoc />
    int ITest.Count => throw new Exception("Test explicit implementation property.");

    public event Action TestEvent
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }
}


public interface ITest
{
    int Count { get; }

    event Action TestEvent;

    void Execute();

    string this[int index] { get; set; }
}