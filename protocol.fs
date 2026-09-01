module ProtocolDecoder

open System
open System.IO

// --- ALGEBRAIC DATA TYPES (ADTs) ---

type Header = {
    Magic: uint16
    SequenceId: uint32
}

// Discriminated Union representing distinct frame variants
type ProtocolFrame =
    | PingFrame of timestamp: int64
    | DataPayload of topic: string * payload: byte[]
    | ErrorFrame of code: int32 * message: string
    | UnknownFrame of opcode: byte

// --- DECODER LOGIC WITH PATTERN MATCHING ---

let decodeFrame (stream: Stream) : Result<Header * ProtocolFrame, string> =
    use reader = new BinaryReader(stream)
    
    if stream.Length < 6L then
        Error "Insufficient bytes for protocol header"
    else
        let magic = reader.ReadUInt16()
        let seqId = reader.ReadUInt32()
        let header = { Magic = magic; SequenceId = seqId }

        // Validate protocol magic bytes
        if magic <> 0x4155us then // 'AU' magic bytes
            Error (sprintf "Invalid magic header: 0x%X" magic)
        else
            let opcode = reader.ReadByte()
            
            // Pattern matching on the frame Opcode byte
            let frame = 
                match opcode with
                | 0x01b -> 
                    let ts = reader.ReadInt64()
                    PingFrame ts

                | 0x02b -> 
                    let topicLength = int (reader.ReadByte())
                    let topicBytes = reader.ReadBytes(topicLength)
                    let topic = Text.Encoding.UTF8.GetString(topicBytes)
                    let payloadLength = reader.ReadInt32()
                    let payload = reader.ReadBytes(payloadLength)
                    DataPayload (topic, payload)

                | 0xFFb -> 
                    let code = reader.ReadInt32()
                    let msg = reader.ReadString()
                    ErrorFrame (code, msg)

                | unknownOpcode -> 
                    UnknownFrame unknownOpcode

            Ok (header, frame)

// --- PATTERN MATCHING HANDLER ---

let processFrame (rawBytes: byte[]) =
    use ms = new MemoryStream(rawBytes)
    match decodeFrame ms with
    | Error err -> 
        printfn "[Protocol Error] Decoding failed: %s" err

    | Ok (header, frame) ->
        printfn "[Header] Seq: %d | Magic: 0x%X" header.SequenceId header.Magic
        
        // Match directly on the ADT variants
        match frame with
        | PingFrame ts -> 
            printfn "  -> Action: Responded to PING sent at timestamp %d" ts

        | DataPayload (topic, payload) -> 
            printfn "  -> Action: Dispatched %d bytes to topic '%s'" payload.Length topic

        | ErrorFrame (code, msg) -> 
            printfn "  -> Action: Handled system error code %d: %s" code msg

        | UnknownFrame op -> 
            printfn "  -> Action: Ignored unsupported opcode 0x%X" op
