module ASTEvaluator

// Recursive ADT representing an Abstract Syntax Tree
type Expr =
    | Num of float
    | Var of string
    | Add of Expr * Expr
    | Multiply of Expr * Expr
    | Let of varName: string * value: Expr * body: Expr

type Environment = Map<string, float>

// Recursive evaluator using structural pattern matching
let rec eval (env: Environment) (expr: Expr) : float =
    match expr with
    | Num value -> 
        value

    | Var name -> 
        match Map.tryFind name env with
        | Some v -> v
        | None -> failwithf "Unbound variable '%s'" name

    | Add (left, right) -> 
        eval env left + eval env right

    | Multiply (left, right) -> 
        eval env left * eval env right

    | Let (varName, valueExpr, bodyExpr) ->
        let value = eval env valueExpr
        let newEnv = Map.add varName value env
        eval newEnv bodyExpr

// --- USAGE EXAMPLE ---
// Expression: let x = 5 in x * (x + 2.5)
let programAST = 
    Let ("x", Num 5.0, 
        Multiply (
            Var "x", 
            Add (Var "x", Num 2.5)
        )
    )

let result = eval Map.empty programAST
// Output: 37.5
