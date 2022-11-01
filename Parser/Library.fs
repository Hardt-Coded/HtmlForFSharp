namespace Parser

open System.Diagnostics

module HtmlParser =
    
    open FParsec
    
    
    type Attribute = 
        | Attribute of posStart:int64 * posEnd:int64 * name:string * value:string
        | LitAttribute of posStart:int64 * posEnd:int64 * name:string * value:string
        | NoAttribute
    
    type Elements =
        | HtmlElement of posStart:int64 * posEnd:int64 * attributeName:string * attributes: Attribute list * children:Elements list
        | HtmlComment of posStart:int64 * posEnd:int64 * content:string
        | Rest of posStart:int64 * posEnd:int64 * content:string
    
    
    let attributeNameDef = letter <|> digit <|> CharParsers.anyOf ['-']
    
    
    let attributeBool = 
        parse {
            let! posStart = getPosition
            let! name = many1CharsTill attributeNameDef (followedBy (pchar '>' <|> pchar ' '))
            let! posEnd = getPosition
            return Attribute(posStart.Index, posEnd.Index, name, "")
        }
    
    
    let attributeWithValue: Parser<Attribute, unit> =
        parse {
            let! posStart = getPosition
            let! name = many1CharsTill attributeNameDef (followedBy (pchar '='))
            do! skipChar '='
            do! skipMany (pchar ''' <|> pchar '"')
            let! value = manyCharsTill anyChar (followedBy (pchar ''' <|> pchar '"' <|> pchar '>'))
            do! skipMany (pchar ''' <|> pchar '"')
            let! posEnd = getPosition
            do! followedBy (pchar ' ' <|> pchar '>')
            return Attribute(posStart.Index, posEnd.Index, name, value)
        }
    
    let litAttributeValue: Parser<Attribute, unit> =
        parse {
            let! posStart = getPosition
            do! skipMany1 (pchar '.' <|> pchar '@')
            let! name = many1CharsTill attributeNameDef (followedBy (pchar '='))
            do! skipChar '='
            do! skipChar '{'
            let! value = manyCharsTill anyChar (followedBy (pchar '}'))
            do! skipChar '}'
            let! posEnd = getPosition
            do! followedBy (pchar ' ' <|> pchar '>')
            return LitAttribute(posStart.Index, posEnd.Index, name, value)
        }
    
    let attribute =
        choice [
            
            attempt attributeWithValue
            attempt litAttributeValue
            attempt attributeBool
        ]
    
    
    let (htmlElement:Parser<Elements, unit>), htmlElementRef = createParserForwardedToRef()
    
    
    let htmlTag: Parser<Elements, unit> =
        parse {
            do! spaces
            let! posStart = getPosition
            do! skipChar '<'
            let! tagName = manyChars (letter <|> digit <|> CharParsers.anyOf ['-'])
            do! spaces
            let! attributes = sepBy attribute (skipChar ' ' .>> spaces)
            
            do! skipChar '>'
            do! spaces
    
            // ...
            let! children = manyTill htmlElement (followedBy (pstring "</"))
    
            do! skipString "</"
            do! skipString tagName // use the result of the earlier parser
            do! spaces
            do! skipChar '>'
            let! posEnd = getPosition
            do! spaces
    
            return HtmlElement (posStart.Index, posEnd.Index, tagName, attributes, children)
        }
    
    let voidElement: Parser<Elements, unit> =
        parse {
            do! spaces
            let! posStart = getPosition
            do! skipChar '<'
            let! tagName = manyChars (letter <|> digit <|> CharParsers.anyOf ['-'])
            do! spaces
            let! attributes = sepBy attribute (skipChar ' ' .>> spaces)
            
            do! skipMany1 (pstring ">" <|> pstring "/>")
            do! spaces
            let! posEnd = getPosition
            do! spaces
    
            return HtmlElement (posStart.Index, posEnd.Index, tagName, attributes, [])
        }
    
    let htmlComment: Parser<Elements, unit> =
        parse {
            do! spaces
            let! posStart = getPosition
            do! skipString "<!--"
            let! content = manyCharsTill anyChar (followedBy (pstring "-->"))
            do! skipString "-->"
            let! posEnd = getPosition
            do! spaces
    
            return HtmlComment (posStart.Index, posEnd.Index, content)
        }
    
    let rest : Parser<Elements, unit> =
        parse {
            let! posStart = getPosition
            do! spaces
            let! content = manyCharsTill anyChar (followedBy (pstring "<"))
            do! spaces
            let! posEnd = getPosition
            return Rest (posStart.Index, posEnd.Index, content)
        }
    
    htmlElementRef.Value <- choice [
        attempt htmlTag
        attempt voidElement
        attempt htmlComment
        rest
    ]
    
    let htmlElements =
        parse {
            let! res = manyTill htmlElement (followedBy (eof))
            return res
        }
    
    open Microsoft.VisualStudio.Text
    open Microsoft.VisualStudio.Text.Classification


    let mapAttributesToClassification 
        (span: SnapshotSpan)
        attributeDelimiter
        litAttributeDelitmiter
        attribute =
        match attribute with
        | Attribute (posStart,posEnd,name,value) ->
            [
                let start = span.Start + (posStart |> int)
                let ende = span.Start + (posEnd |> int)
                let s = SnapshotSpan(span.Snapshot, start, ende )
                ClassificationSpan(s ,attributeDelimiter)
            ]
        | LitAttribute (posStart,posEnd,name,value) ->
            [
                let start = span.Start + (posStart |> int)
                let ende = span.Start + (posEnd |> int)
                let s = SnapshotSpan(span.Snapshot, start, ende )
                ClassificationSpan(s ,litAttributeDelitmiter)
            ]
        | NoAttribute ->
            []


    let rec mapElementToClassification 
        (span: SnapshotSpan)
        
        attributeDelimiter
        litAttributeDelitmiter
        commentDelimiter
        
        element =
        match element with
        | HtmlElement (posStart, posEnd, name, attributes, children) ->
            [
                for attribute in attributes do
                    yield! attribute |> mapAttributesToClassification span attributeDelimiter litAttributeDelitmiter
                for child in children do
                    yield! child |> mapElementToClassification span attributeDelimiter litAttributeDelitmiter commentDelimiter
            ]
        | HtmlComment (posStart, posEnd, content) ->
            [
                let start = span.Start + (posStart |> int)
                let ende = span.Start + (posEnd |> int)
                let s = SnapshotSpan(span.Snapshot, start, ende )
                ClassificationSpan(s ,commentDelimiter)
            ]
        | Rest (posStart, posEnd, content) ->
            [
            ]

    let runHtmlParser 
        snapShot
        
        attributeDelimiter
        litAttributeDelitmiter
        commentDelimiter
    
        string =
        try
            match run htmlElements string with
            | Success (res ,_,_)->
                res 
                |> List.collect (fun e -> 
                    e |> mapElementToClassification snapShot attributeDelimiter litAttributeDelitmiter commentDelimiter
                )
            
            | Failure (error, perror, pos) ->
                Debug.WriteLine $"Parser-Error: %A{error} - %A{perror} - %A{pos}"
                []
        with
        | ex ->
            Debug.WriteLine $"Parser-Error: %A{ex.Message}"
            []
        
    