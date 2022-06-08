using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Net.Leksi.KeyBox;
using System.Diagnostics;

namespace KeyBoxTestProject;

public class KeyBoxTests
{
    [OneTimeSetUp]
    public void Setup()
    {
        Trace.Listeners.Clear();
        Trace.Listeners.Add(new ConsoleTraceListener());
        Trace.AutoFlush = true;
    }

    [Test]
    public void ThrowIfAlreadyMappedTest()
    {
        IHost host = Host.CreateDefaultBuilder().AddKeyBox(config =>
        {
            config.AddPrimaryKey<Poco1>(new Dictionary<string, Type>() { { "ID", typeof(int) } });
            InvalidOperationException ex = Assert.Catch<InvalidOperationException>(() =>
                config.AddPrimaryKey<Poco1>(new Dictionary<string, Type>() { { "ID", typeof(int) } })
            );
            Assert.That(ex.Message, Is.EqualTo($"Key for {typeof(Poco1)} is already mapped"));

            ex = Assert.Catch<InvalidOperationException>(() =>
                config.AddPrimaryKey<Poco1, Poco2>()
            );
            Assert.That(ex.Message, Is.EqualTo($"Key for {typeof(Poco1)} is already mapped"));
        }).Build();
    }

    [Test]
    public void ThrowIfNotClassTest()
    {
        IHost host = Host.CreateDefaultBuilder().AddKeyBox(config =>
        {
            ArgumentException ex = Assert.Catch<ArgumentException>(() =>
                config.AddPrimaryKey(typeof(int), new Dictionary<string, Type>() { { "ID", typeof(int) } })
            );
            Assert.That(ex.Message, Is.EqualTo("targetType must be a class"));

            ex = Assert.Catch<ArgumentException>(() =>
                config.AddPrimaryKey(typeof(IPoco1_1), new Dictionary<string, Type>() { { "ID", typeof(int) } })
            );
            Assert.That(ex.Message, Is.EqualTo("targetType must be a class"));

            ex = Assert.Catch<ArgumentException>(() =>
                config.AddPrimaryKey(typeof(DateTime), new Dictionary<string, Type>() { { "ID", typeof(int) } })
            );
            Assert.That(ex.Message, Is.EqualTo("targetType must be a class"));
        }).Build();
    }

    [Test]
    public void ThrowIfConfiguredTest()
    {
        IHost host = Host.CreateDefaultBuilder().AddKeyBox(config =>
        {
            config.AddPrimaryKey<Poco1>(new Dictionary<string, Type>() { { "ID", typeof(int) } });
            config.AddPrimaryKey<Poco2>(new Dictionary<string, Type>() { { "ID1", typeof(int) }, { "ID2", typeof(string) } });
        }).Build();

        if (host.Services.GetRequiredService<IKeyBox>() is IKeyBoxConfiguration config)
        {
            InvalidOperationException ex = Assert.Catch<InvalidOperationException>(() =>

                config.AddPrimaryKey<Poco3>(new Dictionary<string, Type>() { { "ID1", typeof(int) }, { "ID2", typeof(string) }, { "ID3", typeof(string) } })

            );
            Assert.That(ex.Message, Is.EqualTo($"{typeof(IKeyBox)} is already configured"));
        }
        else
        {
            Assert.Fail();
        }
    }

    [Test]
    public void ThrowIfExampleLoopTest()
    {
        Exception ex = Assert.Catch<Exception>(() =>
            Host.CreateDefaultBuilder().AddKeyBox(config =>
            {
                config.AddPrimaryKey<Poco1, Poco2>();
                config.AddPrimaryKey<Poco2, Poco3>();
                config.AddPrimaryKey<Poco3, Poco1>();
            }).Build()
        );
        Assert.That(ex.Message, Is.EqualTo($"Example loop detected: {typeof(Poco1)}"));

    }

    [Test]
    public void ThrowIfNotMappedTest()
    {
        Exception ex = Assert.Catch<Exception>(() =>
            Host.CreateDefaultBuilder().AddKeyBox(config =>
            {
                config.AddPrimaryKey<Poco1, Poco2>();
                config.AddPrimaryKey<Poco2, Poco3>();
            }).Build()
        );
        Assert.That(ex.Message, Is.EqualTo($"Keys not mapped for: {typeof(Poco1)}, {typeof(Poco2)}, {typeof(Poco3)}"));

    }

    [Test]
    public void KeyBoxTest()
    {
        IHost host = Host.CreateDefaultBuilder().ConfigureServices(services =>
        {
            services.AddTransient<IPoco1_1, Poco1>();
            services.AddTransient<IPoco1_2, Poco1>();
            services.AddTransient<IPoco1_3, Poco1>();
            services.AddTransient<IPoco2_1, Poco2>();
            services.AddTransient<IPoco2_2, Poco2>();
            services.AddTransient<IPoco2_3, Poco2>();
            services.AddTransient<IPoco3_1, Poco3>();
            services.AddTransient<IPoco3_2, Poco3>();
            services.AddTransient<IPoco3_3, Poco3>();
        }).AddKeyBox(config =>
        {
            config.AddPrimaryKey<Poco1>(new Dictionary<string, Type>() { { "ID", typeof(int) } });
            config.AddPrimaryKey<Poco2>(new Dictionary<string, Type>() { { "ID1", typeof(int) }, { "ID2", typeof(string) } });
            config.AddPrimaryKey<Poco3, Poco2>();

        }).Build();

        IKeyBox keyBox = host.Services.GetRequiredService<IKeyBox>();

        IPoco1_1 poco1_1 = host.Services.GetRequiredService<IPoco1_1>();

        IKeyRing keyRing = keyBox.GetKeyRing(poco1_1)!;
        Assert.That(keyRing.Count, Is.EqualTo(1));
        CollectionAssert.AreEqual(keyRing.Keys, new[] { "ID" });
        Assert.That(keyRing.IsCompleted, Is.False);

        keyRing.Set("ID", 42);
        Assert.That(keyBox.GetKeyRing(poco1_1)!["ID"], Is.EqualTo(42));
        Assert.That(keyRing.IsCompleted, Is.True);

        IPoco2_2 poco2_2 = host.Services.GetRequiredService<IPoco2_2>();

        keyRing = keyBox.GetKeyRing(poco2_2)!;
        Assert.That(keyRing.Count, Is.EqualTo(2));
        CollectionAssert.AreEqual(keyRing.Keys, new[] { "ID1", "ID2" });
        Assert.That(keyRing.IsCompleted, Is.False);

        keyRing.Set("ID1", 42);
        Assert.That(keyBox.GetKeyRing(poco2_2)!["ID1"], Is.EqualTo(42));
        Assert.That(keyRing.IsCompleted, Is.False);

        keyRing.Set("ID2", "RULED");
        Assert.That(keyBox.GetKeyRing(poco2_2)!["ID2"], Is.EqualTo("RULED"));
        Assert.That(keyRing.IsCompleted, Is.True);
        CollectionAssert.AreEqual(keyRing.Values, new object[] { 42, "RULED" });

        IPoco3_3 poco3_3 = host.Services.GetRequiredService<IPoco3_3>();

        keyRing = keyBox.GetKeyRing(poco3_3)!;
        Assert.That(keyRing.Count, Is.EqualTo(2));
        CollectionAssert.AreEqual(keyRing.Keys, new[] { "ID1", "ID2" });
        Assert.That(keyRing.IsCompleted, Is.False);

        keyRing.Set("ID1", 42).Set("ID2", "RULED");
        Assert.That(keyBox.GetKeyRing(poco3_3)!["ID1"], Is.EqualTo(42));
        Assert.That(keyBox.GetKeyRing(poco2_2)!["ID2"], Is.EqualTo("RULED"));
        Assert.That(keyRing.IsCompleted, Is.True);
        CollectionAssert.AreEqual(keyRing.Values, new object[] { 42, "RULED" });

    }
}

public interface IPoco1_1 { }
public interface IPoco1_2 { }
public interface IPoco1_3 { }
public interface IPoco2_1 { }
public interface IPoco2_2 { }
public interface IPoco2_3 { }
public interface IPoco3_1 { }
public interface IPoco3_2 { }
public interface IPoco3_3 { }
public class Poco1 : IPoco1_1, IPoco1_2, IPoco1_3 { }
public class Poco2 : IPoco2_1, IPoco2_2, IPoco2_3 { }
public class Poco3 : IPoco3_1, IPoco3_2, IPoco3_3 { }