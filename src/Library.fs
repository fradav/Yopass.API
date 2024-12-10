namespace Yopass.CLI

// Yopass - Secure sharing for secrets, passwords and files

// Flags:
//       --api string          Yopass API server location (default "https://api.yopass.se")
//       --decrypt string      Decrypt secret URL
//       --expiration string   Duration after which secret will be deleted [1h, 1d, 1w] (default "1h")
//       --file string         Read secret from file instead of stdin
//       --key string          Manual encryption/decryption key
//       --one-time            One-time download (default true)
//       --url string          Yopass public URL (default "https://yopass.se")

// Settings are read from flags, environment variables, or a config file located at
// ~/.config/yopass/defaults.<json,toml,yml,hcl,ini,...> in this order. Environment
// variables have to be prefixed with YOPASS_ and dashes become underscores.

// Examples:
//       # Encrypt and share secret from stdin
//       printf 'secret message' | yopass

//       # Encrypt and share secret file
//       yopass --file /path/to/secret.conf

//       # Share secret multiple time a whole day
//       cat secret-notes.md | yopass --expiration=1d --one-time=false

//       # Decrypt secret to stdout
//       yopass --decrypt https://yopass.se/#/...

// Website: https://yopass.se

// Make a module to wrap the yopass CLI with Fli
// the command name of yopass should be "yopass" by default but can be changed by the user
// Converts all CLI arguments to yopass into idiomatic f# function arguments 

open Fli
open System.IO

type Wrapper (?api: string, ?url: string, ?expiration: string, ?key: string, ?oneTime: bool, ?docker: bool) =
    let inputExecConString (excon: string list -> ExecContext) (args: unit -> string list) s =
        excon (args ()) {
            Input s
        }

    let outputCommandToText : ExecContext -> string = 
        Command.execute 
        >> Output.throwIfErrored
        >> Output.toText 

    member val Docker = Option.defaultValue true docker with get, set
    member val Api = api with get, set
    member val Url = url with get, set
    member val Expiration = expiration with get, set
    member val Key = key with get, set
    member val OneTime = oneTime with get, set

    // private values
    member val private MountedFile = None with get, set
    member val private OrigFile = None with get, set
    member private this.File
        with get() = if this.Docker then this.MountedFile else this.OrigFile
        and set(value) =
            match value with
            | None -> this.OrigFile <- None
            | Some v -> 
                if v <> "" && File.Exists v then
                    this.OrigFile <- Some v
                    this.MountedFile <- Some $"/tmp/yopass/{Path.GetFileName v}"
                else
                    failwith "File does not exist"

    member this.ServerArgs() = 
        [ 
            Option.map (fun x -> $"--api {x}") this.Api
            Option.map (fun x -> $"--url {x}") this.Url
        ]
        |> List.filter Option.isSome
        |> List.map Option.get

    member this.SecretArgs() = 
        [ 
            Option.map (fun x -> $"--expiration {x}") this.Expiration
            Option.map (fun x -> $"--key {x}") this.Key
            Option.map (fun x -> $"--one-time {x}") this.OneTime
        ]
        |> List.filter Option.isSome
        |> List.map Option.get

    member this.FileArgs() = 
        [ 
            Option.map (fun x -> $"--file {x}") this.File
        ]
        |> List.filter Option.isSome
        |> List.map Option.get

    member this.Cli arglist= 
        let args = arglist |> String.concat " "
        if this.Docker then
            let argsdocker =
                if Option.isSome this.MountedFile then
                // Get the file name with extension
                    $"run --rm --entrypoint /yopass -i -v {this.OrigFile.Value}:{this.MountedFile.Value} jhaals/yopass {args}"   
                else
                    $"run --rm --entrypoint /yopass -i jhaals/yopass {args}"
            cli {
                Exec "docker"
                Arguments argsdocker
            }
        else
            cli {
                Exec "yopass"
                Arguments $"{args}"
            }

    member this.encrypt= 
        let args() = List.append (this.ServerArgs()) (this.SecretArgs())
        inputExecConString this.Cli args >> outputCommandToText

    member this.decrypt url = 
        let args = List.append (this.ServerArgs()) [$"--decrypt {url}"]
        this.Cli args |> outputCommandToText

    member this.encryptFile file =
        this.File <- Some file
        let args = List.concat [(this.ServerArgs()); (this.SecretArgs()); (this.FileArgs())]
        this.Cli args |> outputCommandToText
