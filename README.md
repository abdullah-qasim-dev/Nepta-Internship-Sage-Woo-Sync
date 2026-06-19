# Nepta Solutions — Sage 50 & WooCommerce Integration

A C# integration system developed during a Software Engineer Internship at **Nepta Solutions** (June 2025 – August 2025). The project automates the synchronization of customer and order data between **WooCommerce** (e-commerce platform) and **Sage 50** (accounting software), eliminating manual data entry and improving operational efficiency.

## Project Overview

The core objective was to establish reliable, automated communication between WooCommerce and Sage 50 using REST APIs and Sage Data Objects (SDO). The system ensures that customer and order records created on the e-commerce platform are accurately and consistently reflected within the accounting system — including new customer creation, order processing, and record updates.

## Key Features

- **Automated Data Sync** — Retrieves customer and order data from WooCommerce via API and transfers it into Sage 50.
- **Customer Management** — Detects and creates new customer records, and updates existing ones to keep both systems consistent.
- **Order Processing** — Validates and processes order data, ensuring accurate transfer between platforms.
- **Scheduled Execution** — Configurable scheduling system (hourly, daily, weekly) to run synchronization automatically at set intervals.
- **Error Handling & Logging** — Tracks sync activity and surfaces issues for easier troubleshooting and debugging.

## Tech Stack

- **Language:** C#
- **Accounting Integration:** Sage 50 via Sage Data Objects (SDO)
- **E-commerce Integration:** WooCommerce REST API
- **Architecture:** Layered design with separate repository and service layers for Sage and WooCommerce

## Project Structure

```
SageIntegration/
├── Client/                # API client logic for external communication
├── Configuration/         # App configuration and settings handling
├── Models/                # Data models shared across the system
├── SageRepository/        # Data access layer for Sage 50
├── SageService/           # Business logic for Sage operations
├── WooRepository/         # Data access layer for WooCommerce
├── WooServices/           # Business logic for WooCommerce operations
├── WindowService/         # Background worker service for scheduled sync
├── Worker.cs              # Main background worker entry point
├── Program.cs             # Application entry point
└── appsettings.json       # Configuration file (see below)
```

## Configuration

The application is configured via `appsettings.json`. Below is a sample structure with placeholder values — **replace these with your own credentials before running:**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "WooCommerce": {
    "Url": "https://your-site.com/wp-json/wc/v3/",
    "Key": "YOUR_WOOCOMMERCE_KEY",
    "Secret": "YOUR_WOOCOMMERCE_SECRET"
  },
  "Sage": {
    "CompanyPath": "C:\\ProgramData\\Sage\\Accounts\\YEAR",
    "UserName": "YOUR_SAGE_USERNAME",
    "Password": "YOUR_SAGE_PASSWORD",
    "WorkSpace": "Sage"
  },
  "Scheduling": {
    "RunType": "Hourly",
    "Hour": 0,
    "Minute": 0,
    "DayOfWeek": "Monday",
    "LastRunTime": ""
  }
}
```

### Scheduling Options

The `Scheduling` section supports multiple run types:

**Every hour, on the hour:**
```json
"Scheduling": {
  "RunType": "Hourly",
  "Minute": 0
}
```

**Every day at a fixed time:**
```json
"Scheduling": {
  "RunType": "Daily",
  "Hour": 14,
  "Minute": 0
}
```

**Every week on a specific day:**
```json
"Scheduling": {
  "RunType": "Weekly",
  "DayOfWeek": "Sunday",
  "Hour": 10,
  "Minute": 0
}
```


## How to Run

1. Clone the repository
2. Open `SageIntegration.sln` in Visual Studio
3. Update `appsettings.json` with your own WooCommerce and Sage credentials
4. Ensure Sage 50 is installed and accessible at the configured `CompanyPath`
5. Build and run the project (F5 or the Local Windows Debugger)

## Skills & Concepts Applied

- C# application development
- RESTful API integration (WooCommerce, WordPress)
- Sage Data Objects (SDO) for accounting software integration
- Data validation, synchronization, and error handling
- Background/scheduled service design
- Debugging and log-based troubleshooting in a real business environment

## Internship Context

This project was developed as part of a remote Software Engineer Internship at Nepta Solutions (UK), focused on real-world enterprise integration between e-commerce and accounting systems.
