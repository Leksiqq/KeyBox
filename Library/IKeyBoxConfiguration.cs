namespace Net.Leksi.KeyBox;

public interface IKeyBoxConfiguration
{
    IKeyBoxConfiguration AddPrimaryKey(Type targetType, IDictionary<string, object> definition);
}
