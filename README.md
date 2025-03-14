# Yopass.API

Yopass.API is a .NET library written in F# that provides a wrapper for interacting with the [Yopass](https://yopass.se) service, either the original one at https://yopass.se or self-hosted. 

[Yopass](https://yopass.se) is a tool for securely sharing secrets using a one-time URL. When using the site directly encryption happens locally in the browser, and the encrypted secret is never sent to the server. This library does the same thing that the browser does, but at API level, encrypting and decrypting secrets inside your .NET application (F# or C#).


## Features

- Encrypt and decrypt secrets using Yopass
- Generate secure encryption keys
- Configure Yopass service settings

## Installation

To install the Yopass.API library, add it to your project using the .NET CLI:

```sh
dotnet add package Yopass.API
```

## Usage

### Encrypting and Decrypting Secrets

For F# 

```fsharp
open Yopass

// Create a wrapper with default settings
let yopass = Wrapper()

// Or create with custom settings
// let yopass = Wrapper(url = "https://yopass.se", api = "https://api.yopass.se", 
//                      expiration = 3600, oneTime = true)

let secret = "my secret"
let encrypted = yopass.encrypt secret
let decrypted = encrypted |> yopass.decryptString

printfn "Encrypted: %s" encrypted
printfn "Decrypted: %s" decrypted
```

For C#

```csharp
using Yopass;
using System;

// Create a wrapper with default settings
var yopass = new Wrapper();

// Or create with custom settings
// var yopass = new Wrapper(url: "https://yopass.se", api: "https://api.yopass.se", 
//                          expiration: 3600, oneTime: true);

string secret = "my secret";
string encrypted = yopass.encrypt(secret);
string decrypted = yopass.decryptString(encrypted);

Console.WriteLine($"Encrypted: {encrypted}");
Console.WriteLine($"Decrypted: {decrypted}");
```

### Using a Custom Key

```fsharp
open Yopass

let yopass = Wrapper(key = "some key")
let secret = "my secret"

let encrypted = secret |> yopass.encrypt
let decrypted = encrypted |> yopass.decryptString
```

```csharp
using Yopass;

var yopass = new Wrapper(key: "some key");
string secret = "my secret";

string encrypted = yopass.encrypt(secret);
string decrypted = yopass.decryptString(encrypted);
```

### Generating a Secure Key

`Yopass.Wrapper` options :
<!-- generate a markdown table with each option  -->

| Option | Description | Default |
| --- | --- | --- |
| `url` | The URL of the Yopass service | `https://yopass.se` |
| `api` | The URL of the Yopass API | `https://api.yopass.se` |
| `expiration` | The expiration time in seconds | `3600` |
| `oneTime` | Whether the secret is one-time | `true` |
| `key` | The encryption key | **Optional** |

## Building

Launch the build process using the .NET CLI:

```sh
dotnet restore
dotnet build
```

## Contributing

Contributions are welcome! Please open an issue or submit a pull request on GitHub.

## License

This project is licensed under the MIT License. See the LICENSE file for details.