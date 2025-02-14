# Yopass.CLI.Wrap

Yopass.CLI.Wrap is a .NET library written in F# that provides a wrapper for interacting with the Yopass CLI. Yopass is a tool for securely sharing secrets using a one-time URL. This library allows you to easily integrate Yopass functionality into your .NET applications.

## Features

- Encrypt and decrypt secrets using Yopass
- Generate secure encryption keys
- Configure Yopass container settings

## Installation

To install the Yopass.CLI.Wrap library, add it to your project using the .NET CLI:

```sh
dotnet add package Yopass.CLI.Wrap
```

## Usage

### Encrypting and Decrypting Secrets, with on the fly key generation

Beware, the returned URL got the key in it, so it's not secure to share it.

```fsharp
open Yopass.CLI.Wrap

let yopass = Yopass.
let secret = "my secret"
let encryptedSecret = Yopass.encrypt secret
let decryptedSecret = Yopass.decrypt encryptedSecret

printfn "Encrypted: %s" encryptedSecret
printfn "Decrypted: %s" decryptedSecret
```

```csharp
using Yopass.CLI.Wrap;

string secret
string encryptedSecret = Yopass.Encrypt(secret);
string decryptedSecret = Yopass.Decrypt(encryptedSecret);

Console.WriteLine($"Encrypted: {encryptedSecret}");
Console.WriteLine($"Decrypted: {decryptedSecret}");
```

### Encrypting and Decrypting Secrets, with a custom key

```fsharp
open Yopass.CLI.Wrap

let secret = "my secret"


## Contributing

Contributions are welcome! Please open an issue or submit a pull request on GitHub.

## License

This project is licensed under the MIT License. See the LICENSE file for details.