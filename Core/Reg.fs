open Aura.Core.Workflows

type UserError = 
    | InvalidEmail of string 
    | UserNotFound of string

let validateEmail email =
    if email |> String.contains "@" then Ok email
    else Error (InvalidEmail email)

let processRegistration request =
    request.Email
    |> validateEmail
    |> Result.map (fun email -> { Id = System.Guid.NewGuid(); Email = email })
