using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Razor;
using System.Web.Razor.Generator;
using System.Web.Razor.Parser;
using System.Web.Razor.Parser.SyntaxTree;
using System.Web.WebPages;

namespace RazorGenerator.Core
{
    public class RazorHost : RazorEngineHost, IRazorHost, ICodeGenerationEventProvider
    {
        private static readonly IEnumerable<string> _defaultImports = new[] {
            "System",
            "System.Collections.Generic",
            "System.IO",
            "System.Linq",
            "System.Net",
            "System.Text",
            "System.Web",
            "System.Web",
            "System.Web.Security",
            "System.Web.UI",
            "System.Web.WebPages",
            "System.Web.Helpers",
        };

        private readonly IRazorCodeTransformer      _codeTransformer;
        private readonly string                     _baseRelativePath;
        private readonly FileInfo                   _fullPath;
        private readonly CodeDomProvider            _codeDomProvider;
        private readonly IDictionary<string,string> _directives;

        private string           _defaultClassName;
        private CodeLanguageUtil _languageUtil;

        public RazorHost(string baseRelativePath, FileInfo fullPath, IRazorCodeTransformer codeTransformer, CodeDomProvider codeDomProvider, IDictionary<string, string> directives)
            : base(RazorCodeLanguage.GetLanguageByExtension(fullPath.Extension))
        {
            if (codeTransformer  == null) throw new ArgumentNullException("codeTransformer");
            if (baseRelativePath == null) throw new ArgumentNullException("baseRelativePath");
            if (fullPath         == null) throw new ArgumentNullException("fullPath");
            if (codeDomProvider  == null) throw new ArgumentNullException("codeDomProvider");

            this._codeTransformer  = codeTransformer;
            this._baseRelativePath = baseRelativePath;
            this._fullPath         = fullPath;
            this._codeDomProvider  = codeDomProvider;
            this._directives       = directives;
            this._languageUtil     = Core.CodeLanguageUtil.GetLanguageUtilFromFileName(fullPath);
            base.DefaultNamespace  = "ASP";
            this.EnableLinePragmas = true;

            base.GeneratedClassContext = new GeneratedClassContext(
                executeMethodName       : GeneratedClassContext.DefaultExecuteMethodName,
                writeMethodName         : GeneratedClassContext.DefaultWriteMethodName,
                writeLiteralMethodName  : GeneratedClassContext.DefaultWriteLiteralMethodName,
                writeToMethodName       : "WriteTo",
                writeLiteralToMethodName: "WriteLiteralTo",
                templateTypeName        : typeof(HelperResult).FullName,
                defineSectionMethodName : "DefineSection"
            );

            base.DefaultBaseClass = typeof(WebPage).FullName;
            foreach (string import in _defaultImports)
            {
                _ = base.NamespaceImports.Add(import);
            }
        }

        public string ProjectRelativePath
        {
            get { return this._baseRelativePath; }
        }

        public string FullPath
        {
            get { return this._fullPath.FullName; }
        }

        public event EventHandler<GeneratorErrorEventArgs> Error;

        public event EventHandler<ProgressEventArgs> Progress;

        public override string DefaultClassName
        {
            get
            {
                return this._defaultClassName ?? this.GetClassName();
            }
            set
            {
                if (!String.Equals(value, "__CompiledTemplate", StringComparison.OrdinalIgnoreCase))
                {
                    //  By default RazorEngineHost assigns the name __CompiledTemplate. We'll ignore this assignment
                    this._defaultClassName = value;
                }
            }
        }

        public CodeLanguageUtil CodeLanguageUtil
        {
            get{
                return this._languageUtil;
            }
        }

        public ParserBase Parser { get; set; }

        public RazorCodeGenerator CodeGenerator { get; set; }

        public bool EnableLinePragmas { get; set; }

        public string GenerateCode()
        {
            this._codeTransformer.Initialize(this, this._directives);

            // Create the engine
            RazorTemplateEngine engine = new RazorTemplateEngine(this);

            // Generate code 
            GeneratorResults results = null;
            try
            {
                using (FileStream stream = File.OpenRead(this._fullPath.FullName))
                using (StreamReader reader = new StreamReader(stream, Encoding.Default, detectEncodingFromByteOrderMarks: true))
                {
                    results = engine.GenerateCode(reader, className: this.DefaultClassName, rootNamespace: this.DefaultNamespace, sourceFileName: this._fullPath.FullName);
                }
            }
            catch (Exception e)
            {
                this.OnGenerateError(4, e.ToString(), 1, 1);
                //Returning null signifies that generation has failed
                return null;
            }

            // Output errors
            foreach (RazorError error in results.ParserErrors)
            {
                this.OnGenerateError(4, error.Message, (uint)error.Location.LineIndex + 1, (uint)error.Location.CharacterIndex + 1);
            }

            try
            {
                this.OnCodeCompletion(50, 100);

                using (StringWriter writer = new StringWriter())
                {
                    CodeGeneratorOptions options = new CodeGeneratorOptions();
                    options.BlankLinesBetweenMembers = false;
                    options.BracingStyle = "C";

                    //Generate the code
                    writer.WriteLine(this.CodeLanguageUtil.GetPreGeneratedCodeBlock());
                    this._codeDomProvider.GenerateCodeFromCompileUnit(results.GeneratedCode, writer, options);
                    writer.WriteLine(this.CodeLanguageUtil.GetPostGeneratedCodeBlock());

                    this.OnCodeCompletion(100, 100);
                    writer.Flush();

                    // Perform output transformations and return
                    string codeContent = writer.ToString();
                    codeContent = this._codeTransformer.ProcessOutput(codeContent);
                    return codeContent;
                }
            }
            catch (Exception e)
            {
                this.OnGenerateError(4, e.ToString(), 1, 1);
                //Returning null signifies that generation has failed
                return null;
            }
        }

        public override void PostProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            this._codeTransformer.ProcessGeneratedCode(codeCompileUnit, generatedNamespace, generatedClass, executeMethod);
        }

        public override RazorCodeGenerator DecorateCodeGenerator(RazorCodeGenerator incomingCodeGenerator)
        {
            RazorCodeGenerator codeGenerator = this.CodeGenerator ?? base.DecorateCodeGenerator(incomingCodeGenerator);
            codeGenerator.GenerateLinePragmas = this.EnableLinePragmas;
            return codeGenerator;
        }

        public override ParserBase DecorateCodeParser(ParserBase incomingCodeParser)
        {
            return this.Parser ?? base.DecorateCodeParser(incomingCodeParser);
        }

        private void OnGenerateError(uint errorCode, string errorMessage, uint lineNumber, uint columnNumber)
        {
            if (Error != null)
            {
                Error(this, new GeneratorErrorEventArgs(errorCode, errorMessage, lineNumber, columnNumber));
            }
        }

        private void OnCodeCompletion(uint completed, uint total)
        {
            if (Progress != null)
            {
                Progress(this, new ProgressEventArgs(completed, total));
            }
        }

        protected virtual string GetClassName()
        {
            return ParserHelpers.SanitizeClassName(this._baseRelativePath);
        }
    }
}
