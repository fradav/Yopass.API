namespace Yopass

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

open FsHttp
open System.Text
open System

type Secret = { message: string }

type Wrapper(?api: string, ?url: string, ?expiration: int, ?key: string, ?oneTime: bool, ?docker: bool) =

    /// Parses a Yopass URL and returns secret ID, key, file option flag, and key option flag
    let parseURL (s: string) : Result<string * string * bool * bool, string> =
        try
            let url = Uri(s.Trim())
            let fragments = url.Fragment.TrimStart('#').Split('/')

            if fragments.Length < 3 || fragments.Length > 4 || fragments.[0] <> "" then
                Error $"unexpected URL: {s}"
            else
                let id = fragments.[2]
                let key = if fragments.Length = 4 then fragments.[3] else ""

                match fragments.[1] with
                | "s" -> Ok(id, key, false, false)
                | "c" -> Ok(id, key, false, true)
                | "f" -> Ok(id, key, true, false)
                | "d" -> Ok(id, key, true, true)
                | _ -> Error $"unexpected URL: {s}"
        with ex ->
            Error $"invalid URL: {ex.Message}"

    member val Api = Option.defaultValue "https://api.yopass.se" api with get, set
    member val Url = Option.defaultValue "https://yopass.se" url with get, set
    member val Expiration = Option.defaultValue (24 * 3600) expiration with get, set
    member val Key = key with get, set
    member val OneTime = Option.defaultValue false oneTime with get, set

    member this.encrypt message =
        let keygen =
            match this.Key with
            | None -> (Crypt.generateKey ()), true
            | Some "" -> (Crypt.generateKey ()), true
            | Some k -> k, false

        let encryptedMessage = Crypt.encrypt message (fst keygen)

        let payload =
            {| expiration = this.Expiration
               message = encryptedMessage
               one_time = this.OneTime |}
            |> Json.JsonSerializer.Serialize
        // printfn "%s" payload
        http {
            POST(this.Api + "/secret")
            body
            text payload
        }
        |> Request.send
        |> Response.toJson
        |> fun json ->
            this.Url
            + "/#/s/"
            + json?message.GetString()
            + "/"
            + (if snd keygen then fst keygen else "")

    member this.decrypt(url: string) =
        // Validate URL configuration
        if not (url.StartsWith(this.Url)) then
            failwith "Unconfigured yopass decrypt URL, set --api and --url"

        // Parse URL and extract components
        let id, key, _, keyOpt =
            match parseURL url with
            | Ok x -> x
            | Error e -> failwith $"Invalid yopass decrypt URL: {e}"

        // Handle key validation
        let decryptKey =
            match (keyOpt, key, this.Key) with
            | true, "", None -> failwith "Manual decryption key required, set --key"
            | _, "", Some k -> k
            | _, k, _ -> k

        if decryptKey = "" then
            failwith $"Invalid decryption key parseURl {(id, key, keyOpt)}"
        // Fetch message
        let msg =
            http { GET(sprintf "%s/secret/%s" this.Api id) }
            |> Request.send
            |> Response.deserializeJson<Secret>
            |> _.message

        // Decrypt message
        try
            Crypt.decrypt msg decryptKey
        with ex ->
            failwith $"Failed to decrypt secret: {ex.Message}\nSecret is : {msg}\nKey is : {decryptKey}"
