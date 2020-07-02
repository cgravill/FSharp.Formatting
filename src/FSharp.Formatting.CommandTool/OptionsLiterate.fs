namespace FSharp.Formatting.Options.Literate

open CommandLine
open CommandLine.Text
open FSharp.Literate

open FSharp.Formatting.Common
open FSharp.Formatting.Options
open FSharp.Formatting.Options.Common
open FSharp.Formatting.IExecutable
open FSharp.Formatting.Razor


/// Process directory containing a mix of Markdown documents and F# Script files
// static member ProcessDirectory
//   ( inputDirectory, ?templateFile, ?outputDirectory, ?format, ?fsharpCompiler, ?prefix, ?compilerOptions,
//     ?lineNumbers, ?references, ?replacements, ?includeSource, ?layoutRoots )
type ProcessDirectoryOptions() =

    [<ParserState>]
    member val LastParserState = null with get, set

    // does not work as desired in F#:
    // the HelpOption attribute is not built,
    // but receive a System.MemberAccessException
    //[<HelpOption>]
    /// autogenerated help text
    member x.GetUsageOfOption() =
        let help = new HelpText()
        help.AddDashesToOption <- true
        help.AddOptions(x)
        "\nfsformatting literate --processDirectory [options]" +
        "\n--------------------------------------------------" +
        help.ToString()

    [<Option("help", Required = false,
        HelpText = "Display this message. All options are case-insensitive.")>]
    member val help = false with get, set

    [<Option("waitForKey", Required = false,
        HelpText = "Wait for key before exit.")>]
    member val waitForKey = false with get, set

    // default settings will be mapped to 'None'

    [<Option("inputDirectory", Required = true,
        HelpText = "Input directory of *.fsx and *.md files.")>]
    member val inputDirectory = "" with get, set

    [<Option("templateFile", Required = false,
        HelpText = "Template file for formatting (optional).")>]
    member val templateFile = "" with get, set

    [<Option("outputDirectory", Required = false,
        HelpText = "Ouput Directory, defaults to input directory (optional).")>]
    member val outputDirectory = "" with get, set

    [<Option("format", Required = false,
        HelpText = "Ouput format either 'latex', 'ipynb' or 'html', defaults to 'html' (optional).")>]
    member val format = "html" with get, set

//    [<Option("formatAgent", Required = false,
//        HelpText = "FSharp Compiler selection, defaults to 'FSharp.Compiler.dll' which throws a 'file not found' exception if not in search path (optional).")>]
//    member val fsharpCompiler = "" with get, set

    [<Option("prefix", Required = false,
        HelpText = "Prefix for formatting, defaults to 'fs' (optional).")>]
    member val prefix = "" with get, set

    [<OptionArray("compilerOptions", Required = false,
        HelpText = "Compiler Options (optional).")>]
    member val compilerOptions = [|""|] with get, set

    [<Option("noLineNumbers", Required = false,
        HelpText = "Don't add line numbers, default is to add line numbers (optional).")>]
    member val noLineNumbers = false with get, set

    [<Option("references", Required = false,
        HelpText = "Turn all indirect links into references, defaults to 'false' (optional).")>]
    member val references = false with get, set

    [<Option("fsieval", Required = false,
        HelpText = "Use the default FsiEvaluator, defaults to 'false'")>]
    member val fsieval = false with set, get

    [<OptionArray("replacements", Required = false,
        HelpText = "A whitespace separated list of string pairs as text replacement patterns for the format template file (optional).")>]
    member val replacements = [|""|] with get, set

    [<Option("includeSource", Required = false,
        HelpText = "Include sourcecode in documentation, defaults to 'false' (optional).")>]
    member val includeSource = false with get, set

    [<OptionArray("layoutRoots", Required = false,
        HelpText = "Search directory list for the Razor Engine (optional).")>]
    member val layoutRoots = [|""|] with get, set

    [<Option("live", Required = false,
        HelpText = "Watches for changes in the input directory and re-runs, if a change occures")>]
    member val live = false with get, set

    interface IExecutable with
        member x.Execute() =
            let mutable res = 0
            use watcher = new System.IO.FileSystemWatcher(x.inputDirectory)
            try
                if x.help then
                    printfn "%s" (x.GetUsageOfOption())
                else
                    let run () =
                        RazorLiterate.ProcessDirectory(
                            x.inputDirectory,
                            ?generateAnchors = Some true,
                            ?templateFile = (evalString x.templateFile),
                            ?outputDirectory = Some (if x.outputDirectory = "" then x.inputDirectory else x.outputDirectory),
                            ?format=
                                Some (let fmt = x.format.ToLower()
                                      if fmt = "html" then OutputKind.Html
                                      elif fmt = "ipynb" then OutputKind.Pynb
                                      elif fmt = "tex" || fmt = "latex" then OutputKind.Latex
                                      else failwithf "unknown format '%s'" x.format),
                            ?formatAgent = None,
                            ?prefix = (evalString x.prefix),
                            ?compilerOptions = (evalString (concat x.compilerOptions)),
                            ?lineNumbers = Some (not x.noLineNumbers),
                            ?references = Some x.references,
                            ?fsiEvaluator = (if x.fsieval then Some ( FsiEvaluator() :> _) else None),
                            ?replacements = (evalPairwiseStringArray x.replacements),
                            ?includeSource = Some x.includeSource,
                            ?layoutRoots = (evalStringArray x.layoutRoots))

                    if x.live then
                        watcher.IncludeSubdirectories <- true
                        watcher.NotifyFilter <- System.IO.NotifyFilters.LastWrite
                        let monitor = obj()
                        x.waitForKey <- true
                        Event.add (fun _ -> try lock monitor run with _ -> ()) watcher.Changed
                        watcher.EnableRaisingEvents <- true

                    run()

            with
                | _ as ex ->
                    Log.errorf "received exception in RazorLiterate.ProcessDirectory:\n %A" ex
                    printfn "Error on RazorLiterate.ProcessDirectory: \n%O" ex
                    res <- -1
            waitForKey x.waitForKey
            res

        member x.GetErrorText() =
            if x.LastParserState = null then ""
            else
                let errors = (x.LastParserState :> IParserState).Errors
                parsingErrorMessage(errors)

        member x.GetUsage() = x.GetUsageOfOption()
