# Daily Reward Claim Backend

## Overview

This project implements a PlayFab-style daily reward claim system using Azure Functions and .NET 8.

The Azure Function `ClaimDailyRewardV1` grants a fixed server-controlled reward of **50 currency.soft** once per player per UTC day.

Duplicate requests are handled through idempotency logic and do not grant rewards multiple times.

---

## Purpose

This solution demonstrates:

- Azure Functions (.NET 8 Isolated Worker)
- Dependency Injection
- Testable business logic
- Idempotency handling
- Infrastructure as Code (Bicep)
- GitHub Actions CI workflow
- Unit testing with xUnit

---

## Architecture

### Request Flow

```text
Unity Client
    ↓
PlayFab ExecuteFunction
    ↓
ClaimDailyRewardV1 Azure Function
    ↓
DailyRewardService
    ↓
IDailyRewardStateStore
    ↓
ICurrencyWallet
    ↓
Response
```

### Components

| Component | Responsibility |
|-----------|---------------|
| ClaimDailyRewardV1 | HTTP endpoint |
| DailyRewardService | Business logic |
| ISystemClock | Provides UTC time |
| IDailyRewardStateStore | Tracks daily claims |
| ICurrencyWallet | Manages player currency |
| InMemoryDailyRewardStateStore | In-memory claim tracking |
| InMemoryCurrencyWallet | In-memory wallet implementation |

---

## Business Rules

- Only `daily_login` is supported.
- Reward currency ID is `currency.soft`.
- Reward amount is `50`.
- Claims are based on UTC date.
- Player identity comes from:
  - `CallerEntityProfile.Entity.Id`
  - `CallerEntityProfile.Entity.Type`
- Client supplied player identity is not trusted.
- Duplicate same-day claims must not grant rewards twice.
- Duplicate same-day claims return a successful response with `alreadyClaimed = true`.

---

## Project Structure

```text
DailyRewardProject
│
├── GameBackend
│   ├── Functions
│   │   └── ClaimDailyRewardV1Function.cs
│   │
│   ├── DailyRewards
│   │   ├── DailyRewardService.cs
│   │   ├── DailyRewardOptions.cs
│   │   ├── ICurrencyWallet.cs
│   │   ├── IDailyRewardStateStore.cs
│   │   ├── ISystemClock.cs
│   │   ├── InMemoryCurrencyWallet.cs
│   │   ├── InMemoryDailyRewardStateStore.cs
│   │   └── SystemClock.cs
│   │
│   ├── Models
│   │   ├── PlayFabExecuteFunctionRequest.cs
│   │   ├── ClaimDailyRewardRequest.cs
│   │   └── ClaimDailyRewardResponse.cs
│   │
│   └── Program.cs
│
├── GameBackend.Tests
│   └── DailyRewardServiceTests.cs
│
├── Infra
│   └── main.bicep
│
├── .github
│   └── workflows
│       └── backend-ci.yml
│
└── README.md
```

---

## Configuration

The application uses the following settings:

```text
PLAYFAB_TITLE_ID
DAILY_REWARD_AMOUNT
DAILY_REWARD_CURRENCY_ID
```

Example values:

```text
PLAYFAB_TITLE_ID=TEST_TITLE
DAILY_REWARD_AMOUNT=50
DAILY_REWARD_CURRENCY_ID=currency.soft
```

---

## Running Locally

### Restore Packages

```bash
dotnet restore
```

### Build

```bash
dotnet build
```

### Run Unit Tests

```bash
dotnet test
```

### Run Azure Function

```bash
func start
```

Function URL:

```text
http://localhost:7071/api/ClaimDailyRewardV1
```

---

## Example Request

```json
{
  "FunctionArgument": {
    "questId": "daily_login",
    "clientRequestId": "request-001",
    "clientPlayerId": "do-not-trust-this-field"
  },
  "CallerEntityProfile": {
    "Entity": {
      "Id": "title_player_123",
      "Type": "title_player_account"
    }
  }
}
```

---

## Example Response - First Claim

```json
{
  "success": true,
  "questId": "daily_login",
  "playerEntityId": "title_player_123",
  "claimDateUtc": "2026-07-09",
  "rewardCurrencyId": "currency.soft",
  "rewardAmount": 50,
  "newlyClaimed": true,
  "alreadyClaimed": false,
  "balance": 50,
  "errorCode": null,
  "errorMessage": null
}
```

---

## Example Response - Duplicate Claim

```json
{
  "success": true,
  "questId": "daily_login",
  "playerEntityId": "title_player_123",
  "claimDateUtc": "2026-07-09",
  "rewardCurrencyId": "currency.soft",
  "rewardAmount": 0,
  "newlyClaimed": false,
  "alreadyClaimed": true,
  "balance": 50,
  "errorCode": null,
  "errorMessage": null
}
```

---

## Unit Tests

The following scenarios are covered:

- First valid claim grants reward.
- Missing caller entity is rejected.
- Duplicate same-day claim does not grant twice.
- Retry/idempotency behavior does not double grant.
- Unknown quest ID is rejected.
- Simulated upstream dependency failure returns a 502 response.

Run tests using:

```bash
dotnet test
```

---

## Infrastructure as Code

Infrastructure is defined using Bicep.

File:

```text
Infra/main.bicep
```

Resources declared:

- Azure Function App
- Azure Storage Account
- Environment configuration
- Application settings
- Key Vault secret placeholder

No real secrets are included.

---

## CI/CD

GitHub Actions workflow:

```text
.github/workflows/backend-ci.yml
```

Pipeline steps:

1. Restore dependencies
2. Build solution
3. Execute unit tests
4. Validate Bicep template
5. Publish build artifacts

Deployment is intentionally left manual for this exercise.

---

## Assumptions

- PlayFab services are mocked.
- Inventory operations are mocked.
- State storage is implemented in-memory.
- Wallet functionality is implemented in-memory.
- Authentication validation is outside the scope of this exercise.
- External infrastructure is not required to run locally.

---

## Security Considerations

- Client identity fields are not trusted.
- Rewards are server-controlled.
- UTC server time is used.
- Duplicate claim prevention uses an idempotency key.
- No secrets are stored in source control.
- Secret placeholders use Key Vault references.

---

## Monitoring and Logging

For a production implementation:

- Application Insights
- Structured logging
- Request tracing
- Error monitoring
- Reward claim metrics
- Alerting

---

## Rollback Strategy

Production deployments should use:

- Deployment slots
- Environment approvals
- Versioned build artifacts
- Controlled release promotion
- Rollback to previous build

---

## Cleanup

Non-production resources can be removed by deleting the associated Azure Resource Group.

---

## Future Improvements

- Cosmos DB persistence
- PlayFab Economy API integration
- Distributed locking
- Rate limiting
- Authentication validation
- Integration tests
- Load testing
- Telemetry dashboards

---

## Tradeoffs

To keep the exercise focused and time-boxed:

- In-memory implementations were used instead of external services.
- Persistence was intentionally simplified.
- PlayFab APIs were abstracted behind interfaces.
- Unit tests focus on business logic rather than end-to-end infrastructure.

These design choices prioritize clarity, testability, and maintainability while demonstrating how the solution would evolve into a production-ready system.