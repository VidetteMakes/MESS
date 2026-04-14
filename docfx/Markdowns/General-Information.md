# General Information regarding MESS (Manufacturing Execution Software System)

## Overview

### Technologies Used
* SDK: **.NET 10.0**
* Web Framework: **Blazor**
* Database (although changeable with a little bit of effort): **PostgreSQL**
* Testing Frameworks:
    * **xUnit** for primary testing
    * **bUnit** for Blazor component testing
    * **Moq** for mocking dependencies in tests
    * **Playwright** for end-to-end testing of the application
* UI Framework: **Bootstrap**
* UI Library: **FluentUI** and **MudBlazor**

### Project Structure

- **MESS.Blazor:** Frontend Project that stores the User Interface
- **MESS.Data:** Project dedicated to storing database models, some seeders, database logic, migrations, and more. Reasoning behind having a data project is for the anticipation of a future WPF or similar desktop application, that may utilize the same data layer logic.
- **MESS.Services:** Stores all services and data transfer objects for the application. Essentially the core logic that does not directly change the UI of MESS is located here. Similar to MESS.Data, Services is 
its own project in anticipation of a migration to WPF or another web frontend in the future.
- **MESS.Tests:** Stores all tests for the entire MESS Solution.

### Prerequisites
* .NET 10.0
* A running instance of PostgreSQL
* A basic understanding of SQL (Structured Query Language) and relational databases.

### System Requirements
* Dual-Core CPU
* 2GB RAM
* High Speed bandwidth