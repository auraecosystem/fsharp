# Aura Ecosystem: F# Core Foundation

`Aura.FSharp` is the functional core library and domain engine powering services within the **Aura Ecosystem**. It provides strongly typed domain primitives, railway-oriented workflow combinators, concurrency models via stateful actors, and interop utilities targeting modern .NET runtimes.

---

## Architecture Overview

The repository follows a clean, modular F# project structure designed to separate pure domain types from workflows and infrastructure:

```text
auraecosystem/fsharp
├── src/
│   ├── Aura.Core/                 # Core F# domain engine & functional utilities
│   │   ├── Domain/                # Pure types, value objects, and domain abstractions
│   │   ├── Workflows/             # Railway-oriented processing pipelines
│   │   ├── Concurrency/           # MailboxProcessor state machine actors
│   │   └── Aura.Core.fsproj
│   └── Aura.Cli/                  # Administrative & local execution CLI tool
│       ├── Program.fs
│       └── Aura.Cli.fsproj
├── tests/
│   ├── Aura.Core.Tests/           # Unit & property-based test suites (Expecto / xUnit)
│   └── Aura.Core.Tests.fsproj
├── Aura.sln                       # .NET Solution file
├── .gitignore
└── README.md

```

---

## Core Features & Usage Examples

### 1. Strongly Typed Value Objects & Domain Models

Guarantees domain invariants at compile time using F# single-case discriminated unions and smart constructors.

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

### 2. Railway-Oriented Workflow Pipelines (ROP)

Chains operations that can fail without relying on exceptions or deeply nested `match` expressions.

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

    // Pipeline composition
    let registerUser (req: RegistrationRequest) : Result<User, string> =
        req
        |> validateRequest
        |> Result.bind createUser

```

### 3. Stateful Actor Concurrency (`MailboxProcessor`)

Provides in-memory thread-safe state management without external locks.

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
            | Put (k, v) -> 
                return! loop (Map.add k v store)
            | Get (k, replyChannel) ->
                replyChannel.Reply(Map.tryFind k store)
                return! loop store
        }
        loop Map.empty)

    member _.Put(key, value) = agent.Post(Put(key, value))
    member _.GetAsync(key) = agent.PostAndAsyncReply(fun ch -> Get(key, ch))

```

---

## Prerequisites

* [.NET 8.0 SDK](https://dotnet.microsoft.com/download) or higher
* [F# 8.0+](https://fsharp.org/) (bundled with .NET SDK)

---

## Setup & Local Development

### 1. Clone the Repository

```bash
git clone [https://github.com/auraecosystem/fsharp.git](https://github.com/auraecosystem/fsharp.git)
cd fsharp

```

### 2. Build the Solution

```bash
dotnet restore
dotnet build --configuration Release

```

### 3. Run the Test Suite

```bash
dotnet test

```

### 4. Execute the CLI Tool

```bash
dotnet run --project src/Aura.Cli

```

---

## Contributing

1. Fork the repository.
2. Create your feature branch (`git checkout -b feature/my-new-feature`).
3. Ensure code formatting complies with Fantomas (`dotnet fantomas . --check`).
4. Commit your changes (`git commit -m 'Add new feature'`).
5. Push to the branch (`git push origin feature/my-new-feature`).
6. Open a Pull Request.

---

## License

Distributed under the [MIT License](https://www.google.com/search?q=LICENSE).

# Aura.Core: F# Domain Engine & Utilities

A high-performance, functional-first foundation for the **Aura Ecosystem**. `Aura.Core` leverages F#'s type system to enforce domain boundaries, handle asynchronous workflows, and provide resilient error handling across all microservices.

---

## Architecture Overview

```text
├── src/
│   ├── Aura.Core/                 # Core domain models, workflows, & ROP utilities
│   │   ├── Domain/                # Pure types (Unions, Records, Value Objects)
│   │   ├── Workflows/             # Railway-oriented pipeline operations
│   │   ├── Concurrency/           # MailboxProcessor actor implementations
│   │   └── Aura.Core.fsproj
│   └── Aura.Cli/                  # CLI runner for local administration
├── tests/
│   ├── Aura.Core.Tests/           # Expecto / xUnit unit & integration tests
│   └── Aura.Core.Tests.fsproj
├── .gitignore                     # Standard .NET / F# gitignore rules
├── Aura.sln                       # .NET Solution file
└── README.md                      # Project documentation
