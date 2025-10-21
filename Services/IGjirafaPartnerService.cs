using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public interface IGjirafaPartnerService
{
    Task<bool> ValidatePartnerAsync(string email, string password);
}

public class GjirafaPartnerService : IGjirafaPartnerService
{
    private readonly IConfiguration _configuration;

    public GjirafaPartnerService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<bool> ValidatePartnerAsync(string email, string password)
    {
        // Get partner credentials from appsettings.json
        var partners = _configuration.GetSection("GjirafaPartners").GetChildren();

        foreach (var partner in partners)
        {
            var partnerEmail = partner["Email"];
            var partnerPassword = partner["Password"];

            if (email == partnerEmail && password == partnerPassword)
            {
                return true;
            }
        }

        return false;
    }
}