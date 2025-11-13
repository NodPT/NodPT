using System;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using DevExpress.Xpo;
using Microsoft.AspNetCore.Mvc.ModelBinding;

public class XpoMetadataProvider : DefaultModelMetadataProvider
{
    public XpoMetadataProvider(ICompositeMetadataDetailsProvider detailsProvider) : base(detailsProvider)
    {

    }
    public XpoMetadataProvider(ICompositeMetadataDetailsProvider detailsProvider, IOptions<MvcOptions> optionsAccessor) : base(detailsProvider, optionsAccessor)
    {

    }
    protected override DefaultMetadataDetails[] CreatePropertyDetails(ModelMetadataIdentity key)
    {
        // Return base details for now. Filtering service fields caused compatibility issues
        // with newer MVC ModelMetadataIdentity shape; keep default behavior.
        DefaultMetadataDetails[] result = base.CreatePropertyDetails(key);
        return result;
    }
    // Note: intentionally not filtering property details to avoid relying on ModelMetadataIdentity internals.
}