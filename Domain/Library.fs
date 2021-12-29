module Domain
    

type State =
    | Default
    | OpenAngleBracket
    | ElementName
    | InsideAttributeList
    | AttributeName
    | AfterAttributeName
    | AfterAttributeEqualSign
    | AfterOpenDoubleQuote
    | AfterOpenSingleQuote
    | AttributeValue
    | InsideElement
    | CloseAngleBracket
    | AfterOpenTagSlash
    | CloseTagSlash
    | LitAttributeName
    | LitAttributeValue

open System

let (|OtherChar|_|) (c:char) =
    match c with
    | '\''
    | '"'
    | ' '
    | '.'
    | '<'
    | '>'
    | '/'
    | '='
    | '}'
    | '{'
    | c when Char.IsControl(c) ->
        None
    | Some ()
    


let parse chars state =
    let check idx c s : struct (State * int)=
        match s with
        | Default                 ->
            match c with
            | '<'   -> (OpenAngleBracket, idx)
            | _     -> (Default, idx)
        | OpenAngleBracket        ->
            match c with
            | OtherChar -> (ElementName, idx)
            | '/'       -> ()
            | '>'       -> (Default, idx)
        | ElementName             ->
        | InsideAttributeList     ->
        | AttributeName           ->
        | AfterAttributeName      ->
        | AfterAttributeEqualSign ->
        | AfterOpenDoubleQuote    ->
        | AfterOpenSingleQuote    ->
        | AttributeValue          ->
        | InsideElement           ->
        | CloseAngleBracket       ->
        | AfterOpenTagSlash       ->
        | CloseTagSlash           ->
        | LitAttributeName        ->
        | LitAttributeValue       ->
        

