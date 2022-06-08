namespace Net.Leksi.KeyBox;

public interface IKeyBoxConfiguration
{
    IKeyBoxConfiguration AddPrimaryKey(Type targetType, IDictionary<string, Type> definition);
    IKeyBoxConfiguration AddPrimaryKey(Type targetType, Type exampleType);
    IKeyBoxConfiguration AddForeignKey(Type targetType, IDictionary<string, Type> definition);
    IKeyBoxConfiguration AddForeignKey(Type targetType, Type exampleType);
}
