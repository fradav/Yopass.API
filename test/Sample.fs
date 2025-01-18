module Tests

open System.Text
open Expecto
open Yopass

open DotNet.Testcontainers.Builders
open System

let network = NetworkBuilder().Build()

let memcachedContainer =
    ContainerBuilder().WithImage("memcached").WithNetwork(network).WithNetworkAliases("memcached").Build()

let yopassContainer =
    ContainerBuilder()
        .DependsOn(memcachedContainer)
        .WithImage("jhaals/yopass")
        .WithNetwork(network)
        .WithCommand($"--memcached=memcached:11211")
        .WithPortBinding("1337", true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(fun r -> r.ForPort(uint16 1337)))
        .Build()

yopassContainer.StartAsync().Wait()

let yopassUrl =
    UriBuilder("http", yopassContainer.Hostname, int (yopassContainer.GetMappedPublicPort(1337)))
        .Uri.ToString()
        .TrimEnd('/')

let setupYopass f () =
    let yopass = Wrapper(yopassUrl, yopassUrl)
    f yopass

let fixtureTests =
    testFixture setupYopass
    <| [ "encrypt a simple string with a generated key and decrypt it",
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

             Expect.equal input decrypted "The decrypted string should be the same as the input" ]
    |> List.ofSeq

[<Tests>]
let tests = testList "samples" fixtureTests |> testSequenced
