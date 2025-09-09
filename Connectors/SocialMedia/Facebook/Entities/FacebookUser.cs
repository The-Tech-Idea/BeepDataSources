using System;

namespace TheTechIdea.Beep.FacebookDataSource.Entities
{
    /// <summary>
    /// Represents a Facebook user
    /// </summary>
    public class FacebookUser
    {
        /// <summary>
        /// The user ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The user's full name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The user's first name
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// The user's last name
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// The user's middle name
        /// </summary>
        public string MiddleName { get; set; }

        /// <summary>
        /// The user's email address
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// The user's profile picture URL
        /// </summary>
        public string Picture { get; set; }

        /// <summary>
        /// The user's cover photo URL
        /// </summary>
        public string Cover { get; set; }

        /// <summary>
        /// The user's gender
        /// </summary>
        public string Gender { get; set; }

        /// <summary>
        /// The user's birthday
        /// </summary>
        public DateTime? Birthday { get; set; }

        /// <summary>
        /// The user's location
        /// </summary>
        public FacebookLocation Location { get; set; }

        /// <summary>
        /// The user's hometown
        /// </summary>
        public FacebookLocation Hometown { get; set; }

        /// <summary>
        /// The user's bio/description
        /// </summary>
        public string Bio { get; set; }

        /// <summary>
        /// The user's website
        /// </summary>
        public string Website { get; set; }

        /// <summary>
        /// The user's relationship status
        /// </summary>
        public string RelationshipStatus { get; set; }

        /// <summary>
        /// The user's political views
        /// </summary>
        public string Political { get; set; }

        /// <summary>
        /// The user's religion
        /// </summary>
        public string Religion { get; set; }

        /// <summary>
        /// The user's quotes
        /// </summary>
        public string Quotes { get; set; }

        /// <summary>
        /// The user's about information
        /// </summary>
        public string About { get; set; }

        /// <summary>
        /// The user's username
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The user's link to their profile
        /// </summary>
        public string Link { get; set; }

        /// <summary>
        /// The user's timezone
        /// </summary>
        public int? Timezone { get; set; }

        /// <summary>
        /// The user's locale
        /// </summary>
        public string Locale { get; set; }

        /// <summary>
        /// Whether the user is verified
        /// </summary>
        public bool? Verified { get; set; }

        /// <summary>
        /// The user's updated time
        /// </summary>
        public DateTime? UpdatedTime { get; set; }

        /// <summary>
        /// The user's age range
        /// </summary>
        public FacebookAgeRange AgeRange { get; set; }

        /// <summary>
        /// The user's currency
        /// </summary>
        public FacebookCurrency Currency { get; set; }

        /// <summary>
        /// The user's payment pricepoints
        /// </summary>
        public FacebookPaymentPricepoints PaymentPricepoints { get; set; }

        /// <summary>
        /// The user's favorite athletes
        /// </summary>
        public List<FacebookExperience> FavoriteAthletes { get; set; } = new();

        /// <summary>
        /// The user's favorite teams
        /// </summary>
        public List<FacebookExperience> FavoriteTeams { get; set; } = new();

        /// <summary>
        /// The user's inspirational people
        /// </summary>
        public List<FacebookExperience> InspirationalPeople { get; set; } = new();

        /// <summary>
        /// The user's languages
        /// </summary>
        public List<FacebookExperience> Languages { get; set; } = new();

        /// <summary>
        /// The user's sports
        /// </summary>
        public List<FacebookExperience> Sports { get; set; } = new();

        /// <summary>
        /// The user's work experiences
        /// </summary>
        public List<FacebookWork> Work { get; set; } = new();

        /// <summary>
        /// The user's education
        /// </summary>
        public List<FacebookEducation> Education { get; set; } = new();

        /// <summary>
        /// The user's friends count
        /// </summary>
        public int? FriendsCount { get; set; }

        /// <summary>
        /// The user's followers count
        /// </summary>
        public int? FollowersCount { get; set; }

        /// <summary>
        /// Custom fields for extensibility
        /// </summary>
        public Dictionary<string, object> CustomFields { get; set; } = new();
    }

    /// <summary>
    /// Represents a Facebook location
    /// </summary>
    public class FacebookLocation
    {
        /// <summary>
        /// The location ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The location name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The location street address
        /// </summary>
        public string Street { get; set; }

        /// <summary>
        /// The location city
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// The location state/province
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// The location country
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// The location zip code
        /// </summary>
        public string Zip { get; set; }

        /// <summary>
        /// The location latitude
        /// </summary>
        public double? Latitude { get; set; }

        /// <summary>
        /// The location longitude
        /// </summary>
        public double? Longitude { get; set; }
    }

    /// <summary>
    /// Represents a Facebook age range
    /// </summary>
    public class FacebookAgeRange
    {
        /// <summary>
        /// The minimum age
        /// </summary>
        public int? Min { get; set; }

        /// <summary>
        /// The maximum age
        /// </summary>
        public int? Max { get; set; }
    }

    /// <summary>
    /// Represents Facebook currency information
    /// </summary>
    public class FacebookCurrency
    {
        /// <summary>
        /// The currency code
        /// </summary>
        public string CurrencyOffset { get; set; }

        /// <summary>
        /// The currency offset
        /// </summary>
        public string UsdExchange { get; set; }

        /// <summary>
        /// The USD exchange rate
        /// </summary>
        public double? UsdExchangeInverse { get; set; }
    }

    /// <summary>
    /// Represents Facebook payment pricepoints
    /// </summary>
    public class FacebookPaymentPricepoints
    {
        /// <summary>
        /// The mobile pricepoints
        /// </summary>
        public List<FacebookPricepoint> Mobile { get; set; } = new();
    }

    /// <summary>
    /// Represents a Facebook pricepoint
    /// </summary>
    public class FacebookPricepoint
    {
        /// <summary>
        /// The pricepoint credits
        /// </summary>
        public int Credits { get; set; }

        /// <summary>
        /// The pricepoint local currency
        /// </summary>
        public string LocalCurrency { get; set; }

        /// <summary>
        /// The pricepoint user price
        /// </summary>
        public string UserPrice { get; set; }
    }

    /// <summary>
    /// Represents a Facebook experience (work, education, etc.)
    /// </summary>
    public class FacebookExperience
    {
        /// <summary>
        /// The experience ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The experience name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The experience description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The experience with (for work)
        /// </summary>
        public FacebookUser With { get; set; }

        /// <summary>
        /// The experience start date
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// The experience end date
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// The experience type
        /// </summary>
        public string Type { get; set; }
    }

    /// <summary>
    /// Represents Facebook work experience
    /// </summary>
    public class FacebookWork : FacebookExperience
    {
        /// <summary>
        /// The employer
        /// </summary>
        public FacebookPage Employer { get; set; }

        /// <summary>
        /// The job position
        /// </summary>
        public string Position { get; set; }

        /// <summary>
        /// The work location
        /// </summary>
        public FacebookLocation Location { get; set; }

        /// <summary>
        /// The work projects
        /// </summary>
        public List<FacebookProject> Projects { get; set; } = new();
    }

    /// <summary>
    /// Represents Facebook education
    /// </summary>
    public class FacebookEducation : FacebookExperience
    {
        /// <summary>
        /// The school
        /// </summary>
        public FacebookPage School { get; set; }

        /// <summary>
        /// The degree
        /// </summary>
        public string Degree { get; set; }

        /// <summary>
        /// The field of study
        /// </summary>
        public string FieldOfStudy { get; set; }

        /// <summary>
        /// The education type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The concentration
        /// </summary>
        public List<FacebookConcentration> Concentration { get; set; } = new();

        /// <summary>
        /// The classes
        /// </summary>
        public List<FacebookClass> Classes { get; set; } = new();
    }

    /// <summary>
    /// Represents a Facebook concentration
    /// </summary>
    public class FacebookConcentration
    {
        /// <summary>
        /// The concentration ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The concentration name
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// Represents a Facebook class
    /// </summary>
    public class FacebookClass
    {
        /// <summary>
        /// The class ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The class name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The class description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The class with (friends)
        /// </summary>
        public List<FacebookUser> With { get; set; } = new();
    }

    /// <summary>
    /// Represents a Facebook project
    /// </summary>
    public class FacebookProject
    {
        /// <summary>
        /// The project ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The project name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The project description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The project start date
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// The project end date
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// The project with (team members)
        /// </summary>
        public List<FacebookUser> With { get; set; } = new();
    }
}
