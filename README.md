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
