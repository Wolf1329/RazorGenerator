using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

using RazorGenerator.Core.CodeTransformers;

namespace RazorGenerator.Core
{
    [Export("Template", typeof(IOutputRazorCodeTransformer))]
    public class TemplateCodeTransformer : AggregateCodeTransformer, IOutputRazorCodeTransformer
    {
        private const string GenerationEnvironmentPropertyName = "GenerationEnvironment";

        private static readonly IEnumerable<string> _defaultImports = new[] {
            "System",
            "System.Collections.Generic",
            "System.Linq",
            "System.Text"
        };

        private readonly RazorCodeTransformerBase[] _codeTransforms = new RazorCodeTransformerBase[] {
            new SetImports(_defaultImports, replaceExisting: true),
            new AddGeneratedClassAttribute(),
            new DirectivesBasedTransformers(),
            new SetBaseType("RazorGenerator.Templating.RazorTemplateBase"),
        };

        protected override IEnumerable<RazorCodeTransformerBase> CodeTransformers
        {
            get { return this._codeTransforms; }
        }

        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            base.ProcessGeneratedCode(codeCompileUnit, generatedNamespace, generatedClass, executeMethod);
            generatedClass.IsPartial = true;
            // The generated class has a constructor in there by default.
            generatedClass.Members.Remove(generatedClass.Members.OfType<CodeConstructor>().SingleOrDefault());
        }
    }
}