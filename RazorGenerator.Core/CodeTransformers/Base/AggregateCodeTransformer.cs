using System.CodeDom;
using System.Collections.Generic;

namespace RazorGenerator.Core.CodeTransformers
{
    /// <summary>Applies multiple <see cref="RazorCodeTransformerBase"/> transformers as a single transformation.</summary>
    public abstract class AggregateCodeTransformer : RazorCodeTransformerBase
    {
        protected abstract IEnumerable<RazorCodeTransformerBase> CodeTransformers
        {
            get;
        }

        public override void Initialize(IRazorHost razorHost, IDictionary<string, string> directives)
        {
            base.Initialize(razorHost, directives);

            foreach (RazorCodeTransformerBase transformer in this.CodeTransformers)
            {
                transformer.Initialize(razorHost, directives);
            }
        }

        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            foreach (RazorCodeTransformerBase transformer in this.CodeTransformers)
            {
                transformer.ProcessGeneratedCode(codeCompileUnit, generatedNamespace, generatedClass, executeMethod);
            }
        }

        public override string ProcessOutput(string codeContent)
        {
            foreach (RazorCodeTransformerBase transformer in this.CodeTransformers)
            {
                codeContent = transformer.ProcessOutput(codeContent);
            }

            return codeContent;
        }
    }
}