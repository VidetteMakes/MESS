# Deploying MESS on a Raspberry Pi
This setup assumes that you already have a PostgreSQL database for MESS up and running. For information on how to setup Microsoft SQL Server for production, visit [here](https://learn.microsoft.com/en-us/sql/database-engine/install-windows/install-sql-server?view=sql-server-ver17).

If you are looking to deploy MESS without a docker container for the web server and with **Nginx**, visit the [deployment](https://github.com/SensitTechnologies/MESS/wiki/Deployment) wiki page.

### 1. Flash an  SD Card with the latest 64 bit Raspberry Pi OS.
This can be done in any way you wish, although the simplest way to do so is with the [Raspberry Pi Imager](https://www.raspberrypi.com/software/). All variants of the OS (the standard install, install with additional recommended software, or the Lite version ) will all work. The Raspberry Pi Imager software conveniently allows you to set a custom username, hostname, and password. Like always, remember to take note of what you choose for these.

### 2. Connect your Pi to the Internet
Once you can boot into your Pi, connect it to the Internet. 

**Important Note:** Because this Pi will be running a web server, it is recommended to assign your Pi a static IP address to make full DNS setup easier. However, because the Raspberry Pi OS comes with mDNS, this is not essential.

### 3. (Optional) Enable Raspberry Pi Connect
If you would like easy remote access to your Pi from anywhere in the world with an Internet connection, check out [Raspberry Pi Connect](https://www.raspberrypi.com/software/connect/)

### 4. Update Raspberry Pi Packages
Before installing dependencies for MESS, we must update all packages on the system. You can use these commands...

```bash
sudo apt update
```

```bash
sudo apt full-upgrade
```

```bash
sudo apt autoremove
```

If prompted to reboot during these updates, it is recommended to do so.

### 5. Install Dependencies
For this deployment setup, we will assume that you would like to run your MESS web server inside a docker container.

To Install Docker

```bash
sudo apt install docker.io
```

To Install .NET

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel STS
```

After installing dotnet, you will want to include it in your system path. To do this, we can run these commands to append the pathing information to our `.bashrc`.

```bash 
echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.bashrc
```

```bash
echo 'export PATH=$PATH:$HOME/.dotnet' >> ~/.bashrc
```

To apply the .bashrc changes to our current shell session run

```bash
source ~/.bashrc
```

### 6. Download MESS
To ensure compatibility with the auto-update script, run this command to the latest version of MESS into your home directory.

```bash
git clone https://github.com/VidetteMakes/MESS.git
```

### 7. Set the Repository Directory as Safe for Git
To allow git to work properly with the files created by root from the update script, run this command.
```bash
sudo git config --global --add safe.directory ~/MESS
```
### 8. Update `~/MESS/deployment-resources/raspberry-pi/.env`
Inside this file you should see...
```dotenv
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__MESSConnection=Server=YOUR_CONNECTION_STRING_GOES_HERE
```
**Remember to populate your database connection string in this file.** Otherwise, the web server will be unable to connect to your database. To see how you can setup a SQL Server database in a docker container (not recommended for full production scenarios but good for testing) visit the [local deployment wiki page](https://github.com/SensitTechnologies/MESS/wiki/Local-Development).

### 9. Make the `mess-update.sh` Script Executable
Run this chmod command to make the update script executable.
```bash
chmod +x ~/MESS/deployment-resources/raspberry-pi/update-mess.sh
```

### 10. Set up a Cron Job for the Script
To run the mess-update script on a schedule, you can set up a cron job.

Run
```bash
sudo crontab -e
```
Then, append a line like this to the end of the file. **Remember to replace YOUR-USERNAME with your actual username.**
```cron
59 23 * * 1-4 /home/YOUR-USERNAME/MESS/deployment-resources/raspberry-pi/update-mess.sh >> /home/YOUR-USERNAME/MESS/deployment-resources/raspberry-pi/update.log 2>&1
```

### 11. Set up Logrotate to Manage Deployment Logs
Using **logrotate** to manage logs for deployments allows the automated pruning of logs so they don't pile up.

To set this up, first create a config file for the deployment script logs.
```bash
sudo nano /etc/logrotate.d/mess-update
```
Inside your text editor, add this. 
```conf
/home/YOUR-USERNAME/MESS/deployment-resources/raspberry-pi/update-mess.sh update.log {
    weekly
    rotate 4
    compress
    missingok
    notifempty
    create 644 admin admin
}
```

**For those curious, here is a breakdown of the options...**

`weekly` → Rotate once per week (other options: `daily`, `monthly`).

`rotate 4` → Keep the last 4 rotated logs; older ones are deleted automatically.

`compress` → Compress old logs with gzip.

`missingok` → Don’t fail if the log file doesn’t exist yet.

`notifempty` → Don’t rotate if the log is empty.

`create 644 user group` → After rotation, create a new log file with these permissions.

**(Optional)** If you would like to do a dry run of your log rotation setup for deployments (without actually rotating the logs), you can run the command below.
```bash
sudo logrotate -d /etc/logrotate.d/mess-update
```

**Important Note:** By default, `logrotate` runs via a cron job daily (`/etc/cron.daily/logrotate`). It will check your config and rotate weekly or as configured.

### Deployment Completed!
At this point you should have MESS running on your Raspberry Pi (or other Unix based machine). Congratulations!
