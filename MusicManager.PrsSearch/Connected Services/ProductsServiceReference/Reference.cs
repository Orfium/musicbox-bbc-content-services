﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ProductsServiceReference
{
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.2")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="ProductsServiceReference.GetProductsSoap")]
    public interface GetProductsSoap
    {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/NewProductsSearch", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<ProductsServiceReference.NewProductsSearchResponse> NewProductsSearchAsync(ProductsServiceReference.NewProductsSearchRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/GetCachedPage", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<ProductsServiceReference.GetCachedPageResponse> GetCachedPageAsync(ProductsServiceReference.GetCachedPageRequest request);
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.2")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://tempuri.org/")]
    public partial class AuthenticationToken
    {
        
        private string sessionIdField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public string SessionId
        {
            get
            {
                return this.sessionIdField;
            }
            set
            {
                this.sessionIdField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.2")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://tempuri.org/")]
    public partial class WebServiceProductSummaryBO
    {
        
        private string productIDField;
        
        private string title1Field;
        
        private string title2Field;
        
        private string labelField;
        
        private string artist1Field;
        
        private string artist2Field;
        
        private string recordCompanyField;
        
        private string compilationFlagField;
        
        private string catalogueNumber2Field;
        
        private string catalogueNumber3Field;
        
        private string catalogueNumber4Field;
        
        private string catalogueNumber5Field;
        
        private string catalogueNumber1Field;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public string ProductID
        {
            get
            {
                return this.productIDField;
            }
            set
            {
                this.productIDField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public string Title1
        {
            get
            {
                return this.title1Field;
            }
            set
            {
                this.title1Field = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=2)]
        public string Title2
        {
            get
            {
                return this.title2Field;
            }
            set
            {
                this.title2Field = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=3)]
        public string Label
        {
            get
            {
                return this.labelField;
            }
            set
            {
                this.labelField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=4)]
        public string Artist1
        {
            get
            {
                return this.artist1Field;
            }
            set
            {
                this.artist1Field = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=5)]
        public string Artist2
        {
            get
            {
                return this.artist2Field;
            }
            set
            {
                this.artist2Field = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=6)]
        public string RecordCompany
        {
            get
            {
                return this.recordCompanyField;
            }
            set
            {
                this.recordCompanyField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=7)]
        public string CompilationFlag
        {
            get
            {
                return this.compilationFlagField;
            }
            set
            {
                this.compilationFlagField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=8)]
        public string CatalogueNumber2
        {
            get
            {
                return this.catalogueNumber2Field;
            }
            set
            {
                this.catalogueNumber2Field = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=9)]
        public string CatalogueNumber3
        {
            get
            {
                return this.catalogueNumber3Field;
            }
            set
            {
                this.catalogueNumber3Field = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=10)]
        public string CatalogueNumber4
        {
            get
            {
                return this.catalogueNumber4Field;
            }
            set
            {
                this.catalogueNumber4Field = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=11)]
        public string CatalogueNumber5
        {
            get
            {
                return this.catalogueNumber5Field;
            }
            set
            {
                this.catalogueNumber5Field = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=12)]
        public string CatalogueNumber1
        {
            get
            {
                return this.catalogueNumber1Field;
            }
            set
            {
                this.catalogueNumber1Field = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.2")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://tempuri.org/")]
    public partial class MessageBO
    {
        
        private string callNameField;
        
        private string callDurationField;
        
        private string errorCodeField;
        
        private string errorMessageField;
        
        private string errorSeverityField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public string CallName
        {
            get
            {
                return this.callNameField;
            }
            set
            {
                this.callNameField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public string CallDuration
        {
            get
            {
                return this.callDurationField;
            }
            set
            {
                this.callDurationField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=2)]
        public string ErrorCode
        {
            get
            {
                return this.errorCodeField;
            }
            set
            {
                this.errorCodeField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=3)]
        public string ErrorMessage
        {
            get
            {
                return this.errorMessageField;
            }
            set
            {
                this.errorMessageField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=4)]
        public string ErrorSeverity
        {
            get
            {
                return this.errorSeverityField;
            }
            set
            {
                this.errorSeverityField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.2")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://tempuri.org/")]
    public partial class WebServiceProductSummariesBO
    {
        
        private int totalRecdgsFoundField;
        
        private string searchIdField;
        
        private MessageBO[] messagesField;
        
        private WebServiceProductSummaryBO[] productSummaryListField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public int TotalRecdgsFound
        {
            get
            {
                return this.totalRecdgsFoundField;
            }
            set
            {
                this.totalRecdgsFoundField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public string SearchId
        {
            get
            {
                return this.searchIdField;
            }
            set
            {
                this.searchIdField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order=2)]
        public MessageBO[] Messages
        {
            get
            {
                return this.messagesField;
            }
            set
            {
                this.messagesField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order=3)]
        public WebServiceProductSummaryBO[] ProductSummaryList
        {
            get
            {
                return this.productSummaryListField;
            }
            set
            {
                this.productSummaryListField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.2")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://tempuri.org/")]
    public enum ProductEnquiryType
    {
        
        /// <remarks/>
        Title,
        
        /// <remarks/>
        Artist_Name,
        
        /// <remarks/>
        Title_Artist_Name,
        
        /// <remarks/>
        Catalogue_Number,
        
        /// <remarks/>
        Recording_ID,
        
        /// <remarks/>
        Tunecode,
        
        /// <remarks/>
        Product_ID,
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.2")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="NewProductsSearch", WrapperNamespace="http://tempuri.org/", IsWrapped=true)]
    public partial class NewProductsSearchRequest
    {
        
        [System.ServiceModel.MessageHeaderAttribute(Namespace="http://tempuri.org/")]
        public ProductsServiceReference.AuthenticationToken AuthenticationToken;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=0)]
        public string Title;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=1)]
        public string ArtistName;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=2)]
        public string CatalogueNumber;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=3)]
        public string RecordingID;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=4)]
        public string ProductID;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=5)]
        public string Tunecode;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=6)]
        public string FuzzySearch;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=7)]
        public ProductsServiceReference.ProductEnquiryType ipType;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=8)]
        public int startRecord;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=9)]
        public int pageSize;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=10)]
        public string IncludeCompilations;
        
        public NewProductsSearchRequest()
        {
        }
        
        public NewProductsSearchRequest(ProductsServiceReference.AuthenticationToken AuthenticationToken, string Title, string ArtistName, string CatalogueNumber, string RecordingID, string ProductID, string Tunecode, string FuzzySearch, ProductsServiceReference.ProductEnquiryType ipType, int startRecord, int pageSize, string IncludeCompilations)
        {
            this.AuthenticationToken = AuthenticationToken;
            this.Title = Title;
            this.ArtistName = ArtistName;
            this.CatalogueNumber = CatalogueNumber;
            this.RecordingID = RecordingID;
            this.ProductID = ProductID;
            this.Tunecode = Tunecode;
            this.FuzzySearch = FuzzySearch;
            this.ipType = ipType;
            this.startRecord = startRecord;
            this.pageSize = pageSize;
            this.IncludeCompilations = IncludeCompilations;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.2")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="NewProductsSearchResponse", WrapperNamespace="http://tempuri.org/", IsWrapped=true)]
    public partial class NewProductsSearchResponse
    {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=0)]
        public ProductsServiceReference.WebServiceProductSummariesBO NewProductsSearchResult;
        
        public NewProductsSearchResponse()
        {
        }
        
        public NewProductsSearchResponse(ProductsServiceReference.WebServiceProductSummariesBO NewProductsSearchResult)
        {
            this.NewProductsSearchResult = NewProductsSearchResult;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.2")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="GetCachedPage", WrapperNamespace="http://tempuri.org/", IsWrapped=true)]
    public partial class GetCachedPageRequest
    {
        
        [System.ServiceModel.MessageHeaderAttribute(Namespace="http://tempuri.org/")]
        public ProductsServiceReference.AuthenticationToken AuthenticationToken;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=0)]
        public string searchId;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=1)]
        public int startRecord;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=2)]
        public int PageSize;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=3)]
        public ProductsServiceReference.ProductEnquiryType ipType;
        
        public GetCachedPageRequest()
        {
        }
        
        public GetCachedPageRequest(ProductsServiceReference.AuthenticationToken AuthenticationToken, string searchId, int startRecord, int PageSize, ProductsServiceReference.ProductEnquiryType ipType)
        {
            this.AuthenticationToken = AuthenticationToken;
            this.searchId = searchId;
            this.startRecord = startRecord;
            this.PageSize = PageSize;
            this.ipType = ipType;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.2")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="GetCachedPageResponse", WrapperNamespace="http://tempuri.org/", IsWrapped=true)]
    public partial class GetCachedPageResponse
    {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://tempuri.org/", Order=0)]
        public ProductsServiceReference.WebServiceProductSummariesBO GetCachedPageResult;
        
        public GetCachedPageResponse()
        {
        }
        
        public GetCachedPageResponse(ProductsServiceReference.WebServiceProductSummariesBO GetCachedPageResult)
        {
            this.GetCachedPageResult = GetCachedPageResult;
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.2")]
    public interface GetProductsSoapChannel : ProductsServiceReference.GetProductsSoap, System.ServiceModel.IClientChannel
    {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.2")]
    public partial class GetProductsSoapClient : System.ServiceModel.ClientBase<ProductsServiceReference.GetProductsSoap>, ProductsServiceReference.GetProductsSoap
    {
        
        /// <summary>
        /// Implement this partial method to configure the service endpoint.
        /// </summary>
        /// <param name="serviceEndpoint">The endpoint to configure</param>
        /// <param name="clientCredentials">The client credentials</param>
        static partial void ConfigureEndpoint(System.ServiceModel.Description.ServiceEndpoint serviceEndpoint, System.ServiceModel.Description.ClientCredentials clientCredentials);
        
        public GetProductsSoapClient(EndpointConfiguration endpointConfiguration) : 
                base(GetProductsSoapClient.GetBindingForEndpoint(endpointConfiguration), GetProductsSoapClient.GetEndpointAddress(endpointConfiguration))
        {
            this.Endpoint.Name = endpointConfiguration.ToString();
            ConfigureEndpoint(this.Endpoint, this.ClientCredentials);
        }
        
        public GetProductsSoapClient(EndpointConfiguration endpointConfiguration, string remoteAddress) : 
                base(GetProductsSoapClient.GetBindingForEndpoint(endpointConfiguration), new System.ServiceModel.EndpointAddress(remoteAddress))
        {
            this.Endpoint.Name = endpointConfiguration.ToString();
            ConfigureEndpoint(this.Endpoint, this.ClientCredentials);
        }
        
        public GetProductsSoapClient(EndpointConfiguration endpointConfiguration, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(GetProductsSoapClient.GetBindingForEndpoint(endpointConfiguration), remoteAddress)
        {
            this.Endpoint.Name = endpointConfiguration.ToString();
            ConfigureEndpoint(this.Endpoint, this.ClientCredentials);
        }
        
        public GetProductsSoapClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress)
        {
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<ProductsServiceReference.NewProductsSearchResponse> ProductsServiceReference.GetProductsSoap.NewProductsSearchAsync(ProductsServiceReference.NewProductsSearchRequest request)
        {
            return base.Channel.NewProductsSearchAsync(request);
        }
        
        public System.Threading.Tasks.Task<ProductsServiceReference.NewProductsSearchResponse> NewProductsSearchAsync(ProductsServiceReference.AuthenticationToken AuthenticationToken, string Title, string ArtistName, string CatalogueNumber, string RecordingID, string ProductID, string Tunecode, string FuzzySearch, ProductsServiceReference.ProductEnquiryType ipType, int startRecord, int pageSize, string IncludeCompilations)
        {
            ProductsServiceReference.NewProductsSearchRequest inValue = new ProductsServiceReference.NewProductsSearchRequest();
            inValue.AuthenticationToken = AuthenticationToken;
            inValue.Title = Title;
            inValue.ArtistName = ArtistName;
            inValue.CatalogueNumber = CatalogueNumber;
            inValue.RecordingID = RecordingID;
            inValue.ProductID = ProductID;
            inValue.Tunecode = Tunecode;
            inValue.FuzzySearch = FuzzySearch;
            inValue.ipType = ipType;
            inValue.startRecord = startRecord;
            inValue.pageSize = pageSize;
            inValue.IncludeCompilations = IncludeCompilations;
            return ((ProductsServiceReference.GetProductsSoap)(this)).NewProductsSearchAsync(inValue);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<ProductsServiceReference.GetCachedPageResponse> ProductsServiceReference.GetProductsSoap.GetCachedPageAsync(ProductsServiceReference.GetCachedPageRequest request)
        {
            return base.Channel.GetCachedPageAsync(request);
        }
        
        public System.Threading.Tasks.Task<ProductsServiceReference.GetCachedPageResponse> GetCachedPageAsync(ProductsServiceReference.AuthenticationToken AuthenticationToken, string searchId, int startRecord, int PageSize, ProductsServiceReference.ProductEnquiryType ipType)
        {
            ProductsServiceReference.GetCachedPageRequest inValue = new ProductsServiceReference.GetCachedPageRequest();
            inValue.AuthenticationToken = AuthenticationToken;
            inValue.searchId = searchId;
            inValue.startRecord = startRecord;
            inValue.PageSize = PageSize;
            inValue.ipType = ipType;
            return ((ProductsServiceReference.GetProductsSoap)(this)).GetCachedPageAsync(inValue);
        }
        
        public virtual System.Threading.Tasks.Task OpenAsync()
        {
            return System.Threading.Tasks.Task.Factory.FromAsync(((System.ServiceModel.ICommunicationObject)(this)).BeginOpen(null, null), new System.Action<System.IAsyncResult>(((System.ServiceModel.ICommunicationObject)(this)).EndOpen));
        }
        
        public virtual System.Threading.Tasks.Task CloseAsync()
        {
            return System.Threading.Tasks.Task.Factory.FromAsync(((System.ServiceModel.ICommunicationObject)(this)).BeginClose(null, null), new System.Action<System.IAsyncResult>(((System.ServiceModel.ICommunicationObject)(this)).EndClose));
        }
        
        private static System.ServiceModel.Channels.Binding GetBindingForEndpoint(EndpointConfiguration endpointConfiguration)
        {
            if ((endpointConfiguration == EndpointConfiguration.GetProductsSoap))
            {
                System.ServiceModel.BasicHttpBinding result = new System.ServiceModel.BasicHttpBinding();
                result.MaxBufferSize = int.MaxValue;
                result.ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max;
                result.MaxReceivedMessageSize = int.MaxValue;
                result.AllowCookies = true;
                result.Security.Mode = System.ServiceModel.BasicHttpSecurityMode.Transport;
                return result;
            }
            if ((endpointConfiguration == EndpointConfiguration.GetProductsSoap12))
            {
                System.ServiceModel.Channels.CustomBinding result = new System.ServiceModel.Channels.CustomBinding();
                System.ServiceModel.Channels.TextMessageEncodingBindingElement textBindingElement = new System.ServiceModel.Channels.TextMessageEncodingBindingElement();
                textBindingElement.MessageVersion = System.ServiceModel.Channels.MessageVersion.CreateVersion(System.ServiceModel.EnvelopeVersion.Soap12, System.ServiceModel.Channels.AddressingVersion.None);
                result.Elements.Add(textBindingElement);
                System.ServiceModel.Channels.HttpsTransportBindingElement httpsBindingElement = new System.ServiceModel.Channels.HttpsTransportBindingElement();
                httpsBindingElement.AllowCookies = true;
                httpsBindingElement.MaxBufferSize = int.MaxValue;
                httpsBindingElement.MaxReceivedMessageSize = int.MaxValue;
                result.Elements.Add(httpsBindingElement);
                return result;
            }
            throw new System.InvalidOperationException(string.Format("Could not find endpoint with name \'{0}\'.", endpointConfiguration));
        }
        
        private static System.ServiceModel.EndpointAddress GetEndpointAddress(EndpointConfiguration endpointConfiguration)
        {
            if ((endpointConfiguration == EndpointConfiguration.GetProductsSoap))
            {
                return new System.ServiceModel.EndpointAddress("https://services.prsformusic.com/coredataws/getproducts.asmx");
            }
            if ((endpointConfiguration == EndpointConfiguration.GetProductsSoap12))
            {
                return new System.ServiceModel.EndpointAddress("https://services.prsformusic.com/coredataws/getproducts.asmx");
            }
            throw new System.InvalidOperationException(string.Format("Could not find endpoint with name \'{0}\'.", endpointConfiguration));
        }
        
        public enum EndpointConfiguration
        {
            
            GetProductsSoap,
            
            GetProductsSoap12,
        }
    }
}
