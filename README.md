# TradingApp

A full-stack trading application built with ASP.NET Core, React and PostgreSQL.

The project started as a small application for a real-world use case and evolved into a personal software engineering project focused on clean separation of responsibilities, external market data integrations, testable trading logic and paper trading.

The application is intended for simulation and educational purposes only. It does not execute trades with real money.

## Features

* User authentication with JWT and refresh tokens
* Paper trading accounts
* Buy and sell orders
* Position and portfolio management
* Profit and loss calculation
* Market and limit order handling
* Stop Loss and Take Profit logic
* Trading strategy evaluation
* Signal aggregation
* Risk evaluation
* External market data providers
* Background market data processing
* Real-time updates using SignalR
* REST API
* React-based web frontend
* Automated unit and integration tests

## Tech Stack

### Backend

* C#
* ASP.NET Core
* Entity Framework Core
* PostgreSQL
* SignalR
* JWT Authentication
* Background Services

### Frontend

* React
* TypeScript

### Integrations

The application supports multiple external market data sources, including:

* OANDA
* Finnhub
* Yahoo Finance

External providers are accessed through abstractions so that market data sources can be exchanged or extended without coupling the trading logic directly to a specific provider.

### Testing

The solution contains separate test projects for both the application and the trading engine.

Tests cover areas such as:

* Trading strategies
* Signal evaluation
* Risk evaluation
* Order processing
* Position handling
* Portfolio calculations
* Business rules

## Architecture

The solution is currently divided into the following main components:

```text
TradingApp/
├── TradingApp
│   ├── ASP.NET Core API
│   ├── Persistence
│   ├── Authentication
│   ├── Market Data Integrations
│   ├── Paper Trading
│   └── Background Services
│
├── TradingApp.TradingEngine
│   ├── Trading Strategies
│   ├── Signal Aggregation
│   ├── Risk Evaluation
│   └── Trading Models
│
├── TradingApp.Tests
│
├── TradingApp.TradingEngine.Tests
│
└── frontend
    └── React / TypeScript
```

One of the main architectural goals is to keep the trading logic independent from infrastructure concerns such as HTTP APIs, databases and external market data providers.

The `TradingApp.TradingEngine` project therefore contains the core trading and strategy logic without depending directly on the ASP.NET Core application.

## Example Flow

A simplified paper trading flow looks like this:

```text
Market Data Provider
        │
        ▼
Market Data Service
        │
        ▼
Trading Engine
        │
        ├── Strategies
        ├── Signal Aggregation
        └── Risk Evaluation
        │
        ▼
Paper Trading Service
        │
        ├── Orders
        ├── Positions
        └── Portfolio
        │
        ▼
PostgreSQL
```

The frontend communicates with the backend through the REST API and receives selected real-time updates through SignalR.

## Design Decisions

### Separate Trading Engine

Trading strategies and risk evaluation are separated from the web application.

This makes the trading logic easier to test and prevents infrastructure concerns such as controllers, Entity Framework or external APIs from becoming part of the core trading logic.

### Market Data Abstraction

External market data providers are accessed through interfaces rather than directly from the trading logic.

This allows different providers to be used for different scenarios and makes it possible to test the application without depending on live external services.

### Paper Trading

The application currently focuses on paper trading instead of real brokerage integration.

This keeps the system safe to develop and test while still allowing realistic workflows such as order execution, position management and portfolio calculations.

### Testing Business Logic

Trading rules contain a significant amount of business logic. For that reason, strategies, order processing and risk evaluation are designed to be testable independently.

## Running the Project

### Requirements

* .NET SDK
* PostgreSQL
* Node.js
* npm

### Backend

Clone the repository:

```bash
git clone https://github.com/theSuitedDott/TradingApp.git
cd TradingApp
```

Restore dependencies:

```bash
dotnet restore
```

Configure the PostgreSQL connection string and required API credentials using local configuration or environment variables.

Apply the database migrations:

```bash
dotnet ef database update
```

Start the backend:

```bash
dotnet run --project TradingApp
```

### Frontend

Navigate to the frontend directory:

```bash
cd frontend
npm install
npm run dev
```

## Configuration

External API credentials should not be committed to the repository.

The application expects configuration for services such as market data providers and authentication to be supplied through environment variables, user secrets or local development configuration.

Example:

```text
ConnectionStrings__DefaultConnection=...
Jwt__Key=...
Finnhub__ApiKey=...
Oanda__ApiKey=...
```

## Tests

Run all backend tests with:

```bash
dotnet test
```

The solution contains tests for both application-level behavior and the independent trading engine.

## Current Status

This project is actively being developed and used as a software engineering portfolio project.

Current areas of improvement include:

* improving service boundaries
* extending integration tests
* improving error handling and observability
* adding CI/CD with GitHub Actions
* improving Docker support
* expanding architecture documentation

## Disclaimer

This application is a software development project for simulation and educational purposes.

It is not financial advice and it is not intended for executing real-money trades.
