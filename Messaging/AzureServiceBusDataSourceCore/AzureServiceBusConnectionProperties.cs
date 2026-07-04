using System;
using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.AzureServiceBus
{
    /// <summary>
    /// Connection properties specific to Azure Service Bus — full rewrite against BeepDM 3.1.0
    /// (Phase 10 Messaging folder refresh). Carries Azure connection string + entity names; implements
    /// the current <see cref="IConnectionProperties"/> interface (with all new members required by
    /// BeepDM 3.1.0, including typed collections and the <c>AuthTypeEnum</c>).
    /// </summary>
    public class AzureServiceBusConnectionProperties : IConnectionProperties
    {
        #region Core Connection Properties
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string ConnectionName { get; set; }
        public string ConnectionString { get; set; }
        public string Database { get; set; }
        public string OracleSIDorService { get; set; }
        public DatasourceCategory Category { get; set; } = DatasourceCategory.MessageQueue;
        public DataSourceType DatabaseType { get; set; } = DataSourceType.AzureServiceBus;
        public string DriverName { get; set; }
        public string DriverVersion { get; set; }
        public string Host { get; set; }
        public string Parameters { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
        public string SchemaName { get; set; }
        public string UserID { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string Ext { get; set; }
        public bool Drawn { get; set; }
        public string CertificatePath { get; set; }
        public string Url { get; set; }
        public string KeyToken { get; set; }
        public string ApiKey { get; set; }
        public List<string> Databases { get; set; } = new();
        public List<EntityStructure> Entities { get; set; } = new();
        public char Delimiter { get; set; }
        public bool Favourite { get; set; }
        public bool IsLocal { get; set; }
        public bool IsRemote { get; set; } = true;
        public bool IsWebApi { get; set; }
        public bool IsFile { get; set; }
        public bool IsDatabase { get; set; }
        public bool IsComposite { get; set; }
        public bool IsCloud { get; set; } = true;
        public bool IsFavourite { get; set; }
        public bool IsDefault { get; set; }
        public bool IsInMemory { get; set; }
        #endregion

        #region Azure Service Bus Specific Properties
        /// <summary>Connection string (Endpoint=sb://...;SharedAccessKeyName=...;SharedAccessKey=...).</summary>
        public string ServiceBusConnectionString { get; set; }
        /// <summary>Fully qualified namespace (e.g. "mybus.servicebus.windows.net").</summary>
        public string FullyQualifiedNamespace { get; set; }
        public string NamespaceName { get; set; }
        public string QueueName { get; set; }
        public string TopicName { get; set; }
        public string SubscriptionName { get; set; }
        public string EntityPath { get; set; }
        public bool UseManagedIdentity { get; set; } = false;
        public string TenantId { get; set; }
        public int MessageTimeToLive { get; set; } = 14;
        public int LockDuration { get; set; } = 60;
        public int MaxDeliveryCount { get; set; } = 10;
        public bool EnableSessions { get; set; } = true;
        public bool EnablePartitioning { get; set; } = false;
        #endregion

        #region IConnectionProperties Required (Not Used by Service Bus)
        public bool IntegratedSecurity { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool PersistSecurityInfo { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool TrustedConnection { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool EncryptConnection { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool MultiSubnetFailover { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool TrustServerCertificate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool AllowPublicKeyRetrieval { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool UseSSL { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool RequireSSL { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool BypassServerCertificateValidation { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool UseWindowsAuthentication { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool UseOAuth { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool UseApiKey { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool UseCertificate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool UseUserAndPassword { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool SavePassword { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool ReadOnly { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool AllowLoadLocalInfile { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string SSLMode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int SSLTimeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string AuthenticationType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Authority { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ApplicationId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string RedirectUriAuth { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Resource { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Audience { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        // Added for BeepDM 3.1.0 IConnectionProperties.
        public bool ValidateServerCertificate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ClientCertificatePath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ClientCertificatePassword { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool RequiresAuthentication { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool RequiresTokenRefresh { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        // Typed members (interface expects specific types, not string).
        public string AdditionalAuthInfo { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ApiKeyHeader { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string AuthCode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public AuthTypeEnum AuthType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string AuthUrl { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<WebApiParameter> BodyParameters { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool BypassProxyOnLocal { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ClientCertificateStoreLocation { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ClientCertificateStoreName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ClientCertificateSubjectName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ClientCertificateThumbprint { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ClientId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ClientSecret { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<DefaultValue> DatasourceDefaults { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Domain { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<WebApiFileParameter> FileParameters { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<WebApiParameter> FormParameters { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string GrantType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<WebApiHeader> Headers { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IgnoreSSLErrors { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string KerberosConfigPath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string KerberosKdc { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string KerberosRealm { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string KerberosServiceName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int MaxRetries { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string OAuthAccessToken { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string OAuthClientSecret { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string OAuthCodeChallenge { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string OAuthCodeChallengeMethod { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string OAuthCodeVerifier { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string OAuthGrantType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string OAuthRefreshToken { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string OAuthScope { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string OAuthState { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string OAuthTokenEndpoint { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Dictionary<string, string> ParameterList { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ProxyPassword { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int ProxyPort { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ProxyUrl { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ProxyUser { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<WebApiParameter> QueryParameters { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string RedirectUri { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<string> Regions { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int RetryIntervalMs { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Scope { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int TimeoutMs { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string TokenUrl { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool UseDefaultProxyCredentials { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool UseProxy { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string WorkstationID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        #endregion
    }
}
