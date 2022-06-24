namespace Net.Leksi.KeyBox;

public interface IKeyBox
{
    IKeyRing? GetKeyRing(object source);
    IKeyRing? GetKeyRing(Type type);
    IKeyRing? GetKeyRing<T>() where T : class;
    bool HasMappedPrimaryKeys(Type type);
    bool HasMappedPrimaryKeys<T>();
}
