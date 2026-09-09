# Aura Ecosystem: F# Core Foundation (`Aura.FSharp`)

[![CI Pipeline](https://github.com/auraecosystem/fsharp/actions/workflows/ci.yml/badge.svg)](https://github.com/auraecosystem/fsharp/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET SDK](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)

`Aura.FSharp` is the functional core library and domain engine powering services within the **Aura Ecosystem**. Built on F# and .NET 8, it provides compile-time enforced domain primitives, railway-oriented workflow combinators (ROP), stateful concurrency actors, and local management tooling.

---

## Repository Structure

The project is structured to enforce a strict separation of pure domain types, workflow pipelines, stateful concurrency engines, and administrative tooling.

```text
auraecosystem/fsharp
├── .github/
│   └── workflows/
│       └── ci.yml                 # GitHub Actions build, lint, and test pipeline
├── src/
│   ├── Aura.Core/                 # Pure functional domain library
│   │   ├── Domain/                # Value objects, smart constructors, and entities
│   │   ├── Workflows/             # Railway-Oriented processing pipelines (ROP)
│   │   ├── Concurrency/           # MailboxProcessor state actors
│   │   └── Aura.Core.fsproj
│   └── Aura.Cli/                  # Administrative runner & execution entry point
│       ├── Program.fs
│       └── Aura.Cli.fsproj
├── tests/
│   ├── Aura.Core.Tests/           # Expecto functional test suite
│   │   ├── Tests.fs
│   │   ├── Program.fs             # Expecto CLI entry point
│   │   └── Aura.Core.Tests.fsproj
├── Aura.sln                       # Visual Studio / .NET Solution
├── .gitignore
└── README.md

```

---

## Key Architectural Principles & Features

### 1. Enforced Domain Boundaries

Guarantees domain invariants at compile time using single-case discriminated unions and private constructors (`Smart Constructors`).

```fsharp
namespace Aura.Core.Domain

type UserId = UserId of System.Guid

type Email = private Email of string with
    static member Create (input: string) : Result<Email, string> =
        if System.String.IsNullOrWhiteSpace(input) || not (input.Contains("@")) then
            Error "Invalid email address format."
        else
            Ok (Email input)
    member this.Value = match this with Email e -> e

type User = {
    Id: UserId
    Email: Email
    CreatedAt: System.DateTimeOffset
}

```

### 2. Railway-Oriented Programming (ROP)

Chains computations that can fail into seamless pipelines using F#'s standard `Result<'T, 'Error>` type without throwing exceptions or writing deeply nested pattern matches.

```fsharp
namespace Aura.Core.Workflows

module UserRegistration =
    open Aura.Core.Domain

    type RegistrationRequest = { EmailInput: string }

    let validateRequest (req: RegistrationRequest) : Result<Email, string> =
        Email.Create req.EmailInput

    let createUser (email: Email) : Result<User, string> =
        Ok {
            Id = UserId (System.Guid.NewGuid())
            Email = email
            CreatedAt = System.DateTimeOffset.UtcNow
        }

    /// ROP Composition Pipeline
    let registerUser (req: RegistrationRequest) : Result<User, string> =
        req
        |> validateRequest
        |> Result.bind createUser

```

### 3. Stateful Actor Concurrency

Provides in-memory, lock-free, thread-safe state management utilizing `MailboxProcessor` state machines.

```fsharp
namespace Aura.Core.Concurrency

type CacheMessage<'K, 'V when 'K: comparison> =
    | Put of key: 'K * value: 'V
    | Get of key: 'K * replyChannel: AsyncReplyChannel<'V option>

type InMemoryCache<'K, 'V when 'K: comparison>() =
    let agent = MailboxProcessor.Start(fun inbox ->
        let rec loop (store: Map<'K, 'V>) = async {
            let! msg = inbox.Receive()
            match msg with
            | Put (k, v) -> return! loop (Map.add k v store)
            | Get (k, replyChannel) ->
                replyChannel.Reply(Map.tryFind k store)
                return! loop store
        }
        loop Map.empty)

    member _.Put(key, value) = agent.Post(Put(key, value))
    member _.GetAsync(key) = agent.PostAndAsyncReply(fun ch -> Get(key, ch))

```

---

## Local Development & Setup

### Prerequisites

* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or higher
* [F# 8.0+](https://fsharp.org/) (bundled with the .NET SDK)

### Quick Start

1. **Clone the Repository:**
```bash
git clone [https://github.com/auraecosystem/fsharp.git](https://github.com/auraecosystem/fsharp.git)
cd fsharp

```


2. **Build the Solution:**
```bash
dotnet restore
dotnet build --configuration Release

```


3. **Run the Expecto Test Suite:**
```bash
dotnet test

```


*Alternatively, run the Expecto binary directly for detailed CLI options:*
```bash
dotnet run --project tests/Aura.Core.Tests -- --summary

```


4. **Execute the CLI Application:**
```bash
dotnet run --project src/Aura.Cli

```



---

## Testing Framework (Expecto)

Unit and integration tests are written using **Expecto**. Test cases are declared as pure F# expressions inside modules:

```fsharp
module Aura.Core.Tests

open Expecto
open Aura.Core.Domain
open Aura.Core.Workflows.UserRegistration

[<Tests>]
let registrationTests =
    testList "User Registration Pipeline" [
        testCase "Valid email address registers user" <| fun () ->
            let request = { EmailInput = "dev@auraecosystem.org" }
            let result = registerUser request
            Expect.isOk result "User registration should succeed with valid email"

        testCase "Invalid email address returns error" <| fun () ->
            let request = { EmailInput = "invalid-email" }
            let result = registerUser request
            Expect.isError result "User registration should fail with invalid email"
    ]

```

---

## Code Style & Formatting

Code formatting is enforced using **[Fantomas](https://github.com/fsprojects/fantomas)**.

To check code formatting before committing:

```bash
dotnet tool restore
dotnet fantomas . --check

```

To automatically format the repository:

```bash
dotnet fantomas .

```

---

## Contributing

1. Fork the repository.
2. Create a feature branch (`git checkout -b feature/amazing-feature`).
3. Commit your changes (`git commit -m 'feat: Add amazing feature'`).
4. Ensure code formatting passes Fantomas checks.
5. Push to your feature branch (`git push origin feature/amazing-feature`).
6. Open a Pull Request.

---

## License

Distributed under the [MIT License](https://www.google.com/search?q=LICENSE).

```

```
