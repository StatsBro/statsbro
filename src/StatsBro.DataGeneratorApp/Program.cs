// See https://aka.ms/new-console-template for more information
namespace StatsBro.DataGeneratorApp;

public class Program
{
    public static void Main() 
    {
        var staticGenerator = new StaticDataGenerator();
        staticGenerator.Run();

        //var dummyGenerator = new DummyDataGenerator();
        //dummyGenerator.Run();
    }
}
