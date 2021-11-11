namespace FSharp.Formatting.ApiDocs

[<assembly: System.Runtime.CompilerServices.InternalsVisibleTo("fsdocs")>]
do ()

/// <summary>
///  This type exposes the functionality for producing documentation model from `dll` files with associated `xml` files
///  generated by the F# or C# compiler. To generate documentation model, use one of the overloades of the `Generate` method.
/// </summary>
///
/// <namespacedoc>
///   <summary>Functionality relating to generating API documentation</summary>
/// </namespacedoc>

type ApiDocs =

    /// <summary>
    /// Generates a documentation model for the assemblies specified by the `inputs` parameter
    /// </summary>
    ///
    /// <param name="inputs">the components to generate documentation for</param>
    /// <param name="collectionName">the overall collection name</param>
    /// <param name="substitutions">the substitutions to use in content and templates</param>
    /// <param name="qualify">qualify the output set by collection name, e.g. <c>reference/FSharp.Core/...</c></param>
    /// <param name="libDirs">Use this to specify additional paths where referenced DLL files can be found when formatting code snippets inside Markdown comments</param>
    /// <param name="otherFlags">Additional flags that are passed to the F# compiler (you can use this if you want to
    ///    specify references explicitly etc.)</param>
    /// <param name="root">The root url of the generated documentation within the website</param>
    /// <param name="urlRangeHighlight">A function that can be used to override the default way of generating GitHub links</param>
    /// <param name="strict">Fail if any assembly is missing XML docs or can't be resolved</param>
    /// <param name="extension">The extensions used for files and URLs</param>
    /// <param name="onError"></param>
    ///
    static member GenerateModel
        (
            inputs: ApiDocInput list,
            collectionName,
            substitutions,
            ?qualify,
            ?libDirs,
            ?otherFlags,
            ?root,
            ?urlRangeHighlight,
            ?onError,
            ?extension
        ) =
        let root = defaultArg root "/"
        let qualify = defaultArg qualify false
        let onError = defaultArg onError ignore

        let extensions = defaultArg extension { InFile = ".html"; InUrl = ".html" }

        ApiDocModel.Generate(
            inputs,
            collectionName = collectionName,
            libDirs = libDirs,
            qualify = qualify,
            otherFlags = otherFlags,
            urlRangeHighlight = urlRangeHighlight,
            root = root,
            substitutions = substitutions,
            onError = onError,
            extensions = extensions
        )

    /// <summary>
    /// Generates the search index from the given documentation model
    /// </summary>
    ///
    /// <param name="model">the model for documentation</param>
    static member SearchIndexEntriesForModel(model: ApiDocModel) =
        GenerateSearchIndex.searchIndexEntriesForModel model

    /// Like GenerateHtml but allows for intermediate phase to insert other global substitutions
    /// and combine search index
    static member internal GenerateHtmlPhased
        (
            inputs,
            output,
            collectionName,
            substitutions,
            ?template,
            ?root,
            ?qualify,
            ?libDirs,
            ?otherFlags,
            ?urlRangeHighlight,
            ?onError
        ) =
        let root = defaultArg root "/"
        let qualify = defaultArg qualify false
        let onError = defaultArg onError ignore
        let extensions = { InFile = ".html"; InUrl = ".html" }

        let model =
            ApiDocModel.Generate(
                inputs,
                collectionName = collectionName,
                libDirs = libDirs,
                qualify = qualify,
                otherFlags = otherFlags,
                urlRangeHighlight = urlRangeHighlight,
                root = root,
                substitutions = substitutions,
                onError = onError,
                extensions = extensions
            )

        let renderer = GenerateHtml.HtmlRender(model)

        let index = GenerateSearchIndex.searchIndexEntriesForModel model

        model,
        renderer.GlobalSubstitutions,
        index,
        (fun globalParameters -> renderer.Generate(output, template, collectionName, globalParameters))

    /// <summary>
    /// Generates default HTML pages for the assemblies specified by the `inputs` parameter
    /// </summary>
    ///
    /// <param name="inputs">the components to generate documentation for</param>
    /// <param name="output">the output directory</param>
    /// <param name="collectionName">the overall collection name</param>
    /// <param name="substitutions">the substitutions to use in content and templates</param>
    /// <param name="template">the template to use for each documentation page</param>
    /// <param name="root">The root url of the generated documentation within the website</param>
    /// <param name="qualify">qualify the output set by collection name, e.g. `reference/FSharp.Core/...`</param>
    /// <param name="libDirs">Use this to specify additional paths where referenced DLL files can be found when formatting code snippets inside Markdown comments</param>
    /// <param name="otherFlags">Additional flags that are passed to the F# compiler to specify references explicitly etc.</param>
    /// <param name="urlRangeHighlight">A function that can be used to override the default way of generating GitHub links</param>
    /// <param name="strict">Fail if any assembly is missing XML docs or can't be resolved</param>
    static member GenerateHtml
        (
            inputs,
            output,
            collectionName,
            substitutions,
            ?template,
            ?root,
            ?qualify,
            ?libDirs,
            ?otherFlags,
            ?urlRangeHighlight,
            ?onError
        ) =
        let root = defaultArg root "/"
        let qualify = defaultArg qualify false
        let onError = defaultArg onError ignore
        let extensions = { InFile = ".html"; InUrl = ".html" }

        let model =
            ApiDocModel.Generate(
                inputs,
                collectionName = collectionName,
                libDirs = libDirs,
                qualify = qualify,
                otherFlags = otherFlags,
                urlRangeHighlight = urlRangeHighlight,
                root = root,
                substitutions = substitutions,
                onError = onError,
                extensions = extensions
            )

        let renderer = GenerateHtml.HtmlRender(model)

        let index = GenerateSearchIndex.searchIndexEntriesForModel model

        renderer.Generate(output, template, collectionName, renderer.GlobalSubstitutions)
        model, index

    /// Like GenerateMarkdown but allows for intermediate phase to insert other global substitutions
    /// and combine search index
    static member internal GenerateMarkdownPhased
        (
            inputs,
            output,
            collectionName,
            substitutions,
            ?template,
            ?root,
            ?qualify,
            ?libDirs,
            ?otherFlags,
            ?urlRangeHighlight,
            ?onError
        ) =
        let root = defaultArg root "/"
        let qualify = defaultArg qualify false
        let onError = defaultArg onError ignore
        let extensions = { InFile = ".md"; InUrl = "" }

        let model =
            ApiDocModel.Generate(
                inputs,
                collectionName = collectionName,
                libDirs = libDirs,
                qualify = qualify,
                otherFlags = otherFlags,
                urlRangeHighlight = urlRangeHighlight,
                root = root,
                substitutions = substitutions,
                onError = onError,
                extensions = extensions
            )

        let renderer = GenerateMarkdown.MarkdownRender(model)

        let index = GenerateSearchIndex.searchIndexEntriesForModel model

        model,
        renderer.GlobalSubstitutions,
        index,
        (fun globalParameters -> renderer.Generate(output, template, collectionName, globalParameters))

    /// <summary>
    /// Generates default Markdown pages for the assemblies specified by the `inputs` parameter
    /// </summary>
    ///
    /// <param name="inputs">the components to generate documentation for</param>
    /// <param name="output">the output directory</param>
    /// <param name="collectionName">the overall collection name</param>
    /// <param name="substitutions">the substitutions to use in content and templates</param>
    /// <param name="template">the template to use for each documentation page</param>
    /// <param name="root">The root url of the generated documentation within the website</param>
    /// <param name="qualify">qualify the output set by collection name, e.g. `reference/FSharp.Core/...`</param>
    /// <param name="libDirs">Use this to specify additional paths where referenced DLL files can be found when formatting code snippets inside Markdown comments</param>
    /// <param name="otherFlags">Additional flags that are passed to the F# compiler to specify references explicitly etc.</param>
    /// <param name="urlRangeHighlight">A function that can be used to override the default way of generating GitHub links</param>
    /// <param name="strict">Fail if any assembly is missing XML docs or can't be resolved</param>
    ///
    static member GenerateMarkdown
        (
            inputs,
            output,
            collectionName,
            substitutions,
            ?template,
            ?root,
            ?qualify,
            ?libDirs,
            ?otherFlags,
            ?urlRangeHighlight,
            ?onError
        ) =
        let root = defaultArg root "/"
        let qualify = defaultArg qualify false
        let onError = defaultArg onError ignore
        let extensions = { InFile = ".md"; InUrl = "" }

        let model =
            ApiDocModel.Generate(
                inputs,
                collectionName = collectionName,
                libDirs = libDirs,
                qualify = qualify,
                otherFlags = otherFlags,
                urlRangeHighlight = urlRangeHighlight,
                root = root,
                substitutions = substitutions,
                onError = onError,
                extensions = extensions
            )

        let renderer = GenerateMarkdown.MarkdownRender(model)

        let index = GenerateSearchIndex.searchIndexEntriesForModel model

        renderer.Generate(output, template, collectionName, renderer.GlobalSubstitutions)
        model, index
