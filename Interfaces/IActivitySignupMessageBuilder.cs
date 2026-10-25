using IchigoHoshimiya.Entities.General;
using NetCord.Rest;

namespace IchigoHoshimiya.Interfaces;

public interface IActivitySignupMessageBuilder
{
    string BuildTitle(ActivitySlot slot);

    EmbedProperties BuildSignupEmbed(ActivitySignup signup);

    // Empty once the signup is closed, so sending it strips the buttons
    IReadOnlyList<IMessageComponentProperties> BuildComponents(ActivitySignup signup);

    EmbedProperties BuildTally(ActivitySignup signup);
}
