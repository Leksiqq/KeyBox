using Net.Leksi.KeyBox;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKeyBox(config =>
{
    config.AddPrimaryKey<Poco1>(new Dictionary<string, object>() { { "ID1", typeof(int) }, { "ID2", "/Poco/ID2" } });
    config.AddPrimaryKey<Poco2>(new Dictionary<string, object>() { { "ID1", typeof(int) }, { "ID2", typeof(string) } });
});

var app = builder.Build();

app.UseKeyBox();

app.MapGet("/", () => "Hello World!");

app.Run();

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
public class Poco3 : IPoco3_1, IPoco3_2, IPoco3_3 { }