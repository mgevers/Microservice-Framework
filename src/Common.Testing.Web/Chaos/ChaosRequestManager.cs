namespace Common.Testing.Integration.Chaos;

public class ChaosRequestManager
{
    public IDictionary<object, bool> ChaosMap { get; set; } = new Dictionary<object, bool>();
}
