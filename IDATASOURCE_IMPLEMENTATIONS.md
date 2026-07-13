# IDataSource Implementations in BeepDataSources

Inventory of every project folder under `BeepDataSources/`. For each folder
containing a `.csproj`, the `*DataSource.cs` file inside it is read to capture
the implementation class name and namespace. The `Create*Config` column shows
whether `ConnectionHelper` already has a matching driver config (matched by
`classHandler` = class name).

- **Project folders:** 164
- **Top-level categories:** 8
- **ConnectionHelper Create\*Config entries:** 214
- **Folders with a matching Create\*Config:** 73
- **Folders with NO Create\*Config:** 91

## Summary by category

| Category | Projects | With Create\*Config | Without |
|---|---:|---:|---:|
| Connectors | 94 | 43 | 51 |
| DataSourcesPlugins | 1 | 0 | 1 |
| DataSourcesPluginsCore | 53 | 23 | 30 |
| InMemoryDB | 1 | 1 | 0 |
| Messaging | 8 | 4 | 4 |
| VectorDatabase | 5 | 2 | 3 |
| tempReflection | 1 | 0 | 1 |
| tests | 1 | 0 | 1 |

## Connectors

| Class | Namespace | Create\*Config | Project folder | .csproj | File |
|---|---|---|---|---|---|
| `FreshBooksDataSource` | `TheTechIdea.Beep.Connectors.FreshBooks` | `(none)` | `Connectors\Accounting\FreshBooks` | `FreshBooks.csproj` | `Connectors\Accounting\FreshBooks\FreshBooksDataSource.cs` |
| `MYOBDataSource` | `TheTechIdea.Beep.Connectors.MYOB` | `(none)` | `Connectors\Accounting\MYOB` | `MYOB.csproj` | `Connectors\Accounting\MYOB\MYOBDataSource.cs` |
| `QuickBooksOnlineDataSource` | `TheTechIdea.Beep.Connectors.QuickBooksOnline` | `(none)` | `Connectors\Accounting\QuickBooksOnline` | `QuickBooksOnline.csproj` | `Connectors\Accounting\QuickBooksOnline\QuickBooksOnlineDataSource.cs` |
| `SageIntacctDataSource` | `TheTechIdea.Beep.Connectors.SageIntacct` | `(none)` | `Connectors\Accounting\SageIntacct` | `SageIntacct.csproj` | `Connectors\Accounting\SageIntacct\SageIntacctDataSource.cs` |
| `WaveDataSource` | `TheTechIdea.Beep.Connectors.Wave` | `(none)` | `Connectors\Accounting\Wave` | `Wave.csproj` | `Connectors\Accounting\Wave\WaveDataSource.cs` |
| `XeroDataSource` | `TheTechIdea.Beep.Connectors.Xero` | `(none)` | `Connectors\Accounting\Xero` | `Xero.csproj` | `Connectors\Accounting\Xero\XeroDataSource.cs` |
| `ZohoBooksDataSource` | `TheTechIdea.Beep.Connectors.ZohoBooks` | `(none)` | `Connectors\Accounting\ZohoBooks` | `ZohoBooks.csproj` | `Connectors\Accounting\ZohoBooks\ZohoBooksDataSource.cs` |
| `TableauDataSource` | `TheTechIdea.Beep.Connectors.Tableau` | `(none)` | `Connectors\BusinessIntelligence\TableauDataSource` | `TableauDataSource.csproj` | `Connectors\BusinessIntelligence\TableauDataSource\TableauDataSource.cs` |
| `AmazonS3DataSource` | `TheTechIdea.Beep.Connectors.AmazonS3` | `AmazonS3` | `Connectors\Cloud-Storage\AmazonS3` | `AmazonS3.csproj` | `Connectors\Cloud-Storage\AmazonS3\AmazonS3DataSource.cs` |
| `BoxDataSource` | `TheTechIdea.Beep.Connectors.Box` | `(none)` | `Connectors\Cloud-Storage\Box` | `Box.csproj` | `Connectors\Cloud-Storage\Box\BoxDataSource.cs` |
| `CitrixShareFileDataSource` | `TheTechIdea.Beep.Connectors.CitrixShareFile` | `(none)` | `Connectors\Cloud-Storage\CitrixShareFile` | `CitrixShareFile.csproj` | `Connectors\Cloud-Storage\CitrixShareFile\CitrixShareFileDataSource.cs` |
| `DropboxDataSource` | `TheTechIdea.Beep.Connectors.Dropbox` | `(none)` | `Connectors\Cloud-Storage\Dropbox` | `Dropbox.csproj` | `Connectors\Cloud-Storage\Dropbox\DropboxDataSource.cs` |
| `EgnyteDataSource` | `TheTechIdea.Beep.Connectors.Egnyte` | `(none)` | `Connectors\Cloud-Storage\Egnyte` | `Egnyte.csproj` | `Connectors\Cloud-Storage\Egnyte\EgnyteDataSource.cs` |
| `GoogleDriveDataSource` | `TheTechIdea.Beep.Connectors.GoogleDrive` | `(none)` | `Connectors\Cloud-Storage\GoogleDrive` | `GoogleDrive.csproj` | `Connectors\Cloud-Storage\GoogleDrive\GoogleDriveDataSource.cs` |
| `iCloudDataSource` | `TheTechIdea.Beep.Connectors.iCloud` | `(none)` | `Connectors\Cloud-Storage\iCloud` | `iCloud.csproj` | `Connectors\Cloud-Storage\iCloud\iCloudDataSource.cs` |
| `MediaFireDataSource` | `TheTechIdea.Beep.Connectors.MediaFire` | `(none)` | `Connectors\Cloud-Storage\MediaFire` | `MediaFire.csproj` | `Connectors\Cloud-Storage\MediaFire\MediaFireDataSource.cs` |
| `OneDriveDataSource` | `TheTechIdea.Beep.Connectors.OneDrive` | `(none)` | `Connectors\Cloud-Storage\OneDrive` | `OneDrive.csproj` | `Connectors\Cloud-Storage\OneDrive\OneDriveDataSource.cs` |
| `pCloudDataSource` | `TheTechIdea.Beep.Connectors.pCloud` | `(none)` | `Connectors\Cloud-Storage\pCloud` | `pCloud.csproj` | `Connectors\Cloud-Storage\pCloud\pCloudDataSource.cs` |
| `ChantyDataSource` | `TheTechIdea.Beep.Connectors.Communication.Chanty` | `Chanty` | `Connectors\Communication\ChantyDataSource` | `ChantyDataSource.csproj` | `Connectors\Communication\ChantyDataSource\ChantyDataSource.cs` |
| `DiscordDataSource` | `TheTechIdea.Beep.Connectors.Communication.Discord` | `Discord` | `Connectors\Communication\DiscordDataSource` | `DiscordDataSource.csproj` | `Connectors\Communication\DiscordDataSource\DiscordDataSource.cs` |
| `FlockDataSource` | `TheTechIdea.Beep.Connectors.Communication.Flock` | `Flock` | `Connectors\Communication\FlockDataSource` | `FlockDataSource.csproj` | `Connectors\Communication\FlockDataSource\FlockDataSource.cs` |
| `GoogleChatDataSource` | `TheTechIdea.Beep.Connectors.Communication.GoogleChat` | `GoogleChat` | `Connectors\Communication\GoogleChatDataSource` | `GoogleChatDataSource.csproj` | `Connectors\Communication\GoogleChatDataSource\GoogleChatDataSource.cs` |
| `MicrosoftTeamsDataSource` | `TheTechIdea.Beep.Connectors.Communication.MicrosoftTeams` | `MicrosoftTeams` | `Connectors\Communication\MicrosoftTeamsDataSource` | `MicrosoftTeamsDataSource.csproj` | `Connectors\Communication\MicrosoftTeamsDataSource\MicrosoftTeamsDataSource.cs` |
| `RocketChatDataSource` | `TheTechIdea.Beep.Connectors.Communication.RocketChat` | `RocketChat` | `Connectors\Communication\RocketChatDataSource` | `RocketChatDataSource.csproj` | `Connectors\Communication\RocketChatDataSource\RocketChatDataSource.cs` |
| `SlackDataSource` | `TheTechIdea.Beep.Connectors.Communication.Slack` | `Slack` | `Connectors\Communication\SlackDataSource` | `SlackDataSource.csproj` | `Connectors\Communication\SlackDataSource\SlackDataSource.cs` |
| `TelegramDataSource` | `TheTechIdea.Beep.Connectors.Communication.Telegram` | `Telegram` | `Connectors\Communication\TelegramDataSource` | `TelegramDataSource.csproj` | `Connectors\Communication\TelegramDataSource\TelegramDataSource.cs` |
| `TwistDataSource` | `TheTechIdea.Beep.Connectors.Communication.Twist` | `Twist` | `Connectors\Communication\TwistDataSource` | `TwistDataSource.csproj` | `Connectors\Communication\TwistDataSource\TwistDataSource.cs` |
| `WhatsAppBusinessDataSource` | `TheTechIdea.Beep.Connectors.Communication.WhatsAppBusiness` | `WhatsAppBusiness` | `Connectors\Communication\WhatsAppBusinessDataSource` | `WhatsAppBusinessDataSource.csproj` | `Connectors\Communication\WhatsAppBusinessDataSource\WhatsAppBusinessDataSource.cs` |
| `ZoomDataSource` | `TheTechIdea.Beep.Connectors.Communication.Zoom` | `Zoom` | `Connectors\Communication\ZoomDataSource` | `ZoomDataSource.csproj` | `Connectors\Communication\ZoomDataSource\ZoomDataSource.cs` |
| `WordPressDataSource` | `TheTechIdea.Beep.Connectors.WordPress` | `(none)` | `Connectors\ContentManagement\WordPressDataSource` | `WordPressDataSource.csproj` | `Connectors\ContentManagement\WordPressDataSource\WordPressDataSource.cs` |
| `CopperDataSource` | `TheTechIdea.Beep.Connectors.Copper` | `Copper` | `Connectors\CRM\CopperDataSource` | `CopperDataSource.csproj` | `Connectors\CRM\CopperDataSource\CopperDataSource.cs` |
| `Dynamics365DataSource` | `TheTechIdea.Beep.Connectors.Dynamics365` | `(none)` | `Connectors\CRM\Dynamics365DataSource` | `Dynamics365DataSource.csproj` | `Connectors\CRM\Dynamics365DataSource\Dynamics365DataSource.cs` |
| `FreshsalesDataSource` | `TheTechIdea.Beep.Connectors.Freshsales` | `Freshsales` | `Connectors\CRM\FreshsalesDataSource` | `FreshsalesDataSource.csproj` | `Connectors\CRM\FreshsalesDataSource\FreshsalesDataSource.cs` |
| `HubSpotDataSource` | `TheTechIdea.Beep.Connectors.HubSpot` | `HubSpot` | `Connectors\CRM\HubSpotDataSource` | `HubSpotDataSource.csproj` | `Connectors\CRM\HubSpotDataSource\HubSpotDataSource.cs` |
| `InsightlyDataSource` | `TheTechIdea.Beep.Connectors.InsightlyDataSource` | `Insightly` | `Connectors\CRM\InsightlyDataSource` | `InsightlyDataSource.csproj` | `Connectors\CRM\InsightlyDataSource\InsightlyDataSource.cs` |
| `NutshellDataSource` | `TheTechIdea.Beep.Connectors.NutshellDataSource` | `Nutshell` | `Connectors\CRM\NutshellDataSource` | `NutshellDataSource.csproj` | `Connectors\CRM\NutshellDataSource\NutshellDataSource.cs` |
| `PipedriveDataSource` | `TheTechIdea.Beep.Connectors.PipedriveDataSource` | `Pipedrive` | `Connectors\CRM\PipedriveDataSource` | `PipedriveDataSource.csproj` | `Connectors\CRM\PipedriveDataSource\PipedriveDataSource.cs` |
| `SalesforceDataSource` | `TheTechIdea.Beep.Connectors.Salesforce` | `Salesforce` | `Connectors\CRM\SalesforceDataSource` | `SalesforceDataSource.csproj` | `Connectors\CRM\SalesforceDataSource\SalesforceDataSource.cs` |
| `SugarCRMDataSource` | `TheTechIdea.Beep.Connectors.SugarCRM` | `SugarCRM` | `Connectors\CRM\SugarCRMDataSource` | `SugarCRMDataSource.csproj` | `Connectors\CRM\SugarCRMDataSource\SugarCRMDataSource.cs` |
| `ZohoDataSource` | `TheTechIdea.Beep.Connectors.ZohoDataSource` | `Zoho` | `Connectors\CRM\ZohoDataSource` | `ZohoDataSource.csproj` | `Connectors\CRM\ZohoDataSource\ZohoDataSource.cs` |
| `FreshdeskDataSource` | `TheTechIdea.Beep.FreshdeskDataSource` | `(none)` | `Connectors\CustomerSupport\Freshdesk` | `Freshdesk.csproj` | `Connectors\CustomerSupport\Freshdesk\FreshdeskDataSource.cs` |
| `FrontDataSource` | `TheTechIdea.Beep.DataSources` | `(none)` | `Connectors\CustomerSupport\Front` | `Front.csproj` | `Connectors\CustomerSupport\Front\FrontDataSource.cs` |
| `HelpScoutDataSource` | `TheTechIdea.Beep.DataSources` | `(none)` | `Connectors\CustomerSupport\HelpScout` | `HelpScout.csproj` | `Connectors\CustomerSupport\HelpScout\HelpScoutDataSource.cs` |
| `KayakoDataSource` | `TheTechIdea.Beep.DataSources` | `(none)` | `Connectors\CustomerSupport\Kayako` | `Kayako.csproj` | `Connectors\CustomerSupport\Kayako\KayakoDataSource.cs` |
| `LiveAgentDataSource` | `TheTechIdea.Beep.Connectors.LiveAgent` | `(none)` | `Connectors\CustomerSupport\LiveAgent` | `LiveAgent.csproj` | `Connectors\CustomerSupport\LiveAgent\LiveAgentDataSource.cs` |
| `ZendeskDataSource` | `TheTechIdea.Beep.Connectors.Zendesk` | `(none)` | `Connectors\CustomerSupport\Zendesk` | `Zendesk.csproj` | `Connectors\CustomerSupport\Zendesk\ZendeskDataSource.cs` |
| `ZohoDeskDataSource` | `TheTechIdea.Beep.Connectors.ZohoDesk` | `(none)` | `Connectors\CustomerSupport\ZohoDesk` | `ZohoDesk.csproj` | `Connectors\CustomerSupport\ZohoDesk\ZohoDeskDataSource.cs` |
| `BigCommerceDataSource` | `TheTechIdea.Beep.Connectors.Ecommerce.BigCommerceDataSource` | `BigCommerce` | `Connectors\E-commerce\BigCommerceDataSource` | `BigCommerceDataSource.csproj` | `Connectors\E-commerce\BigCommerceDataSource\BigCommerceDataSource.cs` |
| `EcwidDataSource` | `TheTechIdea.Beep.Connectors.Ecommerce.EcwidDataSource` | `Ecwid` | `Connectors\E-commerce\EcwidDataSource` | `EcwidDataSource.csproj` | `Connectors\E-commerce\EcwidDataSource\EcwidDataSource.cs` |
| `EtsyDataSource` | `TheTechIdea.Beep.Connectors.Ecommerce.Etsy` | `Etsy` | `Connectors\E-commerce\EtsyDataSource` | `EtsyDataSource.csproj` | `Connectors\E-commerce\EtsyDataSource\EtsyDataSource.cs` |
| `MagentoDataSource` | `TheTechIdea.Beep.Connectors.Ecommerce.Magento` | `Magento` | `Connectors\E-commerce\MagentoDataSource` | `MagentoDataSource.csproj` | `Connectors\E-commerce\MagentoDataSource\MagentoDataSource.cs` |
| `OpenCartDataSource` | `TheTechIdea.Beep.Connectors.Ecommerce.OpenCart` | `OpenCart` | `Connectors\E-commerce\OpenCartDataSource` | `OpenCartDataSource.csproj` | `Connectors\E-commerce\OpenCartDataSource\OpenCartDataSource.cs` |
| `ShopifyDataSource` | `TheTechIdea.Beep.Connectors.Ecommerce.Shopify` | `Shopify` | `Connectors\E-commerce\ShopifyDataSource` | `ShopifyDataSource.csproj` | `Connectors\E-commerce\ShopifyDataSource\ShopifyDataSource.cs` |
| `SquarespaceDataSource` | `TheTechIdea.Beep.Connectors.Ecommerce.Squarespace` | `Squarespace` | `Connectors\E-commerce\SquarespaceDataSource` | `SquarespaceDataSource.csproj` | `Connectors\E-commerce\SquarespaceDataSource\SquarespaceDataSource.cs` |
| `VolusionDataSource` | `TheTechIdea.Beep.Connectors.Ecommerce.Volusion` | `Volusion` | `Connectors\E-commerce\VolusionDataSource` | `VolusionDataSource.csproj` | `Connectors\E-commerce\VolusionDataSource\VolusionDataSource.cs` |
| `WixDataSource` | `TheTechIdea.Beep.Connectors.Ecommerce.WixDataSource` | `Wix` | `Connectors\E-commerce\WixDataSource` | `WixDataSource.csproj` | `Connectors\E-commerce\WixDataSource\WixDataSource.cs` |
| `WooCommerceDataSource` | `TheTechIdea.Beep.Connectors.Ecommerce.WooCommerce` | `WooCommerce` | `Connectors\E-commerce\WooCommerceDataSource` | `WooCommerceDataSource.csproj` | `Connectors\E-commerce\WooCommerceDataSource\WooCommerceDataSource.cs` |
| `JotformDataSource` | `TheTechIdea.Beep.Connectors.Jotform` | `(none)` | `Connectors\Forms\JotformDataSource` | `JotformDataSource.csproj` | `Connectors\Forms\JotformDataSource\JotformDataSource.cs` |
| `TypeformDataSource` | `TheTechIdea.Beep.Connectors.Typeform` | `(none)` | `Connectors\Forms\TypeformDataSource` | `TypeformDataSource.csproj` | `Connectors\Forms\TypeformDataSource\TypeformDataSource.cs` |
| `AWSIoTDataSource` | `TheTechIdea.Beep.Connectors.AWSIoT` | `AWSIoT` | `Connectors\IoT\AWSIoT` | `AWSIoTDataSource.csproj` | `Connectors\IoT\AWSIoT\AWSIoTDataSource.cs` |
| `AzureIoTHubDataSource` | `TheTechIdea.Beep.Connectors.AzureIoTHub` | `(none)` | `Connectors\IoT\AzureIoTHub` | `AzureIoTHubDataSource.csproj` | `Connectors\IoT\AzureIoTHub\AzureIoTHubDataSource.cs` |
| `ParticleDataSource` | `TheTechIdea.Beep.Connectors.Particle` | `(none)` | `Connectors\IoT\Particle` | `ParticleDataSource.csproj` | `Connectors\IoT\Particle\ParticleDataSource.cs` |
| `GmailDataSource` | `TheTechIdea.Beep.Connectors.Gmail` | `(none)` | `Connectors\MailServices\Gmail` | `GmailDataSource.csproj` | `Connectors\MailServices\Gmail\GmailDataSource.cs` |
| `OutlookDataSource` | `TheTechIdea.Beep.Connectors.Outlook` | `(none)` | `Connectors\MailServices\Outlook` | `OutlookDataSource.csproj` | `Connectors\MailServices\Outlook\OutlookDataSource.cs` |
| `YahooDataSource` | `TheTechIdea.Beep.Connectors.Yahoo` | `(none)` | `Connectors\MailServices\Yahoo` | `YahooDataSource.csproj` | `Connectors\MailServices\Yahoo\YahooDataSource.cs` |
| `ActiveCampaignDataSource` | `TheTechIdea.Beep.Connectors.Marketing.ActiveCampaign` | `ActiveCampaign` | `Connectors\Marketing\ActiveCampaignDataSource` | `ActiveCampaignDataSource.csproj` | `Connectors\Marketing\ActiveCampaignDataSource\ActiveCampaignDataSource.cs` |
| `CampaignMonitorDataSource` | `TheTechIdea.Beep.Connectors.Marketing.CampaignMonitor` | `CampaignMonitor` | `Connectors\Marketing\CampaignMonitorDataSource` | `CampaignMonitorDataSource.csproj` | `Connectors\Marketing\CampaignMonitorDataSource\CampaignMonitorDataSource.cs` |
| `ConstantContactDataSource` | `TheTechIdea.Beep.Connectors.Marketing.ConstantContact` | `ConstantContact` | `Connectors\Marketing\ConstantContactDataSource` | `ConstantContactDataSource.csproj` | `Connectors\Marketing\ConstantContactDataSource\ConstantContactDataSource.cs` |
| `ConvertKitDataSource` | `TheTechIdea.Beep.Connectors.Marketing.ConvertKitDataSource` | `ConvertKit` | `Connectors\Marketing\ConvertKitDataSource` | `ConvertKitDataSource.csproj` | `Connectors\Marketing\ConvertKitDataSource\ConvertKitDataSource.cs` |
| `DripDataSource` | `TheTechIdea.Beep.Connectors.Marketing.Drip` | `Drip` | `Connectors\Marketing\DripDataSource` | `DripDataSource.csproj` | `Connectors\Marketing\DripDataSource\DripDataSource.cs` |
| `GoogleAdsDataSource` | `TheTechIdea.Beep.Connectors.Marketing.GoogleAds` | `GoogleAds` | `Connectors\Marketing\GoogleAdsDataSource` | `GoogleAdsDataSource.csproj` | `Connectors\Marketing\GoogleAdsDataSource\GoogleAdsDataSource.cs` |
| `KlaviyoDataSource` | `TheTechIdea.Beep.Connectors.Marketing.Klaviyo` | `Klaviyo` | `Connectors\Marketing\KlaviyoDataSource` | `KlaviyoDataSource.csproj` | `Connectors\Marketing\KlaviyoDataSource\KlaviyoDataSource.cs` |
| `MailchimpDataSource` | `TheTechIdea.Beep.Connectors.Marketing.Mailchimp` | `Mailchimp` | `Connectors\Marketing\MailchimpDataSource` | `MailchimpDataSource.csproj` | `Connectors\Marketing\MailchimpDataSource\MailchimpDataSource.cs` |
| `MailerLiteDataSource` | `TheTechIdea.Beep.Connectors.Marketing.MailerLite` | `MailerLite` | `Connectors\Marketing\MailerLiteDataSource` | `MailerLiteDataSource.csproj` | `Connectors\Marketing\MailerLiteDataSource\MailerLiteDataSource.cs` |
| `MarketoDataSource` | `TheTechIdea.Beep.Connectors.Marketing.Marketo` | `Marketo` | `Connectors\Marketing\MarketoDataSource` | `MarketoDataSource.csproj` | `Connectors\Marketing\MarketoDataSource\MarketoDataSource.cs` |
| `SendinblueDataSource` | `TheTechIdea.Beep.Connectors.Marketing.Sendinblue` | `Sendinblue` | `Connectors\Marketing\SendinblueDataSource` | `SendinblueDataSource.csproj` | `Connectors\Marketing\SendinblueDataSource\SendinblueDataSource.cs` |
| `FathomDataSource` | `TheTechIdea.Beep.Connectors.Fathom` | `(none)` | `Connectors\MeetingTools\FathomDataSource` | `FathomDataSource.csproj` | `Connectors\MeetingTools\FathomDataSource\FathomDataSource.cs` |
| `TLDVDataSource` | `TheTechIdea.Beep.Connectors.TLDV` | `(none)` | `Connectors\MeetingTools\TLDVDataSource` | `TLDVDataSource.csproj` | `Connectors\MeetingTools\TLDVDataSource\TLDVDataSource.cs` |
| `ClickSendDataSource` | `TheTechIdea.Beep.Connectors.ClickSend` | `(none)` | `Connectors\SMS\ClickSendDataSource` | `ClickSendDataSource.csproj` | `Connectors\SMS\ClickSendDataSource\ClickSendDataSource.cs` |
| `KudosityDataSource` | `TheTechIdea.Beep.Connectors.Kudosity` | `(none)` | `Connectors\SMS\KudosityDataSource` | `KudosityDataSource.csproj` | `Connectors\SMS\KudosityDataSource\KudosityDataSource.cs` |
| `BufferDataSource` | `TheTechIdea.Beep.BufferDataSource` | `(none)` | `Connectors\SocialMedia\Buffer` | `Buffer.csproj` | `Connectors\SocialMedia\Buffer\BufferDataSource.cs` |
| `FacebookDataSource` | `TheTechIdea.Beep.FacebookDataSource` | `(none)` | `Connectors\SocialMedia\Facebook` | `Facebook.csproj` | `Connectors\SocialMedia\Facebook\FacebookDataSource.cs` |
| `HootsuiteDataSource` | `TheTechIdea.Beep.HootsuiteDataSource` | `(none)` | `Connectors\SocialMedia\Hootsuite` | `Hootsuite.csproj` | `Connectors\SocialMedia\Hootsuite\HootsuiteDataSource.cs` |
| `InstagramDataSource` | `TheTechIdea.Beep.Connectors.Instagram` | `(none)` | `Connectors\SocialMedia\Instagram` | `Instagram.csproj` | `Connectors\SocialMedia\Instagram\InstagramDataSource.cs` |
| `EntityMetadata` | `BeepDataSources.Connectors.SocialMedia.LinkedIn` | `(none)` | `Connectors\SocialMedia\LinkedIn` | `LinkedIn.csproj` | `Connectors\SocialMedia\LinkedIn\LinkedInDataSource.cs` |
| `LoomlyDataSource` | `TheTechIdea.Beep.Connectors.Loomly` | `(none)` | `Connectors\SocialMedia\LoomlyDataSource` | `LoomlyDataSource.csproj` | `Connectors\SocialMedia\LoomlyDataSource\LoomlyDataSource.cs` |
| `PinterestDataSource` | `TheTechIdea.Beep.PinterestDataSource` | `(none)` | `Connectors\SocialMedia\Pinterest` | `Pinterest.csproj` | `Connectors\SocialMedia\Pinterest\PinterestDataSource.cs` |
| `for` | `TheTechIdea.Beep.Connectors.Reddit` | `(none)` | `Connectors\SocialMedia\Reddit` | `Reddit.csproj` | `Connectors\SocialMedia\Reddit\RedditDataSource.cs` |
| `SnapchatConfig` | `TheTechIdea.Beep.Connectors.Snapchat` | `(none)` | `Connectors\SocialMedia\Snapchat` | `Snapchat.csproj` | `Connectors\SocialMedia\Snapchat\SnapchatDataSource.cs` |
| `for` | `BeepDataSources.Connectors.SocialMedia.TikTok` | `(none)` | `Connectors\SocialMedia\TikTok` | `TikTok.csproj` | `Connectors\SocialMedia\TikTok\TikTokDataSource.cs` |
| `TikTokAdsDataSource` | `TheTechIdea.Beep.TikTokAdsDataSource` | `(none)` | `Connectors\SocialMedia\TikTokAds` | `TikTokAds.csproj` | `Connectors\SocialMedia\TikTokAds\TikTokAdsDataSource.cs` |
| `TwitterDataSource` | `TheTechIdea.Beep.Connectors.Twitter` | `(none)` | `Connectors\SocialMedia\Twitter` | `TwitterDataSource.csproj` | `Connectors\SocialMedia\Twitter\TwitterDataSource.cs` |
| `YouTubeDataSource` | `BeepDataSources.Connectors.SocialMedia.YouTube` | `(none)` | `Connectors\SocialMedia\YouTube` | `YouTube.csproj` | `Connectors\SocialMedia\YouTube\YouTubeDataSource.cs` |
| `AnyDoDataSource` | `TheTechIdea.Beep.Connectors.AnyDo` | `(none)` | `Connectors\TaskManagement\AnyDoDataSource` | `AnyDoDataSource.csproj` | `Connectors\TaskManagement\AnyDoDataSource\AnyDoDataSource.cs` |

## DataSourcesPlugins

| Class | Namespace | Create\*Config | Project folder | .csproj | File |
|---|---|---|---|---|---|
| `(no *DataSource.cs in folder)` | `(unknown)` | `(none)` | `DataSourcesPlugins\RDBMSDataSource` | `RDBDataSource.csproj` | `` |

## DataSourcesPluginsCore

| Class | Namespace | Create\*Config | Project folder | .csproj | File |
|---|---|---|---|---|---|
| `AmazonCloudDynamoDbDataSource` | `TheTechIdea.Beep.Cloud` | `(none)` | `DataSourcesPluginsCore\AmazonCloudDatasourceCore` | `AmazonCloudDatasourceCore.csproj` | `DataSourcesPluginsCore\AmazonCloudDatasourceCore\AmazonCloudDynamoDbDataSource.cs` |
| `AmazonCloudS3DataSource` | `TheTechIdea.Beep.Cloud` | `(none)` | `DataSourcesPluginsCore\AmazonCloudDatasourceCore` | `AmazonCloudDatasourceCore.csproj` | `DataSourcesPluginsCore\AmazonCloudDatasourceCore\AmazonCloudS3DataSource.cs` |
| `AzureCloudCosmosDataSource` | `TheTechIdea.Beep.Cloud` | `(none)` | `DataSourcesPluginsCore\AzureCloudDataSourceCore` | `AzureCloudDataSourceCore.csproj` | `DataSourcesPluginsCore\AzureCloudDataSourceCore\AzureCloudCosmosDataSource.cs` |
| `AzureCloudDocumentDataSource` | `TheTechIdea.Beep.Cloud` | `(none)` | `DataSourcesPluginsCore\AzureCloudDataSourceCore` | `AzureCloudDataSourceCore.csproj` | `DataSourcesPluginsCore\AzureCloudDataSourceCore\AzureCloudDocumentDataSource.cs` |
| `CockRoachDataSource` | `TheTechIdea.Beep.DataBase` | `(none)` | `DataSourcesPluginsCore\CockroachDBDataSourceCore` | `CockroachDBDataSourceCore.csproj` | `DataSourcesPluginsCore\CockroachDBDataSourceCore\CockRoachDataSource.cs` |
| `CompositeLayerDataSource` | `TheTechIdea.Beep.Composite` | `(none)` | `DataSourcesPluginsCore\CompositeLayerDataSource6` | `CompositeLayerDataSourceCore.csproj` | `DataSourcesPluginsCore\CompositeLayerDataSource6\CompositeLayerDataSource.cs` |
| `CouchBaseDataSource` | `CouchBaseDataSourceCore` | `(none)` | `DataSourcesPluginsCore\CouchBaseDataSourceCore` | `CouchBaseDataSourceCore.csproj` | `DataSourcesPluginsCore\CouchBaseDataSourceCore\CouchBaseDataSource.cs` |
| `CouchBaseLiteDataSource` | `TheTechIdea.Beep.Local.CouchbaseLite` | `CouchbaseLite` | `DataSourcesPluginsCore\CouchBaseLiteDataSourceCore` | `CouchBaseLiteDataSourceCore.csproj` | `DataSourcesPluginsCore\CouchBaseLiteDataSourceCore\CouchBaseLiteDataSource.cs` |
| `CouchDBDataSource` | `TheTechIdea.Beep.NOSQL.CouchDB` | `CouchDB` | `DataSourcesPluginsCore\CouchDBDataSourceCore` | `CouchDBDataSourceCore.csproj` | `DataSourcesPluginsCore\CouchDBDataSourceCore\CouchDBDataSource.cs` |
| `(no *DataSource.cs in folder)` | `(unknown)` | `(none)` | `DataSourcesPluginsCore\CSVDatasourceCore` | `CSVDatasourceCore.csproj` | `` |
| `DaprDataSource` | `DaprClient` | `(none)` | `DataSourcesPluginsCore\DaprClient` | `DaprClientCore.csproj` | `DataSourcesPluginsCore\DaprClient\DaprDataSource.cs` |
| `(no *DataSource.cs in folder)` | `(unknown)` | `(none)` | `DataSourcesPluginsCore\DataBricksDataSource` | `DataBricksDataSourceCore.csproj` | `` |
| `FireBaseDataSource` | `TheTechIdea.Beep.Cloud.Firebase` | `(none)` | `DataSourcesPluginsCore\FireBaseDataSourceCore` | `FireBaseDataSourceCore.csproj` | `DataSourcesPluginsCore\FireBaseDataSourceCore\FireBaseDataSource.cs` |
| `FireBirdEmbeddedDataSource` | `TheTechIdea.Beep.DataBase` | `(none)` | `DataSourcesPluginsCore\FirebirdDataSourceCore` | `FirebirdDataSourceCore.csproj` | `DataSourcesPluginsCore\FirebirdDataSourceCore\FireBirdEmbeddedDataSource.cs` |
| `handles` | `TheTechIdea.Beep.DataBase` | `(none)` | `DataSourcesPluginsCore\FirebirdDataSourceCore` | `FirebirdDataSourceCore.csproj` | `DataSourcesPluginsCore\FirebirdDataSourceCore\FireBirdDataSource.cs` |
| `FireBoltDataSource` | `TheTechIdea.Beep.Cloud.Firebolt` | `(none)` | `DataSourcesPluginsCore\FireboltDataSource` | `FireboltDataSourceCore.csproj` | `DataSourcesPluginsCore\FireboltDataSource\FireBoltDataSource.cs` |
| `GoogleBigQueryDataSource` | `TheTechIdea.Beep.Cloud.GoogleBigQuery` | `GoogleBigQuery` | `DataSourcesPluginsCore\GoogleBigQuery` | `GoogleBigQueryDatasourceCore.csproj` | `DataSourcesPluginsCore\GoogleBigQuery\GoogleBigQueryDataSource.cs` |
| `GoogleSheetsDataSource` | `TheTechIdea.Beep.Cloud.GoogleSheets` | `(none)` | `DataSourcesPluginsCore\GoogleSheets` | `GoogleSheetsDataSourceCore.csproj` | `DataSourcesPluginsCore\GoogleSheets\GoogleSheetsDataSource.cs` |
| `HadoopDataSource` | `TheTechIdea.Beep.DataBase` | `Hadoop` | `DataSourcesPluginsCore\HadoopDataSourceCore` | `HadoopDataSourceCore.csproj` | `DataSourcesPluginsCore\HadoopDataSourceCore\HadoopDataSource.cs` |
| `HanaDataSource` | `TheTechIdea.Beep.DataBase` | `Hana` | `DataSourcesPluginsCore\HanaDataSource` | `HanaDataSourceCore.csproj` | `DataSourcesPluginsCore\HanaDataSource\HanaDataSource.cs` |
| `Hdf5DataSource` | `TheTechIdea.Beep.FileManager` | `Hdf5DataSource` | `DataSourcesPluginsCore\hdf5DataSource` | `Hdf5DataSourceCore.csproj` | `DataSourcesPluginsCore\hdf5DataSource\Hdf5DataSource.cs` |
| `(no *DataSource.cs in folder)` | `(unknown)` | `(none)` | `DataSourcesPluginsCore\HologresDataSource` | `HologresDataSource.csproj` | `` |
| `InfluxDBDataSource` | `InfluxDBDataSourceCore` | `InfluxDB` | `DataSourcesPluginsCore\InfluxDBDataSourceCore` | `InfluxDBDataSourceCore.csproj` | `DataSourcesPluginsCore\InfluxDBDataSourceCore\InfluxDBDataSource.cs` |
| `KustoDataSource` | `TheTechIdea.Beep.Cloud.Kusto` | `(none)` | `DataSourcesPluginsCore\KustoDataSource` | `KustoDataSourceCore.csproj` | `DataSourcesPluginsCore\KustoDataSource\KustoDataSource.cs` |
| `LevelDBDataSource` | `LevelDBDataSourceCore` | `(none)` | `DataSourcesPluginsCore\LevelDBDataSourceCore` | `LevelDBDataSourceCore.csproj` | `DataSourcesPluginsCore\LevelDBDataSourceCore\LevelDBDataSource.cs` |
| `LiteDBDataSource` | `LiteDBDataSourceCore` | `LiteDBDataSource` | `DataSourcesPluginsCore\LiteDBDataSourceCore` | `LiteDBDataSourceCore.csproj` | `DataSourcesPluginsCore\LiteDBDataSourceCore\LiteDBDataSource.cs` |
| `LMDBDataSource` | `LMDBDataSourceCore` | `(none)` | `DataSourcesPluginsCore\LMDBDataSourceCore` | `LMDBDataSourceCore.csproj` | `DataSourcesPluginsCore\LMDBDataSourceCore\LMDBDataSource.cs` |
| `(no *DataSource.cs in folder)` | `(unknown)` | `(none)` | `DataSourcesPluginsCore\MlModelDataSource` | `MlModelDataSourceCore.csproj` | `` |
| `MongoDBDataSource` | `TheTechIdea.Beep.NOSQL` | `MongoDB` | `DataSourcesPluginsCore\MongoDBDataSourceCore` | `MongoDBDataSourceCore.csproj` | `DataSourcesPluginsCore\MongoDBDataSourceCore\MongoDBDataSource.cs` |
| `MySQLDataSource` | `TheTechIdea.Beep.DataBase` | `MySql` | `DataSourcesPluginsCore\MySqlDataSourceCore` | `MySqlDataSourceCore.csproj` | `DataSourcesPluginsCore\MySqlDataSourceCore\MySQLDataSource.cs` |
| `OnnxDataSource` | `TheTechIdea.Beep.FileManager` | `(none)` | `DataSourcesPluginsCore\OnnxDataSource` | `OnnxDataSourceCore.csproj` | `DataSourcesPluginsCore\OnnxDataSource\OnnxDataSource.cs` |
| `OPCDataSource` | `OPCUADataSource` | `OPC` | `DataSourcesPluginsCore\OPCUADataSource` | `OPCUADataSourceCore.csproj` | `DataSourcesPluginsCore\OPCUADataSource\OPCDataSource.cs` |
| `OracleDataSource` | `TheTechIdea.Beep.DataBase` | `Oracle` | `DataSourcesPluginsCore\OracleDataSourceCore` | `OracleDataSourceCore.csproj` | `DataSourcesPluginsCore\OracleDataSourceCore\OracleDataSource.cs` |
| `ParquetDataSource` | `ParquetDataSourceCore` | `ParquetDataSource` | `DataSourcesPluginsCore\ParquetDataSource` | `ParquetDataSourceCore.csproj` | `DataSourcesPluginsCore\ParquetDataSource\ParquetDataSource.cs` |
| `PetastormDataSource` | `TheTechIdea.Beep.FileManager` | `Petastorm` | `DataSourcesPluginsCore\PetastormDataSource` | `PetastormDataSourceCore.csproj` | `DataSourcesPluginsCore\PetastormDataSource\PetastormDataSource.cs` |
| `PostgreDataSource` | `TheTechIdea.Beep.DataBase` | `Postgre` | `DataSourcesPluginsCore\PostgreDataSourceCore` | `PostgreDataSourceCore.csproj` | `DataSourcesPluginsCore\PostgreDataSourceCore\PostgreDataSource.cs` |
| `PrestoDataSource` | `TheTechIdea.Beep.Cloud.Presto` | `(none)` | `DataSourcesPluginsCore\PrestoDatasource` | `PrestoDatasourceCore.csproj` | `DataSourcesPluginsCore\PrestoDatasource\PrestoDataSource.cs` |
| `RavenDBDataSource` | `TheTechIdea.Beep.NOSQL.RavenDB` | `RavenDB` | `DataSourcesPluginsCore\RavenDBDataSourceCore` | `RavenDBDataSourceCore.csproj` | `DataSourcesPluginsCore\RavenDBDataSourceCore\RavenDBDataSource.cs` |
| `RealMDataSource` | `TheTechIdea.Beep.DataSource` | `RealIM` | `DataSourcesPluginsCore\RealMDataSource` | `RealMDataSourceCore.csproj` | `DataSourcesPluginsCore\RealMDataSource\RealMDataSource.cs` |
| `RedisDataSource` | `TheTechIdea.Beep.Redis` | `Redis` | `DataSourcesPluginsCore\RedisDataSourceCore` | `RedisDataSourceCore.csproj` | `DataSourcesPluginsCore\RedisDataSourceCore\RedisDataSource.cs` |
| `RocksDBDataSource` | `RocksDBDataSourceCore` | `(none)` | `DataSourcesPluginsCore\RocksDBDataSourceCore` | `RocksDBDataSourceCore.csproj` | `DataSourcesPluginsCore\RocksDBDataSourceCore\RocksDBDataSource.cs` |
| `RocksetDataSource` | `TheTechIdea.Beep.Cloud.Rockset` | `(none)` | `DataSourcesPluginsCore\RocksetDatasource` | `RocksetDatasourceCore.csproj` | `DataSourcesPluginsCore\RocksetDatasource\RocksetDataSource.cs` |
| `SnowFlakeDataSource` | `TheTechIdea.Beep.Cloud.Snowflake` | `SnowFlake` | `DataSourcesPluginsCore\SnowFlakeDataSource` | `SnowFlakeDataSourceCore.csproj` | `DataSourcesPluginsCore\SnowFlakeDataSource\SnowFlakeDataSource.cs` |
| `SpannerDataSource` | `TheTechIdea.Beep.Cloud.Spanner` | `Spanner` | `DataSourcesPluginsCore\SpannerDataSourceCore` | `SpannerDataSourceCore.csproj` | `DataSourcesPluginsCore\SpannerDataSourceCore\SpannerDataSource.cs` |
| `(no *DataSource.cs in folder)` | `(unknown)` | `(none)` | `DataSourcesPluginsCore\SqlCompactDatasourceCore` | `SqlCompactDatasourceCore.csproj` | `` |
| `SQLiteDataSource` | `TheTechIdea.Beep.DataBase` | `SQLite` | `DataSourcesPluginsCore\SqliteDatasourceCore` | `SqliteDatasourceCore.csproj` | `DataSourcesPluginsCore\SqliteDatasourceCore\SQLiteDataSource.cs` |
| `SQLServerDataSource` | `TheTechIdea.Beep.DataBase` | `SqlServer` | `DataSourcesPluginsCore\SQlServerDataSourceCore` | `SqlServerDataSourceCore.csproj` | `DataSourcesPluginsCore\SQlServerDataSourceCore\SQLServerDataSource.cs` |
| `SupabaseDataSource` | `SupabaseDataSourceCore` | `Supabase` | `DataSourcesPluginsCore\SupabaseDataSourceCore` | `SupabaseDataSourceCore.csproj` | `DataSourcesPluginsCore\SupabaseDataSourceCore\SupabaseDataSource.cs` |
| `(no *DataSource.cs in folder)` | `(unknown)` | `(none)` | `DataSourcesPluginsCore\TerraDataDataSource` | `TerraDataDataSourceCore.csproj` | `` |
| `(no *DataSource.cs in folder)` | `(unknown)` | `(none)` | `DataSourcesPluginsCore\TimeScaleDBDataSource` | `TimeScaleDBDataSource.csproj` | `` |
| `(no *DataSource.cs in folder)` | `(unknown)` | `(none)` | `DataSourcesPluginsCore\TrinoDataSource` | `TrinoDataSourceCore.csproj` | `` |
| `(no *DataSource.cs in folder)` | `(unknown)` | `(none)` | `DataSourcesPluginsCore\TxtXlsCSVFileSourceCore` | `TxtXlsCSVFileSourceCore.csproj` | `` |
| `(no *DataSource.cs in folder)` | `(unknown)` | `(none)` | `DataSourcesPluginsCore\VerticaDataSource` | `VerticaDataSourceCore.csproj` | `` |

## InMemoryDB

| Class | Namespace | Create\*Config | Project folder | .csproj | File |
|---|---|---|---|---|---|
| `DuckDBDataSource` | `DuckDBDataSourceCore` | `DuckDB` | `InMemoryDB\DuckDBDataSourceCore` | `DuckDBDataSourceCore.csproj` | `InMemoryDB\DuckDBDataSourceCore\DuckDBDataSource.cs` |

## Messaging

| Class | Namespace | Create\*Config | Project folder | .csproj | File |
|---|---|---|---|---|---|
| `AmazonSQSDataSource` | `TheTechIdea.Beep.AmazonSQS` | `(none)` | `Messaging\AmazonSQSDataSourceCore` | `AmazonSQSDataSourceCore.csproj` | `Messaging\AmazonSQSDataSourceCore\AmazonSQSDataSource.cs` |
| `AzureServiceBusDataSource` | `TheTechIdea.Beep.AzureServiceBus` | `AzureServiceBus` | `Messaging\AzureServiceBusDataSourceCore` | `AzureServiceBusDataSourceCore.csproj` | `Messaging\AzureServiceBusDataSourceCore\AzureServiceBusDataSource.cs` |
| `GooglePubSubDataSource` | `TheTechIdea.Beep.GooglePubSub` | `(none)` | `Messaging\GooglePubSubDataSourceCore` | `GooglePubSubDataSourceCore.csproj` | `Messaging\GooglePubSubDataSourceCore\GooglePubSubDataSource.cs` |
| `KafkaDataSource` | `TheTechIdea.Beep.EventStream` | `Kafka` | `Messaging\KafkaDataSourceCore` | `KafkaDataSourceCore.csproj` | `Messaging\KafkaDataSourceCore\KafkaDataSource.cs` |
| `MassTransitDataSource` | `TheTechIdea.Beep.MassTransitDataSourceCore` | `MassTransit` | `Messaging\MassTransitDataSource` | `MassTransitDataSourceCore.csproj` | `Messaging\MassTransitDataSource\MassTransitDataSource.cs` |
| `NATSDataSource` | `TheTechIdea.Beep.NATS` | `(none)` | `Messaging\NATSDataSourceCore` | `NATSDataSourceCore.csproj` | `Messaging\NATSDataSourceCore\NATSDataSource.cs` |
| `RabbitMQDataSource` | `RabbitMQDataSourceCore` | `RabbitMQ` | `Messaging\RabbitMQDataSourceCore` | `RabbitMQDataSourceCore.csproj` | `Messaging\RabbitMQDataSourceCore\RabbitMQDataSource.cs` |
| `RedisStreamsDataSource` | `TheTechIdea.Beep.RedisStreams` | `(none)` | `Messaging\RedisStreamsDataSourceCore` | `RedisStreamsDataSourceCore.csproj` | `Messaging\RedisStreamsDataSourceCore\RedisStreamsDataSource.cs` |

## VectorDatabase

| Class | Namespace | Create\*Config | Project folder | .csproj | File |
|---|---|---|---|---|---|
| `ChromaDBDataSource` | `TheTechIdea.Beep.ChromaDBDatasource` | `ChromaDB` | `VectorDatabase\TheTechIdea.Beep.ChromaDBDatasource` | `TheTechIdea.Beep.ChromaDBDatasource.csproj` | `VectorDatabase\TheTechIdea.Beep.ChromaDBDatasource\ChromaDBDataSource.cs` |
| `MilvusDataSource` | `TheTechIdea.Beep.MilvusDatasource` | `Milvus` | `VectorDatabase\TheTechIdea.Beep.MilvusDatasource` | `TheTechIdea.Beep.MilvusDatasource.csproj` | `VectorDatabase\TheTechIdea.Beep.MilvusDatasource\MilvusDataSource.cs` |
| `(no *DataSource.cs in folder)` | `(unknown)` | `(none)` | `VectorDatabase\TheTechIdea.Beep.PineConeDatasource` | `TheTechIdea.Beep.PineConeDatasource.csproj` | `` |
| `(no *DataSource.cs in folder)` | `(unknown)` | `(none)` | `VectorDatabase\TheTechIdea.Beep.QdrantDatasource` | `TheTechIdea.Beep.QdrantDatasource.csproj` | `` |
| `(no *DataSource.cs in folder)` | `(unknown)` | `(none)` | `VectorDatabase\TheTechIdea.Beep.ShapVectorDatasource` | `TheTechIdea.Beep.SharpVectorDatasource.csproj` | `` |

## tempReflection

| Class | Namespace | Create\*Config | Project folder | .csproj | File |
|---|---|---|---|---|---|
| `(no *DataSource.cs in folder)` | `(unknown)` | `(none)` | `tempReflection` | `tempReflection.csproj` | `` |

## tests

| Class | Namespace | Create\*Config | Project folder | .csproj | File |
|---|---|---|---|---|---|
| `(no *DataSource.cs in folder)` | `(unknown)` | `(none)` | `tests\RDBDataSource.Tests` | `RDBDataSource.Tests.csproj` | `` |

## Orphans: Create\*Config exists but no implementation project found

Driver configs in `ConnectionHelper` whose `classHandler` has no matching
`*DataSource.cs` class in any project folder. These need an implementation
project before they can get a fluent extension.

Count: **141**

| Create\*Config | classHandler |
|---|---|
| `CreateADOConfig` | `ADODataSource` |
| `CreateAWSAthenaConfig` | `AWSAthenaDataSource` |
| `CreateAWSGlueConfig` | `AWSGlueDataSource` |
| `CreateAWSIoTAnalyticsConfig` | `AWSIoTAnalyticsDataSource` |
| `CreateAWSIoTCoreConfig` | `AWSIoTCoreDataSource` |
| `CreateAWSKinesisConfig` | `AWSKinesisDataSource` |
| `CreateAWSRDSConfig` | `AWSRDSDataSource` |
| `CreateAWSRedshiftConfig` | `AWSRedshiftDataSource` |
| `CreateAWSSNSConfig` | `AWSSNSDataSource` |
| `CreateAWSSQSConfig` | `AWSSQSDataSource` |
| `CreateAWSStepFunctionsConfig` | `AWSStepFunctionsDataSource` |
| `CreateAWSWorkflowConfig` | `AWSWorkflowDataSource` |
| `CreateActiveMQConfig` | `ActiveMQDataSource` |
| `CreateApacheFlinkConfig` | `ApacheFlinkDataSource` |
| `CreateApacheIgniteCacheConfig` | `ApacheIgniteCacheDataSource` |
| `CreateApacheIgniteConfig` | `ApacheIgniteDataSource` |
| `CreateApacheSparkStreamingConfig` | `ApacheSparkStreamingDataSource` |
| `CreateApacheStormConfig` | `ApacheStormDataSource` |
| `CreateArangoDBConfig` | `ArangoDBDataSource` |
| `CreateAsanaConfig` | `AsanaDataSource` |
| `CreateAvroDataSourceConfig` | `AvroDataSource` |
| `CreateAzureBlobStorageConfig` | `AzureBlobStorageDataSource` |
| `CreateAzureBoardsConfig` | `AzureBoardsDataSource` |
| `CreateAzureCloudConfig` | `AzureCloudDataSource` |
| `CreateAzureDataFactoryConfig` | `AzureDataFactoryDataSource` |
| `CreateAzureSQLConfig` | `AzureSQLDataSource` |
| `CreateAzureSynapseConfig` | `AzureSynapseDataSource` |
| `CreateBasecampConfig` | `BasecampDataSource` |
| `CreateBigCartelConfig` | `BigCartelDataSource` |
| `CreateBitcoinCoreConfig` | `BitcoinCoreDataSource` |
| `CreateCSVDataSourceConfig` | `CSVDataSource` |
| `CreateCachedMemoryConfig` | `CachedMemoryDataSource` |
| `CreateCaffeineCacheConfig` | `CaffeineCacheDataSource` |
| `CreateCassandraConfig` | `CassandraDataSource` |
| `CreateChronicleMapConfig` | `ChronicleMapDataSource` |
| `CreateClickHouseConfig` | `ClickHouseDataSource` |
| `CreateClickUpConfig` | `ClickUpDataSource` |
| `CreateCockroachConfig` | `CockroachDBDataSource` |
| `CreateCouchbaseCacheConfig` | `CouchbaseCacheDataSource` |
| `CreateCouchbaseConfig` | `CouchbaseDataSource` |
| `CreateCriteoConfig` | `CriteoDataSource` |
| `CreateDB2Config` | `DB2DataSource` |
| `CreateDICOMDataSourceConfig` | `DICOMDataSource` |
| `CreateDataBricksConfig` | `DataBricksDataSource` |
| `CreateDataViewConfig` | `DataViewDataSource` |
| `CreateDistributedCacheConfig` | `DistributedCacheDataSource` |
| `CreateDuckDBMemoryConfig` | `DuckDBMemoryDataSource` |
| `CreateDynamoDBConfig` | `DynamoDBDataSource` |
| `CreateEhCacheConfig` | `EhCacheDataSource` |
| `CreateElasticsearchConfig` | `ElasticsearchDatasource` |
| `CreateEthereumConfig` | `EthereumDataSource` |
| `CreateEventHubsConfig` | `EventHubsDataSource` |
| `CreateFeatherDataSourceConfig` | `FeatherDataSource` |
| `CreateFirebirdConfig` | `FireBirdDataSource` |
| `CreateFirebaseConfig` | `FirebaseDataSource` |
| `CreateFireboltConfig` | `FireboltDataSource` |
| `CreateFlatFileDataSourceConfig` | `FlatFileDataSource` |
| `CreateGRPCConfig` | `GRPCDataSource` |
| `CreateGoogleCloudStorageConfig` | `GoogleCloudStorageDataSource` |
| `CreateGraphMLDataSourceConfig` | `GraphMLDataSource` |
| `CreateGraphQLConfig` | `GraphQLDataSource` |
| `CreateGridGainConfig` | `GridGainDataSource` |
| `CreateH2DatabaseConfig` | `H2DatabaseDataSource` |
| `CreateHazelcastCacheConfig` | `HazelcastCacheDataSource` |
| `CreateHazelcastConfig` | `HazelcastDataSource` |
| `CreateHologresConfig` | `HologresDataSource` |
| `CreateHootsuiteMarketingConfig` | `HootsuiteMarketingDataSource` |
| `CreateHybridCacheConfig` | `HybridCacheDataSource` |
| `CreateHyperledgerConfig` | `HyperledgerDataSource` |
| `CreateINIDataSourceConfig` | `INIDataSource` |
| `CreateInMemoryCacheConfig` | `InMemoryCacheDataSource` |
| `CreateInfinispanConfig` | `InfinispanDataSource` |
| `CreateJSONRPCConfig` | `JSONRPCDataSource` |
| `CreateJiraConfig` | `JiraDataSource` |
| `CreateJsonDataSourceConfig` | `JsonDataSource` |
| `CreateL1L2CacheConfig` | `L1L2CacheDataSource` |
| `CreateLASDataSourceConfig` | `LASDataSource` |
| `CreateLibSVMDataSourceConfig` | `LibSVMDataSource` |
| `CreateLogFileDataSourceConfig` | `LogFileDataSource` |
| `CreateMailgunConfig` | `MailgunDataSource` |
| `CreateMarkdownDataSourceConfig` | `MarkdownDataSource` |
| `CreateMattermostConfig` | `MattermostDataSource` |
| `CreateMemcachedConfig` | `MemcachedDataSource` |
| `CreateMemoryCacheConfig` | `MemoryCacheDataSource` |
| `CreateMicrosoftDynamics365Config` | `MicrosoftDynamics365DataSource` |
| `CreateMondayConfig` | `MondayDataSource` |
| `CreateNCacheConfig` | `NCacheDataSource` |
| `CreateNatsConfig` | `NatsDataSource` |
| `CreateNeo4jConfig` | `Neo4jDataSource` |
| `CreateNotionConfig` | `NotionDataSource` |
| `CreateODBCConfig` | `ODBCDataSource` |
| `CreateODataConfig` | `ODataDataSource` |
| `CreateOLEDBConfig` | `OLEDBDataSource` |
| `CreateORCDataSourceConfig` | `ORCDataSource` |
| `CreateOracleCRMConfig` | `OracleCRMDataSource` |
| `CreateOrientDBConfig` | `OrientDBDataSource` |
| `CreatePDFDataSourceConfig` | `PDFDataSource` |
| `CreatePineConeConfig` | `PineConeDataSource` |
| `CreatePodioConfig` | `PodioDataSource` |
| `CreatePrestaShopConfig` | `PrestaShopDataSource` |
| `CreateBeepProxyClusterConfig` | `ProxyCluster` |
| `CreatePulsarConfig` | `PulsarDataSource` |
| `CreateQdrantConfig` | `QdrantDataSource` |
| `CreateRealIMMemoryConfig` | `RealIMDataSource` |
| `CreateRecordIODataSourceConfig` | `RecordIODataSource` |
| `CreateRedisCacheConfig` | `RedisCacheDataSource` |
| `CreateRedisMemoryConfig` | `RedisMemoryDataSource` |
| `CreateRedisVectorConfig` | `RedisVectorDataSource` |
| `CreateBeepProxyNodeConfig` | `RemoteProxyDataSource` |
| `CreateRestApiConfig` | `RestApiDataSource` |
| `CreateRocketChatCommConfig` | `RocketChatCommDataSource` |
| `CreateRocketSetConfig` | `RocketSetDataSource` |
| `CreateSAPCRMConfig` | `SAPCRMDataSource` |
| `CreateSOAPConfig` | `SOAPDataSource` |
| `CreateSqlCompactConfig` | `SQLCompactDataSource` |
| `CreateMauiSQLiteConfig` | `SQLiteMauiDataSource` |
| `CreateSQLiteMemoryConfig` | `SQLiteMemoryDataSource` |
| `CreateSSEConfig` | `SSEDataSource` |
| `CreateSendGridConfig` | `SendGridDataSource` |
| `CreateShapVectorConfig` | `ShapVectorDataSource` |
| `CreateSmartsheetConfig` | `SmartsheetDataSource` |
| `CreateSmartsheetPMConfig` | `SmartsheetPMDataSource` |
| `CreateStackExchangeRedisConfig` | `StackExchangeRedisDatasource` |
| `CreateTeamworkConfig` | `TeamworkDataSource` |
| `CreateTerraDataConfig` | `TerraDataDataSource` |
| `CreateTextFileDataSourceConfig` | `TextFileDataSource` |
| `CreateTimeScaleConfig` | `TimeScaleDBDataSource` |
| `CreateTrelloConfig` | `TrelloDataSource` |
| `CreateTxtXlsCSVFileSourceConfig` | `TxtXlsCSVFileSource` |
| `CreateVerticaConfig` | `VerticaDataSource` |
| `CreateVespaConfig` | `VespaDataSource` |
| `CreateVistaDBConfig` | `VistaDBDataSource` |
| `CreateWeaviateConfig` | `WeaviateDataSource` |
| `CreateWebApiConfig` | `WebApiDataSource` |
| `CreateWebSocketConfig` | `WebSocketDataSource` |
| `CreateWrikeConfig` | `WrikeDataSource` |
| `CreateXMLDataSourceConfig` | `XMLDataSource` |
| `CreateXMLRPCConfig` | `XMLRPCDataSource` |
| `CreateYAMLDataSourceConfig` | `YAMLDataSource` |
| `CreateZeroMQConfig` | `ZeroMQDataSource` |
| `CreateZillizConfig` | `ZillizDataSource` |
