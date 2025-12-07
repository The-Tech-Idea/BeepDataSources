using System;
using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.RedisStreams
{
    public class RedisStreamsConnectionProperties : IConnectionProperties
    {
        #region Core Connection Properties

        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string ConnectionName { get; set; }
        public string ConnectionString { get; set; }
        public string Database { get; set; }
        public string OracleSIDorService { get; set; }
        public DataSourceType DatabaseType { get; set; } = DataSourceType.Redis;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.MessageQueue;
        public string DriverName { get; set; }
        public string DriverVersion { get; set; }
        public string Host { get; set; }
        public string Parameters { get; set; }
        public string Password { get; set; }
        public int Port { get; set; } = 6379;
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
        public List<string> Databases { get; set; } = new List<string>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public char Delimiter { get; set; }
        public bool Favourite { get; set; }
        public bool IsLocal { get; set; }
        public bool IsRemote { get; set; } = true;
        public bool IsWebApi { get; set; }
        public bool IsFile { get; set; }
        public bool IsDatabase { get; set; }
        public bool IsComposite { get; set; }
        public bool IsCloud { get; set; }
        public bool IsFavourite { get; set; }
        public bool IsDefault { get; set; }
        public bool IsInMemory { get; set; } = true;

        #endregion

        #region Redis Streams Specific Properties

        /// <summary>
        /// Redis connection string (e.g., localhost:6379)
        /// </summary>
        public string RedisConnectionString
        {
            get => ConnectionString ?? (string.IsNullOrEmpty(Host) ? "localhost:6379" : $"{Host}:{Port}");
            set => ConnectionString = value;
        }

        /// <summary>
        /// Stream name
        /// </summary>
        public string StreamName { get; set; }

        /// <summary>
        /// Consumer group name
        /// </summary>
        public string ConsumerGroup { get; set; }

        /// <summary>
        /// Consumer name
        /// </summary>
        public string ConsumerName { get; set; }

        /// <summary>
        /// Maximum length of the stream (0 = unlimited)
        /// </summary>
        public int MaxLength { get; set; } = 0;

        /// <summary>
        /// Whether to use approximate trimming
        /// </summary>
        public bool ApproximateMaxLength { get; set; } = false;

        /// <summary>
        /// Block time in milliseconds for XREADGROUP (0 = no blocking)
        /// </summary>
        public int BlockTimeMs { get; set; } = 0;

        /// <summary>
        /// Number of messages to read per call
        /// </summary>
        public int Count { get; set; } = 1;

        #endregion

        #region IConnectionProperties Required Properties (Not Used)

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
        public string TenantId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ApplicationId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string RedirectUriAuth { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Resource { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Audience { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        #endregion
    }
}

