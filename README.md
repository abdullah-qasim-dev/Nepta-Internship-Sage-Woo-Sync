{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "WooCommerce": {
    "Url": "https://developer.meernmeer.com/wp-json/wc/v3/",
    "Key": "ck_15e8da0be2d8f35f073a08d7ff423c0402dbba02",
    "Secret": "cs_7337c6df3ee04a7518974cff16c05db12ef78bd5"
  },
  "Sage": {
    "CompanyPath": "C:\\ProgramData\\Sage\\Accounts\\2024",
    "UserName": "adnan",
    "Password": "123",
    "WorkSpace": "Sage"
  },
  "Scheduling": {
    "RunType": "Hourly", // Options: "Hourly", "Daily", "Weekly", "Monthly"
    "Hour": 0, // Set the hour (24-hour format) for daily/weekly/monthly runs
    "Minute": 0, // Optional: minute within the hour to run
    "DayOfWeek": "Monday", // For weekly runs, specify the day of the week
    "LastRunTime":"",
  },
  //Every Hour 0 minutes
  //"Scheduling": {
  //  "RunType": "Hourly",
  //  "Minute": 0
  //}
  //EveryDay
  //"Scheduling": {
  //  "RunType": "Daily",
  //  "Hour": 14, // 2:00 PM every day
  //  "Minute": 0
  //}
  //Every Week on Sunday
  //"Scheduling": {
  //  "RunType": "Weekly",
  //  "DayOfWeek": "Sunday",
  //  "Hour": 10, // 10:00 AM every Sunday
  //  "Minute": 0
  //}
}
