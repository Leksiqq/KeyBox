namespace Net.Leksi.KeyBox;

public interface IKeyBox
{
    IKeyRing? GetKeyRing(object source);
    bool HasMappedKeys(Type type);
}
