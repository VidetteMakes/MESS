# MESS (Manufacturing Execution Software System)
[![.NET](https://github.com/VidetteMakes/MESS/actions/workflows/dotnet.yml/badge.svg)](https://github.com/VidetteMakes/MESS/actions/workflows/dotnet.yml)
[![Build](https://github.com/VidetteMakes/MESS/actions/workflows/build.yml/badge.svg)](https://github.com/VidetteMakes/MESS/actions/workflows/build.yml)
[![Deploy](https://github.com/VidetteMakes/MESS/actions/workflows/deploy.yml/badge.svg)](https://github.com/VidetteMakes/MESS/actions/workflows/deploy.yml)
[![Deploy API Documentation](https://github.com/VidetteMakes/MESS/actions/workflows/deploy_documentation.yml/badge.svg)](https://github.com/SensitTechnologies/MESS/actions/workflows/deploy_documentation.yml)

# Introduction
Welcome to the MESS Wiki! This wiki currently contains documentation for setting up and using MESS, as well as some technical documentation for developers.

If you are a new developer for MESS, please visit the [Local Development Page](https://videttemakes.github.io/MESS/Markdowns/Local-Development.html)
to learn about how to develop MESS with your favorite IDE.

If you want to deploy your own instance of MESS, please visit either the [Deployment Page](https://videttemakes.github.io/MESS/Markdowns/Deployment.html) 
or the [Raspberry Pi Deployment Page](https://videttemakes.github.io/MESS/Markdowns/Raspberry-Pi-Deployment.html) for more information.

### General Note on MESS Startup

Note: There are several seeders that will input required entries into the database if the database does **not** contain any entries
This includes a default **Technician** account that is required to log in to MESS and to start creating WorkInstructions, Users, etc.

### **Change Default Technician Password.**
As soon as password authentication is added to MESS, it will be very important to change the default password for the Technician account.
This user has role delegation privleges and has access to most data manipulation features of MESS.


## Work Instruction Import and Export

### Currently Supported Rich-Text Features
* Bold
* Hyperlinks
* Underline
* Italics
* Strikethrough
* Colors (Only **Theme** and Direct **Font** Color. Index color currently NOT supported)

Images are fully supported for import and export of work instructions.

### Not Supported Rich-Text Features
* Fonts
* Index colors
* Formulas
* Other more specific features.

## EF Core (Common Database Interactions)
**NOTE:** *For both of these commands you must be in the MESS.Data directory, otherwise you must specify the base project as a command line argument*
#### Adding Migrations
```shell
dotnet ef migrations add "MIGRATION_NAME"
```

#### Applying Migrations / Updating Database
```shell
dotnet ef database update
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
```shell
dotnet user-secrets set "ConnectionStrings:MESSConnection" "Data Source=developmentMESSDb.db"
```