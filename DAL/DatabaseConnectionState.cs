namespace Pocket_Librarian.DAL;

// The manual database connection is a process-wide override used by the
// diagnostics page. All repositories read this value so a successful manual
// connection actually affects the application, not only the pipeline test.
public static class DatabaseConnectionState
{
    private static string? _manualConnectionString;

    public static string? ManualConnectionString => _manualConnectionString;

    public static void SetManualConnectionString(string connectionString)
    {
        _manualConnectionString = connectionString;
    }

    public static string GetActiveConnectionString(string configuredConnectionString)
    {
        return _manualConnectionString ?? configuredConnectionString;
    }
}
