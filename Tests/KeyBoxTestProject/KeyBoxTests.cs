using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Net.Leksi.KeyBox;
using System.Diagnostics;
using System.Text;

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
        IHost host = Host.CreateDefaultBuilder().ConfigureServices(services =>
        {
            services.AddKeyBox(config =>
            {
                config.AddPrimaryKey<Poco1>(new Dictionary<string, object>() { { "ID", typeof(int) } });
                InvalidOperationException ex = Assert.Catch<InvalidOperationException>(() =>
                    config.AddPrimaryKey<Poco1>(new Dictionary<string, object>() { { "ID", typeof(int) } })
                );
                Assert.That(ex.Message, Is.EqualTo($"Key for {typeof(Poco1)} is already mapped"));

            });
        }).Build();
    }

    [Test]
    public void ThrowIfNotClassTest()
    {
        IHost host = Host.CreateDefaultBuilder().ConfigureServices(services =>
        {
            services.AddKeyBox(config =>
            {
                ArgumentException ex = Assert.Catch<ArgumentException>(() =>
                    config.AddPrimaryKey(typeof(int), new Dictionary<string, object>() { { "ID", typeof(int) } })
                );
                Assert.That(ex.Message, Is.EqualTo("targetType must be a class"));

                ex = Assert.Catch<ArgumentException>(() =>
                    config.AddPrimaryKey(typeof(IPoco1_1), new Dictionary<string, object>() { { "ID", typeof(int) } })
                );
                Assert.That(ex.Message, Is.EqualTo("targetType must be a class"));

                ex = Assert.Catch<ArgumentException>(() =>
                    config.AddPrimaryKey(typeof(DateTime), new Dictionary<string, object>() { { "ID", typeof(int) } })
                );
                Assert.That(ex.Message, Is.EqualTo("targetType must be a class"));
            });
        }).Build();
    }

    [Test]
    public void ThrowIfConfiguredTest()
    {
        IHost host = Host.CreateDefaultBuilder().ConfigureServices(services =>
        {
            services.AddKeyBox(config =>
        {
            config.AddPrimaryKey<Poco1>(new Dictionary<string, object>() { { "ID", typeof(int) } });
            config.AddPrimaryKey<Poco2>(new Dictionary<string, object>() { { "ID1", typeof(int) }, { "ID2", typeof(string) } });
        });
        }).Build();

        if (host.Services.GetRequiredService<IKeyBox>() is IKeyBoxConfiguration config)
        {
            InvalidOperationException ex = Assert.Catch<InvalidOperationException>(() =>

                config.AddPrimaryKey<Poco3>(new Dictionary<string, object>() { { "ID1", typeof(int) }, { "ID2", typeof(string) }, { "ID3", typeof(string) } })

            );
            Assert.That(ex.Message, Is.EqualTo($"{typeof(IKeyBox)} is already configured"));
        }
        else
        {
            Assert.Fail();
        }
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
            services.AddKeyBox(config =>
            {
                config.AddPrimaryKey<Poco1>(new Dictionary<string, object>() { { "ID1", typeof(int) } });
                config.AddPrimaryKey<Poco2>(new Dictionary<string, object>() { { "ID1", typeof(int) }, { "ID2", typeof(string) } });

            });
        }).Build();

        IKeyBox keyBox = host.Services.GetRequiredService<IKeyBox>();

        IPoco1_1 poco1_1 = host.Services.GetRequiredService<IPoco1_1>();

        IKeyRing keyRing = keyBox.GetKeyRing(poco1_1)!;
        Assert.That(keyRing.Count, Is.EqualTo(1));
        CollectionAssert.AreEqual(keyRing.Keys, new[] { "ID1" });
        Assert.That(keyRing.IsCompleted, Is.False);

        keyRing.Set("ID1", 42);
        Assert.That(keyBox.GetKeyRing(poco1_1)!["ID1"], Is.EqualTo(42));
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


    }

    [Test]
    public void ThrowIfInvalidPathTest()
    {
        {
            IHost host = Host.CreateDefaultBuilder().ConfigureServices(services =>
            {
                services.AddKeyBox(config =>
                {
                    ArgumentException ex = Assert.Catch<ArgumentException>(() =>
                    {
                        config.AddPrimaryKey<Poco1>(new Dictionary<string, object>() { { "ID1", typeof(int) }, { "ID2", "Poco/ID2" } });
                    });
                    Assert.That(ex.Message, Is.EqualTo("definition path for name ID2 must start with /"));
                });
            }).Build();

        }
        {
            IHost host = Host.CreateDefaultBuilder().ConfigureServices(services =>
            {
                services.AddKeyBox(config =>
                {
                    ArgumentException ex = Assert.Catch<ArgumentException>(() =>
                    {
                        config.AddPrimaryKey<Poco1>(new Dictionary<string, object>() { { "ID1", typeof(int) }, { "ID2", "/ID2" } });
                    });
                    Assert.That(ex.Message, Is.EqualTo("definition path for name ID2 must have at least 2 parts"));
                });
            }).Build();

        }
        {
            IHost host = Host.CreateDefaultBuilder().ConfigureServices(services =>
            {
                services.AddKeyBox(config =>
                {
                    ArgumentException ex = Assert.Catch<ArgumentException>(() =>
                    {
                        config.AddPrimaryKey<Poco1>(new Dictionary<string, object>() { { "ID1", typeof(int) }, { "ID2", "/Poco2/ID2" } });
                    });
                    Assert.That(ex.Message, Is.EqualTo("definition path for name ID2 has invalid part: Poco2"));
                });
            }).Build();

        }
        {
            IHost host = Host.CreateDefaultBuilder().ConfigureServices(services =>
            {
                services.AddKeyBox(config =>
            {
                ArgumentException ex = Assert.Catch<ArgumentException>(() =>
                {
                    config.AddPrimaryKey<Poco2>(new Dictionary<string, object>() { { "ID1", typeof(int) }, { "ID2", "/PocoNullable/ID2" } });
                });
                Assert.That(ex.Message, Is.EqualTo("definition path for name ID2 has nullable part: PocoNullable"));
            });
            }).Build();

        }
        AggregateException aex = Assert.Catch<AggregateException>(() =>
        {
            IHost host = Host.CreateDefaultBuilder().ConfigureServices(services =>
            {
                services.AddKeyBox(config =>
                {
                    config.AddPrimaryKey<Poco1>(new Dictionary<string, object>() { { "ID1", typeof(int) }, { "ID2", "/Poco/ID2" } });
                    config.AddPrimaryKey<Poco2>(new Dictionary<string, object>() { { "ID1", typeof(int) }, { "ID2", typeof(string) }, { "ID3", "/Poco/ID" } });
                });
            }).Build();
        });
        Assert.That(aex.InnerExceptions.Count, Is.EqualTo(1));
        Assert.That(aex.InnerExceptions[0].GetType(), Is.EqualTo(typeof(ArgumentException)));
        Assert.That(aex.InnerExceptions[0].Message, Is.EqualTo("The PropertiesPath at AddPrimaryKey for type KeyBoxTestProject.Poco2 and name ID3 is invalid as primary key field ID is not defined for KeyBoxTestProject.Poco3"));
    }

    [Test]
    public void PathTest()
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
            services.AddKeyBox(config =>
            {
                config.AddPrimaryKey<Poco1>(new Dictionary<string, object>() { { "ID1", typeof(int) }, { "ID2", "/Poco/ID2" } });
                config.AddPrimaryKey<Poco2>(new Dictionary<string, object>() { { "ID1", typeof(int) }, { "ID2", typeof(string) } });
                config.AddPrimaryKey<Poco3>(new Dictionary<string, object>() { { "Code", "/Code" } });

            });
        }).Build();

        Poco1 poco1 = host.Services.GetRequiredService<Poco1>();
        IKeyRing keyRing = host.Services.GetRequiredService<IKeyBox>().GetKeyRing(poco1);
        keyRing["ID1"] = 1;
        keyRing["ID2"] = "KEY";
        Trace.WriteLine(Dump(host, poco1));
        Assert.That(poco1.Poco, Is.Not.Null);
        IKeyRing keyRing1 = host.Services.GetRequiredService<IKeyBox>().GetKeyRing(poco1.Poco);
        Assert.That(keyRing1, Is.Not.Null);
        Assert.That(keyRing["ID2"], Is.EqualTo(keyRing1["ID2"]));
        Poco3 poco3 = host.Services.GetRequiredService<Poco3>();
        keyRing = host.Services.GetRequiredService<IKeyBox>().GetKeyRing(poco3);
        keyRing["Code"] = "SFX";
        Assert.That(poco3.Code, Is.EqualTo("SFX"));
        Trace.WriteLine(Dump(host, poco3));
    }

    private string Dump(IHost host, object? obj, StringBuilder sb = null)
    {
        if (sb is null)
        {
            sb = new StringBuilder();
        }
        if (obj is null)
        {
            sb.Append("NULL");
        }
        else
        {
            Type type = obj.GetType();
            string tab = "    ";
            string indention = string.Join(tab, Environment.StackTrace.Split('\n').Select(s => s.Trim()).Where(s => s.Contains(nameof(Dump))).Select(s => String.Empty));
            if (type.IsClass)
            {
                IKeyRing keyRing = host.Services.GetRequiredService<IKeyBox>().GetKeyRing(obj);
                if (keyRing is { })
                {
                    sb.Append("{\n");
                    foreach (var entry in keyRing.Entries)
                    {
                        sb.Append(indention).Append(tab).Append(entry.Key).Append(": ");
                        Dump(host, entry.Value, sb);
                        sb.AppendLine();
                    }
                    foreach (var pi in type.GetProperties())
                    {
                        sb.Append(indention).Append(tab).Append(pi.Name).Append(": ");
                        Dump(host, pi.GetValue(obj), sb);
                        sb.AppendLine();
                    }
                    sb.Append(indention).Append("}");
                }
                else
                {
                    sb.Append(obj);
                }
            }
            else
            {
                sb.Append(obj);
            }
        }
        return sb.ToString();
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
public class Poco1 : IPoco1_1, IPoco1_2, IPoco1_3
{
    public Poco2 Poco { get; set; }
}
public class Poco2 : IPoco2_1, IPoco2_2, IPoco2_3
{
    public Poco3 Poco { get; set; }
    public Poco3? PocoNullable { get; set; }
}
public class Poco3 : IPoco3_1, IPoco3_2, IPoco3_3
{
    public string Code { get; set; }
}