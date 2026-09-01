module NbtParser =
    open FParsec

    type UserState = unit
    type Parser<'T> = Parser<'T, UserState>

    let ws = skipWhitespace

    // Forward reference for recursive parsing (Compounds inside Compounds)
    let pNbtValue, pNbtValueImpl = createParserForwardedToRef<NbtValue, UserState>()

    // Strings (handles single quotes 'item' or double quotes "item")
    let pQuotedString : Parser<string> =
        let pSingle = between (skipChar '\'') (skipChar '\'') (manySatisfy (fun c -> c <> '\''))
        let pDouble = between (skipChar '"') (skipChar '"') (manySatisfy (fun c -> c <> '"'))
        pSingle <|> pDouble

    // Keys can be quoted strings or raw identifiers (e.g. display or 'display')
    let pNbtKey : Parser<string> =
        pQuotedString <|> many1Chars (satisfy (fun c -> System.Char.IsLetterOrDigit c || c = '_'))

    let pNbtString = pQuotedString |>> NbtString
    let pNbtInt = pint32 .>> opt (skipChar 'i' <|> skipChar 'I') |>> NbtInt // Handles optional 'i' suffix
    let pNbtBool = (stringCI "true" >>% NbtBool true) <|> (stringCI "false" >>% NbtBool false)

    let pNbtList =
        between (skipChar '[' .>> ws) (ws >>. skipChar ']') (sepBy (pNbtValue .>> ws) (skipChar ',' .>> ws))
        |>> NbtList

    let pKeyValuePair =
        parse {
            let! key = pNbtKey .>> ws
            do! skipChar ':' .>> ws
            let! value = pNbtValue
            return (key, value)
        }

    let pNbtCompound =
        between (skipChar '{' .>> ws) (ws >>. skipChar '}') (sepBy (pKeyValuePair .>> ws) (skipChar ',' .>> ws))
        |>> (Map.ofList >> NbtCompound)

    // Complete NBT Value Choice
    do pNbtValueImpl.Value <-
        choice [
            attempt pNbtBool
            attempt pNbtInt
            pNbtString
            pNbtList
            pNbtCompound
        ]

    // Matches: .withTag({ ... })
    let pWithTagSuffix : Parser<NbtValue> =
        parse {
            do! pstring ".withTag(" >>. ws
            let! nbt = pNbtCompound
            do! ws >>. skipChar ')'
            return nbt
        }
