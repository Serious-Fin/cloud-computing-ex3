public static class TireValidator
{
    public static Dictionary<string, string[]> Validate(Tire tire)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(tire.Brand))
            errors["Brand"] = ["Brand is required."];
        else if (tire.Brand.Trim().Length is < 2 or > 50)
            errors["Brand"] = ["Brand must be between 2 and 50 characters."];

        if (tire.Type is not TireType type)
            errors["Type"] = ["Type is required. Allowed values: Summer, Winter, AllSeason."];
        else if (!Enum.IsDefined(type))
            errors["Type"] = ["Type must be one of: Summer, Winter, AllSeason."];

        if (tire.RimDiameter is < 13 or > 24)
            errors["RimDiameter"] = ["RimDiameter must be between 13 and 24 inches."];

        if (tire.Price <= 0 || tire.Price > 10000)
            errors["Price"] = ["Price must be greater than 0 and at most 10000."];
        else if (decimal.Round(tire.Price, 2) != tire.Price)
            errors["Price"] = ["Price can have at most 2 decimal places."];

        if (string.IsNullOrWhiteSpace(tire.ImageUrl))
            errors["ImageUrl"] = ["ImageUrl is required."];
        else if (!Uri.TryCreate(tire.ImageUrl, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            errors["ImageUrl"] = ["ImageUrl must be a valid absolute http(s) URL."];

        return errors;
    }
}
