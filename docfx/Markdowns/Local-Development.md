## MESS Local Development
This will outline a tutorial on how to get the MESS application running on your local machine.

#### A Note on Using Docker
For local development we have created a Dockerfile that will setup a local instance of the PostgreSQL database to run tests on. Of course this can be replaced by any other instance of a Postgres database. NOTE: You may have to modify the connection string for the database if you deviate from this guide.

### 1. Ensure that you have Docker **installed** and **running** on your machine.

Follow this guide for your desired Operating System if you currently do not have Docker installed: https://docs.docker.com/desktop/setup/install/windows-install/

### 2. Creating a PostgreSQL Docker Container
Open a bash or Windows terminal and enter the following commands:

These commands come from the [Docker Hub for PostgreSQL](https://hub.docker.com/_/postgres)

This command will download and start a Docker instance of a **SQL Server version 2022** on the **Linux Ubuntu 22.04** OS

It is recommended to replace `ENTER_YOUR_DB_PASSWORD_HERE` with a secure and complicated password.

```bash
docker run -d --name MESS_Data --hostname mess-db -e POSTGRES_USER=mess -e POSTGRES_PASSWORD="ENTER_YOUR_DB_PASSWORD_HERE" -e POSTGRES_DB=mess -p 5432:5432 postgres:latest
```
#### Apple Silicon Users (M1, etc.):
The docker image in the above command should work on ARM based systems such as the M1 Mac. If the above command does 
not work on your machine with an ARM based processor, consider adding **--platform linux/amd64** 
to your docker run command. The rest of the setup is identical. 

##### macOS Apple Silicon (M1, etc.) and ARM-based systems:
```bash
docker run --platform linux/amd64 -d --name MESS_Data --hostname mess-db -e POSTGRES_USER=mess -e POSTGRES_PASSWORD="ENTER_YOUR_DB_PASSWORD_HERE" -e POSTGRES_DB=mess -p 5432:5432 postgres:latest
```

### 3. Connecting to the Database
To connect to the database locally ensure that you have set up the connection string. For instructions on how to do so please refer to the section on setting up environment variables and secrets.

At this stage if the Docker container has no errors and is running, you should be able to connect to the PostgreSQL instance with a 3rd party tool such as:

* IDE Database Extensions
* psql Command Line Tool
* pgAdmin

If you are unable to connect to the database at this stage, ensure that the Docker instance has no errors.

To connect to your docker container you should be able to use a command similar to this if you have psql installed:
```bash
psql -h localhost -p 5432 -U myUser postgres
```

### 4. Cloning the Repository
At this point you can clone the MESS repository in your IDE of choice. This process will vary depending upon your development environment.

### 5. Connection String
In new development environments, navigate to the MESS.Data directory and use the following command to set a user secret (remember to use your database password from earlier):

```shell
dotnet user-secrets set "ConnectionStrings:MESSConnection" "Host=localhost;Port=5432;Database=mess;Username=myPostgresUser;Password=My Secure Password;Include Error Detail=true"
```

The connection string within the secrets file should look like:

```json
  "ConnectionStrings:MESSConnection": "Host=localhost;Port=5432;Database=mess;Username=myPostgresUser;Password=My Secure Password;Include Error Detail=true"
```

NOTE: If you receive an error when attempting to apply the Entity Framework Core migrations you may have to manually create the _MESS_Data_ database within the PostgreSQL instance. You should be able to use an IDE database extension, pgAdmin tool, or some other database tool to alter the instance.

Additional NOTE: Depending on your environment, especially if it is in the Cloud, you may need to add ``Ssl Mode=Require;``
to your connection string.
### 6. Applying Database Migrations
Once the connection string is set, you will need to update the database with 2 sets of migrations. The first being **ApplicationContext** which contains the database logic for the domain of MESS. And the second being **UserContext** which contains the models for Microsoft's Identity, and is required for authentication and authorization.

Note:  On Windows, you may need to run this first:
```bash
dotnet tool install --global dotnet-ef
```

To apply these migrations run the following commands in the _MESS.Data_ project in the CLI.

```bash
dotnet ef database update --startup-project ..\MESS.Blazor\MESS.Blazor.csproj
```

### 7. Seeders
At this stage MESS is ready for development, but before you start the project it is important to note that there are several seeders that will populate the PostgreSQL database with test data. This includes a default TechnicianUser, along with an assortment of test Products, WorkInstructions, Parts, etc.
The seeders only run when the tables have no entries in them, but they are independent of each other. So if the Users table has 5 users, but you decide to wipe out the Work Instructions table, the seeder will populate the Work Instructions table but not the Users table. To run MESS, simply run the Blazor project from within your IDE.

#### IDE
For the MESS application the IDE should not matter. During initial development the team used a variety of Open Source Compliant IDE's and Editors including:
- Visual Studio Community
- Visual Studio Code
- JetBrain's Rider