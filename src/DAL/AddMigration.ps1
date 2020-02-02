$env:DB_USER = 'postgres';
$env:DB_NAME = 'chatorleansdb';
$env:DB_PORT = '5432';
$env:DB_PASSWORD = 'qwe123';
$env:DB_HOST = 'localhost';
$env:ASPNETCORE_ENVIRONMENT = 'Development';

dotnet ef --startup-project ../API/ migrations add --context ChatDbContext InitMigration