namespace Parser

open System.Diagnostics
open FParsec

module HtmlParser =
    
    type Attribute = 
        | Attribute of posStart:int64 * posEnd:int64 * name:string * value:string
        | LitAttribute of posStart:int64 * posEnd:int64 * name:string * value:string
    
    type Element =
        | OpeningTag of osStart:int64 * posEnd:int64 * name:string * attributes:Attribute list
        | ClosingTag of osStart:int64 * posEnd:int64 * name:string
        | Comment of osStart:int64 * posEnd:int64 * content:string
        | Rest of osStart:int64 * posEnd:int64 * content:string
    
    let names:Parser<char, unit> = letter <|> digit <|> CharParsers.anyOf ['-']
    
    let attributeBool = 
        parse {
            let! posStart = getPosition
            let! name = many1CharsTill names (followedBy (pchar '>' <|> pchar ' ' <|> pchar '/'))
            let! posEnd = getPosition
            return Attribute(posStart.Index, posEnd.Index, name, "")
        }
    
    
    let attributeWithValue1: Parser<Attribute, unit> =
        parse {
            let! posStart = getPosition
            let! name = many1CharsTill names (followedBy (pchar '='))
            do! skipChar '='
            do! notFollowedBy (pchar ''' <|> pchar '"')
            let! value = manyCharsTill anyChar (followedBy (pchar ' ' <|> pchar '>' <|> pchar '/'))
            let! posEnd = getPosition
            do! followedBy (pchar ' ' <|> pchar '>'<|> pchar '/')
            return Attribute(posStart.Index, posEnd.Index, name, value)
        }
    
    let attributeWithValue2: Parser<Attribute, unit> =
        parse {
            let! posStart = getPosition
            let! name = many1CharsTill names (followedBy (pchar '='))
            do! skipChar '='
            do! skipChar '"'
            let! value = manyCharsTill anyChar (followedBy (pchar '"'))
            do! skipChar '"'
            let! posEnd = getPosition
            do! followedBy (pchar ' ' <|> pchar '>'<|> pchar '/')
            return Attribute(posStart.Index, posEnd.Index, name, value)
        }
    
    let attributeWithValue3: Parser<Attribute, unit> =
        parse {
            let! posStart = getPosition
            let! name = many1CharsTill names (followedBy (pchar '='))
            do! skipChar '='
            do! skipChar '''
            let! value = manyCharsTill anyChar (followedBy (pchar '''))
            do! skipChar '''
            let! posEnd = getPosition
            do! followedBy (pchar ' ' <|> pchar '>'<|> pchar '/')
            return Attribute(posStart.Index, posEnd.Index, name, value)
        }
    
    let litAttributeValue: Parser<Attribute, unit> =
        parse {
            let! posStart = getPosition
            do! skipMany1 (pchar '.' <|> pchar '@')
            let! name = many1CharsTill names (followedBy (pchar '='))
            do! skipChar '='
            do! skipChar '{'
            let! value = manyCharsTill anyChar (followedBy (pchar '}'))
            do! skipChar '}'
            let! posEnd = getPosition
            do! followedBy (pchar ' ' <|> pchar '>'<|> pchar '/')
            return LitAttribute(posStart.Index, posEnd.Index, name, value)
        }
    
    let attribute =
        choice [
            
            attempt attributeWithValue1
            attempt attributeWithValue2
            attempt attributeWithValue3
            attempt litAttributeValue
            attempt attributeBool
        ]
    
    
    let openingTag: Parser<Element, unit> =
        parse {
            do! spaces
            let! posStart = getPosition
            do! skipChar '<'
            do! skipMany (pchar '!')
            let! name = many1CharsTill names (followedBy (pstring " " <|> pstring ">" <|> pstring "/>"))
            do! spaces
            let! posEnd = getPosition
            let! attributes = sepBy attribute (skipChar ' ' .>> spaces)
            do! skipString " " <|> skipString ">" <|> skipString "/>"
            
            do! spaces
            return OpeningTag (posStart.Index, posEnd.Index, name, attributes)
        }
    
    
    let closingTag: Parser<Element, unit> =
        parse {
            do! spaces
            let! posStart = getPosition
            do! skipString "</"
            let! name = many1CharsTill names (followedBy (pstring ">"))
            do! skipString ">"
            let! posEnd = getPosition
            do! spaces
            return ClosingTag (posStart.Index, posEnd.Index, name)
        }
    
    
    let htmlComment: Parser<Element, unit> =
        parse {
            do! spaces
            let! posStart = getPosition
            do! skipString "<!--"
            let! content = manyCharsTill anyChar (followedBy (pstring "-->"))
            do! skipString "-->"
            let! posEnd = getPosition
            do! spaces
    
            return Comment (posStart.Index, posEnd.Index, content)
        }
    
    
    let rest : Parser<Element, unit> =
        parse {
            let! posStart = getPosition
            do! spaces
            let! content = 
                manyCharsTill anyChar (followedBy (pchar '<')) <|>
                manyCharsTill anyChar (followedBy (eof))
            
            do! spaces
            let! posEnd = getPosition
            return Rest (posStart.Index, posEnd.Index, content)
        }
    
    
    let htmlElement = choice [
        attempt openingTag
        attempt closingTag
        attempt htmlComment
        rest
    ]
    
    
    let htmlElements =
        parse {
            do! spaces
            let! res = manyTill htmlElement (followedBy (eof))
            do! spaces
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
        


    let rec mapElementToClassification 
        (span: SnapshotSpan)
        
        attributeDelimiter
        litAttributeDelitmiter

        tagDelimiter
        commentDelimiter
        
        element =
        match element with
        | Element.OpeningTag (posStart, posEnd, name, attributes) ->
            [
                for attribute in attributes do
                    yield! attribute |> mapAttributesToClassification span attributeDelimiter litAttributeDelitmiter

                let start = span.Start + (posStart |> int)
                let ende = span.Start + (posEnd |> int)
                let s = SnapshotSpan(span.Snapshot, start, ende )
                ClassificationSpan(s ,tagDelimiter)
            ]
        | Element.ClosingTag (posStart, posEnd, content) ->
            [
                let start = span.Start + (posStart |> int)
                let ende = span.Start + (posEnd |> int)
                let s = SnapshotSpan(span.Snapshot, start, ende )
                ClassificationSpan(s ,tagDelimiter)
            ]
        | Element.Comment (posStart, posEnd, content) ->
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

        tagDelimiter
        commentDelimiter
    
        string =
        try
            match run htmlElements string with
            | Success (res ,_,_)->
                res 
                |> List.collect (fun e -> 
                    e |> mapElementToClassification snapShot attributeDelimiter litAttributeDelitmiter commentDelimiter tagDelimiter
                )
            
            | Failure (error, perror, pos) ->
                Debug.WriteLine $"Parser-Error: %A{error} - %A{perror} - %A{pos}"
                []
        with
        | ex ->
            Debug.WriteLine $"Parser-Error: %A{ex.Message}"
            []
        
    