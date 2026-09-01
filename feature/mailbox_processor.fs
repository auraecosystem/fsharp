open Aura.Core.Concurrency

type CacheMessage =
    | Put of key: string * value: string
    | Get of key: string * replyChannel: AsyncReplyChannel<string option>

let cacheActor = MailboxProcessor.Start(fun inbox ->
    let rec loop map = async {
        let! msg = inbox.Receive()
        match msg with
        | Put (k, v) -> return! loop (Map.add k v map)
        | Get (k, ch) -> 
            ch.Reply(Map.tryFind k map)
            return! loop map
    }
    loop Map.empty)
