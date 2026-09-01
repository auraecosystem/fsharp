module ZenScriptFParsecParser

open FParsec

// ==========================================
// 1. ABSTRACT SYNTAX TREE (AST)
// ==========================================

type BracketHandler =
    | ItemBracket of modId: string * itemId: string * count: int
    | FluidBracket of modId: string * fluidId: string * amountMb: int
    | TagBracket of category: string * path: string

type ZenExpression =
    | BracketExpr of BracketHandler
    | StringLiteral of string
    | ArrayLiteral of ZenExpression list

type ZenStatement =
    | RemoveRecipe of recipeName: string
    | AddShapedRecipe of recipeName: string * output: BracketHandler * matrix: ZenExpression list list

// ==========================================
// 2. FPARSEC COMBINATORS & GRAMMAR
// ==========================================

type UserState = unit
type Parser<'T> = Parser<'T, UserState>

// --- Lexical Helpers ---
let str s : Parser<string> = pstring s
let ws : Parser<unit> = whitespaceChars *> pzero // skip whitespace
let ws1 : Parser<unit> = skipMany1 whitespaceChar
let strWS s = str s .>> skipWhitespace

// --- Identifier Parsers ---
let isIdChar c = System.Char.IsLetterOrDigit c || c = '_' || c = '/'
let identifier : Parser<string> = many1Chars (satisfy isIdChar)

// --- 1. Bracket Handlers Parser ---
// Matches: <item:modid:item_name> (* count)?
let pItemBracket : Parser<BracketHandler> =
    parse {
        do! skipChar '<' >>. str "item:" >>. pzero
        let! modId = identifier
        do! skipChar ':' >>. pzero
        let! itemId = identifier
        do! skipChar '>' >>. pzero
        
        // Optional count multiplier (* N)
        let! countOpt = opt (ws >>. skipChar '*' >>. ws >>. pint32)
        let count = defaultArg countOpt 1
        return ItemBracket (modId, itemId, count)
    }

// Matches: <fluid:modid:fluid_name> (* amount)?
let pFluidBracket : Parser<BracketHandler> =
    parse {
        do! skipChar '<' >>. str "fluid:" >>. pzero
        let! modId = identifier
        do! skipChar ':' >>. pzero
        let! fluidId = identifier
        do! skipChar '>' >>. pzero
        let! amountOpt = opt (ws >>. skipChar '*' >>. ws >>. pint32)
        return FluidBracket (modId, fluidId, defaultArg amountOpt 1000)
    }

// Matches: <tag:items:forge:ingots/copper>
let pTagBracket : Parser<BracketHandler> =
    parse {
        do! skipChar '<' >>. str "tag:" >>. pzero
        let! category = identifier
        do! skipChar ':' >>. pzero
        let! path = many1Chars (satisfy (fun c -> isIdChar c || c = ':'))
        do! skipChar '>' >>. pzero
        return TagBracket (category, path)
    }

let pBracket : Parser<BracketHandler> =
    choice [ pItemBracket; pFluidBracket; pTagBracket ]

// --- 2. Expressions Parser ---
let pStringLiteral : Parser<ZenExpression> =
    between (skipChar '"') (skipChar '"') (manySatisfy (fun c -> c <> '"'))
    |>> StringLiteral

let pExpression, pExpressionImpl = createParserForwardedToRef<ZenExpression, UserState>()

let pArrayLiteral : Parser<ZenExpression> =
    between (strWS "[") (strWS "]") (sepBy pExpression (strWS ","))
    |>> ArrayLiteral

do pExpressionImpl.Value <- 
    choice [
        pBracket |>> BracketExpr
        pStringLiteral
        pArrayLiteral
    ]

// --- 3. Statement Parsers ---

// Matches: recipes.remove("modid:item_name");
let pRemoveRecipe : Parser<ZenStatement> =
    parse {
        do! str "recipes.remove(" >>. skipWhitespace
        let! name = between (skipChar '"') (skipChar '"') (manySatisfy (fun c -> c <> '"'))
        do! skipWhitespace >>. str ");" >>. pzero
        return RemoveRecipe name
    }

// Matches: recipes.addShaped("name", <item:...>, [[<item:...>], [<item:...>]]);
let pAddShapedRecipe : Parser<ZenStatement> =
    parse {
        do! str "recipes.addShaped(" >>. skipWhitespace
        let! name = between (skipChar '"') (skipChar '"') (manySatisfy (fun c -> c <> '"'))
        do! strWS "," >>. pzero
        
        let! output = pBracket
        do! strWS "," >>. pzero

        // Parse nested matrix: [[expr, expr], [expr, expr]]
        let pRow = between (strWS "[") (strWS "]") (sepBy pExpression (strWS ","))
        let pMatrix = between (strWS "[") (strWS "]") (sepBy pRow (strWS ","))
        
        let! matrix = pMatrix
        do! skipWhitespace >>. str ");" >>. pzero
        
        return AddShapedRecipe (name, output, matrix)
    }

let pStatement : Parser<ZenStatement> =
    skipWhitespace >>. choice [ attempt pRemoveRecipe; pAddShapedRecipe ] .>> skipWhitespace

let pZenScriptFile : Parser<ZenStatement list> =
    many pStatement .>> eof

// ==========================================
// 3. EXECUTION & TEST DRIVER
// ==========================================

let parseZenScript (scriptText: string) =
    match run pZenScriptFile scriptText with
    | Success (result, _, _) -> 
        Ok result
    | Failure (errorMsg, _, _) -> 
        Error errorMsg

[<EntryPoint>]
let main _ =
    let sampleScript = """
        recipes.remove("minecraft:stick");

        recipes.addShaped("custom_iron_block", <item:minecraft:iron_block> * 1, [
            [<item:minecraft:iron_ingot>, <item:minecraft:iron_ingot>],
            [<item:minecraft:iron_ingot>, <item:minecraft:iron_ingot>]
        ]);
    """

    printfn "Parsing ZenScript using FParsec..."
    match parseZenScript sampleScript with
    | Ok statements ->
        printfn "\nSuccessfully Parsed %d Statements into F# AST:" statements.Length
        for stmt in statements do
            printfn " -> %A" stmt
    | Error err ->
        printfn "\nParse Error:\n%s" err

    0
