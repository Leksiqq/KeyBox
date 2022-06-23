namespace Net.Leksi.KeyBox;

public class PrimaryKeyEventArgs: EventArgs
{
    public bool IsReading { get; internal set; } = false;
    public Type TypeToConvert { get; set; } = null!;
    public IKeyRing KeyRing { get; internal set; } = null!;
    public bool Interrupt { get; set; } = false;
    public object Value { get; set; } = null!;
}
