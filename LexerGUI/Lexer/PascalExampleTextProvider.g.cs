namespace PascalCompiler.Lexical.Lexer {
    using ActiproSoftware.Text;
    using System;
    
    
    /// <summary>
    /// Represents a example text provider for the <c>Pascal</c> language.
    /// </summary>
    /// <remarks>
    /// This type was generated by the Actipro Language Designer tool v19.1.685.0 (http://www.actiprosoftware.com).
    /// Generated code is based on input created by Actipro Software LLC.
    /// Copyright (c) 2001-2020 Actipro Software LLC.  All rights reserved.
    /// </remarks>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("LanguageDesigner", "19.1.685.0")]
    public partial class PascalExampleTextProvider : Object, IExampleTextProvider {
        
        /// <summary>
        /// Gets the example text, a code snippet of the related language.
        /// </summary>
        /// <value>The example text, a code snippet of the related language.</value>
        public String ExampleText {
            get {
                return @"{ Sample Pascal Program }
program sample(output);
	var i : integer;

	procedure print(var j: integer);

		function next(k: integer): integer;
		begin
			next := k + 1
		end;

	begin
		writeln('The total is: ', j);
		j := next(j)
	end;

begin
	i := 1;
	while i <= 10 do print(i)
end.
";
            }
        }
    }
}
