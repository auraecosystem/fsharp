module ZenScriptHotReloader

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open FParsec

// --- AST & ENGINE STATE ---

type ZenStatement =
    | RemoveRecipe of string
    | LogMessage of string

type CompiledEngineState = {
    LoadedAt: DateTime
    Statements: ZenStatement list
}

// Global thread-safe reference to current active engine state
type ExecutionEngine() =
    let mutable currentState = { LoadedAt = DateTime.MinValue; Statements = [] }

    member _.SwapState(newState: CompiledEngineState) =
        Volatile.Write(&currentState, newState)
        printfn "\n[Engine] State atomically swapped! Loaded %d statements at %s" 
                newState.Statements.Length (newState.LoadedAt.ToString("HH:mm:ss.fff"))

    member _.ExecuteCurrent() =
        let active = Volatile.Read(&currentState)
        printfn "[Execution Run] Current active statements (Compiled: %s):" (active.LoadedAt.ToString("HH:mm:ss"))
        for stmt in active.Statements do
            match stmt with
            | RemoveRecipe name -> printfn "  -> Action: Remove Recipe '%s'" name
            | LogMessage msg -> printfn "  -> Action: Log '%s'" msg

// --- MOCK PARSER ---

module ZenParser =
    let parseScriptContent (content: string) : ZenStatement list =
        content.Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.choose (fun line ->
            let trimmed = line.Trim()
            if trimmed.StartsWith("recipes.remove") then
                let name = trimmed.Split('"').[1]
                Some (RemoveRecipe name)
            elif trimmed.StartsWith("print") then
                let msg = trimmed.Split('"').[1]
                Some (LogMessage msg)
            else
                None
        )
        |> Array.toList

// --- DEBOUNCE ACTOR (MAILBOXPROCESSOR) ---

type ReloadMessage =
    | FileChanged of filePath: string
    | TriggerCompilation of filePath: string

let createHotReloadActor (engine: ExecutionEngine) (debounceMs: int) =
    MailboxProcessor<ReloadMessage>.Start(fun inbox ->
        let rec loop (pendingTimer: CancellationTokenSource option) = async {
            let! msg = inbox.Receive()
            match msg with
            | FileChanged filePath ->
                // Cancel previous pending compilation timer if file is still being written to
                pendingTimer |> Option.iter (fun cts -> cts.Cancel(); cts.Dispose())
                
                let newCts = new CancellationTokenSource()
                
                // Fire debounce timer
                async {
                    do! Async.Sleep debounceMs
                    if not newCts.Token.IsCancellationRequested then
                        inbox.Post (TriggerCompilation filePath)
                } |> Async.Start

                return! loop (Some newCts)

            | TriggerCompilation filePath ->
                try
                    // Give OS file handle time to release locks
                    do! Async.Sleep 50 
                    let content = File.ReadAllText(filePath)
                    let statements = ZenParser.parseScriptContent content
                    
                    let newState = {
                        LoadedAt = DateTime.Now
                        Statements = statements
                    }
                    
                    engine.SwapState(newState)
                with ex ->
                    printfn "[Hot-Reload Error] Failed to read or parse script: %s" ex.Message

                return! loop None
        }
        loop None
    )

// --- FILE SYSTEM WATCHER SETUP ---

type ScriptWatcher(watchPath: string, fileFilter: string, engine: ExecutionEngine) =
    let actor = createHotReloadActor engine 300 // 300ms debounce window
    let watcher = new FileSystemWatcher(watchPath, fileFilter)

    let onChange (e: FileSystemEventArgs) =
        if e.ChangeType = WatcherChangeTypes.Changed || e.ChangeType = WatcherChangeTypes.Created then
            actor.Post (FileChanged e.FullPath)

    member this.Start() =
        watcher.NotifyFilter <- NotifyFilters.LastWrite ||| NotifyFilters.FileName ||| NotifyFilters.Size
        watcher.Changed.Add onChange
        watcher.Created.Add onChange
        watcher.EnableRaisingEvents <- true
        printfn "[Watcher] Monitoring directory: %s (%s)" watchPath fileFilter

    interface IDisposable with
        member _.Dispose() =
            watcher.EnableRaisingEvents <- false
            watcher.Dispose()

// --- DRIVER DEMO ---

[<EntryPoint>]
let main _ =
    let tempDir = Path.Combine(Path.GetTempPath(), "zenscript_watch_demo")
    Directory.CreateDirectory(tempDir) |> ignore
    let scriptFile = Path.Combine(tempDir, "script.zs")

    // Write initial script file
    File.WriteAllText(scriptFile, "print \"v1: initial load\";\nrecipes.remove(\"minecraft:stick\");")

    let engine = ExecutionEngine()

    // Setup and start watcher
    use watcher = new ScriptWatcher(tempDir, "*.zs", engine)
    watcher.Start()

    // Trigger initial compilation manually
    let initialContent = File.ReadAllText(scriptFile)
    engine.SwapState({ LoadedAt = DateTime.Now; Statements = ZenParser.parseScriptContent initialContent })
    engine.ExecuteCurrent()

    // Simulate real-time file modification
    Thread.Sleep(1000)
    printfn "\n[System] Modifying script file on disk..."
    File.WriteAllText(scriptFile, "print \"v2: hot reload active!\";\nrecipes.remove(\"minecraft:iron_sword\");")

    // Wait to allow debounce actor to process changes
    Thread.Sleep(1000)
    engine.ExecuteCurrent()

    0
