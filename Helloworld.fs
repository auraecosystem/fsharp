let hello name =
    printfn $"Hello, {name}!"

let greets = [
    "World"
    "Solar System"
    "Galaxy"
    "Universe"
    "Omniverse"
]

greets |> List.iter hello
