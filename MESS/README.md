# MESS - Reference Sheet
### Startup

Note: There are several seeders that will input required entries into the database if the database does **not** contain any entries
This includes a default **Technician** account that is required to log in to MESS and to start creating WorkInstructions, Users, etc.

### **Change Default Technician Password.**


## Work Instruction Import

### Currently Supported Rich-Text Features
* Bold
* Hyperlinks
* Underline
* Italics
* Strikethrough
* Colors (Only **Theme** and Direct **Font** Color. Index color currently NOT supported)

### Not Supported Rich-Text Features
* Fonts
* Index colors
* Formulas
* Other more specific features.

## EF Core (Common Database Interactions)
**NOTE:** *For both of these commands you must be in the MESS.Data directory, otherwise you must specify the base project as well.*
#### Migrations
```shell
dotnet ef migrations add "MIGRATION_NAME" --startup-project ..\MESS.Blazor\MESS.Blazor.csproj
```

#### Updates
```shell
dotnet ef database update --startup-project ..\MESS.Blazor\MESS.Blazor.csproj
```


### Secret Management 
**NOTE:** *This should only be used for development purposes*

Instead of using "appsettings.*.json" for managing environment variables or user secrets
we have opted to utilize the .NET *Secret Manager* for development purposes.
For more information see [.NET Secret Manager](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-9.0&tabs=linux)

### Quick Start
In terms of the MESS project you should not have to initial the Secret Manager since it will
already be in each appropriate project. However, in the case that the Secret Manager is being
redone use the following command:


##### Instantiate User Manager (New Projects only)
```shell
dotnet user-secrets init
```

##### Set a secret
```shell
dotnet user-secrets set "Key" "Value"

Example

dotnet user-secrets set "DatabaseConnection" "Data Source = 123456.db"

```

#### Setup Development Database connection
The current app uses PostgreSQL, not SQLite. The Blazor app applies EF migrations on startup, so a
Postgres instance must be running before `dotnet run`.

From the `MESS.Blazor` directory:

```shell
dotnet user-secrets set "ConnectionStrings:MESSConnection" "Host=localhost;Port=5432;Database=mess;Username=mess;Password=mypassword123;Include Error Detail=true"
```

One local option with Docker:

```shell
docker run --name mess-postgres ^
  -e POSTGRES_DB=mess ^
  -e POSTGRES_USER=mess ^
  -e POSTGRES_PASSWORD=mypassword123 ^
  -p 5432:5432 ^
  -d postgres:17
```

Then start the app:

```shell
dotnet run --launch-profile http
```
