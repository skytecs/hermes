using System;

namespace Skytecs.Hermes.Models
{
    public class Operation : Entity
    {
        public long ClinicOperationId { get; set; }

        public string Method { get; set; }

        public DateTime Received { get; set; }

        public DateTime? Confirmed { get; set; }

    }
}