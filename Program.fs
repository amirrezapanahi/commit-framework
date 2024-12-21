open Spectre
open System.Diagnostics
open Spectre.Console
open OpenAI.Chat
open System.Text.Json
open System

type State = {
    CommitType: string 
    WhatChanged: string 
    WhyItChanged: string 
    WhatEffect: string 
}with 
    member this.Format() =        
        sprintf $"what changed?: {this.WhatChanged} \nwhy?: {this.WhyItChanged} \neffect?: {this.WhatEffect}"

    member this.AISummary() =        
        let client = ChatClient("gpt-4o-mini", Environment.GetEnvironmentVariable("OPENAI_API_KEY"))

        let messages = [|
            SystemChatMessage("you will summarize a git commit message.") :> ChatMessage
            UserChatMessage(this.Format()) :> ChatMessage
        |]

        let jsonSchema = """
        {
            "type": "object",
            "required": [
              "summary"
            ],
            "properties": {
              "summary": {
                "type": "string",
                "description": "A brief summary, limited to 100 characters."
              }
            },
            "additionalProperties": false
        }
        """

        let options = ChatCompletionOptions(
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName = "summary_schema",
                jsonSchema = BinaryData.FromBytes(Text.Encoding.UTF8.GetBytes(jsonSchema)),
                jsonSchemaIsStrict = true
            )
        )

        let completion = client.CompleteChat(messages, options)
        use structuredJson = JsonDocument.Parse(completion.Value.Content.[0].Text)
        structuredJson.RootElement.GetProperty("summary").GetString()


    member this.ToMessage() = 
        let summary = this.AISummary() 
        sprintf $"[{this.CommitType}]: {summary} \n{this.Format()}"

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
