type NbtValue =
    | NbtString of string
    | NbtInt of int
    | NbtBool of bool
    | NbtList of NbtValue list
    | NbtCompound of Map<string, NbtValue>

type BracketHandler =
    | ItemBracket of modId: string * itemId: string * count: int * tag: NbtValue option
    | FluidBracket of modId: string * fluidId: string * amountMb: int * tag: NbtValue option
    | TagBracket of category: string * path: string
