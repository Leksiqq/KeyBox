namespace Net.Leksi.KeyBox;

public interface IKeyBox
{
    IKeyRing? GetKeyRing(object source);
    bool HasMappedPrimaryKeys(Type type);
}
