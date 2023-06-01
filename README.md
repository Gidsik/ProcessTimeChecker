## ProcessTimeChecker
(*work in progress*)

* todo: merge open-close events into one entry with several timestamps and corresponding timespan

### About
Solution contains several projects:

| project                   |  type     | description |
|---|---|---|
| InterfaceContractLibrary  | (dll)     | grpc contract lib to link with
| WorkerService             | (exe)     | asp.net app that implements gprc servera and tracks user activity and foreground window changes to sqlite db 
| GUIClient                 | (todo)    | gui client that connects to WorkerService via grpc and display data tracked by it + helps with configuration
| ConsoleClient             | (todo)    | console client that able to do easy grpc queries to WorkerService


### How to use
Run WorkerService app as a Windows service with autorun.

It will track your activity (active or idle in case there wasn't any use input tracked by OS for some time)

it will also track foreground window change event.

All of it will go to SQLite db. You can use custom app to view tracked data or use one of bundled Client app to get easy access to data via grpc api.

### How data stored
WorkerService do no track **time** it tracks events instead, so you can calculate time by yourself or using Client app.

Events examples:

| ActivityEvent     |  Timestamp            | 
|---|---|
| ActivityStarted   | 03.03.2023 13:00:05   | 
| ActivityStopped   | 03.03.2023 15:22:43   | 
| IdleStarted       | 03.03.2023 15:22:43   | 
| IdleStopped       | 04.03.2023 10:45:14   | 
| ActivityStarted   | 03.03.2023 10:45:14   | 

| WatchedAppEvent   | ProcessName   |  Timestamp            | 
|---|---|---|
| WindowEntered     | Chrome        | 03.03.2023 13:00:05   | 
| WindowLeaved      | Chrome        | 03.03.2023 14:18:43   | 
| WindowEntered     | Explorer      | 03.03.2023 14:18:43   | 
| WindowLeaved      | Explorer      | 03.03.2023 14:25:14   | 
| WindowEntered     | Chrome        | 03.03.2023 14:25:14   | 
| WindowLeaved      | Chrome        | 03.03.2023 15:22:43   | 
| WindowEntered     | Photoshop     | 03.03.2023 15:22:43   | 