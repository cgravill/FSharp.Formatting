namespace FSharp.Formatting.Literate

open System.IO
open System.Collections.Concurrent
open System.Globalization
open FSharp.Formatting.Literate
open FSharp.Formatting.CodeFormat
open FSharp.Formatting.Markdown

// --------------------------------------------------------------------------------------
// Processing Markdown documents
// --------------------------------------------------------------------------------------

/// [omit]
module Formatting =

  /// Format document with the specified output kind
  let format (doc: MarkdownDocument) generateAnchors outputKind =
    match outputKind with
    | OutputKind.Pynb -> Markdown.ToPynbString(doc)
    | OutputKind.Latex -> Markdown.ToLatexString(doc)
    | OutputKind.Html ->
        let sb = new System.Text.StringBuilder()
        use wr = new StringWriter(sb)
        Html.formatMarkdown wr generateAnchors System.Environment.NewLine true doc.DefinedLinks doc.Paragraphs
        sb.ToString()

  /// Try find first-level heading in the paragraph collection
  let findHeadings paragraphs generateAnchors (outputKind:OutputKind) =
    paragraphs |> Seq.tryPick (function
      | Heading(1, text, r) ->
          let doc = MarkdownDocument([Span(text, r)], dict [])
          Some(format doc generateAnchors outputKind)
      | _ -> None)

  /// Given literate document, get a new MarkdownDocument that represents the
  /// entire source code of the specified document (with possible `fsx` formatting)
  let getSourceDocument (doc:LiterateDocument) =
    match doc.Source with
    | LiterateSource.Markdown text ->
        doc.With(paragraphs = [CodeBlock (text, None, "", "", None)])
    | LiterateSource.Script snippets ->
        let mutable count = 0
        let paragraphs =
          [ for Snippet(name, lines) in snippets do
              if snippets.Length > 1 then
                yield Heading(3, [Literal(name, None)], None)
              let id = count <- count + 1; "cell" + string count
              let opts = { Evaluate=true; ExecutionCount=None; OutputName=id; Visibility=LiterateCodeVisibility.VisibleCode }
              yield EmbedParagraphs(LiterateCode(lines, opts), None) ]
        doc.With(paragraphs = paragraphs)

// --------------------------------------------------------------------------------------
// Generates file using HTML
// --------------------------------------------------------------------------------------

/// [omit]
module Templating =

  // ------------------------------------------------------------------------------------
  // Formate literate document
  // ------------------------------------------------------------------------------------

  let processFile (doc:LiterateDocument) output ctx =

    // If we want to include the source code of the script, then process
    // the entire source and generate replacement {source} => ...some html...
    let sourceReplacements =
      if ctx.IncludeSource then
        let doc =
          Formatting.getSourceDocument doc
          |> Transformations.replaceLiterateParagraphs ctx
        let content = Formatting.format doc.MarkdownDocument ctx.GenerateHeaderAnchors ctx.OutputKind
        [ "source", content ]
      else []

    // Get page title (either heading or file name)
    let pageTitle =
      let name = Path.GetFileNameWithoutExtension(output)
      defaultArg (Formatting.findHeadings doc.Paragraphs ctx.GenerateHeaderAnchors ctx.OutputKind) name

    // To avoid clashes in templating use {contents} for Latex and older {document} for HTML
    let contentTag =
      match ctx.OutputKind with
      | OutputKind.Html -> "document"
      | OutputKind.Latex -> "contents"
      | OutputKind.Pynb -> "cells"

    // Replace all special elements with ordinary Html/Latex Markdown
    let doc = Transformations.replaceLiterateParagraphs ctx doc
    let formattedDocument = Formatting.format doc.MarkdownDocument ctx.GenerateHeaderAnchors ctx.OutputKind
    let tipsHtml = doc.FormattedTips

    // Construct new Markdown document and write it
    let parameters =
      ctx.Replacements @ sourceReplacements @
      [ "page-title", pageTitle
        "page-source", doc.SourceFile
        contentTag, formattedDocument
        "tooltips", tipsHtml ]

    {
      ContentTag   = contentTag
      Parameters   = parameters
    }
