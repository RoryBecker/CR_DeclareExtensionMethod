using System.ComponentModel.Composition;
using DevExpress.CodeRush.Common;

namespace CR_DeclareExtensionMethod
{
    [Export(typeof(IVsixPluginExtension))]
    public class CR_DeclareExtensionMethodExtension : IVsixPluginExtension { }
}