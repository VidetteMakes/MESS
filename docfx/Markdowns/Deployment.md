# Quick Start for Linux Deployment
### Startup

Note: There are several seeders that will input required entries into the database if the database does **not** contain any entries
This includes a default **Technician** account that is required to log in to MESS and to start creating WorkInstructions, Users, etc.

#### Ensure that once the application has a database connection that you **Change Default Technician Password.** 
**This feature does not yet exist (as of 3/31/2026). However, once password support is added this step will be  very important.**

### (Linux/Ubuntu) Initial Project Setup
For more information on installation we followed this tutorial from [Microsoft](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-install?tabs=dotnet9&pivots=os-linux-ubuntu-2404)

##### This tutorial utilizes Ubuntu version **24.04**
To list Ubuntu version use:
```bash
cat /etc/*ease
```


#### .NET Setup
Currently MESS utilizes .NET 10 as of 3/2/2026

##### Note: The SDK installs both the SDK itself and the **runtime**. If you are developing MESS ensure the SDK is installed. If you are only deploying the application install either the SDK or only the **ASP.NET Core Runtime**

- Check if the .NET SDK is already installed
```bash
dotnet --list-sdks
```

- Check if the .NET Runtime is already installed
```bash
dotnet --list-runtimes
```

If you need to install either the SDK or the Runtime please follow this tutorial from [Microsoft](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-install?tabs=dotnet9&pivots=os-linux-ubuntu-2404)

NOTE: It is common practice to create a dedicated user for hosting purposes. This guide will demonstrate how to create a user and group for permissions.

### Creating New Group (OPTIONAL)
Where GROUPNAME is to be replaced with the desired name of the group for the user.
```bash
sudo addgroup GROUPNAME
```

### Creating New User (OPTIONAL)

-m Creates a directory for the new user
-g Assigns the new user to the group. Note that if you did not create a group before or do not want to add this user to a group ignore the _-g GROUPNAME_ flag

```bash
sudo useradd -m -g GROUPNAME USERNAME
```

### Setting Password
Use the following command to set the password for the user.

```bash
sudo passwd USERNAME
```

### Switching Users
Use the following command to switch to the newly created user.

```bash
su USERNAME
```

### Applying Bash Shell (Optional)
When creating a new user, you may notice that the shell experience is different. This is due to the way that Ubuntu creates new users. To change this simply run the following commands while logged in as the new user.

```bash
chsh

# Enter the user's password when prompted
/bin/bash

exit

# Login as the new user to refresh the shell
```

#### Deployment (Ubuntu)
##### Setup production environment variables.

### Create an environment file
```bash
sudo mkdir /etc/mess
sudo nano /etc/mess/env
```

2. Add the following lines to the */etc/mess/env* file

```bash
ASPNETCORE_ENVIRONMENT="Production"
ConnectionStrings__MESSConnection="ENTER_CONNECTION_STRING_HERE"

ASPNETCORE_ENVIRONMENT="Development"
ConnectionStrings__MESSConnection="ENTER_CONNECTION_STRING_HERE"
```

The connection strings should look something like:
```txt
Server=localhost; Database=MESS_Data; User Id=sa; Password=ENTER_YOUR_DB_PASSWORD_HERE; TrustServerCertificate=True;
```

3. Save the file and change permissions
```bash
sudo chown USERNAME:USERNAME /etc/mess/env
sudo chmod 600 /etc/mess/env
```

### Publish
There are several ways to publish the application for deployment. Some of these options include building the source on a separate machine and then copying the files over. Another option is to clone the repo onto the hosting server and to build directly on the hosting server (not recommended for production environment). This guide will be compatible with either option.
Visit ``README.md`` located in ``MESS/deployment-resources`` for further information on publishing. This includes a shell script for
automating the publishing of MESS code (``publish.sh``).

### Install Nginx
For this guide we will be using Nginx, but the same outcome can be reached with Apache or any other web server.
Using this guide: https://docs.nginx.com/nginx/admin-guide/installing-nginx/installing-nginx-open-source/
Please install Nginx for the OS that MESS will be hosted on.


### Create web directory
After Nginx is installed the *www* directory will be available within the /var directory structure.
Create a new directory for the application.
```bash
sudo mkdir /var/www/mess
```

### Apply Permissions
Apply permissions to the newly created directory so that only the newly created user can access it.
```bash
sudo chown -R USERNAME:GROUPNAME /var/www/mess
```

### Cloning the repository
Within a directory on the newly created users account, run the following command to clone the MESS repository.

```bash
git clone https://github.com/VidetteMakes/MESS.git
```

### 1. Navigate to the solution directory
```bash
cd /MESS/MESS
```

### 2. Run the build
Note: If you are building on a separate machine leave out the *-o /var/www/mess* flag as this will direct the output of the build.
```bash
dotnet publish --configuration Release -o /var/www/mess
```

This will build MESS for production. It will store the build files (including the dll) in the following directory: MESS/MESS/MESS.Blazor/bin/Release/net10.0/publish

### 3. Copy build output (Optional if you are not building on the hosting machine)
After running the production build, you will need to copy the entirety of the /bin/Release/net10.0/publish directory to a directory within /var/www/

The command will look something along the lines of:
NOTE: Some of the following commands may require sudo privileges.

```bash
mkdir /var/www/MESS_PRODUCTION

cp /MESS/MESS/MESS.Blazor/bin/Release/net10.0/publish /var/www/MESS_PRODUCTION
```

### Systemd Service File (Optional)
This will allow the mess application to be monitored as a service.
Create the following file:

```bash
sudo nano /etc/systemd/system/mess.service
```

Within this file past the following. You may need to change some of these fields depending on your configuration:
You will need to change the following fields:
* User
* WorkingDirectory
* EnvironmentFile

```nano
[Unit]
Description=My .NET Web App
After=network.target

[Service]
WorkingDirectory=/var/www/myapp
ExecStart=/usr/bin/dotnet /var/www/myapp/MyApp.dll
Restart=always
RestartSec=10
SyslogIdentifier=myapp
User=USERNAME
Environment=ASPNETCORE_ENVIRONMENT=Production
EnvironmentFile=/etc/myapp/env  # Environment File Location

[Install]
WantedBy=multi-user.target
```

NOTE: The sudo user should not be hosting this web application as this can lead to security vulnerabilities. Ideally there should be a designated user that is created for this task.


### Enable & Start MESS Service
```bash
sudo systemctl daemon-reload
sudo systemctl enable mess.service
sudo systemctl start mess.service
```

Note: If you want to check the status of this service use the following command:

```bash
sudo systemctl status mess.service
```

### Reverse Proxy
Note: For the reverse proxy to work effectively it will require a registered domain name.

### Remove contents of default Nginx file
```bash
sudo nano /etc/nginx/sites-available/default
```
Replace the entire file contents with the following:

```txt
map $http_connection $connection_upgrade {
  "~*Upgrade" $http_connection;
  default keep-alive;
}

server {
  listen        80;
  server_name   example.com *.example.com;
  location / {
      proxy_pass         http://127.0.0.1:5000/;
      proxy_http_version 1.1;
      proxy_set_header   Upgrade $http_upgrade;
      proxy_set_header   Connection $connection_upgrade;
      proxy_set_header   Host $host;
      proxy_cache_bypass $http_upgrade;
      proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
      proxy_set_header   X-Forwarded-Proto $scheme;
  }
}
```

### Restart Nginx Service

```bash
sudo systemctl restart nginx.service
```



#### Database
For database setup MESS uses PostgreSQL as its SQL provider. In terms of setting up the database itself, we leave up to the user since there are a variety of different methods on how to setup a Postgres database. However, if you are simply wanting to test MESS in a production environment please refer to the database setup within the **Local Development** section of this Wiki as it will guide you on how to setup a Dockerized Postgres instance.

### (Windows) Initial Project Setup To Be Implemented
...

## EF Core (Common Database Interactions)
**NOTE:** *For both of these commands you must be in the MESS.Data directory, otherwise you must specify the base project as well.*
#### Migrations
```shell
dotnet ef migrations add "MIGRATION_NAME"
```

#### Updates
```shell
dotnet ef database update
```


### Secret Management 
**NOTE:** *This should only be used for development purposes*

Instead of using "appsettings.*.json" for managing environment variables or user secrets
we have opted to utilize the .NET *Secret Manager* for development purposes.
For more information see [.NET Secret Manager](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-9.0&tabs=linux)

#### Quick Start
NOTE: If you are deploying to production you should **not** use the Secret Manager.
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
Below is an example of how to set the connection string for the development database using the Secret Manager.
```shell
dotnet user-secrets set "ConnectionStrings:MESSConnection" "Server=myhost;Database=mess;Port=5432;User Id=myuser;Password=MySuperSecurePassword;"
```