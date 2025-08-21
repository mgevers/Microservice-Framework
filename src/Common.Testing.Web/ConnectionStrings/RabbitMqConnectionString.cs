namespace Common.Testing.Integration.ConnectionStrings;

public class RabbitMqConnectionString
{
    public RabbitMqConnectionString(string connectionString)
    {
        var endProtocolIndex = connectionString.IndexOf("://");
        Protocol = connectionString.Substring(0, endProtocolIndex);

        connectionString = connectionString.Substring(endProtocolIndex + 3);
        var endUserIndex = connectionString.IndexOf(':');
        User = connectionString.Substring(0, endUserIndex);

        connectionString = connectionString.Substring(endUserIndex + 1);
        var endPasswordIndex = connectionString.IndexOf('@');
        Password = connectionString.Substring(0, endPasswordIndex);

        connectionString = connectionString.Substring(endPasswordIndex + 1);
        var endHostIndex = connectionString.IndexOf(':');
        Host = connectionString.Substring(0, endHostIndex);

        connectionString = connectionString.Substring(endHostIndex + 1);
        var endPortIndex = connectionString.IndexOf('/');
        var portString = connectionString.Substring(0, endPortIndex);
        Port = Convert.ToUInt16(portString);
    }

    public string Protocol { get; private set; }
    public string Host { get; private set; }
    public string User { get; private set; }
    public string Password { get; private set; }
    public ushort Port { get; private set; }
}
