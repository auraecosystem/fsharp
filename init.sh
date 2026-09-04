#!/usr/bin/env bash
set -e

PROJECT_NAME="Aura"
ROOT_DIR="fsharp"

echo "Initializing F# workspace in ./${ROOT_DIR}..."

# 1. Create root directory and initialize .NET solution
mkdir -p "$ROOT_DIR"
cd "$ROOT_DIR"
dotnet new sln -n "$PROJECT_NAME"

# 2. Create directory hierarchy
mkdir -p src/Aura.Core/Domain
mkdir -p src/Aura.Core/Workflows
mkdir -p src/Aura.Core/Concurrency
mkdir -p src/Aura.Cli
mkdir -p tests/Aura.Core.Tests

# 3. Create F# projects
dotnet new classlib -lang "F#" -o src/Aura.Core -n Aura.Core
dotnet new console -lang "F#" -o src/Aura.Cli -n Aura.Cli
dotnet new xunit -lang "F#" -o tests/Aura.Core.Tests -n Aura.Core.Tests

# 4. Link project references
dotnet add src/Aura.Cli/Aura.Cli.fsproj reference src/Aura.Core/Aura.Core.fsproj
dotnet add tests/Aura.Core.Tests/Aura.Core.Tests.fsproj reference src/Aura.Core/Aura.Core.fsproj

# 5. Attach projects to solution
dotnet sln add src/Aura.Core/Aura.Core.fsproj
dotnet sln add src/Aura.Cli/Aura.Cli.fsproj
dotnet sln add tests/Aura.Core.Tests/Aura.Core.Tests.fsproj

# 6. Scaffold boilerplate code

# --- Domain Types ---
cat << 'EOF' > src/Aura.Core/Domain/Types.fs
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
EOF

# --- ROP Workflows ---
cat << 'EOF' > src/Aura.Core/Workflows/Pipelines.fs
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

    let registerUser (req: RegistrationRequest) : Result<User, string> =
        req
        |> validateRequest
        |> Result.bind createUser
EOF

# --- Actor Concurrency ---
cat << 'EOF' > src/Aura.Core/Concurrency/Actors.fs
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
EOF

# --- CLI Entry Point ---
cat << 'EOF' > src/Aura.Cli/Program.fs
open System
open Aura.Core.Domain
open Aura.Core.Workflows.UserRegistration

[<EntryPoint>]
let main argv =
    printfn "=== Aura Ecosystem CLI ==="
    let request = { EmailInput = "dev@auraecosystem.org" }
    
    match registerUser request with
    | Ok user -> 
        printfn "Successfully registered user with ID: %A" user.Id
    | Error err -> 
        printfn "Registration failed: %s" err
        
    0
EOF

# --- Unit Test ---
cat << 'EOF' > tests/Aura.Core.Tests/Tests.fs
module Aura.Core.Tests

open Xunit
open Aura.Core.Domain
open Aura.Core.Workflows.UserRegistration

[<Fact>]
let ``Valid email registers successfully`` () =
    let request = { EmailInput = "test@aura.org" }
    let result = registerUser request
    Assert.True(Result.isOk result)

[<Fact>]
let ``Invalid email fails registration`` () =
    let request = { EmailInput = "invalid-email" }
    let result = registerUser request
    Assert.True(Result.isError result)
EOF

# 7. Configure F# Compilation Order in Aura.Core.fsproj
# Note: F# requires explicit file order compilation (Domain -> Workflows -> Concurrency)
cat << 'EOF' > src/Aura.Core/Aura.Core.fsproj
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="Domain/Types.fs" />
    <Compile Include="Workflows/Pipelines.fs" />
    <Compile Include="Concurrency/Actors.fs" />
  </ItemGroup>
</Project>
EOF

# Remove default generated template files
rm -f src/Aura.Core/Library.fs

# 8. Create standard .gitignore
dotnet new gitignore

echo "------------------------------------------------"
echo "Initialization finished. Running verification build..."
echo "------------------------------------------------"

dotnet build
dotnet test

echo "Success! Execute 'cd fsharp && dotnet run --project src/Aura.Cli' to test the CLI tool."
