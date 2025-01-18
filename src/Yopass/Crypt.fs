namespace Yopass

open System.Text
open System.IO
open Org.BouncyCastle.Bcpg
open Org.BouncyCastle.Bcpg.OpenPgp
open Org.BouncyCastle.Security
open System.Buffers.Text

module Crypt =
    open System

    let generateKey () =
        let length = 22
        let b = Array.randomSample length [| 0uy .. 255uy |]
        let base64String = Base64Url.EncodeToString(b)
        base64String.Substring(0, length)

    let encrypt (message: byte[]) (pass: string) =
        use messageStream = new MemoryStream(message)

        let encryptedDataGenerator =
            PgpEncryptedDataGenerator(SymmetricKeyAlgorithmTag.Aes256, true, SecureRandom())

        encryptedDataGenerator.AddMethodRaw(Encoding.UTF8.GetBytes(pass), HashAlgorithmTag.Sha256)

        let compressedDataGenerator =
            PgpCompressedDataGenerator(CompressionAlgorithmTag.Uncompressed)

        let literalDataGenerator = PgpLiteralDataGenerator()

        use encryptedMessageStream = new MemoryStream()

        do
            use armoredStream = new ArmoredOutputStream(encryptedMessageStream)

            use encryptedStream =
                encryptedDataGenerator.Open(armoredStream, Array.zeroCreate<byte> 1048576)

            use compressedStream = compressedDataGenerator.Open(encryptedStream)

            use literalDataStream =
                literalDataGenerator.Open(
                    compressedStream,
                    PgpLiteralData.Binary,
                    "",
                    System.DateTime.Now,
                    Array.zeroCreate<byte> 1048576
                )

            messageStream.Seek(0L, SeekOrigin.Begin) |> ignore
            messageStream.CopyTo(literalDataStream)

        encryptedMessageStream.ToArray() |> Encoding.UTF8.GetString

    let decrypt (encryptedMessage: string) (pass: string) =
        use encryptedMessageStream =
            new MemoryStream(encryptedMessage |> Encoding.UTF8.GetBytes)

        use inputStream = PgpUtilities.GetDecoderStream(encryptedMessageStream)
        let pgpObjectFactory = PgpObjectFactory(inputStream)
        let encrytpetdH = pgpObjectFactory.NextPgpObject() :?> PgpEncryptedDataList

        // let enc =
        //     match encrytpetdH with
        //     | null -> pgpObjectFactory.NextPgpObject() :?> PgpEncryptedDataList
        //     | _ -> encrytpetdH

        let pbe = encrytpetdH[0] :?> PgpPbeEncryptedData

        use clear =
            pbe.GetDataStream(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(pass)).ToCharArray())

        let pgpFact = PgpObjectFactory(clear)
        let oH = pgpFact.NextPgpObject() :?> PgpCompressedData
        let o = PgpObjectFactory(oH.GetDataStream()).NextPgpObject()

        // let o =
        //     match oH with
        //     | :? PgpCompressedData as cData -> PgpObjectFactory(cData.GetDataStream()).NextPgpObject()
        //     | _ -> oH

        use output = new MemoryStream()
        (o :?> PgpLiteralData).GetInputStream().CopyTo(output)
        output.ToArray()
