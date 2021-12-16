using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace HtmlForFSharp
{
    [Export(typeof(IClassifierProvider))]
    [ContentType("F#")] 
    internal class HtmlClassifierProvider : IClassifierProvider
    {
        [Import]
        internal IClassificationTypeRegistryService ClassificationTypeRegistry = null;

        [Import]
        internal IClassifierAggregatorService ClassifierAggregator = null;

        [Import]
        internal IContentTypeRegistryService ContentTypeRegistryService { get; set; }

        public HtmlClassifierProvider()
        {

        }

        private static bool createdClassifier = false;

        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            if (createdClassifier)
            {
                return null;
            }

            try
            {
                createdClassifier = true;

                return buffer.Properties.GetOrCreateSingletonProperty<HtmlClassifier>(delegate
                {
                    return new HtmlClassifier(ClassificationTypeRegistry, ClassifierAggregator.GetClassifier(buffer));
                });
            }
            finally
            {
                createdClassifier = false;
            }
            
        }
    }
}
