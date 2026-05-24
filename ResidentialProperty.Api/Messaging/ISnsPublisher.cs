using ResidentialProperty.Domain.Entities;

namespace ResidentialProperty.Api.Messaging;

public interface ISnsPublisher
{
    public Task Publish(ResidentialPropertyEntity dto);
}

