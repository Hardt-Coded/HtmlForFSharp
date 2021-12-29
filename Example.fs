open System.Xml.Serialization


[<CompiledName("Test1234")>]
module Test =

    let html str = str


    let normalString = "<div attr="test"><p style='css'>I am inner Text</p></div>"

    let htmlStr1 = html "<div attr=\"test\"><p style='css'>I am inner Text</p></div>" 

    let htmlStr2 = html @"<div attr=""test""><p style='css'>I am inner Text</p></div>" 

    let htmlStr3 = html $"<div attr="test"><p style='css'>I am inner Text</p></div>"

    let htmlStr4 = html $"<div attr='test'><p style='css'>I am inner Text{id htmlStr1}</p></div>"

    let htmlStr5 = html $"""
        <div attr='test'>
            <p style='css'>
                I am inner Text{id htmlStr1}
            </p>
        </div>"""

    
    let state = "state"

    let dispatch = id


    let normalString2 = $""" 
        I am a normal string
        {id htmlStr2}
        <div> and so on </div>
    """


    let htmlStr11 = html$"""<div attr="muh">{id state}</div>"""
    let htmlStr12 = html"""<div attr="muh">egal</div>"""

    type Msg =
    | AweSomeMessage
    | OtherAwesomeMessage

    let htmlStr6 = html $""" 
        <div attr='{id htmlStr1}'>
            <p style='{id htmlStr1}' other-attribute="blabla"> 
                I am inner Text 
                <input type="text" .value={state} @input={fun _ -> dispatch AweSomeMessage}>
                <input type="text" .value={state} @input={fun _ -> id "toll" }>
                {
                    [
                        "Hallo"
                        html $"<div id='muh' .value={state}>Hallo</div>"
                    
                    ]
                }
         </p>
    </div>"""


    let htmlStr7 = html $""" <div attr='{id htmlStr1}'>I am inner Text</div> """


    let htmlStr7a = 
        html 
            $" <div attr='{id htmlStr1}'>I am inner Text</div> "

    let htmlStr7b = 
        html 
            @" <div attr='something'>I am inner Text</div> "  

    let htmlStr7c = 
        html 
            " <div attr='bla'>I am inner Text</div> "


    let htmlStr8 = 
        html 
            $""" 
            <div line-feed='true' attr='{id htmlStr1}'>
                <p style='{id htmlStr1}' other-attribute="blabla">
                    I am inner Text 
                </p>
            </div>
            
            <div id='1234'></div>
            """


    