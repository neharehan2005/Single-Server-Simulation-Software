# Single Server Simulation Software

A **C# Windows Forms application** for modeling and simulating a **single-server queueing system**. The software allows users to generate customer data, load historical data from CSV files, run a queue simulation, analyze system performance, and view the results through a graphical user interface.

This project was developed as part of **Modeling & Simulation** coursework.

## Overview

A single-server queueing system consists of customers arriving at a system and waiting for one available server.

This software models the complete process:

**Customer Arrival → Waiting Queue → Server → Service Completion**

The application can work with both **generated/random data** and **historical CSV data**.

The simulation helps analyze how a single-server system behaves and provides important queueing performance measures such as:

* Average Waiting Time
* Maximum Waiting Time
* Average Time in System
* Server Utilization
* Total Customers
* Total Simulation Time
* Queue-related performance measures

## Features

### 1. Customer Data Generation

The application can generate customer arrival and service data for simulation.

Generated data can be used as input for the single-server queue simulation.

### 2. CSV Data Input

The application supports loading customer data from CSV files.

Example datasets included in the project:

* `CSVDataToUpload.csv`
* `SapphireData.csv`

This makes it possible to compare simulation results using different input datasets.

### 3. Single-Server Queue Simulation

The simulation processes customers according to their:

* Arrival time
* Service time
* Waiting time
* Service start time
* Service completion time

The server handles one customer at a time, while other customers wait in the queue.

### 4. Performance Analysis

After the simulation, the application calculates important performance measures, including:

* Total number of customers
* Total simulation time
* Average waiting time
* Maximum waiting time
* Average time in system
* Server utilization

### 5. Historical Data Analysis

Historical customer data can be analyzed to understand the behavior of the queueing system and compare it with generated simulation data.

### 6. Graphical User Interface

The project uses **Windows Forms** to provide a user-friendly interface.

The interface allows users to:

* Select an input source
* Generate or load data
* Run the simulation
* View customer-level results
* View summary statistics
* Analyze simulation performance

## Technologies Used

* **C#**
* **.NET**
* **Windows Forms**
* **System.Windows.Forms.DataVisualization**
* CSV data processing
* Queueing and discrete-event simulation concepts

## Project Structure

```text
Single-Server-Simulation-Software/
│
├── Customer.cs
├── DataGenerator.cs
├── HistoricalDataAnalyzer.cs
├── MainForm.cs
├── Member4_UI.csproj
├── Performance.cs
├── Program.cs
├── Randomgenerator.cs
├── Simulation.cs
│
├── CSVDataToUpload.csv
├── SapphireData.csv
│
└── README.md
```

### Main Files

**Customer.cs**
Contains the customer data structure and customer-related information used by the simulation.

**DataGenerator.cs**
Generates customer input data for the simulation.

**Randomgenerator.cs**
Provides random values required for generating simulation data.

**Simulation.cs**
Contains the main single-server queue simulation logic.

**Performance.cs**
Calculates the performance measures obtained from the simulation.

**HistoricalDataAnalyzer.cs**
Handles analysis of historical customer data.

**MainForm.cs**
Contains the Windows Forms user interface and connects the different components of the project.

**Program.cs**
Contains the application entry point.

**Member4_UI.csproj**
The .NET project configuration file containing project settings and dependencies.

## Requirements

Before running the project, make sure you have:

* Windows
* .NET SDK
* Visual Studio or another environment capable of building .NET Windows Forms applications

The project also uses the Windows Forms Data Visualization package.

## Installation

Clone the repository:

```powershell
git clone https://github.com/neharehan2005/Single-Server-Simulation-Software.git
```

Move into the project directory:

```powershell
cd Single-Server-Simulation-Software
```

## Install Required Package

Install the Windows Forms Data Visualization package:

```powershell
dotnet add package System.Windows.Forms.DataVisualization
```

If the normal package installation does not provide the required version, the prerelease version can be installed:

```powershell
dotnet add package System.Windows.Forms.DataVisualization --prerelease
```

If your system has multiple .NET installations and `dotnet` is not available directly, you can use the full path:

```powershell
& "C:\Program Files\dotnet\dotnet.exe" add package System.Windows.Forms.DataVisualization
```

Or:

```powershell
& "C:\Program Files\dotnet\dotnet.exe" add package System.Windows.Forms.DataVisualization --prerelease
```

## Build the Project

To build the project, run:

```powershell
dotnet build
```

For a complete rebuild without using previous build results:

```powershell
dotnet build --no-incremental
```

If `dotnet` is not recognized, use:

```powershell
& "C:\Program Files\dotnet\dotnet.exe" build --no-incremental
```

## Run the Application

After building the project, run:

```powershell
dotnet run
```

Or, if required on your system:

```powershell
& "C:\Program Files\dotnet\dotnet.exe" run
```

## Complete Command Sequence

If you are setting up the project from scratch, the commands can be run in this order:

```powershell
git clone https://github.com/neharehan2005/Single-Server-Simulation-Software.git

cd Single-Server-Simulation-Software

dotnet add package System.Windows.Forms.DataVisualization

dotnet build --no-incremental

dotnet run
```

If you need the prerelease package:

```powershell
git clone https://github.com/neharehan2005/Single-Server-Simulation-Software.git

cd Single-Server-Simulation-Software

dotnet add package System.Windows.Forms.DataVisualization --prerelease

dotnet build --no-incremental

dotnet run
```

### Using the Full .NET Path

If Windows does not recognize `dotnet` as a command:

```powershell
& "C:\Program Files\dotnet\dotnet.exe" add package System.Windows.Forms.DataVisualization

& "C:\Program Files\dotnet\dotnet.exe" build --no-incremental

& "C:\Program Files\dotnet\dotnet.exe" run
```

## How the Simulation Works

The system follows the basic single-server queueing process:

```text
Customer Arrives
       ↓
Check Server
       ↓
Server Available?
    ↙       ↘
  Yes        No
   ↓          ↓
Start       Join Queue
Service        ↓
   ↓       Wait for Server
Service        ↓
Complete ←─────┘
   ↓
Next Customer
```

For each customer, the simulation determines when the customer arrives, when service can begin, how long the customer waits, and when service is completed.

## Input Data

The application can use generated data or CSV-based data.

A typical customer dataset contains information related to:

* Customer ID
* Arrival Time
* Service Time

The CSV files included in the repository can be used to test the application.

## Performance Measures

The simulation produces several measures that help evaluate the queueing system.

### Average Waiting Time

The average amount of time customers spend waiting before receiving service.

### Maximum Waiting Time

The longest waiting time experienced by any customer.

### Average Time in System

The average amount of time a customer spends in the system, including both waiting and service.

### Server Utilization

The percentage of simulation time during which the server is busy serving customers.

### Total Customers

The total number of customers processed during the simulation.

### Total Simulation Time

The amount of simulated time required to process the customers.

## Purpose of the Project

The main purpose of this project is to apply **Modeling & Simulation** concepts to a practical queueing problem.

The project demonstrates how a real-world service system can be represented as a mathematical and computational model and then analyzed using simulation.

It provides practical experience with:

* Queueing systems
* Single-server models
* Customer arrival and service times
* Discrete-event simulation
* Random data generation
* Historical data analysis
* Performance measurement
* C# Windows Forms development
* Data visualization

## Future Improvements

Possible future improvements include:

* Multiple-server queue simulation
* Additional queueing models
* More visualization options
* Configurable arrival and service distributions
* Exporting simulation results
* More detailed statistical analysis
* Comparison between different queue configurations

## Author

**This project was developed by a group of 7 students as part of the Simulation and Modeling course.

Course: Simulation and Modeling
University: University of Karachi**

BS Computer Science
University of Karachi

GitHub: https://github.com/neharehan2005

## Repository

https://github.com/neharehan2005/Single-Server-Simulation-Software

## License

This project was developed for educational and academic purposes.
