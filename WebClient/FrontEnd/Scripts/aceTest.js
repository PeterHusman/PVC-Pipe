var editor;

window.onload = () => {
    editor = ace.edit("editorDiv");
    editor.setTheme("ace/theme/monokai");
    editor.session.setMode("ace/mode/javascript");
    editorDiv.classList.add("editorStyle");
};

function setTextSample(lang) {
    let csharpData = `using System;

namespace Test
{
    public class TestClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
        }
    }
}
`;

    let sqlData = `SELECT  *
FROM    People
WHERE   PersonID = 3
`;

    // Disclaimer: eval is evil!
    editor.setValue(eval(lang + "Data")); 
    editor.session.setMode(`ace/mode/${lang}`);
}