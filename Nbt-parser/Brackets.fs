open FParsec
open NbtParser

let pItemBracketWithTag : Parser<BracketHandler> =
    parse {
        do! skipChar '<' >>. pstring "item:" >>. pzero
        let! modId = many1Chars (satisfy System.Char.IsLetterOrDigit)
        do! skipChar ':' >>. pzero
        let! itemId = many1Chars (satisfy (fun c -> System.Char.IsLetterOrDigit c || c = '_'))
        do! skipChar '>' >>. pzero
        
        // Optional .withTag({ ... }) method call
        let! tagOpt = opt (ws >>. pWithTagSuffix)
        
        // Optional * count multiplier
        let! countOpt = opt (ws >>. skipChar '*' >>. ws >>. pint32)
        
        return ItemBracket (modId, itemId, defaultArg countOpt 1, tagOpt)
    }
