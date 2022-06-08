namespace Net.Leksi.KeyBox;

public interface IKeyBoxConfiguration
{
    IKeyBoxConfiguration AddPrimaryKey(Type targetType, IDictionary<string, Type> definition);
    IKeyBoxConfiguration AddPrimaryKey(Type targetType, Type exampleType);
}
