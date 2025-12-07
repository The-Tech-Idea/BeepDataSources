using System;
using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.AmazonSQS
{
    /// <summary>
    /// Connection properties specific to Amazon SQS.
    /// </summary>
    public class AmazonSQSConnectionProperties : IConnectionProperties
    {
        #region Core Connection Properties

        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string ConnectionName { get; set; }
        public string ConnectionString { get; set; }
        public string Database { get; set; }
        public string OracleSIDorService { get; set; }
        public DataSourceType DatabaseType { get; set; } = DataSourceType.AmazonSQS;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.MessageQueue;
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
        public bool IsCloud { get; set; } = true;
        public bool IsFavourite { get; set; }
        public bool IsDefault { get; set; }
        public bool IsInMemory { get; set; }

        #endregion

        #region Amazon SQS Specific Properties

        /// <summary>
        /// AWS Access Key ID
        /// </summary>
        public string AccessKey
        {
            get => UserID;
            set => UserID = value;
        }

        /// <summary>
        /// AWS Secret Access Key
        /// </summary>
        public string SecretKey
        {
            get => Password;
            set => Password = value;
        }

        /// <summary>
        /// AWS Region (e.g., us-east-1, eu-west-1)
        /// </summary>
        public string Region { get; set; } = "us-east-1";

        /// <summary>
        /// Queue URL (full URL to the SQS queue)
        /// </summary>
        public string QueueUrl { get; set; }

        /// <summary>
        /// Queue name (used to construct URL if QueueUrl not provided)
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// Whether to use FIFO queue
        /// </summary>
        public bool UseFIFO { get; set; }

        /// <summary>
        /// Visibility timeout in seconds (default: 30)
        /// </summary>
        public int VisibilityTimeout { get; set; } = 30;

        /// <summary>
        /// Message retention period in seconds (default: 345600 = 4 days)
        /// </summary>
        public int MessageRetentionPeriod { get; set; } = 345600;

        /// <summary>
        /// Receive message wait time in seconds (long polling, default: 0 = short polling)
        /// </summary>
        public int ReceiveMessageWaitTimeSeconds { get; set; } = 0;

        /// <summary>
        /// Maximum number of messages to receive per call (1-10)
        /// </summary>
        public int MaxNumberOfMessages { get; set; } = 1;

        /// <summary>
        /// Dead-letter queue URL
        /// </summary>
        public string DeadLetterQueueUrl { get; set; }

        /// <summary>
        /// Maximum receive count before moving to DLQ
        /// </summary>
        public int MaxReceiveCount { get; set; } = 10;

        /// <summary>
        /// Delay seconds for message delivery (0-900)
        /// </summary>
        public int DelaySeconds { get; set; } = 0;

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

