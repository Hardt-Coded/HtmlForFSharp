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


    let htmlStr6 = html $""" 
        <div attr='{id htmlStr1}'>
            <p style='{id htmlStr1}' other-attribute="blabla"> 
                I am inner Text 
         </p>
    </div>"""


    let htmlStr7 = html $""" <div attr='{id htmlStr1}'>I am inner Text</div> """


    let htmlStr8 = html $""" 
    <div attr='{id htmlStr1}'>
        <p style='{id htmlStr1}' other-attribute="blabla">
            I am inner Text 
     </p>
    </div>"""


    