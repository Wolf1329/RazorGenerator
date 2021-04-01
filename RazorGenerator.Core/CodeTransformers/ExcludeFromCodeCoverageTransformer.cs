﻿using System.CodeDom;
using System.Diagnostics.CodeAnalysis;

namespace RazorGenerator.Core.CodeTransformers
{
    public class ExcludeFromCodeCoverageTransformer : RazorCodeTransformerBase
    {
        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            CodeTypeReference codeTypeReference = new CodeTypeReference(typeof(ExcludeFromCodeCoverageAttribute));
            generatedClass.CustomAttributes.Add(new CodeAttributeDeclaration(codeTypeReference));
        }
    }
}