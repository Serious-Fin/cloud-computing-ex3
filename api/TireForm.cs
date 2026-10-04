using System.Globalization;

public static class TireForm
{
    public static Tire Read(IFormCollection form, Dictionary<string, string[]> errors)
    {
        var tire = new Tire { Brand = form["brand"].ToString() };
        if (Enum.TryParse<TireType>(form["type"], true, out var type) && Enum.IsDefined(type))
            tire.Type = type;
        if (int.TryParse(form["rimDiameter"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var rim))
            tire.RimDiameter = rim;
        else errors["RimDiameter"] = ["Rim diameter must be an integer."];
        if (decimal.TryParse(form["price"], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out var price)) tire.Price = price;
        else errors["Price"] = ["Price must be a decimal number."];
        foreach (var error in TireValidator.Validate(tire)) errors.TryAdd(error.Key, error.Value);
        return tire;
    }
}
