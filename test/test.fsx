#r "fsproj: ../src/Yopass.CLI.fsproj"
open Yopass.CLI
open System.IO

// let imagUrl = "https://onesecret.imag.umontpellier.fr"

let yopassImag = Wrapper("http://host.docker.internal", "http://localhost", key = "test")
let machinEncrypted = yopassImag.encrypt "machin"
printfn "%s" machinEncrypted
yopassImag.decrypt machinEncrypted |> printfn "%s"

let gitignoreEncryptedFile = yopassImag.encryptFile (Path.Combine(__SOURCE_DIRECTORY__, "test.fsx")) 
printfn "%s" gitignoreEncryptedFile
yopassImag.decrypt gitignoreEncryptedFile |> printfn "%s"

File.Exists (Path.Combine(__SOURCE_DIRECTORY__, "test.fsx"))