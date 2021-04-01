using System.Collections.Generic;

namespace RazorGenerator.Core.CodeTransformers
{
    public class SetTypeNamespace : RazorCodeTransformerBase
    {
        private readonly string _namespace;

        public SetTypeNamespace(string @namespace)
        {
            this._namespace = @namespace;
        }

        public override void Initialize(IRazorHost razorHost, IDictionary<string, string> directives)
        {
            razorHost.DefaultNamespace = this._namespace;
        }
    }
}