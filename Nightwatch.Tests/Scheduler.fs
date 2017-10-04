module Nightwatch.Tests.Scheduler

open System

open FSharp.Control.Tasks
open Quartz
open Xunit

open Nightwatch
open Nightwatch.Core.Resources
open Nightwatch.Resources

[<Fact>]
let ``Scheduler should be created`` () =
    async {
        let! scheduler = Scheduler.create()
        Assert.NotNull scheduler
    } |> Async.StartAsTask

[<Fact>]
let ``Scheduler should be started`` () =
    async {
        let! scheduler = Scheduler.create()
        do! Scheduler.start scheduler
        Assert.True scheduler.IsStarted
    } |> Async.StartAsTask

[<Fact>]
let ``Scheduler should be stopped`` () =
    async {
        let! scheduler = Scheduler.create()
        do! Scheduler.start scheduler
        do! Scheduler.stop scheduler
        Assert.True scheduler.IsShutdown
    } |> Async.StartAsTask

let trueChecker = ResourceChecker(fun () -> task { return true })

[<Fact>]
let ``Schedule should be prepared`` () =
    let task =
        { id = "1"
          runEvery = TimeSpan.FromMinutes 1.0
          checker = trueChecker }
    let schedule = Scheduler.prepareSchedule [| task |]
    let (job, trigger) = Seq.exactlyOne schedule
    Assert.Equal (JobKey task.id, job.Key)

[<Fact>]
let ``Scheduler should be configured`` () =
    let task =
        { id = "1"
          runEvery = TimeSpan.FromMinutes 1.0
          checker = trueChecker }
    async {
        let! scheduler = Scheduler.create()
        let schedule = Scheduler.prepareSchedule [| task |]
        do! Scheduler.configure scheduler schedule
        let! job = scheduler.GetJobDetail (JobKey task.id)
        Assert.NotNull job
    }

