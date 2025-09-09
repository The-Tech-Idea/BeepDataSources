using System.Collections.Generic;

namespace TheTechIdea.Beep.TwitterDataSource.Entities
{
    /// <summary>
    /// Represents a context annotation for a Tweet
    /// </summary>
    public class ContextAnnotation
    {
        /// <summary>
        /// The domain of the context annotation
        /// </summary>
        public ContextAnnotationDomain Domain { get; set; }

        /// <summary>
        /// The entity of the context annotation
        /// </summary>
        public ContextAnnotationEntity Entity { get; set; }
    }

    /// <summary>
    /// Represents the domain of a context annotation
    /// </summary>
    public class ContextAnnotationDomain
    {
        /// <summary>
        /// The ID of the domain
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The name of the domain
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The description of the domain
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// Represents the entity of a context annotation
    /// </summary>
    public class ContextAnnotationEntity
    {
        /// <summary>
        /// The ID of the entity
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The name of the entity
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The description of the entity
        /// </summary>
        public string Description { get; set; }
    }
}
