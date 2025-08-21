namespace Common.Testing.Integration.ConnectionStrings;

public class SqlConnectionString
{
    private readonly string _connectionString;

    public SqlConnectionString(string connectionString)
    {
        _connectionString = connectionString;
    }

    public string Server => GetValueFromSection("Server");
    public string UserId => GetValueFromSection("User Id");
    public string Password => GetValueFromSection("Password");

    private string GetValueFromSection(string section)
    {
        var startIndex = _connectionString.IndexOf(section);
        var substring = _connectionString.Substring(startIndex + section.Length);
        var value = substring.Trim('=');
        var endIndex = value.IndexOf(';');

        return value[..endIndex];
    }
}
