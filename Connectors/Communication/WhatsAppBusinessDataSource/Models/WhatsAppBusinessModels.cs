using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Connectors.Communication.WhatsAppBusiness.Models
{
    public class WhatsAppMessage
    {
        public string Id { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Direction { get; set; }
        public string Channel { get; set; }
        public string Type { get; set; }
        public WhatsAppMessageContent Content { get; set; }
        public WhatsAppMessageContext Context { get; set; }
        public WhatsAppMessageIdentity Identity { get; set; }
        public DateTime CreatedDateTime { get; set; }
    }

    public class WhatsAppContact
    {
        public string Input { get; set; }
        public WhatsAppContactStatus Status { get; set; }
        public WhatsAppContactWaId WaId { get; set; }
    }

    // Supporting classes for WhatsApp Business
    public class WhatsAppMessageContent { public string Type { get; set; } public WhatsAppText Text { get; set; } public WhatsAppMedia Media { get; set; } public WhatsAppLocation Location { get; set; } public WhatsAppContact Contact { get; set; } }
    public class WhatsAppText { public string Body { get; set; } }
    public class WhatsAppMedia { public string Id { get; set; } public string Caption { get; set; } public string Filename { get; set; } public string MimeType { get; set; } }
    public class WhatsAppLocation { public double Latitude { get; set; } public double Longitude { get; set; } public string Name { get; set; } public string Address { get; set; } }
    public class WhatsAppMessageContext { public string From { get; set; } public string Id { get; set; } }
    public class WhatsAppMessageIdentity { public string Acknowledged { get; set; } public string Created { get; set; } public string Delivered { get; set; } public string Read { get; set; } public string Sent { get; set; } }
    public class WhatsAppContactStatus { public string Input { get; set; } public string Status { get; set; } public WhatsAppContactWaId WaId { get; set; } }
    public class WhatsAppContactWaId { public string Input { get; set; } public string WaId { get; set; } }
}