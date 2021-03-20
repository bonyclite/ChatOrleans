namespace Client.Xamarin
{
    public partial class App
    {
        private const string Host = "3.134.149.250";
        private const int Port = 5432;
        private const string Password = "qwe123";
        private const string User = "postgres";
        private static string GateWayConnectionString => $"Host={Host};Port={Port};Username={User};Password={Password};Database=orleansDb;";
    }
}