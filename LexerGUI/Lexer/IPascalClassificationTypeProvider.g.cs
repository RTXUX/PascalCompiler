namespace PascalCompiler.Lexical.Lexer {
    using ActiproSoftware.Text;
    
    
    /// <summary>
    /// Provides the base requirements of an object that provides <see cref="IClassificationType"/> objects for the <c>Pascal</c> language.
    /// </summary>
    /// <remarks>
    /// This type was generated by the Actipro Language Designer tool v19.1.685.0 (http://www.actiprosoftware.com).
    /// Generated code is based on input created by Actipro Software LLC.
    /// Copyright (c) 2001-2020 Actipro Software LLC.  All rights reserved.
    /// </remarks>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("LanguageDesigner", "19.1.685.0")]
    public interface IPascalClassificationTypeProvider {
        
        /// <summary>
        /// Gets the <c>Comment</c> classification type.
        /// </summary>
        /// <value>The <c>Comment</c> classification type.</value>
        IClassificationType Comment {
            get;
        }
        
        /// <summary>
        /// Gets the <c>Identifier</c> classification type.
        /// </summary>
        /// <value>The <c>Identifier</c> classification type.</value>
        IClassificationType Identifier {
            get;
        }
        
        /// <summary>
        /// Gets the <c>Keyword</c> classification type.
        /// </summary>
        /// <value>The <c>Keyword</c> classification type.</value>
        IClassificationType Keyword {
            get;
        }
        
        /// <summary>
        /// Gets the <c>Number</c> classification type.
        /// </summary>
        /// <value>The <c>Number</c> classification type.</value>
        IClassificationType Number {
            get;
        }
        
        /// <summary>
        /// Gets the <c>Operator</c> classification type.
        /// </summary>
        /// <value>The <c>Operator</c> classification type.</value>
        IClassificationType Operator {
            get;
        }
        
        /// <summary>
        /// Gets the <c>String</c> classification type.
        /// </summary>
        /// <value>The <c>String</c> classification type.</value>
        IClassificationType String {
            get;
        }
    }
}
