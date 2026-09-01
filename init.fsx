module ActorPipeline

open System

// Define the domain message and data models
type StreamEvent = {
    Id: string
    Timestamp: DateTime
    Payload: float
}

type BatchMessage =
    | ProcessEvent of StreamEvent
    | Flush

type IngestMessage =
    | Ingest of StreamEvent

// Stage 2: Batch Processing Actor
let createBatchProcessor (batchSize: int) =
    MailboxProcessor<BatchMessage>.Start(fun inbox ->
        let rec loop (buffer: StreamEvent list) = async {
            let! msg = inbox.Receive()
            match msg with
            | ProcessEvent ev ->
                let updatedBuffer = ev :: buffer
                if updatedBuffer.Length >= batchSize then
                    // Execute bulk operation (e.g., persistence or aggregate calculation)
                    printfn "[BatchProcessor] Flushed batch of %d items (Latest: %s)" 
                            updatedBuffer.Length ev.Id
                    return! loop []
                else
                    return! loop updatedBuffer

            | Flush ->
                if not (List.isEmpty buffer) then
                    printfn "[BatchProcessor] Force flushed %d items" buffer.Length
                return! loop []
        }
        loop []
    )

// Stage 1: Filtering & Ingestion Actor
let createIngestActor (nextStage: MailboxProcessor<BatchMessage>) =
    MailboxProcessor<IngestMessage>.Start(fun inbox ->
        let rec loop () = async {
            let! msg = inbox.Receive()
            match msg with
            | Ingest ev ->
                // Filter out non-positive payload values
                if ev.Payload > 0.0 then
                    nextStage.Post(ProcessEvent ev)
                else
                    printfn "[Ingest] Dropped invalid event %s" ev.Id
                return! loop ()
        }
        loop ()
    )

// Driver / Execution setup
[<EntryPoint>]
let main argv =
    // Initialize pipeline stages
    let batchActor = createBatchProcessor batchSize=3
    let ingestActor = createIngestActor batchActor

    // Simulate concurrent producer tasks
    let produceEvents producerId count = async {
        let rng = Random()
        for i in 1 .. count do
            let ev = {
                Id = sprintf "P%d-E%d" producerId i
                Timestamp = DateTime.UtcNow
                Payload = if i % 4 = 0 then -1.0 else rng.NextDouble() * 100.0
            }
            ingestActor.Post(Ingest ev)
            do! Async.Sleep(50)
    }

    // Run 3 concurrent stream producers
    [1 .. 3]
    |> List.map (fun id -> produceEvents id 5)
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore

    // Force flush remaining buffered items
    batchActor.Post(Flush)
    0
