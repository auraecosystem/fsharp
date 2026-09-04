#!/usr/bin/env bash
set -e

echo "Creating solution and directory structure..."

# 1. Create root directory & initialize solution
mkdir -p fsharp && cd fsharp
dotnet new sln -n Aura

# 2. Create directory hierarchy
mkdir -p src/Aura.Core/Domain
mkdir -p src/Aura.Core/Workflows
mkdir -p src/Aura.Core/Concurrency
mkdir -p src/Aura.Cli
mkdir -p tests/Aura.Core.Tests

# 3. Create F# Projects
dotnet new classlib -lang "F#" -o src/Aura.Core -n Aura.Core
dotnet new console -lang "F#" -o src/Aura.Cli -n Aura.Cli
dotnet new xunit -lang "F#" -o tests/Aura.Core.Tests -n Aura.Core.Tests

# 4. Link project references
dotnet add src/Aura.Cli/Aura.Cli.fsproj reference src/Aura.Core/Aura.Core.fsproj
dotnet add tests/Aura.Core.Tests/Aura.Core.Tests.fsproj reference src/Aura.Core/Aura.Core.fsproj

# 5. Add projects to solution
dotnet sln add src/Aura.Core/Aura.Core.fsproj
dotnet sln add src/Aura.Cli/Aura.Cli.fsproj
dotnet sln add tests/Aura.Core.Tests/Aura.Core.Tests.fsproj

# 6. Scaffold boilerplate F# files
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

# 7. Update Aura.Core.fsproj file order (F# requires explicit compile order)
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

# 8. Create standard .gitignore
dotnet new gitignore

echo "Setup complete! Verifying build..."
dotnet build
