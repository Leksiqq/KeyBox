using Net.Leksi.KeyBox;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKeyBox(config =>
{
    config.AddPrimaryKey<Poco1>(new Dictionary<string, object>() { { "ID1", typeof(int) }, { "ID2", "/Poco/ID2" } });
    config.AddPrimaryKey<Poco2>(new Dictionary<string, object>() { { "ID1", typeof(int) }, { "ID2", typeof(string) } });
});

builder.Services.AddTransient<IPoco1_1, Poco1>();
builder.Services.AddTransient<IPoco1_2, Poco1>();
builder.Services.AddTransient<IPoco1_3, Poco1>();
builder.Services.AddTransient<IPoco2_1, Poco2>();
builder.Services.AddTransient<IPoco2_2, Poco2>();
builder.Services.AddTransient<IPoco2_3, Poco2>();
builder.Services.AddTransient<IPoco3_1, Poco3>();
builder.Services.AddTransient<IPoco3_2, Poco3>();
builder.Services.AddTransient<IPoco3_3, Poco3>();

var app = builder.Build();

app.UseKeyBox();

app.MapGet("/", async context => 
{
    IPoco1_1 poco1_1 = context.RequestServices.GetRequiredService<IPoco1_1>();
    IKeyBox keyBox = context.RequestServices.GetRequiredService<IKeyBox>();

    IKeyRing keyRing = keyBox.GetKeyRing(poco1_1);

    keyRing["ID1"] = 123;
    keyRing["ID2"] = "KEY";

    IKeyRing keyRing1 = keyBox.GetKeyRing(poco1_1.Poco);
    keyRing1["ID1"] = 1234;

    await context.Response.WriteAsync(Dump(context.RequestServices, poco1_1));
});

app.Run();

string Dump(IServiceProvider serviceProvider, object? obj, StringBuilder sb = null)
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
            IKeyRing keyRing = serviceProvider.GetRequiredService<IKeyBox>().GetKeyRing(obj);
            if (keyRing is { })
            {
                sb.Append("{\n");
                foreach (var entry in keyRing.Entries)
                {
                    sb.Append(indention).Append(tab).Append(entry.Key).Append(": ");
                    Dump(serviceProvider, entry.Value, sb);
                    sb.AppendLine();
                }
                foreach (var pi in type.GetProperties())
                {
                    sb.Append(indention).Append(tab).Append(pi.Name).Append(": ");
                    Dump(serviceProvider, pi.GetValue(obj), sb);
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

public interface IPoco1_1 {
    IPoco2_1 Poco { get; }
}
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

    IPoco2_1 IPoco1_1.Poco => Poco;
}
public class Poco2 : IPoco2_1, IPoco2_2, IPoco2_3
{
    public Poco3 Poco { get; set; }
    public Poco3? PocoNullable { get; set; }
}
public class Poco3 : IPoco3_1, IPoco3_2, IPoco3_3 { }

