﻿module Nightwatch.Program

open System
open System.Reflection

open Nightwatch.Configuration
open Nightwatch.FileSystem
open Nightwatch.Resources

let private printVersion() =
    let version = Assembly.GetEntryAssembly().GetName().Version
    printfn "Nightwatch v. %A" version
    printfn "Config file format v. %A" Configuration.configFormatVersion

let private splitResults seq =
    let chooseOk = function
    | Ok x -> Some x
    | Error _ -> None

    let chooseError = function
    | Ok _ -> None
    | Error x -> Some x

    let isOk = chooseOk >> Option.isSome

    let (results, errors) =
        seq
        |> Seq.toArray
        |> Array.partition isOk
    Seq.choose chooseOk results, Seq.choose chooseError errors

let private readConfiguration path : Result<Resource seq, InvalidConfiguration seq> =
    async {
        let! resources = Configuration.read FileSystem.system path
        let (results, errors) = splitResults resources
        return
            if Seq.isEmpty errors
            then Ok results
            else Error errors
    } |> Async.RunSynchronously

let private runScheduler resources =
    let schedule = Scheduler.prepareSchedule resources
    async {
        let! scheduler = Scheduler.create()
        do! Scheduler.configure scheduler schedule
        do! Scheduler.start scheduler
        printfn "Scheduler started"
        printfn "Press any key to stop"
        ignore <| Console.ReadKey()
        printfn "Stopping"
        do! Scheduler.stop scheduler
        printfn "Bye"
    } |> Async.RunSynchronously

module private ExitCodes =
    let success = 0
    let invalidArguments = 1
    let configurationError = 2

let private errorsToString errors =
    let printError { path = (Path path); id = id; message = message } =
        sprintf "Path: %s\nId: %s\nMessage: %s"
            path
            (match id with Some x -> x | None -> "N/A")
            message

    String.Join("\n", Seq.map printError errors)

let private printUsage() =
    printfn "Arguments: <path to config directory>"

[<EntryPoint>]
let main argv =
    printVersion()
    match argv with
    | [| configPath |] ->
        let path = Path configPath
        match readConfiguration path with
        | Ok resources ->
            runScheduler resources
            ExitCodes.success
        | Error errors ->
            printfn "%s" (errorsToString errors)
            ExitCodes.configurationError
    | _ ->
        printUsage()
        ExitCodes.invalidArguments
