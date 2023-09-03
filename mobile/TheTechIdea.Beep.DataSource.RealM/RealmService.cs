using System.Text.Json;
using Realms;
using Realms.Sync;
using System.IO;
using System.Threading.Tasks;

namespace RealmTodo.Services
{
    public static class RealmService
    {
        private static bool serviceInitialised;
        private static Realms.Sync.App app;
        private static Realm mainThreadRealm;
        public static User CurrentUser => app.CurrentUser;

        public static async Task Init()
        {
            if (serviceInitialised)
            {
                return;
            }
            Stream fileStream=null;
#if WINDOWS
        string filepath=Path.Combine(Environment.CurrentDirectory,"atlasConfig.json");
        using FileStream stream = new FileStream(filepath, FileMode.Open);

#endif
#if  ANDROID && IOS 
            using  fileStream = await FileSystem.Current.OpenAppPackageFileAsync("atlasConfig.json");
#endif

            using StreamReader reader = new(fileStream);
            var fileContent = await reader.ReadToEndAsync();

            var config = JsonSerializer.Deserialize<RealmAppConfig>(fileContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true});

            var appConfiguration = new AppConfiguration(config.AppId)
            {
                BaseUri = new Uri(config.BaseUrl)
            };

            app = Realms.Sync.App.Create(appConfiguration);

            serviceInitialised = true;
        }

        public static Realm GetMainThreadRealm()
        {
            return mainThreadRealm ??= GetRealm();
        }

        public static Realm GetRealm()
        {
            var config = new FlexibleSyncConfiguration(app.CurrentUser)
            {
                PopulateInitialSubscriptions = (realm) =>
                {
                    var (query, queryName) = GetQueryForSubscriptionType(realm, SubscriptionType.Mine);
                    realm.Subscriptions.Add(query, new SubscriptionOptions { Name = queryName });
                }
            };

            return Realm.GetInstance(config);
        }

        public static async Task RegisterAsync(string email, string password)
        {
            await app.EmailPasswordAuth.RegisterUserAsync(email, password);
        }

        public static async Task LoginAsync(string email, string password)
        {
            await app.LogInAsync(Credentials.EmailPassword(email, password));

            //This will populate the initial set of subscriptions the first time the realm is opened
            using var realm = GetRealm();
            await realm.Subscriptions.WaitForSynchronizationAsync();
        }

        public static async Task LogoutAsync()
        {
            await app.CurrentUser.LogOutAsync();
            mainThreadRealm?.Dispose();
            mainThreadRealm = null;
        }

        public static async Task SetSubscription(Realm realm, SubscriptionType subType)
        {
            if (GetCurrentSubscriptionType(realm) == subType)
            {
                return;
            }

            realm.Subscriptions.Update(() =>
            {
                realm.Subscriptions.RemoveAll(true);

                var (query, queryName) = GetQueryForSubscriptionType(realm, subType);

                realm.Subscriptions.Add(query, new SubscriptionOptions { Name = queryName });
            });

            //There is no need to wait for synchronization if we are disconnected
            if (realm.SyncSession.ConnectionState != ConnectionState.Disconnected)
            {
                await realm.Subscriptions.WaitForSynchronizationAsync();
            }
        }

        public static SubscriptionType GetCurrentSubscriptionType(Realm realm)
        {
            var activeSubscription = realm.Subscriptions.FirstOrDefault();

            return activeSubscription.Name switch
            {
                "all" => SubscriptionType.All,
                "mine" => SubscriptionType.Mine,
                _ => throw new InvalidOperationException("Unknown subscription type")
            };
        }

        private static (IQueryable<T> Query, string Name) GetQueryForSubscriptionType<T>(Realm realm, SubscriptionType subType) where T : RealmObject
        {
            IQueryable<T> query = null;
            string queryName = null;

            if (subType == SubscriptionType.Mine)
            {
                query = realm.All<T>().Where(i => i.OwnerId == CurrentUser.Id);
                queryName = "mine";
            }
            else if (subType == SubscriptionType.All)
            {
                query = realm.All<T>();
                queryName = "all";
            }
            else
            {
                throw new ArgumentException("Unknown subscription type");
            }

            return (query, queryName);
        }
    }

    public enum SubscriptionType
    {
        Mine,
        All,
    }

    public class RealmAppConfig
    {
        public string AppId { get; set; }

        public string BaseUrl { get; set; }
    }
}

