module Domain =
    type AccountId = AccountId of System.Guid
    type Money = private Money of decimal with
        static member Create amt = 
            if amt >= 0m then Ok (Money amt) 
            else Error "Amount cannot be negative"
        member this.Value = match this with Money m -> m
