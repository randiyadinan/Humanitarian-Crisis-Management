Humanitarian Crisis Management System

Project Overview

The Humanitarian Crisis Management System is a web-based application designed to support humanitarian crisis management activities. It provides a platform for managing crisis events, assistance requests, resources, shelters, volunteers, donations, and AI-based recommendations.

Technologies Used

Backend

* ASP.NET Core Web API
* C#
* .NET 10
* Entity Framework Core

Database

* SQLite

API Documentation

* Swagger / OpenAPI

Frontend

* HTML

Project Structure

HumanitarianAPI/
│
├── Controllers/
├── Data/
├── Migrations/
├── Models/
├── Properties/
├── frontend/
│   └── index.html
│
├── HumanitarianAPI.csproj
├── Program.cs
├── appsettings.json
├── HumanitarianAPI.http
├── .gitignore
└── README.md

Main System Modules

* User Management
* Authentication
* Crisis Event Management
* Assistance Request Management
* Resource Management
* Shelter Management
* Volunteer Management
* Donation Management
* Notification Management
* AI Recommendation Management

Prerequisites

Install the following before running the project:

* .NET 10 SDK
* Visual Studio Code or Visual Studio
* Git

Installation and Setup

1. Clone the Repository

git clone https://github.com/randiyadinan/Humanitarian-Crisis-Management.git

2. Navigate to the Backend Project

cd Humanitarian-Crisis-Management

3. Restore Dependencies

dotnet restore

4. Run the Application

dotnet run

5. Access API Documentation

When the application is running in Development mode, open the Swagger URL displayed in the terminal.

Database Configuration

The application uses SQLite as its database.

Database connection is configured in appsettings.json.

"ConnectionStrings": {
  "DefaultConnection": "Data Source=humanitarian.db"
}

Entity Framework Core is used to manage database operations and migrations.

API Testing

The API can be tested using:

* Swagger UI
* HTTP requests
* Postman

Configuration Notes

* Swagger is enabled in the Development environment.
* SQLite is used for local database storage.
* CORS is configured in the application.
* The current development CORS policy allows all origins, methods, and headers. This should be restricted before production deployment.

Known Limitations

* The current configuration is intended for development.
* Production security and deployment settings may require further configuration.
* Test credentials and API endpoint details should be added when finalized.

Team Collaboration

This project is maintained using Git and GitHub.

Team members can contribute by creating branches, committing changes, and submitting pull requests.

Repository

GitHub Repository:

https://github.com/randiyadinan/Humanitarian-Crisis-Management