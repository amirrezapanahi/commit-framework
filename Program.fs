open Spectre
open System.Diagnostics
open Spectre.Console

type State = {
    CommitType: string 
    WhatChanged: string 
    WhyItChanged: string 
    WhatEffect: string 
}with 
    member this.ToMessage() = 
        sprintf $"[{this.CommitType}] \nwhat changed?: {this.WhatChanged} \nwhy?: {this.WhyItChanged} \neffect?: {this.WhatEffect}"

//Q1: feat, fix, chore, 
let determineCommitType state =
    let choices = [|"feat"; "fix"; "chore"; "docs"; "refactor"|]

    let mutable prompt = SelectionPrompt<string> ()
    prompt.Title <- "What type?"
    prompt.PageSize <- 10
    prompt <- prompt.AddChoices choices

    let choice = AnsiConsole.Prompt(prompt)

    printfn $"What type: {choice}"

    {state with CommitType = choice}

//Q2:What did you change? 
let determineWhatChanged state =
    let prompt = TextPrompt<string>("What changed?")
    let changed = AnsiConsole.Prompt(prompt)

    {state with WhatChanged = changed}

//Q3:Why did you change it? 
let determineWhy state =
    let prompt = TextPrompt<string>("Why did it change?")
    let why = AnsiConsole.Prompt(prompt)
    {state with WhyItChanged = why}

//Q4:What is the effect of the change? 
let determineEffect state =
    let prompt = TextPrompt<string>("What was the effect?")
    let effect = AnsiConsole.Prompt(prompt)
    {state with WhatEffect = effect}

let questions state = 
    state
    |> determineCommitType 
    |> determineWhatChanged
    |> determineWhy
    |> determineEffect

[<EntryPoint>]
let main args =
    let init = {
        CommitType = ""
        WhatChanged = ""
        WhyItChanged = ""
        WhatEffect = ""
    }

    let state = questions init

    let msg = state.ToMessage()

    //make shell command to "git commit -m {msg}"
    let gitCommit message =
        let startInfo = ProcessStartInfo(
            FileName = "git",
            Arguments = sprintf "commit -m \"%s\"" message,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        )

        use proc = Process.Start(startInfo)
        proc.WaitForExit()
        printfn "Commit successful"

    gitCommit msg

    0
