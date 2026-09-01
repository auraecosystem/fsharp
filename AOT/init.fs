open System
open System.Text.Json.Serialization

// Define explicit types for AOT trimming compatibility
type NodeStatus = {
    [<JsonPropertyName("node_id")>] NodeId: string
    [<JsonPropertyName("is_active")>] IsActive: bool
    [<JsonPropertyName("uptime_sec")>] UptimeSec: int64
}

// 1. Use Source Generators for JSON instead of dynamic reflection
[<JsonSourceGenerationOptions(WriteIndented = false)>]
[<JsonSerializable(typeof<NodeStatus>)>]
type NodeStatusJsonContext = JsonSourceGeneratorState

module Program =

    [<EntryPoint>]
    let main argv =
        let status = {
            NodeId = "aura-node-01"
            IsActive = true
            UptimeSec = 86400L
        }

        // Serialize using the AOT source generator context
        let json = System.Text.Json.JsonSerializer.Serialize(status, NodeStatusJsonContext.Default.NodeStatus)
        printfn "[AOT Binary Initialized] %s" json
        0
