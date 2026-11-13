using System.Diagnostics;

namespace StackCleaner.Tests;

internal partial class Program : ITest
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
            //_ = new Program()[1];
            //_ = new Program()[1] = new Version(0, 1);
            // _ = new TestClass();
            // try
            // {
            //     RuntimeHelpers.RunClassConstructor(typeof(TestStaticClass).TypeHandle);
            // }
            // catch (TypeInitializationException ex)
            // {
            //     Console.WriteLine(cleaner.GetString(ex));
            // }
            // new GenericClass<long, int>.GenericClass2<string, DateTime>().Test<Version>(8, 1, new Version(8, 0));

            // new List<string>(-1);

            TestStaticClass.Test();

            await Task.Delay(3);
            await Task.Delay(3);
            //await ((ITest)new Program()).ExecuteAsync();
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

    private Version this[int index]
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

        Console.WriteLine(new StackTrace());

        // await SendTest(3);

        throw new TestException();
    }

    /// <inheritdoc />
    void ITest.Execute()
    {
        throw new TestException();
    }

    Task ITest.ExecuteAsync()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    int ITest.Count => throw new Exception("Test explicit implementation property.");

    public event Action TestEvent
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }
}

public class TestClass
{
    public TestClass()
    {
        throw new TestException();
    }
}

public static class TestStaticClass
{
    static TestStaticClass()
    {
        throw new TestException();
    }

    public static void Test()
    {

    }
}

public interface ITest
{
    int Count { get; }

    event Action TestEvent;

    void Execute();

    string this[int index] { get; set; }

    Task ExecuteAsync();
}

public class GenericClass<T1, T2>
{
    public class GenericClass2<T4, T5>
    {
        public void Test<T3>(T1 t1, T2 t2, T3 t3)
        {
            throw new TestException();
        }
    }
    public void Test<T3>(T1 t1, T2 t2, T3 t3)
    {
        throw new TestException();
    }
}