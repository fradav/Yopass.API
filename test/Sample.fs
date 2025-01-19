module Tests

open System.Text
open Expecto
open Yopass

open DotNet.Testcontainers.Builders
open DotNet.Testcontainers.Containers
open System

let network = NetworkBuilder().Build()

let memcachedContainer =
    ContainerBuilder().WithImage("memcached").WithNetwork(network).WithNetworkAliases("memcached").Build()

let yopassContainer args =
    let cmdargs = [ "--memcached=memcached:11211" ] @ args |> Array.ofList

    ContainerBuilder()
        .DependsOn(memcachedContainer)
        .WithImage("jhaals/yopass")
        .WithNetwork(network)
        .WithCommand(cmdargs)
        .WithPortBinding("1337", true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(fun r -> r.ForPort(uint16 1337)))
        // .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
        .Build()

let yopassWrapper (container: IContainer) =
    let yopassURL =
        UriBuilder("http", container.Hostname, int (container.GetMappedPublicPort(1337))).Uri.ToString().TrimEnd('/')

    Wrapper(yopassURL, yopassURL)

type SetupYopassContainer(args) =
    let container = yopassContainer args
    do container.StartAsync().Wait()

    member x.setupYopass f () =
        let yopass = yopassWrapper container
        f yopass

let setupYopass_normal = SetupYopassContainer([]).setupYopass
let setupYopass_long = SetupYopassContainer([ "--max-length=1000000" ]).setupYopass

[<Tests>]
let fixtureTests =
    testFixture
        setupYopass_normal
        [ "encrypt a simple string with a generated key and decrypt it",
          fun yopass ->
              let someKey = Crypt.generateKey ()
              yopass.Key <- Some someKey
              let input = "hello"
              let encrypted = input |> Encoding.UTF8.GetBytes |> yopass.encrypt
              let decrypted = encrypted |> yopass.decrypt |> Encoding.UTF8.GetString

              Expect.equal input decrypted "The decrypted string should be the same as the input"
          "encrypt a simple string with a generated key and try do decrypt with the wrong key",
          fun yopass ->
              yopass.Key <- Some(Crypt.generateKey ())
              let input = "hello"
              let encrypted = input |> Encoding.UTF8.GetBytes |> yopass.encrypt
              yopass.Key <- Some(Crypt.generateKey ())

              let tryDecrypt =
                  try
                      encrypted |> yopass.decrypt |> ignore
                      false
                  with _ ->
                      true

              Expect.isTrue tryDecrypt "This should fail because the key is wrong"
          "encrypt a simple string, no password and decrypt it (use the provided key)",
          fun yopass ->
              let input = "hello"
              let encrypted = input |> Encoding.UTF8.GetBytes |> yopass.encrypt
              let decrypted = encrypted |> yopass.decrypt |> Encoding.UTF8.GetString

              Expect.equal input decrypted "The decrypted string should be the same as the input"
          "set onetime, encrypt a simple string, no password, decrypt it once and fail to decryt again",
          fun yopass ->
              yopass.OneTime <- true
              let input = "hello"
              let encrypted = input |> Encoding.UTF8.GetBytes |> yopass.encrypt
              let decrypted = encrypted |> yopass.decrypt |> Encoding.UTF8.GetString

              Expect.equal input decrypted "The decrypted string should be the same as the input"

              let tryDecrypt =
                  try
                      encrypted |> yopass.decrypt |> ignore
                      false
                  with _ ->
                      true

              Expect.isTrue tryDecrypt "This should fail because the secret is gone" ]
    |> Seq.toList
    |> testList "samples normal"

[<Tests>]
let fixtureLongTests =
    testFixture
        setupYopass_long
        [ "encrypt a (long) binary random data and decrypt it",
          fun yopass ->
              let input = Array.randomChoices 100000 [| 0uy .. 255uy |]

              let encrypted = input |> yopass.encrypt
              let decrypted = encrypted |> yopass.decrypt

              Expect.equal input decrypted "The decrypted string should be the same as the input" ]
    |> Seq.toList
    |> testList "samples long"
