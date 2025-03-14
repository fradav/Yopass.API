#r "fsproj: ../src/Yopass/Yopass.fsproj"

open Yopass

let yopass = Wrapper(key = "some key")

let secret = "my secret"

let encrypted = secret |> yopass.encrypt
let decrypted = encrypted |> yopass.decryptString

printfn "Secret: %s" secret
printfn "Encrypted: %s" encrypted
printfn "Decrypted: %s" decrypted
